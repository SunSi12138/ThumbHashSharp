using ImageMagick;
using ThumbHashSharp;

if (args.Length == 0)
{
    Console.WriteLine("Usage: dotnet run --project ThumbHash.Demo -- <image-path> [Area|Bilinear|NearestNeighbor]");
    return;
}

string inputPath = Path.GetFullPath(args[0]);
if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Image not found: {inputPath}");
    Environment.ExitCode = 1;
    return;
}

ThumbHashResizeMode resizeMode = ThumbHashResizeMode.Area;
if (args.Length > 1
    && (!Enum.TryParse(args[1], ignoreCase: true, out resizeMode) || !Enum.IsDefined(resizeMode)))
{
    Console.Error.WriteLine($"Unknown resize mode: {args[1]}");
    Environment.ExitCode = 1;
    return;
}

using var source = new MagickImage(inputPath);
int width = checked((int)source.Width);
int height = checked((int)source.Height);
byte[] rgba = source.GetPixels().ToByteArray(PixelMapping.RGBA)
    ?? throw new InvalidOperationException("ImageMagick did not return an RGBA pixel buffer.");
var options = new ThumbHashEncodingOptions { ResizeMode = resizeMode };
string base64 = ThumbHash.EncodeToBase64(width, height, rgba, options);
ThumbHashImage decoded = ThumbHash.DecodeFromBase64(base64);

string outputPath = Path.Combine(
    Path.GetDirectoryName(inputPath) ?? Directory.GetCurrentDirectory(),
    $"thumbhash-{Path.GetFileNameWithoutExtension(inputPath)}.png");

using var placeholder = new MagickImage(
    decoded.Rgba.ToArray(),
    new PixelReadSettings((uint)decoded.Width, (uint)decoded.Height, StorageType.Char, PixelMapping.RGBA));
placeholder.Write(outputPath);

Console.WriteLine($"ThumbHash: {base64}");
Console.WriteLine($"Decoded size: {decoded.Width}x{decoded.Height}");
Console.WriteLine($"Resize mode: {resizeMode}");
Console.WriteLine($"Wrote: {outputPath}");
