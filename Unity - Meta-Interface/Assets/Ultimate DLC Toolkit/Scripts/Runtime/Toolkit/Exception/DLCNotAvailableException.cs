using System;

namespace DLCToolkit
{
    /// <summary>
    /// An exception thrown when DLC is not available upon request.
    /// </summary>
    public sealed class DLCNotAvailableException : Exception
    {
        // Constructor
        /// <summary>
        /// Create a new instance.
        /// </summary>
        public DLCNotAvailableException()
            : base("DLC is not available")
        {
        }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="message">The exception message</param>
        public DLCNotAvailableException(string message)
            : base(message)
        {
        }
    }
}
