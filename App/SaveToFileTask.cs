using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace CSEInverter
{
    public class SaveToFileTask : Chunkable, IProductTask
    {
        [Category("Destination"), DisplayName("Mappe"), PropertyOrder(0), Description("Mappe hvor filerne bliver gemt på")]
        public string OutputDirectory { get; set; } = "./Out/";

        [Category("Tekst Kodning"), DisplayName("Destinations KodningsTabel"), PropertyOrder(1), Description("(https://docs.microsoft.com/en-us/windows/win32/intl/code-page-identifiers) Kodnings Tabellen for destinationen, F.Eks. Er ansi's kodetabel '1252'")]
        public int TargetEncodingCodePage { get; set; } = 1252;

        readonly Encoding TargetEncoding;

        ChunkUpdater updater = null;

        public SaveToFileTask()
        {

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            TargetEncoding = Encoding.GetEncoding(TargetEncodingCodePage);
        }

        public SaveToFileTask(string outputDirectory)
        {
            OutputDirectory = outputDirectory;
        }

        public string GetDescription()
        {
            return "Gem på disk";
        }

        public void Initiate(TaskArguments args)
        {
            try
            {
                IEnumerable<string> directories = Dialog.AskForDirectories("Vælg Filer Til Konvertering");

                RecurseDirectoriesAndStartTask(directories);
            }
            catch (UserDeniedException) // User didn't select a file, ignoring and exiting
            {
                return;
            }
        }

        private void RecurseDirectoriesAndStartTask(IEnumerable<string> directories)
        {
            foreach (string directoryName in directories)
            {
                DirectoryInfo directory = new(directoryName);
                RecurseDirectory(directory.EnumerateFileSystemInfos(), directory.Name);
            }
        }

        private void RecurseDirectory(IEnumerable<FileSystemInfo> entries, string subPath)
        {
            foreach (FileSystemInfo entry in entries)
            {
                if ((FileAttributes.Directory & entry.Attributes) == FileAttributes.Directory)
                {
                    DirectoryInfo directory = new(entry.FullName);
                    string dirPath = $"{subPath}/{entry.Name}";

                    RecurseDirectory(directory.EnumerateFileSystemInfos(), dirPath);
                }
                else
                {
                    RunTaskWithFile(new(entry.FullName), subPath);
                }
            }
        }

        private void RunTaskWithFile(FileInfo file, string path)
        {
            using FileStream fileStream = file.OpenRead();

            TaskArguments args = new();
            args.Stream = fileStream;
            args.FileName = $"{path}/{file.Name}";

            Run(args);
        }

        public void Run(TaskArguments args)
        {
            Stream SourceStream = args.Stream;
            long SourceSize;

            if (args.StreamSize != null) SourceSize = args.StreamSize.Value;
            else SourceSize = SourceStream.Length;

            if (SourceStream == null) throw new ArgumentNullException("args.Stream is null");
            if (SourceStream.CanRead == false) throw new ArgumentException("args.Stream.CanRead is false");

            if (UpdateProgress && StepsOnProgressBar > 0) updater = new(args.ProgressUpdate, $"Gemmer {args.FileName}", BufferSize, StepsOnProgressBar);

            string path = Path.Combine(OutputDirectory, args.FileName.Trim('/', '\\'));

            FileInfo file = new(path);

            Directory.CreateDirectory(file.DirectoryName);

            using (StreamReader reader = new(SourceStream))
            using (Stream fileStream = file.OpenWrite())
            using (StreamWriter writer = new(fileStream, TargetEncoding))
            {
                updater?.CalculateUpdates(SourceSize);

                char[] data = new char[BufferSize];
                int i = 0;
                for (int read = reader.ReadBlock(data, 0, BufferSize); read > 0; read = reader.ReadBlock(data, 0, BufferSize))
                {
                    writer.Write(data, 0, read);

                    updater?.UpdateOnIteration(i++);
                }
            }

            updater?.FinalizeProgress();

            updater = null;
        }
    }
}
