# ThumbHashSharp 渐进加载演示

这是一个完全静态的 ThumbHash 图片加载演示。九张可复用图片、ThumbHash 和解码后的占位 PNG 均已预先生成；浏览器只负责加载原图并完成视觉过渡。

直接打开 `index.html` 即可使用。为了获得与线上一致的缓存行为，也可以在仓库根目录启动任意静态文件服务器，例如：

```shell
python3 -m http.server 8080 --directory ThumbHash.WebDemo
```

然后访问 `http://localhost:8080`。

## 重新生成素材

生成器会从 Wikimedia Commons、Picsum/Unsplash 与 OpenMoji 下载指定图片，将最长边限制在 1440 像素，转换为演示所需的 JPEG、WebP、AVIF 或 PNG，然后使用 ThumbHashSharp 默认的 `Area` 模式编码：

```shell
HTTPS_PROXY=http://127.0.0.1:7890 \
HTTP_PROXY=http://127.0.0.1:7890 \
dotnet run --project ThumbHash.WebDemo.Generator --configuration Release -- ThumbHash.WebDemo
```

生成结果：

- `assets/images`：页面加载的优化原图；
- `assets/placeholders`：由 ThumbHash 解码得到的微型 PNG；
- `demo-data.js`：尺寸、文件大小、Base64 ThumbHash、平均色和图片署名。

每张图片的名称直接链接到对应来源页；作者和许可仍保存在生成的 `demo-data.js` 中。
