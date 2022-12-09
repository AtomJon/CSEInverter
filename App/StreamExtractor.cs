using SevenZipExtractor;
using System;
using System.IO;

namespace CSEInverter
{
    public class StreamExtractor : IDisposable
    {
        readonly ArchiveFile Archive;
        readonly bool ArchiveIsInvalid = false;

        public int AmountOfEntries => Archive.Entries.Count;

        public StreamExtractor(Stream inputStream)
        {
            try
            {
                Archive = new(inputStream);
            }
            catch (Exception)
            {
                ArchiveIsInvalid = true;
            }
        }

        public bool CanBeExtracted()
        {
            if (ArchiveIsInvalid) return false;

            try
            {
                _ = Archive.Entries;
            }
            catch (SevenZipException)
            {
                return false;
            }

            return true;
        }

        public void ExtractArchives(Action<Stream, string, ulong> entryExtractedCallback)
        {
            Logger.WriteLine("Extracting archive", true);

            foreach (Entry entry in Archive.Entries)
            {
                Logger.WriteLine(entry.FileName, true);

                if (entry.IsFolder) continue;

                using (MemoryStream stream = new((int)entry.Size))
                {
                    entry.Extract(stream);
                    stream.Position = 0;

                    Logger.WriteLine("Extracted entry", true);

                    entryExtractedCallback(stream, entry.FileName, entry.Size);
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Archive?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
