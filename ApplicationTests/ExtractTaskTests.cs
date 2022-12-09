using Microsoft.VisualStudio.TestTools.UnitTesting;
using SevenZipExtractor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CSEInverter.Tests
{
    [TestClass]
    public class ExtractTaskTests
    {
        private const string RecursingSampleFile = @"D:\MyPrograms\SampleFiles\SampleFolder\dir2_compressed.zip";
        private const string SampleFile = @"D:\MyPrograms\SampleFiles\sample.7z";
        private const string UncompressedSampleFile = @"D:\MyPrograms\SampleFiles\uncompressed_sample.docx";

        private static ExtractTask task = new ExtractTask();

        static string uncompressedSampleFileContents;

        class TestUncompressTask : IProductTask
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
                byte[] buffer = new byte[(int)args.StreamSize];
                args.Stream.Read(buffer);

                File.WriteAllBytes(@"D:\MyPrograms\SampleFiles\uncompressed_test.docx", buffer);

                uncompressedSampleFileContents = Hasher.Hash(buffer);
            }
        }

        [TestMethod]
        public void TestZipTask()
        {
            using (Stream compressedStream = new FileStream(SampleFile, FileMode.Open, FileAccess.Read))
            {
                LinkedList<IProductTask> list = new LinkedList<IProductTask>(new IProductTask[] { task, new TestUncompressTask() });

                task.Run(new() { Stream = compressedStream, FileName = "SampleFile.7z", Node = list.First });
            }

            byte[] buffer = File.ReadAllBytes(UncompressedSampleFile);
            string expected = Hasher.Hash(buffer);

            Assert.AreEqual(expected, uncompressedSampleFileContents);
        }

        [TestMethod]
        public void CanExtractRecursingArchives()
        {
            using (Stream compressedStream = new FileStream(RecursingSampleFile, FileMode.Open, FileAccess.Read))
            {
                ExtractTask task = new ExtractTask();
                task.RecursionFilters_CommaSepareted = "*";

                task.Run(new TaskArguments { Stream = compressedStream, FileName = "RecursingSampleFile" });
            }
        }

        [TestMethod]
        public void DoesUpdateProgress()
        {
            AssertingProgressManager progressManager = new(true, false);

            using (Stream compressedStream = new FileStream(SampleFile, FileMode.Open, FileAccess.Read))
            using (Stream uncompressedBuffer = new MemoryStream())
            {
                TaskArguments args = new TaskArguments();
                args.Stream = compressedStream;
                args.FileName = "SampleFile";
                args.ProgressUpdate = progressManager.ProgressUpdate;

                task.Run(args);
            }

            progressManager.AssertUpdates();
        }

        [TestMethod]
        public void DoesNotUpdateProgress()
        {
            AssertingProgressManager progressManager = new(false, false);

            ExtractTask task = new();
            task.UpdateProgress = false;

            using (Stream compressedStream = new FileStream(SampleFile, FileMode.Open, FileAccess.Read))
            using (Stream uncompressedBuffer = new MemoryStream())
            {
                TaskArguments args = new TaskArguments();
                args.Stream = compressedStream;
                args.FileName = "SampleFile";
                args.ProgressUpdate = progressManager.ProgressUpdate;

                task.Run(args);
            }

            progressManager.AssertUpdates();
        }

        [TestMethod]
        public void DoesFinalize()
        {
            AssertingProgressManager progressManager = new(true, true);

            using (Stream compressedStream = new FileStream(SampleFile, FileMode.Open, FileAccess.Read))
            using (Stream uncompressedBuffer = new MemoryStream())
            {
                TaskArguments args = new TaskArguments();
                args.Stream = compressedStream;
                args.FileName = "SampleFile";
                args.ProgressUpdate = progressManager.ProgressUpdate;

                task.Run(args);
            }

            progressManager.AssertUpdates();
        }

        [TestMethod]
        public void NonReadableStream()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                using (Stream stream = new LimitedStream(false, true, true))
                {
                    task.Run(new() { Stream = stream, });
                }
            });
        }

        [TestMethod]
        public void EmptyStream()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                task.Run(new() { Stream = stream, });
            }
        }

        [TestMethod]
        public void NullStream()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                task.Run(new() { });
            });
        }

        [TestMethod]
        public void BadStream()
        {
            using (MemoryStream stream = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("BAD");
                writer.Flush();

                task.Run(new() { Stream = stream });
            }
        }
    }

    //[TestClass]
    public class UnzippingTests
    {
        private const string SampleFile = @"D:\MyPrograms\SampleFiles\compressed_file.7z";
        //private const string SampleFile = @"D:\MyPrograms\SampleFiles\sample.7z";
        private const string OutputDirectory = @"D:\MyPrograms\SampleFiles\Out\";

        [TestMethod]
        public void SevenZipExtractToFile()
        {
            using (Stream inStream = new FileStream(SampleFile, FileMode.Open, FileAccess.Read))
            using (ArchiveFile archive = new ArchiveFile(inStream))
            {
                archive.Extract(OutputDirectory + "SevenZip\\", true);
            }
        }

        [TestMethod]
        public void SevenZipExtract()
        {
            using (Stream inStream = new FileStream(SampleFile, FileMode.Open, FileAccess.Read))
            using (SevenZipExtractor.ArchiveFile archive = new SevenZipExtractor.ArchiveFile(inStream))
            {
                foreach (var entry in archive.Entries)
                {
                    using (MemoryStream outputStream = new MemoryStream(new byte[entry.Size]))
                        entry.Extract(outputStream);
                }
            }
        }
    }
}