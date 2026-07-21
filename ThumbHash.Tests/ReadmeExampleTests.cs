namespace ThumbHashSharp.Tests;

[TestClass]
public sealed class ReadmeExampleTests
{
    [TestMethod]
    public void ReadmeExample_CompilesAndRuns()
    {
        const int width = 2;
        const int height = 2;
        ReadOnlySpan<byte> rgba =
        [
            255, 0, 0, 255,
            0, 255, 0, 255,
            0, 0, 255, 255,
            255, 255, 255, 255,
        ];

        string base64 = ThumbHash.EncodeToBase64(width, height, rgba);
        ThumbHashImage image = ThumbHash.DecodeFromBase64(base64);
        RgbaColor average = ThumbHash.GetAverageColorFromBase64(base64);
        float ratio = ThumbHash.GetApproximateAspectRatioFromBase64(base64);

        Assert.IsNotEmpty(base64);
        Assert.IsNotEmpty(image.Rgba.ToArray());
        Assert.AreEqual(1f, average.A);
        Assert.AreEqual(1f, ratio);
    }
}
