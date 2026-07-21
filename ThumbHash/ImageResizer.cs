using System;

namespace ThumbHashSharp;

internal static class ImageResizer
{
    internal static void Resize(
        int sourceWidth,
        int sourceHeight,
        ReadOnlySpan<byte> source,
        int destinationWidth,
        int destinationHeight,
        Span<byte> destination,
        ThumbHashResizeMode mode)
    {
        switch (mode)
        {
            case ThumbHashResizeMode.Area:
                ResizeArea(sourceWidth, sourceHeight, source, destinationWidth, destinationHeight, destination);
                break;
            case ThumbHashResizeMode.Bilinear:
                ResizeBilinear(sourceWidth, sourceHeight, source, destinationWidth, destinationHeight, destination);
                break;
            case ThumbHashResizeMode.NearestNeighbor:
                ResizeNearest(sourceWidth, sourceHeight, source, destinationWidth, destinationHeight, destination);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown resize mode.");
        }
    }

    private static void ResizeArea(
        int sourceWidth,
        int sourceHeight,
        ReadOnlySpan<byte> source,
        int destinationWidth,
        int destinationHeight,
        Span<byte> destination)
    {
        double scaleX = sourceWidth / (double)destinationWidth;
        double scaleY = sourceHeight / (double)destinationHeight;

        for (int destinationY = 0; destinationY < destinationHeight; destinationY++)
        {
            double sourceTop = destinationY * scaleY;
            double sourceBottom = (destinationY + 1) * scaleY;
            int firstSourceY = (int)Math.Floor(sourceTop);
            int lastSourceY = Math.Min(sourceHeight - 1, (int)Math.Ceiling(sourceBottom) - 1);

            for (int destinationX = 0; destinationX < destinationWidth; destinationX++)
            {
                double sourceLeft = destinationX * scaleX;
                double sourceRight = (destinationX + 1) * scaleX;
                int firstSourceX = (int)Math.Floor(sourceLeft);
                int lastSourceX = Math.Min(sourceWidth - 1, (int)Math.Ceiling(sourceRight) - 1);

                double totalWeight = 0;
                double alphaWeight = 0;
                double premultipliedRed = 0;
                double premultipliedGreen = 0;
                double premultipliedBlue = 0;

                for (int sourceY = firstSourceY; sourceY <= lastSourceY; sourceY++)
                {
                    double verticalWeight = Math.Min(sourceBottom, sourceY + 1d) - Math.Max(sourceTop, sourceY);
                    for (int sourceX = firstSourceX; sourceX <= lastSourceX; sourceX++)
                    {
                        double horizontalWeight = Math.Min(sourceRight, sourceX + 1d) - Math.Max(sourceLeft, sourceX);
                        double weight = horizontalWeight * verticalWeight;
                        int sourceIndex = ((sourceY * sourceWidth) + sourceX) * 4;
                        double alpha = source[sourceIndex + 3] / 255d;
                        double weightedAlpha = weight * alpha;

                        totalWeight += weight;
                        alphaWeight += weightedAlpha;
                        premultipliedRed += source[sourceIndex] * weightedAlpha;
                        premultipliedGreen += source[sourceIndex + 1] * weightedAlpha;
                        premultipliedBlue += source[sourceIndex + 2] * weightedAlpha;
                    }
                }

                int destinationIndex = ((destinationY * destinationWidth) + destinationX) * 4;
                WriteUnpremultipliedPixel(
                    destination,
                    destinationIndex,
                    premultipliedRed,
                    premultipliedGreen,
                    premultipliedBlue,
                    alphaWeight,
                    totalWeight);
            }
        }
    }

