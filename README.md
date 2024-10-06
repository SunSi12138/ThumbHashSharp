# ThumbHashCSharp

这是一个 ThumbHash 的 C# 实现。ThumbHash 是一种生成图像缩略图（通常使用base64存储）的算法，具体原理见：[ThumbHash 原理](https://evanw.github.io/thumbhash/#:~:text=ThumbHash%20generates%20an%20image)。
这个算法很适合用来制作图片加载的占位符
## 示例

以下是几个图片前后的效果展示：

### 示例 1
<div align = "center">    
<img  src="ThumbHash.Demo/1.png" width="49%" />
<img  src="ThumbHash.Demo/thumbhash-1.png" width="49%" />
<br>
  png图片
</div>

### 示例 2
<div align = "center">    
<img  src="ThumbHash.Demo/2.jpg" width="49%" />
<img  src="ThumbHash.Demo/thumbhash-2.jpg" width="49%" height="180px" />
<br>
  jpg图片
</div>

### 示例 3
<div align = "center">    
<img  src="ThumbHash.Demo/3.webp" width="49%" />
<img  src="ThumbHash.Demo/thumbhash-3.webp" width="49%" height="256.77px" />
<br>
  webp图片(因为webp图片的解码问题预览图保存为webp放大效果有问题，保存为其他格式会好一些）
</div>
