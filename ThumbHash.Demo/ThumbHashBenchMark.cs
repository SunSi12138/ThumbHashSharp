using System.IO;
using BenchmarkDotNet.Attributes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ThumbHash;

public class ThumbHashBenchMark
{
    public static string path = "";
    readonly Image<Rgba32> imageSharp = Image.Load<Rgba32>(Path.Combine(Directory.GetCurrentDirectory(), path));
    int width;
    int height;
    byte[] rgba;
    static string thumbHash;
    [GlobalSetup]
    public void Setup()
    {
        imageSharp.CopyPixelDataTo(rgba);
        thumbHash = ThumbHashHelper.RgbaToThumbHashBase64(width, height, rgba);
        width = imageSharp.Width;
        height = imageSharp.Height;
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
