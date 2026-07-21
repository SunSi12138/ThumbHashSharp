namespace ThumbHashSharp.Tests;

[TestClass]
public sealed class PropertyTests
{
    [TestMethod]
    public void RandomImages_EncodeAndDecode_MaintainInvariants()
    {
        var random = new Random(0x5EED);
        ThumbHashResizeMode[] modes = Enum.GetValues<ThumbHashResizeMode>();

        for (int iteration = 0; iteration < 50; iteration++)
        {
            int width = random.Next(1, 141);
            int height = random.Next(1, 141);
            byte[] rgba = new byte[width * height * 4];
            random.NextBytes(rgba);
            ThumbHashResizeMode mode = modes[iteration % modes.Length];
            var options = new ThumbHashEncodingOptions { ResizeMode = mode };

            byte[] firstHash = ThumbHash.Encode(width, height, rgba, options);
            byte[] secondHash = ThumbHash.Encode(width, height, rgba, options);
            ThumbHashImage image = ThumbHash.Decode(firstHash);

            CollectionAssert.AreEqual(firstHash, secondHash, $"Iteration {iteration} was not deterministic.");
            Assert.IsInRange(13, 25, firstHash.Length, $"Iteration {iteration} produced an invalid hash length.");
            Assert.IsInRange(1, 32, image.Width, $"Iteration {iteration} produced an invalid width.");
            Assert.IsInRange(1, 32, image.Height, $"Iteration {iteration} produced an invalid height.");
            Assert.AreEqual(image.Width * image.Height * 4, image.Rgba.Length);
        }
    }
}
