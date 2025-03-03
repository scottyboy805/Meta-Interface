using System;

namespace DLCToolkit.DRM
{
    internal sealed class DirectDRMServiceProvider : IDRMServiceProvider
    {
        // Private
        private IDRMProvider drmProvider = null;

        // Constructor
        public DirectDRMServiceProvider(IDRMProvider drmProvider)
        {
            // Check for null
            if(drmProvider == null)
                throw new ArgumentNullException(nameof(drmProvider));

            this.drmProvider = drmProvider;
        }

        // Methods
        public IDRMProvider GetDRMProvider()
        {
            return drmProvider;
        }
    }
}
