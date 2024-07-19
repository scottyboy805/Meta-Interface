using System.IO;

namespace UltimateReplay.Storage
{
    internal sealed class ReplayStreamSource_FromStream : ReplayStreamSource
    {
        // Private
        private Stream stream = null;

        public override bool CanRead
        {
            get { return stream != null && stream.CanRead == true; }
        }

        public override bool CanWrite
        {
            get { return stream != null && stream.CanWrite == true; }
        }

        // Constructor
        public ReplayStreamSource_FromStream(Stream targetStream)
            : base(true)
        {
            this.stream = targetStream;
        }

        // Methods
        protected override Stream OpenForReading()
        {
            return stream;
        }

        protected override Stream OpenForWriting()
        {
            return stream;
        }
    }
}
