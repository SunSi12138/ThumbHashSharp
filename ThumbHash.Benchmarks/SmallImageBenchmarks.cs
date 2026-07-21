using BenchmarkDotNet.Attributes;

namespace ThumbHashSharp.Benchmarks;

[MemoryDiagnoser]
public class SmallImageBenchmarks
{
    private byte[] _rgba = null!;
    private byte[] _hash = null!;

    [Params(32, 100)]
    public int Size { get; set; }

    [Params(false, true)]
    public bool HasAlpha { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rgba = BenchmarkImages.Create(Size, Size, HasAlpha);
        _hash = ThumbHash.Encode(Size, Size, _rgba);
    }

    [Benchmark]
    public byte[] Encode() => ThumbHash.Encode(Size, Size, _rgba);

    [Benchmark]
    public ThumbHashImage Decode() => ThumbHash.Decode(_hash);
}
