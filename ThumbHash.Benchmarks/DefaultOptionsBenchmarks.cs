using BenchmarkDotNet.Attributes;

namespace ThumbHashSharp.Benchmarks;

[MemoryDiagnoser]
public class DefaultOptionsBenchmarks
{
    private const int Width = 1920;
    private const int Height = 1080;
    private byte[] _rgba = null!;
    private ThumbHashEncodingOptions _areaOptions = null!;

    [GlobalSetup]
    public void Setup()
    {
        _rgba = BenchmarkImages.Create(Width, Height, hasAlpha: true);
        _areaOptions = new ThumbHashEncodingOptions { ResizeMode = ThumbHashResizeMode.Area };
    }

    [Benchmark(Baseline = true)]
    public byte[] EncodeDefault() => ThumbHash.Encode(Width, Height, _rgba);

    [Benchmark]
    public byte[] EncodeExplicitArea() => ThumbHash.Encode(Width, Height, _rgba, _areaOptions);
}
