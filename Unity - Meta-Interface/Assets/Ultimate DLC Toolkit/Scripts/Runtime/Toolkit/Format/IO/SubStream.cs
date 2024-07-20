using System;
using System.IO;

namespace DLCToolkit.Format.IO
{
    /// <summary>
    /// A sub stream implementation to access only a portion of a larger stream for reading only specific sections such as files in a zip archive.
    /// </summary>
    internal sealed class SubStream : Stream
    {
        // Private
        private Stream baseStream = null;
        private long offset = 0;
        private long size = 0;
        private long position = 0;

        // Properties
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => size;
        public override long Position 
        {
            get { return position; }
            set { position = Seek(value, SeekOrigin.Begin); }
        }

        // Constructor
        public SubStream(Stream baseStream, long offset, long length)
        {
            if (length < 1) throw new ArgumentException("Length must be greater than zero.");

            this.baseStream = baseStream;
            this.offset = offset;
            this.size = length;

            baseStream.Seek(offset, SeekOrigin.Begin);
        }

        // Methods
        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            long remaining = size - position;
            if (remaining <= 0) return 0;
            if (remaining < count) count = (int)remaining;
            int read = baseStream.Read(buffer, offset, count);
            position += read;
            return read;
        }

        private void CheckDisposed()
        {
            if (baseStream == null) 
                throw new ObjectDisposedException(GetType().Name);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long pos = position;

            if (origin == SeekOrigin.Begin)
                pos = offset;
            else if (origin == SeekOrigin.End)
                pos = size + offset;
            else if (origin == SeekOrigin.Current)
                pos += offset;

            if (pos < 0) pos = 0;
            else if (pos >= size) pos = size - 1;

            position = baseStream.Seek(this.offset + pos, SeekOrigin.Begin) - this.offset;

            return pos;
        }

        public override void Flush() => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

