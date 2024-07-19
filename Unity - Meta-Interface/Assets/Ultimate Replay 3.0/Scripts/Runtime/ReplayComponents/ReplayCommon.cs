using System;

namespace UltimateReplay
{
    // Type
    /// <summary>
    /// Flags to specify which elements of an axis should be recorded.
    /// In supported components, you can also specify that the axis should be interpolated for smoother replays.
    /// </summary>
    [Flags]
    public enum RecordAxisFlags
    {
        /// <summary>
        /// No data will be recorded or updated
        /// </summary>
        None = 0,
        /// <summary>
        /// The X component of the transform element should be recorded.
        /// </summary>
        X = 1 << 0,
        /// <summary>
        /// The Y component of the transform element should be recorded.
        /// </summary>
        Y = 1 << 1,
        /// <summary>
        /// The Z component of the transform element should be recorded.
        /// </summary>
        Z = 1 << 2,
        /// <summary>
        /// The axis values will be interpolated during playback for smoother replays.
        /// </summary>
        Interpolate = 1 << 4,
        /// <summary>
        /// All axis of the transform element should be recorded.
        /// For rotation elements, full axis rotation will be recorded as quaternion.
        /// </summary>
        XYZ = X | Y | Z,

        /// <summary>
        /// All axis of the transform element should be recorded with full interpolation.
        /// For rotation elements, full axis rotation will be recorded as quaternion.
        /// </summary>
        XYZInterpolate = X | Y | Z | Interpolate,
    }

    public enum RecordFullAxisFlags
    {
        None = 0,
        XYZ = 1 << 1,
        Interpolate = 1 << 2,
        XYZInterpolate = XYZ | Interpolate,
    }

    /// <summary>
    /// For transform related replay components, specify whether local or world space should be used for recording.
    /// </summary>
    public enum RecordSpace
    {
        /// <summary>
        /// Record the associated transform data using world space.
        /// </summary>
        World = 1,
        /// <summary>
        /// Record the associated transform data using local space.
        /// Recommended for child transforms such as bone hierarchies or similar.
        /// </summary>
        Local = 2,
    }

    /// <summary>
    /// Specify how much precision is required when serializing a particular value.
    /// Use lower precisions where possible to save on storage space and overall performance.
    /// </summary>
    public enum RecordPrecision
    {
        /// <summary>
        /// Record value in full 32-bit precision, assuming value type is Single.
        /// </summary>
        FullPrecision32Bit,
        /// <summary>
        /// Record value in half 16-bit precision to reduce space.
        /// Generally a floating point value serialize at half precision will remain accurate to roughly 3 decimal places, depending upon usage.
        /// Recommended for objects that don't move much, are close to the origin, and not in main focus of the active rendering camera such as player controller.
        /// </summary>
        HalfPrecision16Bit,
    }
}
