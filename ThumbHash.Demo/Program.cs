using System;
using System.IO;
using BenchmarkDotNet.Running;
using ImageMagick;
using ThumbHash;

//1.png
//2.jpg
//3.webp
var relativePath = ""; 
while (true)
{
    Console.WriteLine("请选择图片格式：");
    Console.WriteLine("1. PNG");
    Console.WriteLine("2. JPEG");
    Console.WriteLine("3. WEBP");
    Console.WriteLine("4. AVIF");
    Console.WriteLine("按 Ctrl+C 退出");

    var formatKey = Console.ReadKey(intercept: true).KeyChar;
    relativePath = formatKey switch
    {
        '1' => "1.png",
        '2' => "2.jpg",
        '3' => "3.webp",
        '4' => "4.avif",
        _ => null
    };

    if (relativePath == null)
    {
        Console.WriteLine("\n无效的选择，请重试。");
        continue;
    }

    var path = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
    ThumbHashBenchMark.path = path;
    if (!File.Exists(path))
    {
        Console.WriteLine($"\n文件未找到: {path}");
        continue;
    }

    while (true)
    {
        Console.WriteLine("\n请选择操作：");
        Console.WriteLine("1. 运行 Benchmark");
        Console.WriteLine("2. 运行普通测试");
        Console.WriteLine("按 Ctrl+C 退出");

        var inputKey = Console.ReadKey(intercept: true).KeyChar;

        if (inputKey == '1')
        {
            var summary = BenchmarkRunner.Run<ThumbHashBenchMark>();
        }
        else if (inputKey == '2')
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var thumbHash = TestEncode(path);
            sw.Stop();
            Console.WriteLine($"编码测试完成，耗时: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine("ThumbHash: " + thumbHash);
            sw.Restart();
            var decodedImage = TestDecode(thumbHash);
            sw.Stop();
            Console.WriteLine($"解码测试完成，耗时: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"Decoded Image - Width: {decodedImage.Width}, Height: {decodedImage.Height}");
            // Save the decoded image
            using (var imageSharp = new MagickImage(decodedImage.Rgba, new PixelReadSettings((uint)decodedImage.Width, (uint)decodedImage.Height, StorageType.Char, PixelMapping.RGBA)))
            {
                imageSharp.Write(Path.Combine(Directory.GetCurrentDirectory(), "thumbhash-" + relativePath.Split('.')[0] + ".png"));
            }
        }
        else
        {
            break;
        }
    }
}
string TestEncode(string path)
{
    using var image = new MagickImage(path);
    int width = (int)image.Width;
    int height = (int)image.Height;
    byte[] rgba = image.GetPixels().ToByteArray(PixelMapping.RGBA);

    // 计算 ThumbHash
    return ThumbHashHelper.RgbaToThumbHashBase64(width, height, rgba);
    
}

ThumbHashHelper.Image TestDecode(string thumbHash)
{
    // 解码回 RGBA
    return ThumbHashHelper.ThumbHashToRgba(thumbHash);
}