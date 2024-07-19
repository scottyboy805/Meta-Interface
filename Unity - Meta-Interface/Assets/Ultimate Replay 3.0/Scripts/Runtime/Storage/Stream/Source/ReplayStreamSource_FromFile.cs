using System.IO;
using UnityEngine;

namespace UltimateReplay.Storage
{
    internal sealed class ReplayStreamSource_FromFile : ReplayStreamSource
    {
        // Private
        private string filePath = "";

        // Properties
        /// <summary>
        /// Get a value indicating whether this storage target is readable.
        /// Value will be true if the specified file exists.
        /// </summary>
        public override bool CanRead
        {
            get 
            { 
                // Check for exists
                bool exists = File.Exists(filePath);

                // Log a warning if not so users aren't left wondering what went wrong
                if (exists == false)
                    Debug.LogWarning("The target replay file does not exist: " + filePath);

                return exists;
            }
        }

        /// <summary>
        /// Get a value indicating whether this storage target is writable.
        /// Value will be true if the file path is valid and accessible.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                try
                {
                    Path.GetDirectoryName(filePath);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        // Public
        public ReplayStreamSource_FromFile(string filePath)
            : base(false)
        {
            this.filePath = filePath;
        }

        // Methods
        protected override Stream OpenForReading()
        {
            // Check for exists
            if (File.Exists(filePath) == false)
                return null;

            // Open file
            return File.OpenRead(filePath);
        }

        protected override Stream OpenForWriting()
        {
            // Create the file
            return File.Create(filePath);
        }
    }
}
