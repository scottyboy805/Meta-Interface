/// <summary>
/// This source file was auto-generated by a tool - Any changes may be overwritten!
/// From Unity assembly definition: UltimateReplay.dll
/// From source file: Assets/Ultimate Replay 3.0/Scripts/Runtime/Storage/Stream/Source/ReplayStreamSource.cs
/// </summary>
using System;
using System.IO;

namespace UltimateReplay.Storage
{
    /// <summary>
    /// Represents a data stream source that a replay stream can work with.
    /// </summary>
    public abstract class ReplayStreamSource : IDisposable
    {
        // Properties
        /// <summary>
        /// Return a value indicating whether the current stream source can be read from.
        /// </summary>
        public abstract bool CanRead
        {
            get;
        }

        /// <summary>
        /// Return a value indicating whether the current stream source can be written to.
        /// </summary>
        public abstract bool CanWrite
        {
            get;
        }

        // Constructor
        protected ReplayStreamSource(bool keepStreamOpen) => throw new System.NotImplementedException();
        // Methods
        /// <summary>
        /// In derived types, should return an opened stream ready to receive read operations, or null if the stream could not be initialized.
        /// The resulting stream should support seeking operations.
        /// </summary>
        /// <returns>A valid stream object containing replay data ready for reading</returns>
        protected abstract Stream OpenForReading();
        /// <summary>
        /// In derived types, should return an opened stream ready to receive write operations, or null if the stream could not be initialized.
        /// The resulting stream should support seeking, Position, and Length operations.
        /// </summary>
        /// <returns>A valid stream object containing replay data ready for writing</returns>
        protected abstract Stream OpenForWriting();
        /// <summary>
        /// In derived types, should dispose, close or finalize the specified stream as it will no longer be used by the owning <see cref = "ReplayStreamStorage"/>.
        /// The specified input stream will be a stream object created by either <see cref = "OpenForReading"/> or <see cref = "OpenForWriting"/>.
        /// The default behaviour will simply call <see cref = "Stream.Dispose"/>.
        /// </summary>
        /// <param name = "input">The target stream object to dispose</param>
        protected virtual void DisposeStream(Stream input) => throw new System.NotImplementedException();
        /// <summary>
        /// Open the stream source for writing.
        /// </summary>
        /// <returns>The open stream ready for writing</returns>
        /// <exception cref = "IOException">Could not open the target stream</exception>
        public Stream OpenWrite() => throw new System.NotImplementedException();
        /// <summary>
        /// Open the stream source for reading.
        /// </summary>
        /// <returns>The open stream ready for reading</returns>
        /// <exception cref = "IOException">Could not open the target stream</exception>
        public Stream OpenRead() => throw new System.NotImplementedException();
        /// <summary>
        /// Dispose this stream source and close any remaining IO streams.
        /// </summary>
        public void Dispose() => throw new System.NotImplementedException();
        /// <summary>
        /// Create a <see cref = "ReplayStreamSource"/> from the specified IO stream.
        /// The specified stream must contain a valid replay data or playback will fail.
        /// </summary>
        /// <param name = "inputStream">The IO stream source</param>
        /// <returns>A <see cref = "ReplayStreamSource"/> created from the specified IO stream</returns>
        public static ReplayStreamSource FromStream(Stream inputStream) => throw new System.NotImplementedException();
        /// <summary>
        /// Create a <see cref = "ReplayStreamSource"/> from the specified file path.
        /// The specified file must contain a valid replay data or playback will fail.
        /// </summary>
        /// <param name = "filePath">The target replay file path</param>
        /// <returns>A <see cref = "ReplayStreamSource"/> created from the specified file path</returns>
        public static ReplayStreamSource FromFile(string filePath) => throw new System.NotImplementedException();
        /// <summary>
        /// Create a <see cref = "ReplayStreamSource"/> from the specified byte array data.
        /// The specified array data must contain a valid replay data stream or playback will fail.
        /// </summary>
        /// <param name = "data">Data source array</param>
        /// <returns>A <see cref = "ReplayStreamSource"/> created from the specified input data</returns>
        public static ReplayStreamSource FromData(byte[] data) => throw new System.NotImplementedException();
    }
}