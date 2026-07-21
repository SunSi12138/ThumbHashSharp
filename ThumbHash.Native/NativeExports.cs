using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ManagedThumbHash = ThumbHashSharp.ThumbHash;

namespace ThumbHashSharp.Native;

internal static unsafe class NativeExports
{
    private const int Success = 0;
    private const int NullPointer = 1;
    private const int InvalidArgument = 2;
    private const int BufferTooSmall = 3;
    private const int InvalidResizeMode = 4;
    private const int InvalidHash = 5;
    private const int InternalError = 255;
    private const uint Version = 2u << 16;

    [UnmanagedCallersOnly(
        EntryPoint = "thumbhashsharp_version",
        CallConvs = [typeof(CallConvCdecl)])]
    internal static uint GetVersion() => Version;

    [UnmanagedCallersOnly(
        EntryPoint = "thumbhashsharp_encode",
        CallConvs = [typeof(CallConvCdecl)])]
    internal static int Encode(
        int width,
        int height,
        byte* rgba,
        int rgbaLength,
        int resizeMode,
        byte* hash,
        int hashCapacity,
        int* hashLength)
    {
        if (hashLength is null)
        {
            return NullPointer;
        }

        *hashLength = 0;
        if (rgba is null || (hash is null && hashCapacity > 0))
        {
            return NullPointer;
        }

        if (rgbaLength < 0 || hashCapacity < 0)
        {
            return InvalidArgument;
        }

        if (resizeMode < (int)ThumbHashResizeMode.Area
            || resizeMode > (int)ThumbHashResizeMode.NearestNeighbor)
        {
            return InvalidResizeMode;
        }

        try
        {
            ThumbHashEncodingOptions? options = resizeMode == (int)ThumbHashResizeMode.Area
                ? null
                : new ThumbHashEncodingOptions { ResizeMode = (ThumbHashResizeMode)resizeMode };
            byte[] encoded = ManagedThumbHash.Encode(
                width,
                height,
                new ReadOnlySpan<byte>(rgba, rgbaLength),
                options);

            *hashLength = encoded.Length;
            if (hashCapacity < encoded.Length)
            {
                return BufferTooSmall;
            }

            if (hash is null)
            {
                return NullPointer;
            }

            encoded.AsSpan().CopyTo(new Span<byte>(hash, hashCapacity));
            return Success;
        }
        catch (ArgumentException)
        {
            return InvalidArgument;
        }
        catch (OverflowException)
        {
            return InvalidArgument;
        }
        catch (Exception)
        {
            return InternalError;
        }
    }

    [UnmanagedCallersOnly(
        EntryPoint = "thumbhashsharp_decode",
        CallConvs = [typeof(CallConvCdecl)])]
    internal static int Decode(
        byte* hash,
        int hashLength,
        byte* rgba,
        int rgbaCapacity,
        int* width,
        int* height,
        int* rgbaLength)
    {
        if (hash is null || width is null || height is null || rgbaLength is null)
        {
            return NullPointer;
        }

        *width = 0;
        *height = 0;
        *rgbaLength = 0;
        if (hashLength < 0 || rgbaCapacity < 0)
        {
            return InvalidArgument;
        }

        if (rgba is null && rgbaCapacity > 0)
        {
            return NullPointer;
        }

        try
        {
            ThumbHashImage decoded = ManagedThumbHash.Decode(new ReadOnlySpan<byte>(hash, hashLength));
            *width = decoded.Width;
            *height = decoded.Height;
            *rgbaLength = decoded.Rgba.Length;

            if (rgbaCapacity < decoded.Rgba.Length)
            {
                return BufferTooSmall;
            }

            if (rgba is null)
            {
                return NullPointer;
            }

            decoded.Rgba.Span.CopyTo(new Span<byte>(rgba, rgbaCapacity));
            return Success;
        }
        catch (ArgumentException)
        {
            return InvalidHash;
        }
        catch (Exception)
        {
            return InternalError;
        }
    }

    [UnmanagedCallersOnly(
        EntryPoint = "thumbhashsharp_average_color",
        CallConvs = [typeof(CallConvCdecl)])]
    internal static int GetAverageColor(
        byte* hash,
        int hashLength,
        float* red,
        float* green,
        float* blue,
        float* alpha)
    {
        if (hash is null || red is null || green is null || blue is null || alpha is null)
        {
            return NullPointer;
        }

        if (hashLength < 0)
        {
            return InvalidArgument;
        }

        try
        {
            RgbaColor color = ManagedThumbHash.GetAverageColor(new ReadOnlySpan<byte>(hash, hashLength));
            *red = color.R;
            *green = color.G;
            *blue = color.B;
            *alpha = color.A;
            return Success;
        }
        catch (ArgumentException)
        {
            return InvalidHash;
        }
        catch (Exception)
        {
            return InternalError;
        }
    }

    [UnmanagedCallersOnly(
        EntryPoint = "thumbhashsharp_aspect_ratio",
        CallConvs = [typeof(CallConvCdecl)])]
    internal static int GetAspectRatio(byte* hash, int hashLength, float* ratio)
    {
        if (hash is null || ratio is null)
        {
            return NullPointer;
        }

        if (hashLength < 0)
        {
            return InvalidArgument;
        }

        try
        {
            *ratio = ManagedThumbHash.GetApproximateAspectRatio(new ReadOnlySpan<byte>(hash, hashLength));
            return Success;
        }
        catch (ArgumentException)
        {
            return InvalidHash;
        }
        catch (Exception)
        {
            return InternalError;
        }
    }
}
