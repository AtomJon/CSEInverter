using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace CSEInverter.Tests
{
    [TestClass]
    public class StreamExtractorTests
    {
        private const string SampleFile = @"D:\MyPrograms\SampleFiles\sample.7z";
        private const string UncompressedSampleFile = @"D:\MyPrograms\SampleFiles\uncompressed_sample.docx";

        [TestMethod]
        public void CanBeExtractedTest()
        {
            StreamExtractor extractor = new(File.OpenRead(SampleFile));

            Assert.IsTrue(extractor.CanBeExtracted());

            extractor.Dispose();

            extractor = new(File.OpenRead(UncompressedSampleFile));

            Assert.IsFalse(extractor.CanBeExtracted());
        }

        [TestMethod]
        public void DisposeTest()
        {
            using MemoryStream stream = new(File.ReadAllBytes(SampleFile));

            StreamExtractor extractor = new(stream);

            extractor.ExtractArchives((_,name,_) => Debug.WriteLine(name));

            stream.ReadByte();

            extractor.Dispose();

            Assert.ThrowsException<InvalidComObjectException>(() => extractor.ExtractArchives((_,name,_) => Debug.WriteLine(name)));

            Assert.ThrowsException<ObjectDisposedException>(() => stream.ReadByte());
        }

        [TestMethod]
        public void DisposeInvalidArchiveTest()
        {
            using MemoryStream stream = new(File.ReadAllBytes(UncompressedSampleFile));

            using StreamExtractor extractor = new(stream);

            Assert.IsFalse(extractor.CanBeExtracted());

            stream.ReadByte();
        }

        [TestMethod]
        public void InvalidStream()
        {
            using MemoryStream stream = new LimitedStream(false, false, false);

            using StreamExtractor extractor = new(stream);

            Assert.IsFalse(extractor.CanBeExtracted());
        }
    }
}