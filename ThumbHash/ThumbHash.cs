using System;
using System.Buffers;

namespace ThumbHashSharp;

/// <summary>
/// Encodes and decodes ThumbHash image placeholders.
/// </summary>
public static class ThumbHash
{
    private const int MaximumInputDimension = 100;
    private const int MaximumHashLength = 25;

    /// <summary>
    /// Encodes unpremultiplied RGBA8888 pixels into a ThumbHash.
    /// </summary>
    /// <param name="width">The source image width.</param>
    /// <param name="height">The source image height.</param>
    /// <param name="rgba">Pixels in row-major RGBA8888 order.</param>
    /// <param name="options">Optional encoding configuration.</param>
    /// <returns>The encoded ThumbHash bytes.</returns>
    public static byte[] Encode(
        int width,
        int height,
        ReadOnlySpan<byte> rgba,
        ThumbHashEncodingOptions? options = null)
    {
        ValidateImage(width, height, rgba);

        ThumbHashResizeMode resizeMode = options?.ResizeMode ?? ThumbHashResizeMode.Area;
        ValidateResizeMode(resizeMode);

        if (width <= MaximumInputDimension && height <= MaximumInputDimension)
        {
            return EncodeCore(width, height, rgba);
        }

        GetResizedDimensions(width, height, out int resizedWidth, out int resizedHeight);
        int resizedLength = resizedWidth * resizedHeight * 4;
        byte[] resized = ArrayPool<byte>.Shared.Rent(resizedLength);
        try
        {
            Span<byte> resizedSpan = resized.AsSpan(0, resizedLength);
            ImageResizer.Resize(width, height, rgba, resizedWidth, resizedHeight, resizedSpan, resizeMode);
            return EncodeCore(resizedWidth, resizedHeight, resizedSpan);
        }
        finally
        {
            Array.Clear(resized, 0, resizedLength);
            ArrayPool<byte>.Shared.Return(resized);
        }
    }

    /// <summary>
    /// Encodes unpremultiplied RGBA8888 pixels into a Base64 ThumbHash.
    /// </summary>
    public static string EncodeToBase64(
        int width,
        int height,
        ReadOnlySpan<byte> rgba,
        ThumbHashEncodingOptions? options = null) =>
        Convert.ToBase64String(Encode(width, height, rgba, options));

