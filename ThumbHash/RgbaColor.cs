using System;

namespace ThumbHashSharp;

/// <summary>
/// Represents an unpremultiplied RGBA color whose components range from 0 to 1.
/// </summary>
public readonly struct RgbaColor : IEquatable<RgbaColor>
{
    /// <summary>
    /// Initializes a new RGBA color.
    /// </summary>
    public RgbaColor(float red, float green, float blue, float alpha)
    {
        R = red;
        G = green;
        B = blue;
        A = alpha;
    }

    /// <summary>Gets the red component.</summary>
    public float R { get; }

    /// <summary>Gets the green component.</summary>
    public float G { get; }

    /// <summary>Gets the blue component.</summary>
    public float B { get; }

    /// <summary>Gets the alpha component.</summary>
    public float A { get; }

    /// <inheritdoc />
    public bool Equals(RgbaColor other) =>
        R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B) && A.Equals(other.A);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is RgbaColor other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            int hashCode = R.GetHashCode();
            hashCode = (hashCode * 397) ^ G.GetHashCode();
            hashCode = (hashCode * 397) ^ B.GetHashCode();
            hashCode = (hashCode * 397) ^ A.GetHashCode();
            return hashCode;
        }
    }

    /// <summary>Determines whether two colors are equal.</summary>
    public static bool operator ==(RgbaColor left, RgbaColor right) => left.Equals(right);

    /// <summary>Determines whether two colors are different.</summary>
    public static bool operator !=(RgbaColor left, RgbaColor right) => !left.Equals(right);
}
