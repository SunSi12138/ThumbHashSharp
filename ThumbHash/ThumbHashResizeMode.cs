namespace ThumbHashSharp;

/// <summary>
/// Specifies how images larger than ThumbHash's 100-pixel input limit are resized.
/// </summary>
public enum ThumbHashResizeMode
{
    /// <summary>
    /// Uses coverage-weighted area sampling. This is the default and is best suited to downscaling.
    /// </summary>
    Area,

    /// <summary>
    /// Uses bilinear interpolation in premultiplied-alpha color space.
    /// </summary>
    Bilinear,

    /// <summary>
    /// Uses the nearest source pixel without blending colors.
    /// </summary>
    NearestNeighbor,
}
