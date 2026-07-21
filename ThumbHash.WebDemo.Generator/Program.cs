using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ImageMagick;
using ThumbHashSharp;

SourceDefinition[] sources =
[
    new(
        "yangshuo-panorama.jpg",
        "阳朔翠屏全景",
        "超宽全景 · JPEG",
        "wide",
        "Chensiyuan",
        "CC BY-SA 4.0",
        "https://commons.wikimedia.org/wiki/File:1_pano_cuiping_yangshuo_2016.jpg",
        "https://upload.wikimedia.org/wikipedia/commons/thumb/1/1b/1_pano_cuiping_yangshuo_2016.jpg/1920px-1_pano_cuiping_yangshuo_2016.jpg"),
    new(
        "santorini-architecture.webp",
        "圣托里尼巷道",
        "竖幅建筑 · WebP",
        "tall",
        "Norbert Nagel",
        "CC BY-SA 3.0",
        "https://commons.wikimedia.org/wiki/File:07-17-2012_-_Emborio_-_Emporio_-_Santorini_-_Greece_-_11.jpg",
        "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c8/07-17-2012_-_Emborio_-_Emporio_-_Santorini_-_Greece_-_11.jpg/1920px-07-17-2012_-_Emborio_-_Emporio_-_Santorini_-_Greece_-_11.jpg"),
    new(
        "blue-city.avif",
        "舍夫沙万蓝城",
        "高饱和街景 · AVIF",
        "standard",
        "Fbrandao.1963",
        "CC BY-SA 4.0",
        "https://commons.wikimedia.org/wiki/File:2018_01_(Blue)_-_Chaouen.jpg",
        "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c7/2018_01_%28Blue%29_-_Chaouen.jpg/1920px-2018_01_%28Blue%29_-_Chaouen.jpg"),
    new(
        "lake-canoe.jpg",
        "湖上独木舟",
        "旅行纪实 · JPEG",
        "standard",
        "Roberto Nickson",
        "Unsplash License",
        "https://unsplash.com/photos/7BjmDICVloE",
        "https://picsum.photos/id/1011/1600/1067"),
    new(
        "portrait.webp",
        "窗边人物肖像",
        "竖幅人像 · WebP",
        "tall",
        "Roksolana Zasiadko",
        "Unsplash License",
        "https://unsplash.com/photos/LyeduBb2Auk",
        "https://picsum.photos/id/1027/1067/1600"),
    new(
        "strawberries.jpg",
        "新鲜草莓",
        "高饱和食物 · JPEG",
        "standard",
        "veeterzy",
        "Unsplash License",
        "https://unsplash.com/photos/OJJIaFZOeX4",
        "https://picsum.photos/id/1080/1600/1067"),
    new(
        "black-puppy.avif",
        "黑色幼犬",
        "动物特写 · AVIF",
        "standard",
        "André Spieker",
        "Unsplash License",
        "https://unsplash.com/photos/8wTPqxlnKM4",
        "https://picsum.photos/id/237/1600/958"),
    new(
        "transparent-cactus.png",
        "透明仙人掌图标",
        "透明插画 · PNG",
        "standard",
        "OpenMoji contributors",
        "CC BY-SA 4.0",
        "https://github.com/hfg-gmuend/openmoji/blob/master/color/618x618/1F335.png",
        "https://raw.githubusercontent.com/hfg-gmuend/openmoji/master/color/618x618/1F335.png"),
    new(
        "lighthouse.webp",
        "星空灯塔",
        "夜景建筑 · WebP",
        "tall",
        "Joshua Hibbert",
        "Unsplash License",
        "https://unsplash.com/photos/Pn6iimgM-wo",
        "https://picsum.photos/id/870/1070/1600"),
];

bool forceDownload = args.Contains("--force", StringComparer.OrdinalIgnoreCase);
string? outputArgument = args.FirstOrDefault(argument => !argument.StartsWith("--", StringComparison.Ordinal));
string outputRoot = outputArgument is not null
    ? Path.GetFullPath(outputArgument)
    : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "ThumbHash.WebDemo"));
string imageDirectory = Path.Combine(outputRoot, "assets", "images");
string placeholderDirectory = Path.Combine(outputRoot, "assets", "placeholders");
Directory.CreateDirectory(imageDirectory);
Directory.CreateDirectory(placeholderDirectory);

using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ThumbHashSharp-WebDemo/2.0");
var results = new List<DemoImage>(sources.Length);

