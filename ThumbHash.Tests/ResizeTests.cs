namespace ThumbHashSharp.Tests;

[TestClass]
public sealed class ResizeTests
{
    [TestMethod]
    public void Encode_NoOptions_EqualsExplicitArea()
    {
        byte[] rgba = TestImages.CreatePattern(160, 120);

        byte[] defaultHash = ThumbHash.Encode(160, 120, rgba);
        byte[] explicitHash = ThumbHash.Encode(
            160,
            120,
            rgba,
            new ThumbHashEncodingOptions { ResizeMode = ThumbHashResizeMode.Area });

        CollectionAssert.AreEqual(defaultHash, explicitHash);
    }

    [TestMethod]
    public void Encode_SmallImage_IgnoresResizeMode()
    {
        byte[] rgba = TestImages.CreatePattern(100, 73);
        byte[] area = ThumbHash.Encode(100, 73, rgba, new ThumbHashEncodingOptions { ResizeMode = ThumbHashResizeMode.Area });
        byte[] bilinear = ThumbHash.Encode(100, 73, rgba, new ThumbHashEncodingOptions { ResizeMode = ThumbHashResizeMode.Bilinear });
        byte[] nearest = ThumbHash.Encode(100, 73, rgba, new ThumbHashEncodingOptions { ResizeMode = ThumbHashResizeMode.NearestNeighbor });

        CollectionAssert.AreEqual(area, bilinear);
        CollectionAssert.AreEqual(area, nearest);
    }

    [TestMethod]
    [DataRow(ThumbHashResizeMode.Area)]
    [DataRow(ThumbHashResizeMode.Bilinear)]
    [DataRow(ThumbHashResizeMode.NearestNeighbor)]
    public void Encode_LargeImage_IsDeterministic(ThumbHashResizeMode mode)
    {
        byte[] rgba = TestImages.CreatePattern(160, 120);
        var options = new ThumbHashEncodingOptions { ResizeMode = mode };

        byte[] first = ThumbHash.Encode(160, 120, rgba, options);
        byte[] second = ThumbHash.Encode(160, 120, rgba, options);

        CollectionAssert.AreEqual(first, second);
    }

    [TestMethod]
    [DataRow(ThumbHashResizeMode.Area, "IAiCBIAImKenuHoKTH3v0Q272Sovn5rTCQ==")]
    [DataRow(ThumbHashResizeMode.Bilinear, "IAiCBIAImKinuWkJbIvvsQsLmbg4iXuXCA==")]
    [DataRow(ThumbHashResizeMode.NearestNeighbor, "IAiCBIAIh2eHiVsIqWeflAkImMdHeHqHCA==")]
    public void Encode_LargeImage_MatchesResizeVector(ThumbHashResizeMode mode, string expectedBase64)
    {
        byte[] rgba = TestImages.CreatePattern(160, 120);
        var options = new ThumbHashEncodingOptions { ResizeMode = mode };

        string actual = ThumbHash.EncodeToBase64(160, 120, rgba, options);

        Assert.AreEqual(expectedBase64, actual);
    }

    [TestMethod]
    [DataRow(ThumbHashResizeMode.Area)]
    [DataRow(ThumbHashResizeMode.Bilinear)]
    public void Resize_TransparentColor_DoesNotBleed(ThumbHashResizeMode mode)
    {
        byte[] source = [255, 0, 0, 255, 0, 0, 255, 0];
        Span<byte> destination = stackalloc byte[4];

        ImageResizer.Resize(2, 1, source, 1, 1, destination, mode);

        Assert.AreEqual((byte)255, destination[0]);
        Assert.AreEqual((byte)0, destination[1]);
        Assert.AreEqual((byte)0, destination[2]);
        Assert.AreEqual((byte)128, destination[3]);
    }

    [TestMethod]
    public void Resize_FullyTransparentPixel_NormalizesRgbToBlack()
    {
        byte[] source = [12, 99, 220, 0, 240, 10, 33, 0];
        Span<byte> destination = stackalloc byte[4];

        ImageResizer.Resize(2, 1, source, 1, 1, destination, ThumbHashResizeMode.Area);

        CollectionAssert.AreEqual(new byte[4], destination.ToArray());
    }
}
