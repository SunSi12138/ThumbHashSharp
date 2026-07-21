using BenchmarkDotNet.Attributes;

namespace ThumbHashSharp.Benchmarks;

[MemoryDiagnoser]
public class LargeImageResizeBenchmarks
{
    private const int Width = 1920;
    private const int Height = 1080;
    private byte[] _rgba = null!;
    private ThumbHashEncodingOptions _options = null!;

    [ParamsAllValues]
    public ThumbHashResizeMode ResizeMode { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rgba = BenchmarkImages.Create(Width, Height, hasAlpha: true);
        _options = new ThumbHashEncodingOptions { ResizeMode = ResizeMode };
    }

    [Benchmark]
    public byte[] Encode() => ThumbHash.Encode(Width, Height, _rgba, _options);
}
