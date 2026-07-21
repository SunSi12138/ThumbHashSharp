namespace ThumbHashSharp.Tests;

[TestClass]
public sealed class ValidationTests
{
    [TestMethod]
    [DataRow(0, 1, "width")]
    [DataRow(-1, 1, "width")]
    [DataRow(1, 0, "height")]
    [DataRow(1, -1, "height")]
    public void Encode_NonPositiveDimension_Throws(int width, int height, string parameterName)
    {
        var exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThumbHash.Encode(width, height, []));

        Assert.AreEqual(parameterName, exception.ParamName);
    }

    [TestMethod]
    [DataRow(3)]
    [DataRow(5)]
    public void Encode_MismatchedBufferLength_Throws(int length)
    {
        var exception = Assert.ThrowsExactly<ArgumentException>(() => ThumbHash.Encode(1, 1, new byte[length]));

        Assert.AreEqual("rgba", exception.ParamName);
    }

    [TestMethod]
    public void Encode_OverflowingDimensions_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThumbHash.Encode(int.MaxValue, int.MaxValue, []));
    }

    [TestMethod]
    public void Encode_UnknownResizeMode_ThrowsEvenForSmallImage()
    {
        var options = new ThumbHashEncodingOptions { ResizeMode = (ThumbHashResizeMode)99 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThumbHash.Encode(1, 1, [0, 0, 0, 255], options));
    }

    [TestMethod]
    public void Decode_TruncatedHash_Throws()
    {
        byte[] hash = ThumbHash.Encode(2, 2, TestImages.CreatePattern(2, 2));

        Assert.ThrowsExactly<ArgumentException>(() => ThumbHash.Decode(hash.AsSpan(0, hash.Length - 1)));
    }

    [TestMethod]
    public void Decode_HashWithTrailingByte_Throws()
    {
        byte[] hash = ThumbHash.Encode(2, 2, TestImages.CreatePattern(2, 2));
        byte[] extended = [.. hash, 0];

        Assert.ThrowsExactly<ArgumentException>(() => ThumbHash.Decode(extended));
    }

    [TestMethod]
    public void HeaderReaders_OpaqueFiveByteHeader_Succeeds()
    {
        byte[] hash = ThumbHash.Encode(1, 1, [255, 0, 0, 255]);
        ReadOnlySpan<byte> header = hash.AsSpan(0, 5);

        RgbaColor average = ThumbHash.GetAverageColor(header);
        float ratio = ThumbHash.GetApproximateAspectRatio(header);

        Assert.AreEqual(1f, average.A);
        Assert.AreEqual(1f, ratio);
    }

    [TestMethod]
    public void GetAverageColor_AlphaHeaderWithoutSixthByte_Throws()
    {
        byte[] header = [0, 0, 0x80, 0, 0];

        Assert.ThrowsExactly<ArgumentException>(() => ThumbHash.GetAverageColor(header));
    }

    [TestMethod]
    [DataRow("not base64")]
    [DataRow("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void DecodeFromBase64_InvalidValue_ThrowsFormatException(string value)
    {
        Assert.ThrowsExactly<FormatException>(() => ThumbHash.DecodeFromBase64(value));
    }

    [TestMethod]
    public void DecodeFromBase64_EmptyHash_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => ThumbHash.DecodeFromBase64(string.Empty));
    }

    [TestMethod]
    public void DecodeFromBase64_Null_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => ThumbHash.DecodeFromBase64(null!));
    }
}
