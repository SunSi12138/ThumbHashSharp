namespace ThumbHashSharp.Tests;

internal static class TestImages
{
    internal static byte[] CreatePattern(int width, int height, bool hasAlpha = true)
    {
        var rgba = new byte[checked(width * height * 4)];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = ((y * width) + x) * 4;
                rgba[index] = (byte)((x * 17 + y * 3 + 11) & 255);
                rgba[index + 1] = (byte)((x * 5 + y * 23 + 47) & 255);
                rgba[index + 2] = (byte)((x * 29 + y * 7 + 89) & 255);
                rgba[index + 3] = hasAlpha ? (byte)((x * 13 + y * 19 + 31) & 255) : (byte)255;
            }
        }

        return rgba;
    }
}