    /// <summary>
    /// Decodes a ThumbHash into a small unpremultiplied RGBA8888 image.
    /// </summary>
    /// <param name="hash">The complete ThumbHash bytes.</param>
    /// <returns>The rendered placeholder image.</returns>
    public static ThumbHashImage Decode(ReadOnlySpan<byte> hash)
    {
        Header header = ParseHeader(hash, requireCompleteHash: true);
        int acStart = header.HasAlpha ? 6 : 5;
        int acIndex = 0;

        var luminance = new Channel(header.Lx, header.Ly);
        var chromaP = new Channel(3, 3);
        var chromaQ = new Channel(3, 3);
        Channel? alpha = header.HasAlpha ? new Channel(5, 5) : null;

        acIndex = luminance.Decode(hash, acStart, acIndex, header.LScale);
        acIndex = chromaP.Decode(hash, acStart, acIndex, header.PScale * 1.25d);
        acIndex = chromaQ.Decode(hash, acStart, acIndex, header.QScale * 1.25d);
        if (alpha is not null)
        {
            alpha.Decode(hash, acStart, acIndex, header.AScale);
        }

        float ratio = GetApproximateAspectRatio(hash);
        int width = RoundNonNegative(ratio > 1f ? 32d : 32d * ratio);
        int height = RoundNonNegative(ratio > 1f ? 32d / ratio : 32d);
        byte[] rgba = new byte[width * height * 4];

        int maximumXCoefficient = Math.Max(header.Lx, header.HasAlpha ? 5 : 3);
        int maximumYCoefficient = Math.Max(header.Ly, header.HasAlpha ? 5 : 3);
        Span<double> cosineX = stackalloc double[width * maximumXCoefficient];
        Span<double> cosineY = stackalloc double[height * maximumYCoefficient];
        FillCosineTable(cosineX, width, maximumXCoefficient);
        FillCosineTable(cosineY, height, maximumYCoefficient);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double l = header.LDc;
                double p = header.PDc;
                double q = header.QDc;
                double a = header.ADc;

                int coefficientIndex = 0;
                for (int cy = 0; cy < header.Ly; cy++)
                {
                    double fy = cosineY[(cy * height) + y] * 2d;
                    for (int cx = cy > 0 ? 0 : 1; cx * header.Ly < header.Lx * (header.Ly - cy); cx++, coefficientIndex++)
                    {
                        l += luminance.Ac[coefficientIndex] * cosineX[(cx * width) + x] * fy;
                    }
                }

                coefficientIndex = 0;
                for (int cy = 0; cy < 3; cy++)
                {
                    double fy = cosineY[(cy * height) + y] * 2d;
                    for (int cx = cy > 0 ? 0 : 1; cx < 3 - cy; cx++, coefficientIndex++)
                    {
                        double basis = cosineX[(cx * width) + x] * fy;
                        p += chromaP.Ac[coefficientIndex] * basis;
                        q += chromaQ.Ac[coefficientIndex] * basis;
                    }
                }

                if (alpha is not null)
                {
                    coefficientIndex = 0;
                    for (int cy = 0; cy < 5; cy++)
                    {
                        double fy = cosineY[(cy * height) + y] * 2d;
                        for (int cx = cy > 0 ? 0 : 1; cx < 5 - cy; cx++, coefficientIndex++)
                        {
                            a += alpha.Ac[coefficientIndex] * cosineX[(cx * width) + x] * fy;
                        }
                    }
                }

                double blue = l - ((2d / 3d) * p);
                double red = ((3d * l) - blue + q) / 2d;
                double green = red - q;
                int pixelIndex = ((y * width) + x) * 4;
                rgba[pixelIndex] = ToUnitByte(red);
                rgba[pixelIndex + 1] = ToUnitByte(green);
                rgba[pixelIndex + 2] = ToUnitByte(blue);
                rgba[pixelIndex + 3] = ToUnitByte(a);
            }
        }

        return new ThumbHashImage(width, height, rgba);
    }

    /// <summary>
    /// Decodes a Base64 ThumbHash into a small unpremultiplied RGBA8888 image.
    /// </summary>
    public static ThumbHashImage DecodeFromBase64(string base64) =>
        DecodeBase64(base64, Decode);

    /// <summary>
    /// Extracts the average unpremultiplied color from a ThumbHash header.
    /// </summary>
    public static RgbaColor GetAverageColor(ReadOnlySpan<byte> hash)
    {
        Header header = ParseHeader(hash, requireCompleteHash: false);
        double blue = header.LDc - ((2d / 3d) * header.PDc);
        double red = ((3d * header.LDc) - blue + header.QDc) / 2d;
        double green = red - header.QDc;
        return new RgbaColor(
            (float)ClampUnit(red),
            (float)ClampUnit(green),
            (float)ClampUnit(blue),
            (float)header.ADc);
    }

    /// <summary>
    /// Extracts the average unpremultiplied color from a Base64 ThumbHash header.
    /// </summary>
    public static RgbaColor GetAverageColorFromBase64(string base64) =>
        DecodeBase64(base64, GetAverageColor);

    /// <summary>
    /// Extracts the original image's approximate width-to-height ratio from a ThumbHash header.
    /// </summary>
    public static float GetApproximateAspectRatio(ReadOnlySpan<byte> hash)
    {
        Header header = ParseHeader(hash, requireCompleteHash: false);
        return header.Lx / (float)header.Ly;
    }

    /// <summary>
    /// Extracts the original image's approximate width-to-height ratio from a Base64 ThumbHash header.
    /// </summary>
    public static float GetApproximateAspectRatioFromBase64(string base64) =>
        DecodeBase64(base64, GetApproximateAspectRatio);

    private static byte[] EncodeCore(int width, int height, ReadOnlySpan<byte> rgba)
    {
        int pixelCount = width * height;
        double averageRed = 0;
        double averageGreen = 0;
        double averageBlue = 0;
        double averageAlpha = 0;

        for (int pixel = 0, sourceIndex = 0; pixel < pixelCount; pixel++, sourceIndex += 4)
        {
            double alpha = rgba[sourceIndex + 3] / 255d;
            averageRed += alpha / 255d * rgba[sourceIndex];
            averageGreen += alpha / 255d * rgba[sourceIndex + 1];
            averageBlue += alpha / 255d * rgba[sourceIndex + 2];
            averageAlpha += alpha;
        }

        if (averageAlpha > 0d)
        {
            averageRed /= averageAlpha;
            averageGreen /= averageAlpha;
            averageBlue /= averageAlpha;
        }

        bool hasAlpha = averageAlpha < pixelCount;
        int luminanceLimit = hasAlpha ? 5 : 7;
        int maximumDimension = Math.Max(width, height);
        int lx = Math.Max(1, RoundNonNegative(luminanceLimit * width / (double)maximumDimension));
        int ly = Math.Max(1, RoundNonNegative(luminanceLimit * height / (double)maximumDimension));

        double[] luminanceData = ArrayPool<double>.Shared.Rent(pixelCount);
        double[] chromaPData = ArrayPool<double>.Shared.Rent(pixelCount);
        double[] chromaQData = ArrayPool<double>.Shared.Rent(pixelCount);
        double[]? alphaData = hasAlpha ? ArrayPool<double>.Shared.Rent(pixelCount) : null;

        try
        {
            for (int pixel = 0, sourceIndex = 0; pixel < pixelCount; pixel++, sourceIndex += 4)
            {
                double alpha = rgba[sourceIndex + 3] / 255d;
                double red = (averageRed * (1d - alpha)) + (alpha / 255d * rgba[sourceIndex]);
                double green = (averageGreen * (1d - alpha)) + (alpha / 255d * rgba[sourceIndex + 1]);
                double blue = (averageBlue * (1d - alpha)) + (alpha / 255d * rgba[sourceIndex + 2]);
                luminanceData[pixel] = (red + green + blue) / 3d;
                chromaPData[pixel] = ((red + green) / 2d) - blue;
                chromaQData[pixel] = red - green;
                if (alphaData is not null)
                {
                    alphaData[pixel] = alpha;
                }
            }

            var luminance = new Channel(Math.Max(3, lx), Math.Max(3, ly));
            var chromaP = new Channel(3, 3);
            var chromaQ = new Channel(3, 3);
            Channel? alphaChannel = hasAlpha ? new Channel(5, 5) : null;

            int maximumXCoefficient = Math.Max(luminance.Nx, hasAlpha ? 5 : 3);
            int maximumYCoefficient = Math.Max(luminance.Ny, hasAlpha ? 5 : 3);
            Span<double> cosineX = stackalloc double[width * maximumXCoefficient];
            Span<double> cosineY = stackalloc double[height * maximumYCoefficient];
            FillCosineTable(cosineX, width, maximumXCoefficient);
            FillCosineTable(cosineY, height, maximumYCoefficient);

            luminance.Encode(width, height, luminanceData.AsSpan(0, pixelCount), cosineX, cosineY);
            chromaP.Encode(width, height, chromaPData.AsSpan(0, pixelCount), cosineX, cosineY);
            chromaQ.Encode(width, height, chromaQData.AsSpan(0, pixelCount), cosineX, cosineY);
            if (alphaChannel is not null && alphaData is not null)
            {
                alphaChannel.Encode(width, height, alphaData.AsSpan(0, pixelCount), cosineX, cosineY);
            }

            bool isLandscape = width > height;
            int header24 = QuantizeUnit(luminance.Dc, 63)
                | (QuantizeUnit((chromaP.Dc + 1d) / 2d, 63) << 6)
                | (QuantizeUnit((chromaQ.Dc + 1d) / 2d, 63) << 12)
                | (QuantizeUnit(luminance.Scale, 31) << 18)
                | (hasAlpha ? 1 << 23 : 0);
            int header16 = (isLandscape ? ly : lx)
                | (QuantizeUnit(chromaP.Scale, 63) << 3)
                | (QuantizeUnit(chromaQ.Scale, 63) << 9)
                | (isLandscape ? 1 << 15 : 0);

            int acStart = hasAlpha ? 6 : 5;
            int acCount = luminance.Ac.Length + chromaP.Ac.Length + chromaQ.Ac.Length + (alphaChannel?.Ac.Length ?? 0);
            byte[] hash = new byte[acStart + ((acCount + 1) / 2)];
            hash[0] = (byte)header24;
            hash[1] = (byte)(header24 >> 8);
            hash[2] = (byte)(header24 >> 16);
            hash[3] = (byte)header16;
            hash[4] = (byte)(header16 >> 8);
            if (alphaChannel is not null)
            {
                hash[5] = (byte)(QuantizeUnit(alphaChannel.Dc, 15) | (QuantizeUnit(alphaChannel.Scale, 15) << 4));
            }

            int acIndex = 0;
            acIndex = luminance.WriteTo(hash, acStart, acIndex);
            acIndex = chromaP.WriteTo(hash, acStart, acIndex);
            acIndex = chromaQ.WriteTo(hash, acStart, acIndex);
            if (alphaChannel is not null)
            {
                alphaChannel.WriteTo(hash, acStart, acIndex);
            }

            return hash;
        }
        finally
        {
            Return(luminanceData, pixelCount);
            Return(chromaPData, pixelCount);
            Return(chromaQData, pixelCount);
            if (alphaData is not null)
            {
                Return(alphaData, pixelCount);
            }
        }
    }

    private static TResult DecodeBase64<TResult>(string base64, SpanDecoder<TResult> decoder)
    {
        if (base64 is null)
        {
            throw new ArgumentNullException(nameof(base64));
        }

        Span<byte> hash = stackalloc byte[MaximumHashLength];
        if (!Convert.TryFromBase64String(base64, hash, out int bytesWritten))
        {
            throw new FormatException("The value is not a valid Base64 ThumbHash.");
        }

        return decoder(hash.Slice(0, bytesWritten));
    }

    private static void ValidateImage(int width, int height, ReadOnlySpan<byte> rgba)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
        }

        long pixelCount = (long)width * height;
        if (pixelCount > int.MaxValue / 4)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "The image dimensions are too large for a contiguous RGBA buffer.");
        }

        long requiredLength = pixelCount * 4L;

        if (rgba.Length != (int)requiredLength)
        {
            throw new ArgumentException($"RGBA data must contain exactly {requiredLength} bytes.", nameof(rgba));
        }
    }

    private static void ValidateResizeMode(ThumbHashResizeMode mode)
    {
        if (mode < ThumbHashResizeMode.Area || mode > ThumbHashResizeMode.NearestNeighbor)
        {
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown resize mode.");
        }
    }

    private static void GetResizedDimensions(int width, int height, out int resizedWidth, out int resizedHeight)
    {
        if (width >= height)
        {
            resizedWidth = MaximumInputDimension;
            resizedHeight = Math.Max(1, RoundNonNegative(height * MaximumInputDimension / (double)width));
        }
        else
        {
            resizedWidth = Math.Max(1, RoundNonNegative(width * MaximumInputDimension / (double)height));
            resizedHeight = MaximumInputDimension;
        }
    }

    private static Header ParseHeader(ReadOnlySpan<byte> hash, bool requireCompleteHash)
    {
        if (hash.Length < 5)
        {
            throw new ArgumentException("A ThumbHash header must contain at least 5 bytes.", nameof(hash));
        }

        int header24 = hash[0] | (hash[1] << 8) | (hash[2] << 16);
        int header16 = hash[3] | (hash[4] << 8);
        bool hasAlpha = (header24 & (1 << 23)) != 0;
        if (hasAlpha && hash.Length < 6)
        {
            throw new ArgumentException("A ThumbHash with alpha must contain a 6-byte header.", nameof(hash));
        }

        bool isLandscape = (header16 & (1 << 15)) != 0;
        int lx = Math.Max(3, isLandscape ? (hasAlpha ? 5 : 7) : header16 & 7);
        int ly = Math.Max(3, isLandscape ? header16 & 7 : (hasAlpha ? 5 : 7));

        if (requireCompleteHash)
        {
            int acCount = CountAc(lx, ly) + CountAc(3, 3) + CountAc(3, 3) + (hasAlpha ? CountAc(5, 5) : 0);
            int expectedLength = (hasAlpha ? 6 : 5) + ((acCount + 1) / 2);
            if (hash.Length != expectedLength)
            {
                throw new ArgumentException($"The ThumbHash must contain exactly {expectedLength} bytes, but contains {hash.Length}.", nameof(hash));
            }
        }

        return new Header(
            (header24 & 63) / 63d,
            ((header24 >> 6) & 63) / 31.5d - 1d,
            ((header24 >> 12) & 63) / 31.5d - 1d,
            ((header24 >> 18) & 31) / 31d,
            ((header16 >> 3) & 63) / 63d,
            ((header16 >> 9) & 63) / 63d,
            hasAlpha,
            lx,
            ly,
            hasAlpha ? (hash[5] & 15) / 15d : 1d,
            hasAlpha ? ((hash[5] >> 4) & 15) / 15d : 0d);
    }

    private static int CountAc(int nx, int ny)
    {
        int count = 0;
        for (int cy = 0; cy < ny; cy++)
        {
            for (int cx = cy > 0 ? 0 : 1; cx * ny < nx * (ny - cy); cx++)
            {
                count++;
            }
        }

        return count;
    }

    private static void FillCosineTable(Span<double> table, int sampleCount, int coefficientCount)
    {
        for (int coefficient = 0; coefficient < coefficientCount; coefficient++)
        {
            int offset = coefficient * sampleCount;
            for (int sample = 0; sample < sampleCount; sample++)
            {
                table[offset + sample] = Math.Cos(Math.PI / sampleCount * coefficient * (sample + 0.5d));
            }
        }
    }

    private static int QuantizeUnit(double value, int maximum)
    {
        double scaled = ClampUnit(value) * maximum;
        return Math.Min(maximum, Math.Max(0, RoundNonNegative(scaled)));
    }

    private static int RoundNonNegative(double value) => (int)Math.Floor(value + 0.5d);

    private static byte ToUnitByte(double value) => (byte)RoundNonNegative(ClampUnit(value) * 255d);

    private static double ClampUnit(double value) => Math.Max(0d, Math.Min(1d, value));

    private static void Return(double[] buffer, int length)
    {
        Array.Clear(buffer, 0, length);
        ArrayPool<double>.Shared.Return(buffer);
    }

    private delegate TResult SpanDecoder<TResult>(ReadOnlySpan<byte> hash);

    private readonly struct Header
    {
        internal Header(
            double lDc,
            double pDc,
            double qDc,
            double lScale,
            double pScale,
            double qScale,
            bool hasAlpha,
            int lx,
            int ly,
            double aDc,
            double aScale)
        {
            LDc = lDc;
            PDc = pDc;
            QDc = qDc;
            LScale = lScale;
            PScale = pScale;
            QScale = qScale;
            HasAlpha = hasAlpha;
            Lx = lx;
            Ly = ly;
            ADc = aDc;
            AScale = aScale;
        }

        internal double LDc { get; }
        internal double PDc { get; }
        internal double QDc { get; }
        internal double LScale { get; }
        internal double PScale { get; }
        internal double QScale { get; }
        internal bool HasAlpha { get; }
        internal int Lx { get; }
        internal int Ly { get; }
        internal double ADc { get; }
        internal double AScale { get; }
    }

    private sealed class Channel
    {
        internal Channel(int nx, int ny)
        {
            Nx = nx;
            Ny = ny;
            Ac = new double[CountAc(nx, ny)];
        }

        internal int Nx { get; }
        internal int Ny { get; }
        internal double Dc { get; private set; }
        internal double[] Ac { get; }
        internal double Scale { get; private set; }

        internal void Encode(
            int width,
            int height,
            ReadOnlySpan<double> channel,
            ReadOnlySpan<double> cosineX,
            ReadOnlySpan<double> cosineY)
        {
            int acIndex = 0;
            for (int cy = 0; cy < Ny; cy++)
            {
                for (int cx = 0; cx * Ny < Nx * (Ny - cy); cx++)
                {
                    double coefficient = 0;
                    for (int y = 0; y < height; y++)
                    {
                        double fy = cosineY[(cy * height) + y];
                        int rowOffset = y * width;
                        int cosineOffset = cx * width;
                        for (int x = 0; x < width; x++)
                        {
                            coefficient += channel[rowOffset + x] * cosineX[cosineOffset + x] * fy;
                        }
                    }

                    coefficient /= width * height;
                    if (cx == 0 && cy == 0)
                    {
                        Dc = coefficient;
                    }
                    else
                    {
                        Ac[acIndex++] = coefficient;
                        Scale = Math.Max(Scale, Math.Abs(coefficient));
                    }
                }
            }

            if (Scale > 0d)
            {
                for (int index = 0; index < Ac.Length; index++)
                {
                    Ac[index] = 0.5d + ((0.5d / Scale) * Ac[index]);
                }
            }
        }

        internal int Decode(ReadOnlySpan<byte> hash, int start, int index, double scale)
        {
            for (int acIndex = 0; acIndex < Ac.Length; acIndex++, index++)
            {
                int packed = hash[start + (index / 2)];
                int data = (packed >> ((index & 1) * 4)) & 15;
                Ac[acIndex] = ((data / 7.5d) - 1d) * scale;
            }

            return index;
        }

        internal int WriteTo(Span<byte> hash, int start, int index)
        {
            for (int acIndex = 0; acIndex < Ac.Length; acIndex++, index++)
            {
                int quantized = QuantizeUnit(Ac[acIndex], 15);
                int hashIndex = start + (index / 2);
                if ((index & 1) == 0)
                {
                    hash[hashIndex] = (byte)quantized;
                }
                else
                {
                    hash[hashIndex] |= (byte)(quantized << 4);
                }
            }

            return index;
        }
    }
}
