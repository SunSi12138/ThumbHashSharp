using System;
using System.IO;
using BenchmarkDotNet.Running;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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
    Console.WriteLine("按 Ctrl+C 退出");

    var formatInput = Console.ReadKey();

    switch (formatInput.KeyChar)
    {
        //switch key pressed    
        case '1':
            relativePath = "1.png";
            break;
        case '2':
            relativePath = "2.jpg";
            break;
        case '3':
            relativePath = "3.webp";
            break;
        default:
        {
            Console.WriteLine("退出");
            return;
        }
    }

    var path = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
    while (true)
    {
        Console.WriteLine("请选择操作：");
        Console.WriteLine("1. 运行 Benchmark");
        Console.WriteLine("2. 运行普通测试");
        Console.WriteLine("其他返回上一级");
        Console.WriteLine("按 Ctrl+C 退出");

        var input = Console.ReadKey();

        if (input.KeyChar == '1')
        {
            ThumbHashBenchMark.path = relativePath;
            var summary = BenchmarkRunner.Run<ThumbHashBenchMark>();
        }
        else if (input.KeyChar == '2')
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
            using (Image<Rgba32> imageSharp = Image.LoadPixelData<Rgba32>(decodedImage.Rgba, decodedImage.Width, decodedImage.Height))
            {
                imageSharp.Save(Path.Combine(Directory.GetCurrentDirectory(), "thumbhash-"+relativePath.Split('.')[0]+".png")); 
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
    using Image<Rgba32> imageSharp = Image.Load<Rgba32>(path);
    int width = imageSharp.Width;
    int height = imageSharp.Height;
    // 提取 RGBA 数据
    byte[] rgba = new byte[width * height * 4];
    imageSharp.CopyPixelDataTo(rgba);

    // 计算ThumbHash
    return ThumbHashHelper.RgbaToThumbHashBase64(width, height, rgba);
    
}

ThumbHashHelper.Image TestDecode(string thumbHash)
{
    // Decode back to RGBA
    return ThumbHashHelper.ThumbHashToRgba(thumbHash);
}