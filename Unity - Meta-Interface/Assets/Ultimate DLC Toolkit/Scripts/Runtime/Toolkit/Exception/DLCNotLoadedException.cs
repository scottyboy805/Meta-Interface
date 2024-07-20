using System;

namespace DLCToolkit
{
    /// <summary>
    /// An exception thrown when DLC content is trying to be accessed but the DLC is not loaded.
    /// </summary>
    public sealed class DLCNotLoadedException : Exception
    {
        // Constructor
        /// <summary>
        /// Create new instance.
        /// </summary>
        public DLCNotLoadedException()
            : base("DLC is not loaded")
        {
        }

        /// <summary>
        /// Create new instance.
        /// </summary>
        /// <param name="message">The exception message</param>
        public DLCNotLoadedException(string message) 
            : base(message)
        {
        }
    }
}
