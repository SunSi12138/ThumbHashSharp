using System.Reflection;

namespace ThumbHashSharp.Tests;

[TestClass]
public sealed class PublicApiTests
{
    [TestMethod]
    public void Assembly_ExportsOnlyV2TypesAndThumbHashMethods()
    {
        Assembly assembly = typeof(ThumbHash).Assembly;
        string[] exportedTypes = assembly
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .Order()
            .ToArray();
        string[] methods = typeof(ThumbHash)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .Order()
            .ToArray();

        Assert.AreEqual("ThumbHashSharp", assembly.GetName().Name);
        CollectionAssert.AreEqual(
            new[]
            {
                "ThumbHashSharp.RgbaColor",
                "ThumbHashSharp.ThumbHash",
                "ThumbHashSharp.ThumbHashEncodingOptions",
                "ThumbHashSharp.ThumbHashImage",
                "ThumbHashSharp.ThumbHashResizeMode",
            },
            exportedTypes);
        CollectionAssert.AreEqual(
            new[]
            {
                "Decode",
                "DecodeFromBase64",
                "Encode",
                "EncodeToBase64",
                "GetApproximateAspectRatio",
                "GetApproximateAspectRatioFromBase64",
                "GetAverageColor",
                "GetAverageColorFromBase64",
            },
            methods);
        Assert.IsNull(assembly.GetType("ThumbHash.ThumbHashHelper"));
    }
}
