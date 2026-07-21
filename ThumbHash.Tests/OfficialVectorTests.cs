namespace ThumbHashSharp.Tests;

[TestClass]
public sealed class OfficialVectorTests
{
    [TestMethod]
    [DynamicData(nameof(EncodeVectors))]
    public void Encode_OfficialVector_MatchesReference(
        string name,
        int width,
        int height,
        byte[] rgba,
        string expectedBase64)
    {
        string actual = ThumbHash.EncodeToBase64(width, height, rgba);

        Assert.AreEqual(expectedBase64, actual, $"Reference vector '{name}' did not match.");
    }

    [TestMethod]
    [DynamicData(nameof(DecodeVectors))]
    public void Decode_OfficialVector_MatchesReferenceSamples(
        string hex,
        int expectedWidth,
        int expectedHeight,
        float expectedRatio,
        RgbaColor expectedAverage,
        PixelSample[] samples)
    {
        byte[] hash = Convert.FromHexString(hex);

        ThumbHashImage image = ThumbHash.Decode(hash);
        float ratio = ThumbHash.GetApproximateAspectRatio(hash);
        RgbaColor average = ThumbHash.GetAverageColor(hash);

        Assert.AreEqual(expectedWidth, image.Width);
        Assert.AreEqual(expectedHeight, image.Height);
        Assert.AreEqual(expectedRatio, ratio, 0.000001f);
        Assert.AreEqual(expectedAverage.R, average.R, 0.000001f);
        Assert.AreEqual(expectedAverage.G, average.G, 0.000001f);
        Assert.AreEqual(expectedAverage.B, average.B, 0.000001f);
        Assert.AreEqual(expectedAverage.A, average.A, 0.000001f);

        ReadOnlySpan<byte> rgba = image.Rgba.Span;
        foreach (PixelSample sample in samples)
        {
            int index = ((sample.Y * image.Width) + sample.X) * 4;
            for (int component = 0; component < 4; component++)
            {
                int difference = Math.Abs(rgba[index + component] - sample.Rgba[component]);
                Assert.IsLessThanOrEqualTo(1, difference, $"{hex} pixel ({sample.X},{sample.Y}) component {component}");
            }
        }
    }

    public static IEnumerable<(string name, int width, int height, byte[] rgba, string expectedBase64)> EncodeVectors =>
    [
        ("opaque-red-1x1", 1, 1, [255, 0, 0, 255], "1fsrB38I9wiIh4hwj3CI+AiIgIAICIgA"),
        ("transparent-blue-1x1", 1, 1, [0, 0, 255, 0], "AAiCBQAAAAAAAAAAAAAAAAAAAAAAAAAAAA=="),
        ("mixed-2x2", 2, 2,
            [255, 0, 0, 255, 0, 255, 0, 128, 0, 0, 255, 0, 255, 255, 255, 255],
            "ZmrOPSSZdgmGeIeYB3xv+JiLdgmpWIhoBQ=="),
        ("opaque-gradient-4x3", 4, 3,
            [11, 48, 85, 255, 159, 196, 233, 255, 51, 88, 125, 255, 199, 236, 17, 255,
             91, 128, 165, 255, 239, 20, 57, 255, 131, 168, 205, 255, 23, 60, 97, 255,
             171, 208, 245, 255, 63, 100, 137, 255, 211, 248, 29, 255, 103, 140, 177, 255],
            "4OcJNYpYeaZDA/drOoeIlFBIq4II"),
        ("alpha-gradient-3x4", 3, 4,
            [7, 36, 65, 0, 123, 152, 181, 41, 239, 12, 41, 82, 99, 128, 157, 123,
             215, 244, 17, 164, 75, 104, 133, 205, 191, 220, 249, 246, 51, 80, 109, 31,
             167, 196, 225, 72, 27, 56, 85, 113, 143, 172, 201, 154, 3, 32, 61, 195],
            "4OeFHAQXT5iVCDafZ+BXoi9tiwRlJ1PQBw=="),
    ];

    public static IEnumerable<(string hex, int width, int height, float ratio, RgbaColor average, PixelSample[] samples)> DecodeVectors =>
    [
        (
            "934A062D069256C374055867DA8AB6679490510719",
            23,
            32,
            5f / 7f,
            new RgbaColor(0.48412699f, 0.34126985f, 0.07936508f, 1f),
            [
                new PixelSample(0, 0, [27, 53, 52, 255]),
                new PixelSample(11, 16, [160, 94, 6, 255]),
                new PixelSample(22, 31, [158, 103, 0, 255]),
                new PixelSample(7, 10, [121, 91, 40, 255]),
                new PixelSample(15, 21, [155, 82, 0, 255]),
            ]),
        (
            "A1198A1C02383A25D727F68B971FF7F9717F80376758987906",
            26,
            32,
            4f / 5f,
            new RgbaColor(0.6164021f, 0.56878304f, 0.3862434f, 0.53333336f),
            [
                new PixelSample(0, 0, [169, 144, 66, 0]),
                new PixelSample(13, 16, [203, 210, 213, 255]),
                new PixelSample(25, 31, [172, 134, 22, 42]),
                new PixelSample(8, 10, [150, 151, 137, 158]),
                new PixelSample(17, 21, [191, 185, 154, 255]),
            ]),
    ];

    public sealed record PixelSample(int X, int Y, byte[] Rgba);
}
