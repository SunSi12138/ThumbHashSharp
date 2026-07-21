namespace ThumbHashSharp;

/// <summary>
/// Configures ThumbHash encoding.
/// </summary>
public sealed class ThumbHashEncodingOptions
{
    /// <summary>
    /// Gets or sets the resize algorithm used when either input dimension is larger than 100 pixels.
    /// </summary>
    public ThumbHashResizeMode ResizeMode { get; set; } = ThumbHashResizeMode.Area;
}