    private static void ResizeBilinear(
        int sourceWidth,
        int sourceHeight,
        ReadOnlySpan<byte> source,
        int destinationWidth,
        int destinationHeight,
        Span<byte> destination)
    {
        double scaleX = sourceWidth / (double)destinationWidth;
        double scaleY = sourceHeight / (double)destinationHeight;

        for (int destinationY = 0; destinationY < destinationHeight; destinationY++)
        {
            double sourceY = ((destinationY + 0.5d) * scaleY) - 0.5d;
            int top = (int)Math.Floor(sourceY);
            int bottom = top + 1;
            double verticalFraction = sourceY - top;
            top = Clamp(top, 0, sourceHeight - 1);
            bottom = Clamp(bottom, 0, sourceHeight - 1);

            for (int destinationX = 0; destinationX < destinationWidth; destinationX++)
            {
                double sourceX = ((destinationX + 0.5d) * scaleX) - 0.5d;
                int left = (int)Math.Floor(sourceX);
                int right = left + 1;
                double horizontalFraction = sourceX - left;
                left = Clamp(left, 0, sourceWidth - 1);
                right = Clamp(right, 0, sourceWidth - 1);

                double topLeftWeight = (1d - horizontalFraction) * (1d - verticalFraction);
                double topRightWeight = horizontalFraction * (1d - verticalFraction);
                double bottomLeftWeight = (1d - horizontalFraction) * verticalFraction;
                double bottomRightWeight = horizontalFraction * verticalFraction;

                double alphaWeight = 0;
                double premultipliedRed = 0;
                double premultipliedGreen = 0;
                double premultipliedBlue = 0;

                Accumulate(source, sourceWidth, left, top, topLeftWeight, ref alphaWeight, ref premultipliedRed, ref premultipliedGreen, ref premultipliedBlue);
                Accumulate(source, sourceWidth, right, top, topRightWeight, ref alphaWeight, ref premultipliedRed, ref premultipliedGreen, ref premultipliedBlue);
                Accumulate(source, sourceWidth, left, bottom, bottomLeftWeight, ref alphaWeight, ref premultipliedRed, ref premultipliedGreen, ref premultipliedBlue);
                Accumulate(source, sourceWidth, right, bottom, bottomRightWeight, ref alphaWeight, ref premultipliedRed, ref premultipliedGreen, ref premultipliedBlue);

                int destinationIndex = ((destinationY * destinationWidth) + destinationX) * 4;
                WriteUnpremultipliedPixel(
                    destination,
                    destinationIndex,
                    premultipliedRed,
                    premultipliedGreen,
                    premultipliedBlue,
                    alphaWeight,
                    1d);
            }
        }
    }

    private static void ResizeNearest(
        int sourceWidth,
        int sourceHeight,
        ReadOnlySpan<byte> source,
        int destinationWidth,
        int destinationHeight,
        Span<byte> destination)
    {
        double scaleX = sourceWidth / (double)destinationWidth;
        double scaleY = sourceHeight / (double)destinationHeight;

        for (int destinationY = 0; destinationY < destinationHeight; destinationY++)
        {
            int sourceY = Clamp((int)Math.Floor(((destinationY + 0.5d) * scaleY)), 0, sourceHeight - 1);
            for (int destinationX = 0; destinationX < destinationWidth; destinationX++)
            {
                int sourceX = Clamp((int)Math.Floor(((destinationX + 0.5d) * scaleX)), 0, sourceWidth - 1);
                int sourceIndex = ((sourceY * sourceWidth) + sourceX) * 4;
                int destinationIndex = ((destinationY * destinationWidth) + destinationX) * 4;
                source.Slice(sourceIndex, 4).CopyTo(destination.Slice(destinationIndex, 4));
            }
        }
    }

    private static void Accumulate(
        ReadOnlySpan<byte> source,
        int sourceWidth,
        int x,
        int y,
        double weight,
        ref double alphaWeight,
        ref double premultipliedRed,
        ref double premultipliedGreen,
        ref double premultipliedBlue)
    {
        int sourceIndex = ((y * sourceWidth) + x) * 4;
        double alpha = source[sourceIndex + 3] / 255d;
        double weightedAlpha = weight * alpha;
        alphaWeight += weightedAlpha;
        premultipliedRed += source[sourceIndex] * weightedAlpha;
        premultipliedGreen += source[sourceIndex + 1] * weightedAlpha;
        premultipliedBlue += source[sourceIndex + 2] * weightedAlpha;
    }

    private static void WriteUnpremultipliedPixel(
        Span<byte> destination,
        int destinationIndex,
        double premultipliedRed,
        double premultipliedGreen,
        double premultipliedBlue,
        double alphaWeight,
        double totalWeight)
    {
        if (alphaWeight > 0d)
        {
            destination[destinationIndex] = ToByte(premultipliedRed / alphaWeight);
            destination[destinationIndex + 1] = ToByte(premultipliedGreen / alphaWeight);
            destination[destinationIndex + 2] = ToByte(premultipliedBlue / alphaWeight);
        }
        else
        {
            destination[destinationIndex] = 0;
            destination[destinationIndex + 1] = 0;
            destination[destinationIndex + 2] = 0;
        }

        destination[destinationIndex + 3] = ToByte(255d * alphaWeight / totalWeight);
    }

    private static byte ToByte(double value)
    {
        value = Math.Max(0d, Math.Min(255d, value));
        return (byte)Math.Floor(value + 0.5d);
    }

    private static int Clamp(int value, int minimum, int maximum) =>
        Math.Max(minimum, Math.Min(maximum, value));
}
