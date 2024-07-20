using System;

namespace DLCToolkit
{
    /// <summary>
    /// The content flags that indicate which types of content are included in a given DLC.
    /// </summary>
    [Flags]
    public enum DLCContentFlags
    {
        /// <summary>
        /// The DLC contains one or more shared assets such as prefabs, textures, materials, audio clip, etc.
        /// </summary>
        Assets = 1,
        /// <summary>
        /// The DLC contains one or more scenes.
        /// </summary>
        Scenes = 2,
        /// <summary>
        /// The DLC contains one or more script assemblies.
        /// </summary>
        Scripts = 4,
    }

    /// <summary>
    /// Access all metadata for a given DLC.
    /// </summary>
    public interface IDLCMetadata
    {
        // Properties
        /// <summary>
        /// Access the name info for the DLC content.
        /// </summary>
        IDLCNameInfo NameInfo { get; }

        /// <summary>
        /// The unique Guid for the DLC content assigned at creation.
        /// </summary>
        string Guid { get; }

        /// <summary>
        /// The description for this DLC content.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The developer name who created this DLC content.
        /// </summary>
        string Developer { get; }

        /// <summary>
        /// The publisher name who published this DLC content.
        /// </summary>
        string Publisher { get; }

        /// <summary>
        /// The DLC toolkit version required to load this DLC.
        /// </summary>
        Version ToolkitVersion { get; }

        /// <summary>
        /// The Unity version string required to load this DLC.
        /// </summary>
        string UnityVersion { get; }

        /// <summary>
        /// Access the content flags for this DLC content to determine which types of assets are included.
        /// </summary>
        DLCContentFlags ContentFlags { get; }

        /// <summary>
        /// Get the time stamp for when the DLC was built.
        /// Can be used as a unique DLC identifier to use in multiplayer scenarios or similar where you need to ensure that the same DLC is used between clients and the name/version combo may not give enough distinction.
        /// </summary>
        public DateTime BuildTime { get; }

        // Methods
        /// <summary>
        /// Returns a unique string identifier for this DLC including name and version information which may be used between networking clients to compare DLC content in use.
        /// Can be used to determine if two network clients are using the same DLC (Or atleast have the same unique key and version) which will usually be a good enough indicator.
        /// Be sure to enable <paramref name="includeBuildStamp"/> if you want an explicit version match of DLC's (Exact same build) as it will serialize the build time stamp for when the DLC file was created and will almost certainly identify that the DLC is the exact same version and build.
        /// Even if the unique key and version information match, there could be potential for the DLC's to be matched if the version was not updated correctly for example. 
        /// </summary>
        /// <param name="includeBuildStamp">Should the build time stamp be included in the unique string which will guarantee an explicit DLC match</param>
        /// <returns>A short string identifier to uniquely identify this DLC content and can be shared between network clients to compare DLC's in use</returns>
        public string GetNetworkUniqueIdentifier(bool includeBuildStamp);

        public string GetNetworkUniqueIdentifierHash();
    }
}
