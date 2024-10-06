using System.IO;
using BenchmarkDotNet.Attributes;
using ImageMagick;
using ThumbHash;

public class ThumbHashBenchMark
{
    public static string path = "";
    readonly MagickImage image = new(path);
    int width;
    int height;
    byte[] rgba;
    static string thumbHash;
    [GlobalSetup]
    public void Setup()
    {
        rgba = image.GetPixels().ToByteArray(PixelMapping.RGBA);
        thumbHash = ThumbHashHelper.RgbaToThumbHashBase64(width, height, rgba);
        width = (int)image.Width;
        height = (int)image.Height;
        rgba = new byte[width * height * 4];
    }
    [Benchmark]
    public void RgbaToThumbHash()
    {
        thumbHash = ThumbHashHelper.RgbaToThumbHashBase64(width, height, rgba);
    }

    [Benchmark]
    public void ThumbHashToRgba()
    {
        var decodedImage = ThumbHashHelper.ThumbHashToRgba(thumbHash);
    }
}
