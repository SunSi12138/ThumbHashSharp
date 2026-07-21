using System;

namespace ThumbHashSharp;

/// <summary>
/// Represents the small RGBA placeholder rendered from a ThumbHash.
/// </summary>
public sealed class ThumbHashImage
{
    internal ThumbHashImage(int width, int height, byte[] rgba)
    {
        Width = width;
        Height = height;
        Rgba = rgba;
    }

    /// <summary>Gets the rendered width.</summary>
    public int Width { get; }

    /// <summary>Gets the rendered height.</summary>
    public int Height { get; }

    /// <summary>Gets the unpremultiplied RGBA8888 pixels in row-major order.</summary>
    public ReadOnlyMemory<byte> Rgba { get; }
}
