# ThumbHashSharp.Native

NativeAOT shared-library wrapper for ThumbHashSharp. It exports a stable C ABI and does not include an image decoder or Magick.NET. Inputs and outputs use unpremultiplied, row-major RGBA8888 pixels.

## Build

Publish separately for each target runtime identifier:

```shell
dotnet publish ThumbHash.Native/ThumbHashSharp.Native.csproj -c Release -r osx-arm64 -o artifacts/native/osx-arm64
dotnet publish ThumbHash.Native/ThumbHashSharp.Native.csproj -c Release -r linux-x64 -o artifacts/native/linux-x64
dotnet publish ThumbHash.Native/ThumbHashSharp.Native.csproj -c Release -r win-x64 -o artifacts/native/win-x64
```

The result is a self-contained `thumbhashsharp_native.dylib`, `thumbhashsharp_native.so`, or `thumbhashsharp_native.dll`; the target machine does not need .NET installed. Copy [include/thumbhashsharp.h](include/thumbhashsharp.h) alongside the platform library for consumers.

NativeAOT shared libraries must not be unloaded with `dlclose` or `FreeLibrary` after loading.

## C API

```c
#include "thumbhashsharp.h"

uint8_t hash[THUMBHASHSHARP_MAX_HASH_LENGTH];
int32_t hash_length;

int32_t result = thumbhashsharp_encode(
    width,
    height,
    rgba,
    rgba_length,
    THUMBHASHSHARP_RESIZE_AREA,
    hash,
    sizeof(hash),
    &hash_length);
```

Every function returns a `thumbhashsharp_result` code. No managed object, exception, or allocated buffer crosses the ABI boundary. To decode, first call `thumbhashsharp_decode` with a null RGBA pointer and zero capacity to query the required dimensions and byte length; this query returns `THUMBHASHSHARP_BUFFER_TOO_SMALL` after filling the output values.

Base64 conversion intentionally remains the responsibility of the calling language.
