using System.IO;

namespace CSEInverter.Tests
{
    class LimitedStream : MemoryStream
    {
        bool IsReadAllowed;
        public override bool CanRead => IsReadAllowed;

        bool IsSeekAllowed;
        public override bool CanSeek => IsSeekAllowed;

        bool IsWriteAllowed;
        public override bool CanWrite => IsWriteAllowed;

        public LimitedStream(bool isReadAllowed, bool isSeekAllowed, bool isWriteAllowed) : base()
        {
            IsReadAllowed = isReadAllowed;
            IsSeekAllowed = isSeekAllowed;
            IsWriteAllowed = isWriteAllowed;
        }
    }
}