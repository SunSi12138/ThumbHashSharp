# ThumbHashSharp

ThumbHashSharp 是一个用于生成和解码 ThumbHash 的 C# 库。

## 安装

你可以通过 NuGet 安装 ThumbHashSharp：

```bash
dotnet add package ThumbHashSharp
```

## 使用方法

以下是如何使用 ThumbHashSharp 的一些示例：

```csharp
using ThumbHashSharp;

// 将 RGBA 图像编码为 ThumbHash
int width = 100;
int height = 100;
ReadOnlySpan<byte> rgba = ...; // 你的 RGBA 数据
string thumbHashBase64 = ThumbHashHelper.RgbaToThumbHashBase64(width, height, rgba);

// 将 ThumbHash 解码为 RGBA 图像
Image image = ThumbHashHelper.ThumbHashToRgba(thumbHashBase64);

// 从 ThumbHash 中提取平均颜色
RGBA averageColor = ThumbHashHelper.ThumbHashToAverageRgba(thumbHashBase64);

// 提取原始图像的近似宽高比
float aspectRatio = ThumbHashHelper.ThumbHashToApproximateAspectRatio(thumbHashBase64);
```

## 原理

有关 ThumbHash 的详细原理，请参阅 [ThumbHash 原理](https://evanw.github.io/thumbhash/#:~:text=ThumbHash%20generates%20an%20image)。

## GitHub 链接

你可以在 GitHub 上找到 ThumbHashSharp 的源代码：[ThumbHashSharp](https://github.com/SunSi12138/ThumbHashSharp)