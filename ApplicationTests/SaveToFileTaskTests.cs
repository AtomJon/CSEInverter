using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CSEInverter.Tests
{
    [TestClass]
    public class SaveToFileTaskTests
    {
        private SaveToFileTask Task;
        private string TempFile;

        [TestInitialize]
        public void SetupTask()
        {
            TempFile = Path.GetTempFileName();

            Task = new SaveToFileTask(Path.GetDirectoryName(TempFile));
        }

        [TestMethod]
        public void SaveFileAndCompare()
        {
            using (Stream buffer = CreateTestStream())
            {
                Task.Run(new() { Stream = buffer, FileName = TempFile });

                using (StreamReader fileReader = new StreamReader(TempFile))
                using (StreamReader bufferReader = new StreamReader(CreateTestStream()))
                {
                    string actual = fileReader.ReadToEnd();
                    string expected = bufferReader.ReadToEnd();

                    Assert.AreEqual(expected, actual);
                }
            }
        }

        [TestMethod]
        public void RunSingleByteStream()
        {
            AssertingProgressManager progressManager = new(true, true);

            using (Stream buffer = new MemoryStream())
            {
                buffer.Write(new byte[] { 0 });
                Task.Run(new() { Stream = buffer, FileName = Path.GetFileName(TempFile), ProgressUpdate = progressManager.ProgressUpdate });
            }

            progressManager.AssertUpdates();
        }

        [TestMethod]
        public void TaskUpdatesProgress()
        {
            AssertingProgressManager progressManager = new(true, false);

            using (Stream buffer = CreateTestStream())
            {
                Task.Run(new() { Stream = buffer, FileName = Path.GetFileName(TempFile), ProgressUpdate = progressManager.ProgressUpdate });
            }

            progressManager.AssertUpdates();
        }

        [TestMethod]
        public void TaskDoeNotUpdatesProgress()
        {
            Task.UpdateProgress = false;

            AssertingProgressManager progressManager = new(false, false);

            using (Stream buffer = CreateTestStream())
            {
                Task.Run(new() { Stream = buffer, FileName = Path.GetFileName(TempFile), ProgressUpdate = progressManager.ProgressUpdate });
            }

            progressManager.AssertUpdates();

            SetupTask();
        }

        [TestMethod]
        public void TaskStartsAndFinishes()
        {
            AssertingProgressManager progressManager = new(true, true);

            using (Stream buffer = CreateTestStream())
            {
                Task.Run(new() { Stream = buffer, FileName = Path.GetFileName(TempFile), ProgressUpdate = progressManager.ProgressUpdate });
            }

            progressManager.AssertUpdates();
        }

        [TestMethod]
        public void SupportsLimitedStreamAndDoesNotUpdateProgress()
        {
            AssertingProgressManager progressManager = new(false, false);

            using (Stream stream = new LimitedStream(true, false, true))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                string tempFile = Path.GetTempFileName();

                SaveToFileTask task = new SaveToFileTask(Path.GetDirectoryName(tempFile));
                task.UpdateProgress = false;

                writer.WriteLine("TEST");
                writer.Flush();

                task.Run(new() { Stream = stream, FileName = Path.GetFileName(tempFile), ProgressUpdate = progressManager.ProgressUpdate });

                progressManager.AssertUpdates();
            }
        }

        private Stream CreateTestStream()
        {
            Stream stream = new MemoryStream();

            using (StreamWriter writer = new StreamWriter(stream, leaveOpen: true))
            {
                writer.WriteLine("This");
                writer.WriteLine("Is");
                writer.WriteLine("A");
                writer.WriteLine("Test");
                writer.Flush();
            }

            stream.Position = 0;

            return stream;
        }
    }
}