foreach (SourceDefinition source in sources)
{
    Console.WriteLine($"Processing {source.Title}...");
    string imagePath = Path.Combine(imageDirectory, source.FileName);
    using var image = !forceDownload && File.Exists(imagePath)
        ? new MagickImage(imagePath)
        : new MagickImage(await DownloadWithRetryAsync(httpClient, source.DownloadUrl));
    if (forceDownload || !File.Exists(imagePath))
    {
        image.AutoOrient();
        ResizeToFit(image, 1440);
        image.Strip();
        image.Format = GetFormat(source.FileName);
        image.Quality = source.FileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? 95u : 84u;
        image.Write(imagePath);
        await Task.Delay(750);
    }

    int width = checked((int)image.Width);
    int height = checked((int)image.Height);
    byte[] rgba = image.GetPixels().ToByteArray(PixelMapping.RGBA)
        ?? throw new InvalidOperationException($"Could not read RGBA pixels for {source.FileName}.");
    string thumbHash = ThumbHash.EncodeToBase64(width, height, rgba);
    ThumbHashImage decoded = ThumbHash.DecodeFromBase64(thumbHash);
    RgbaColor average = ThumbHash.GetAverageColorFromBase64(thumbHash);

    string placeholderFileName = Path.GetFileNameWithoutExtension(source.FileName) + ".png";
    string placeholderPath = Path.Combine(placeholderDirectory, placeholderFileName);
    using (var placeholder = new MagickImage(
        decoded.Rgba.ToArray(),
        new PixelReadSettings((uint)decoded.Width, (uint)decoded.Height, StorageType.Char, PixelMapping.RGBA)))
    {
        placeholder.Format = MagickFormat.Png;
        placeholder.Write(placeholderPath);
    }

    results.Add(new DemoImage(
        source.Title,
        source.Kind,
        source.Layout,
        $"assets/images/{source.FileName}",
        $"assets/placeholders/{placeholderFileName}",
        width,
        height,
        new FileInfo(imagePath).Length,
        Convert.FromBase64String(thumbHash).Length,
        thumbHash,
        ToCssColor(average),
        source.Author,
        source.License,
        source.SourcePage));
}

var payload = new
{
    generatedWith = "ThumbHashSharp 2.0.0",
    resizeMode = ThumbHashResizeMode.Area.ToString(),
    images = results,
};
var jsonOptions = new JsonSerializerOptions
{
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
};
string json = JsonSerializer.Serialize(payload, jsonOptions);
await File.WriteAllTextAsync(
    Path.Combine(outputRoot, "demo-data.js"),
    $"window.THUMBHASH_DEMO = {json};{Environment.NewLine}",
    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

Console.WriteLine($"Generated {results.Count} images in {outputRoot}");

static async Task<byte[]> DownloadWithRetryAsync(HttpClient httpClient, string url)
{
    for (int attempt = 0; ; attempt++)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync();
        }

        if ((int)response.StatusCode != 429 || attempt >= 5)
        {
            response.EnsureSuccessStatusCode();
        }

        TimeSpan delay = response.Headers.RetryAfter?.Delta
            ?? TimeSpan.FromSeconds(Math.Min(30, 2 << attempt));
        Console.WriteLine($"Wikimedia rate limit reached; retrying in {delay.TotalSeconds:0} seconds...");
        await Task.Delay(delay);
    }
}

static void ResizeToFit(MagickImage image, uint maximumDimension)
{
    uint currentMaximum = Math.Max(image.Width, image.Height);
    if (currentMaximum <= maximumDimension)
    {
        return;
    }

    double scale = maximumDimension / (double)currentMaximum;
    uint width = Math.Max(1u, (uint)Math.Floor((image.Width * scale) + 0.5d));
    uint height = Math.Max(1u, (uint)Math.Floor((image.Height * scale) + 0.5d));
    image.Resize(width, height);
}

static MagickFormat GetFormat(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
{
    ".avif" => MagickFormat.Avif,
    ".jpg" or ".jpeg" => MagickFormat.Jpeg,
    ".png" => MagickFormat.Png,
    ".webp" => MagickFormat.WebP,
    string extension => throw new ArgumentOutOfRangeException(nameof(fileName), extension, "Unknown image format."),
};

static string ToCssColor(RgbaColor color)
{
    int red = ToByte(color.R);
    int green = ToByte(color.G);
    int blue = ToByte(color.B);
    return $"rgb({red} {green} {blue})";
}

static int ToByte(float value) =>
    (int)Math.Floor((Math.Clamp(value, 0f, 1f) * 255f) + 0.5f);

internal sealed record SourceDefinition(
    string FileName,
    string Title,
    string Kind,
    string Layout,
    string Author,
    string License,
    string SourcePage,
    string DownloadUrl);

internal sealed record DemoImage(
    string Title,
    string Kind,
    string Layout,
    string Source,
    string Placeholder,
    int Width,
    int Height,
    long FileBytes,
    int HashBytes,
    string ThumbHash,
    string AverageColor,
    string Author,
    string License,
    string SourcePage);
