using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CSEInverter.Tests
{
    [TestClass]
    public partial class DownloadFTPTaskTests
    {
        static readonly Uri ServerURI = new("ftp://127.0.0.1:22/");
        const string ServerFolder = @"D:\MyPrograms\SampleFiles\SampleFolder";
        const string AnonymousCredentials = "anonymous";

        private struct DownloadedFileInfo
        {
            public string Filename;
            public string Data;

            public DownloadedFileInfo(string filename, string data)
            {
                this.Filename = filename;
                this.Data = data;
            }
        }

        private static List<DownloadedFileInfo> FilesThatHaveBeenRanByNextTask = new();

        private class TestFTPTask : IProductTask
        {
            public string GetDescription()
            {
                throw new NotImplementedException();
            }

            public void Initiate(TaskArguments args)
            {
                throw new NotImplementedException();
            }

            public void Run(TaskArguments args)
            {
                Debug.WriteLine("Downloaded File: " + args.FileName);

                Assert.IsTrue(args.Stream.CanSeek, "FTP Stream does not support seeking");

                string data = new StreamReader(args.Stream).ReadToEnd();
                FilesThatHaveBeenRanByNextTask.Add(new(args.FileName, data));
            }
        }

        [TestMethod]
        public void TaskCallsNextTask()
        {
            DownloadFTPTask task = CreateTask();

            LinkedList<IProductTask> list = new LinkedList<IProductTask>(new IProductTask[] { task, new TestFTPTask() });

            task.Initiate(new() { Node = list.First });

            Assert.IsTrue(FilesThatHaveBeenRanByNextTask.Count > 0, "No files were ran by next task");

            var files = new DirectoryInfo(ServerFolder).GetFiles("*", SearchOption.AllDirectories);

            foreach (var expectetFile in files)
            {
                List<DownloadedFileInfo> matches = FilesThatHaveBeenRanByNextTask.FindAll((actualFile) => MatchesFile(actualFile, expectetFile));

                Assert.IsTrue(matches.Count > 0, $"The FTPClient did not include file: '{expectetFile.Name}'");

                string actual = matches[0].Data;
                string expected = expectetFile.OpenText().ReadToEnd();

                if (Math.Abs(String.Compare(actual, expected)) > 0)
                {
                    Debug.WriteLine($"Expected: {Convert.ToHexString(Encoding.UTF8.GetBytes(expected))}");
                    Debug.WriteLine($"Actual: {Convert.ToHexString(Encoding.UTF8.GetBytes(actual))}");
                    Assert.Fail($"The file: '{expectetFile.Name}', does not contain the right data");
                }
            }
        }

        private bool MatchesFile(DownloadedFileInfo actualFile, FileInfo expectetFile)
        {
            string relativePath = Path.GetRelativePath(ServerFolder, expectetFile.FullName).Replace('\\', '/').TrimStart('/', '\\');
            string relativeActualPath = actualFile.Filename.TrimStart('/', '\\');
            return relativeActualPath == relativePath;
        }

        [TestMethod]
        public void TaskUpdatesProgress()
        {
            AssertingProgressManager progressManager = new(true, false);

            DownloadFTPTask task = CreateTask();

            task.Initiate(new() { ProgressUpdate = progressManager.ProgressUpdate });

            progressManager.AssertUpdates();
        }

        [TestMethod]
        public void TaskDoesNotUpdatesProgress()
        {
            AssertingProgressManager progressManager = new(false, false);

            DownloadFTPTask task = CreateTask();
            task.UpdateProgress = false;

            task.Initiate(new() { ProgressUpdate = progressManager.ProgressUpdate });

            progressManager.AssertUpdates();
        }

        [TestMethod]
        public void TaskParsesDirectories()
        {
            DownloadFTPTask task = CreateTask();
            task.UpdateProgress = false;

            task.Directories_CommaSeparated = "   /dir2/dir3  ,    /dir1/,,dir1";

            task.Initiate(new() { });
        }

        [TestMethod]
        public void TaskFinalizesProgress()
        {
            AssertingProgressManager progressManager = new(false, true);

            DownloadFTPTask task = CreateTask();
            task.UpdateProgress = true;

            task.Initiate(new() { ProgressUpdate = progressManager.ProgressUpdate });

            progressManager.AssertUpdates();
        }

        private DownloadFTPTask CreateTask()
        {
            DownloadFTPTask task = new DownloadFTPTask();
            task.ServerHost = ServerURI.Host;
            task.ServerPort = ServerURI.Port;
            task.Username = AnonymousCredentials;
            task.Password = AnonymousCredentials;
            task.Directories_CommaSeparated = "/";
            task.RecurseSubdirectories = true;

            return task;
        }
    }
}