using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System.Security.Cryptography;

namespace CSEInverter
{
    public class DownloadFTPTask : IProductTask
    {
        [Category("FTP Server"), PropertyOrder(0), DisplayName("Brugernavn"), Description("Brugernavn for autentificering")]
        public string Username { get; set; }

        [Category("FTP Server"), PropertyOrder(1), DisplayName("Adgangskode"), Description("Adgangskode for autentificering")]
        public string Password { get; set; }

        [Category("FTP Server"), PropertyOrder(2), DisplayName("Værts Navn"), Description("En acceptabel vært, f.eks. '127.0.0.1' eller 'localhost'")]
        public string ServerHost { get; set; } = "127.0.0.1";

        [Category("FTP Server"), PropertyOrder(3), DisplayName("Værts Port"), Description("En tcp port at forbinde til, oftest er ssh porten '22'")]
        public int ServerPort { get; set; } = 22;

        [Category("FTP Server"), PropertyOrder(4), DisplayName("Mapper Til Hentning"), Description("En Komma Separeret Liste Med De Mapper Der Skal Hentes. F.Eks. '/mappe1, /mappe2/ , /mappe1/mappe2/'")]
        public string Directories_CommaSeparated { get; set; } = "/";

        [Category("FTP Server"), DisplayName("Recurse Undermapper"), PropertyOrder(5), Description("Går igennem alle undermapper")]
        public bool RecurseSubdirectories { get; set; } = true;

        [Category("Debug"), DisplayName("Gem hentet filer"), PropertyOrder(0), Description("Gemmer downloaded filer under './Downloaded Files'")]
        public bool BackupFiles { get; set; } = false;

        [Category("Status"), DisplayName("Opdater Status Bar"), PropertyOrder(0), Description("Slå Opdateringer Fra For At Få Mere Fart")]
        public bool UpdateProgress { get; set; } = true;

        string[] Directories => Directories_CommaSeparated.Split(',').Where(dir=>!String.IsNullOrWhiteSpace(dir)).Select(dir => dir.Trim()).ToArray();

        const string AcceptedHostKeysFile = "AcceptedHostKeys.csv";
        const string BackupFileLocation = "DownloadedFiles";
        const string Status = "Filer";

        Action<TaskProgressUpdateArgs> ProgressUpdate;
        LinkedListNode<IProductTask> Node;

        private struct DownloadedFileInfo
        {
            public string Filename;
            public Stream Stream;

            public DownloadedFileInfo(string filename, Stream buffer)
            {
                this.Filename = filename;
                this.Stream = buffer;
            }
        }

        public string GetDescription()
        {
            return "Hent Fil Fra FTP Server";
        }

        public void Initiate(TaskArguments args)
        {
            if (String.IsNullOrEmpty(ServerHost)) throw new ArgumentException($"ServerHost: '{ServerHost}' is empty");
            if (ServerPort < 0) throw new ArgumentOutOfRangeException($"ServerPort: '{ServerPort}' is less than zero");
            if (String.IsNullOrEmpty(Username)) throw new ArgumentException($"Username: '{Username}' is empty");
            if (String.IsNullOrEmpty(Password)) throw new ArgumentException($"Password: '{Password}' is empty");
            if (Directories == null || Directories.Length < 1) throw new ArgumentException(nameof(Directories));

            Run(args);
        }

        public void Run(TaskArguments args)
        {
            ProgressUpdate = args.ProgressUpdate;
            Node = args.Node;

            Update(TaskProgressUpdateType.TaskStartet);

            NetworkCredential credential = new(Username, Password);

            Logger.WriteLine($"Connecting to the server: {ServerHost}:{ServerPort}", true);

            using SftpClient client = new(ServerHost, ServerPort, Username, Password);

            client.HostKeyReceived += Client_HostKeyReceived;
            client.Connect();

            foreach (var directoryToBeListed in Directories)
            {
                RecurseDirectory(directoryToBeListed, client);
            }

            Update(TaskProgressUpdateType.TaskFinished);

            Node = null;
            ProgressUpdate = null;
        }

