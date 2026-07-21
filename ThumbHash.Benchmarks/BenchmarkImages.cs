namespace ThumbHashSharp.Benchmarks;

internal static class BenchmarkImages
{
    internal static byte[] Create(int width, int height, bool hasAlpha)
    {
        var random = new Random(42);
        var rgba = new byte[checked(width * height * 4)];
        random.NextBytes(rgba);
        for (int index = 3; index < rgba.Length; index += 4)
        {
            rgba[index] = hasAlpha ? rgba[index] : (byte)255;
        }

        return rgba;
    }
}
