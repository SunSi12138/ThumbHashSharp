# ThumbHashSharp

[ThumbHash](https://evanw.github.io/thumbhash/) 的零图像库依赖 C# 实现，可将 RGBA 图像编码成紧凑的占位图哈希。

v2 会在输入图片任一边超过 100 像素时自动等比例缩小，最长边固定为 100。缩放算法可按每次编码选择，默认使用适合大比例缩小的面积缩放。

## 安装

```shell
dotnet add package ThumbHashSharp
```

核心包面向 `netstandard2.1`，不依赖 ImageMagick、ImageSharp 或 SkiaSharp。

## 快速开始

输入必须是按行排列、**未预乘透明度**的 RGBA8888 数据，长度严格等于 `width * height * 4`。

```csharp
using ThumbHashSharp;

const int width = 2;
const int height = 2;
ReadOnlySpan<byte> rgba =
[
    255, 0, 0, 255,
    0, 255, 0, 255,
    0, 0, 255, 255,
    255, 255, 255, 255,
];

string base64 = ThumbHash.EncodeToBase64(width, height, rgba);
ThumbHashImage image = ThumbHash.DecodeFromBase64(base64);
RgbaColor average = ThumbHash.GetAverageColorFromBase64(base64);
float ratio = ThumbHash.GetApproximateAspectRatioFromBase64(base64);

ReadOnlyMemory<byte> decodedRgba = image.Rgba;
```

字节形式使用 `ThumbHash.Encode` 和 `ThumbHash.Decode`。Base64 形式使用对应的 `ToBase64`、`FromBase64` 方法。

## 大图缩放

```csharp
var options = new ThumbHashEncodingOptions
{
    ResizeMode = ThumbHashResizeMode.Bilinear,
};

byte[] hash = ThumbHash.Encode(width, height, rgba, options);
```

| 模式 | 特点 | 适合场景 |
| --- | --- | --- |
| `Area` | 默认；按源像素覆盖面积加权 | 大比例缩小，优先保留整体颜色与细节 |
| `Bilinear` | 按像素中心双线性插值 | 质量与速度折中 |
| `NearestNeighbor` | 只选择最近源像素，不混色 | 速度优先或像素风图像 |

`Area` 和 `Bilinear` 都在预乘透明度空间中计算，再恢复成未预乘 RGBA，可避免透明边缘产生 RGB 色晕。输入不超过 100×100 时不会重采样，因此三种模式会得到完全相同的哈希。配置只影响当前调用，没有全局可变状态。

## API

```csharp
ThumbHash.Encode(width, height, rgba, options: null)
ThumbHash.EncodeToBase64(width, height, rgba, options: null)
ThumbHash.Decode(hash)
ThumbHash.DecodeFromBase64(base64)
ThumbHash.GetAverageColor(hash)
ThumbHash.GetAverageColorFromBase64(base64)
ThumbHash.GetApproximateAspectRatio(hash)
ThumbHash.GetApproximateAspectRatioFromBase64(base64)
```

`Decode` 会严格校验完整哈希长度；平均色与宽高比方法只读取并校验它们实际需要的头部。

## 示例程序

演示项目使用 ImageMagick 读取常见图片格式，将解码后的占位图写到输入图片旁边：

```shell
dotnet run --project ThumbHash.Demo -- path/to/image.jpg
dotnet run --project ThumbHash.Demo -- path/to/image.jpg Bilinear
```

可选模式为 `Area`、`Bilinear` 和 `NearestNeighbor`。

## 图片渐进加载 Web Demo

[ThumbHash.WebDemo](ThumbHash.WebDemo) 提供一个预生成的静态页面，展示 ThumbHash 占位图平滑过渡到 JPEG、WebP、AVIF 和透明 PNG 原图的效果。页面包含九张不同题材的可复用图片、模拟加载延迟、重播加载、按住对比占位图和 ThumbHash 复制功能。

```shell
python3 -m http.server 8080 --directory ThumbHash.WebDemo
```

访问 `http://localhost:8080`。素材已经由 `ThumbHash.WebDemo.Generator` 使用默认 `Area` 模式处理好，打开页面时不需要 .NET 或图像库。

## NativeAOT 原生库

[ThumbHashSharp.Native](ThumbHash.Native) 将相同的 C# 核心算法发布为自包含的动态库，提供稳定 C ABI，可供 C、C++、Rust、Swift、Go、Python 等语言调用。原生层仍只接收未预乘 RGBA8888，不包含 Magick.NET 或其他图片解码器。

```shell
dotnet publish ThumbHash.Native/ThumbHashSharp.Native.csproj \
  --configuration Release \
  --runtime osx-arm64 \
  --output artifacts/native/osx-arm64
```

跨语言声明与错误码见 [thumbhashsharp.h](ThumbHash.Native/include/thumbhashsharp.h)。每个平台和 CPU 架构需要分别发布 `.dll`、`.so` 或 `.dylib`。

## 开发与验证

```shell
dotnet build ThumbHash.sln --configuration Release
dotnet test ThumbHash.Tests/ThumbHashSharp.Tests.csproj --configuration Release
dotnet pack ThumbHash/ThumbHash.csproj --configuration Release --output artifacts/packages
```

测试覆盖官方互操作向量、三种大图缩放模式、透明边缘、参数错误、随机属性和 README 示例。

性能基准包含 32×32、100×100、1920×1080，透明/不透明图像，以及默认配置与显式 `Area` 的对照。请在机器空闲且使用 Release 配置时运行：

```shell
dotnet run --project ThumbHash.Benchmarks --configuration Release -- --filter "*"
```

结果由 BenchmarkDotNet 输出到 `BenchmarkDotNet.Artifacts`。

## 许可证

ThumbHashSharp 采用 [MIT 许可证](LICENSE)。原始 ThumbHash 实现的版权和许可证见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。
