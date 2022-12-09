using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Linq;
using DotNet.Globbing;

namespace CSEInverter
{
    public class ExtractTask : IProductTask
    {
        public ExtractTask() { }

        [Category("Status"), DisplayName("Opdater Status Bar"), PropertyOrder(0), Description("Slå Opdateringer Fra For At Få Mere Fart")]
        public bool UpdateProgress { get; set; } = true;

        [Category("Dekomprimering"), DisplayName("Rekursivt Filter"), PropertyOrder(0), Description("Komma separeret filter for alle filer der skal dekomprimeres, f.eks. '*.7z,arkiv.bin,*.zip'")]
        public string RecursionFilters_CommaSepareted { get; set; } = "*.7z,*.zip";

        IEnumerable<Glob> RecursionFilters;

        Stream Stream;
        Action<TaskProgressUpdateArgs> ProgressUpdate;
        LinkedListNode<IProductTask> Node;

        public string GetDescription()
        {
            return "Udpak";
        }

        public void Initiate(TaskArguments args)
        {
            try
            {
                Stream compressedFileStream = Dialog.AskForFile("Vælg Fil Til Udpakning", "LZMA Komprimeret Fil (*.7z)|*.7z|Alle filer (*.*)|*.*");

                args.Stream = compressedFileStream;
                args.FileName = "UserPermittedFile";

                Run(args);
            }
            catch (UserDeniedException) // User didn't select a file, ignoring and exiting
            {
                return;
            }
        }

        public void Run(TaskArguments args)
        {
            if (args.Stream == null) throw new ArgumentNullException("args.Stream is null");
            if (args.Stream.CanRead == false) throw new ArgumentException("args.Stream.CanRead is false");
            if (args.Stream.CanSeek == false) throw new ArgumentException("args.Stream.CanSeek is false");

            Stream = args.Stream;
            ProgressUpdate = args.ProgressUpdate;
            Node = args.Node;

            RecursionFilters = RecursionFilters_CommaSepareted.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(filter => filter.Trim()).Select(Glob.Parse);

            TryExtractOrSendToTask(Stream, args.FileName);

            Stream = null;
            Node = null;
            ProgressUpdate = null;
        }

        private void TryExtractOrSendToTask(Stream stream, string fileName)
        {
            string workStatus = $"Udpakker {fileName}";

            using StreamExtractor archive = new(stream);

            if (archive.CanBeExtracted())
            {
                UpdateWorkStartet(workStatus);
                Logger.WriteLine($"Extracting: {fileName}", true);

                string archiveName = fileName.Split('.')[0];

                AddWork(workStatus, archive.AmountOfEntries);

                archive.ExtractArchives((Stream stream, string fileName, ulong size) =>
                {
                    string path = $"{archiveName}/{fileName}";

                    Logger.WriteLine($"Entry: {path}", false);

                    WorkDone(workStatus);

                    if (RecursionFilters.Any(filter => filter.IsMatch(fileName)))
                    {
                        TryExtractOrSendToTask(stream, path);
                    }
                    else
                    {
                        StartNextTask(stream, path, (long)size);
                    }

                });

                UpdateWorkFinished(workStatus);
            }
            else
            {
                Logger.WriteLine("Could not extract", true);
                StartNextTask(stream, fileName, stream.Length);
            }
        }

        private void StartNextTask(Stream stream, string fileName, long fileSize)
        {
            if (Node != null && Node.Next != null)
            {
                LinkedListNode<IProductTask> nextNode = Node.Next;

                nextNode.Value.Run(new()
                {
                    Stream = stream,
                    FileName = fileName,
                    StreamSize = fileSize,
                    Node = nextNode,
                    ProgressUpdate = ProgressUpdate
                });
            }
        }

        private void UpdateWorkStartet(string workStatus)
        {
            ProgressUpdate?.Invoke(new() { Type = TaskProgressUpdateType.TaskStartet, WorkStatus = workStatus });
        }

        private void UpdateWorkFinished(string workStatus)
        {
            ProgressUpdate?.Invoke(new() { Type = TaskProgressUpdateType.TaskFinished, WorkStatus = workStatus });
        }

        private void AddWork(string workStatus, int amount)
        {
            if (CanUpdate())
            {
                ProgressUpdate.Invoke(new() { Type = TaskProgressUpdateType.AddWork, AmountOfWork = amount, WorkStatus = workStatus });
            }
        }

        private void WorkDone(string workStatus)
        {
            if (CanUpdate())
            {
                ProgressUpdate.Invoke(new() { Type = TaskProgressUpdateType.WorkDone, WorkStatus = workStatus });
            }
        }

        private bool CanUpdate()
        {
            return UpdateProgress && ProgressUpdate != null;
        }
    }
}