        private void Client_HostKeyReceived(object sender, Renci.SshNet.Common.HostKeyEventArgs e)
        {
            string fingerprint = Convert.ToBase64String(new SHA256Managed().ComputeHash(e.HostKey));

            Logger.WriteLine($"Fingerprint: '{fingerprint}'", true);

            string[] acceptedKeys = File.Exists(AcceptedHostKeysFile) ? File.ReadAllLines(AcceptedHostKeysFile) : new string[0];

            bool keyHasAlreadyBeenAccepted = acceptedKeys.Contains(fingerprint);

            if (keyHasAlreadyBeenAccepted)
            {
                Logger.WriteLine("Key was already accepted", true);
                e.CanTrust = true;
                return;
            }
            else
            {
                bool keyIsAccepted = Dialog.YesOrNo("SSH Nøgle", $"En ny ssh fingerprint er blevet sendt, og kan ikke genkendes:\nNavn: {e.HostKeyName}\nFingerprint: {fingerprint}");

                if (keyIsAccepted)
                {
                    var newAcceptedKeys = acceptedKeys.Append(fingerprint);
                    File.WriteAllLines(AcceptedHostKeysFile, newAcceptedKeys);

                    e.CanTrust = true;
                    return;
                }
                else
                {
                    e.CanTrust = false;
                    return;
                }
            }
        }

        private void RecurseDirectory(string directory, SftpClient client)
        {
            IEnumerable<SftpFile> items = client.ListDirectory(directory);

            Update(TaskProgressUpdateType.AddWork, Status, items.Count());

            foreach (SftpFile item in items)
            {
                string path = directory.TrimEnd('/') + "/" + item.Name;

                if (IsUnixDirectoryName(item.FullName)) continue;

                if (item.IsRegularFile)
                {
                    path = path.TrimStart('/', '\\');
                    Logger.WriteLine($"Found file: {path}, {item.Length}", false);

                    DownloadFile(path, client);
                    Update(TaskProgressUpdateType.WorkDone);
                }
                else if (RecurseSubdirectories && item.IsDirectory)
                {
                    RecurseDirectory(path, client);
                }
            }
        }

        private bool IsUnixDirectoryName(string name)
        {
            return name.Contains("/.");
        }

        private void DownloadFile(string filePath, SftpClient client)
        {
            string status = $"Henter: {filePath}";
            Update(TaskProgressUpdateType.TaskStartet, status);
            Update(TaskProgressUpdateType.AddWork, status, 100);

            using MemoryStream bufferStream = new();

            client.DownloadFile(filePath, bufferStream, progress => FileProgress(progress, status));
            bufferStream.Position = 0;

            CheckAndBackup(bufferStream, filePath);

            Update(TaskProgressUpdateType.TaskFinished, status);

            if (Node != null && Node.Next != null)
            {
                LinkedListNode<IProductTask> nextNode = Node.Next;

                nextNode.Value.Run(new()
                {
                    Stream = bufferStream,
                    Node = nextNode,
                    FileName = filePath,
                    StreamSize = bufferStream.Length,
                    ProgressUpdate = ProgressUpdate
                });
            }
        }

        private void FileProgress(ulong progress, string status)
        {
            Update(TaskProgressUpdateType.WorkDone, status);
        }

        private void CheckAndBackup(Stream downloadedFileStream, string filename)
        {
            if (BackupFiles)
            {
                string path = Path.Combine(BackupFileLocation, filename.Trim('/', '\\'));

                Directory.CreateDirectory(String.Join('/', path.Split('\\', '/').SkipLast(1)));

                using Stream fileStream = File.Create(path);

                byte[] buffer = new byte[8096 * 8];
                for (int read = downloadedFileStream.Read(buffer); read > 0; read = downloadedFileStream.Read(buffer))
                {
                    fileStream.Write(buffer, 0, read);
                }
            }

            downloadedFileStream.Position = 0;
        }

        private void Update(TaskProgressUpdateType type, string status = Status, int work = -1)
        {
            if (UpdateProgress && ProgressUpdate != null)
            {
                ProgressUpdate(new() { Type = type, AmountOfWork = work, WorkStatus = status });
            }
        }
    }
}