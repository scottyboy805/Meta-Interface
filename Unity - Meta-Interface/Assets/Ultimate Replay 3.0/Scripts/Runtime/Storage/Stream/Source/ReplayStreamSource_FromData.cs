using System;
using System.IO;

namespace UltimateReplay.Storage
{
    /// <summary>
    /// Used for working with replay stream data stored in byte array.
    /// Note: Does not support writing.
    /// </summary>
    internal sealed class ReplayStreamSource_FromData : ReplayStreamSource
    {
        // Private
        private byte[] data = null;

        // Properties
        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        // Constructor
        public ReplayStreamSource_FromData(byte[] data)
            : base(false)
        {
            this.data = data;
        }

        // Methods
        /// <summary>
        /// Create a memory stream for the specified data.
        /// </summary>
        /// <returns>Stream object</returns>
        protected override Stream OpenForReading()
        {
            return new MemoryStream(data);  
        }

        /// <summary>
        /// Not supported. Stream source is read only.
        /// </summary>
        /// <returns>None - Method is not supported</returns>
        /// <exception cref="NotSupportedException">This method is not supported</exception>
        protected override Stream OpenForWriting()
        {
            throw new NotSupportedException();
        }
    }
}
