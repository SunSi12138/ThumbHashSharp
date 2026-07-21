#include "thumbhashsharp.h"

#include <stdint.h>
#include <stdio.h>

static int check(int condition, const char* message)
{
    if (!condition) {
        fprintf(stderr, "FAILED: %s\n", message);
        return 0;
    }

    return 1;
}

int main(void)
{
    const uint8_t source[] = {
        255, 0, 0, 255,
        0, 255, 0, 255,
        0, 0, 255, 128,
        255, 255, 255, 0
    };
    uint8_t hash[THUMBHASHSHARP_MAX_HASH_LENGTH] = {0};
    int32_t hash_length = 0;

    if (!check(thumbhashsharp_version() == 0x00020000u, "unexpected native library version")) return 1;

    int32_t result = thumbhashsharp_encode(
        2,
        2,
        source,
        (int32_t)sizeof(source),
        THUMBHASHSHARP_RESIZE_AREA,
        NULL,
        0,
        &hash_length);
    if (!check(result == THUMBHASHSHARP_BUFFER_TOO_SMALL, "encode size query failed")) return 1;
    if (!check(hash_length >= 13 && hash_length <= THUMBHASHSHARP_MAX_HASH_LENGTH, "invalid queried hash length")) return 1;

    result = thumbhashsharp_encode(
        2,
        2,
        source,
        (int32_t)sizeof(source),
        THUMBHASHSHARP_RESIZE_AREA,
        hash,
        (int32_t)sizeof(hash),
        &hash_length);
    if (!check(result == THUMBHASHSHARP_SUCCESS, "encode failed")) return 1;
    if (!check(hash_length >= 13 && hash_length <= THUMBHASHSHARP_MAX_HASH_LENGTH, "invalid hash length")) return 1;
    const int32_t encoded_hash_length = hash_length;

    int32_t width = 0;
    int32_t height = 0;
    int32_t rgba_length = 0;
    result = thumbhashsharp_decode(hash, hash_length, NULL, 0, &width, &height, &rgba_length);
    if (!check(result == THUMBHASHSHARP_BUFFER_TOO_SMALL, "decode size query failed")) return 1;
    if (!check(width > 0 && width <= 32 && height > 0 && height <= 32, "invalid decoded dimensions")) return 1;
    if (!check(rgba_length == width * height * 4, "invalid decoded RGBA length")) return 1;

    uint8_t decoded[THUMBHASHSHARP_MAX_DECODED_RGBA_LENGTH] = {0};
    result = thumbhashsharp_decode(
        hash,
        hash_length,
        decoded,
        (int32_t)sizeof(decoded),
        &width,
        &height,
        &rgba_length);
    if (!check(result == THUMBHASHSHARP_SUCCESS, "decode failed")) return 1;
    const int32_t decoded_width = width;
    const int32_t decoded_height = height;

    float red = 0.0f;
    float green = 0.0f;
    float blue = 0.0f;
    float alpha = 0.0f;
    result = thumbhashsharp_average_color(hash, hash_length, &red, &green, &blue, &alpha);
    if (!check(result == THUMBHASHSHARP_SUCCESS, "average color failed")) return 1;
    if (!check(red >= 0.0f && red <= 1.0f, "invalid red component")) return 1;
    if (!check(green >= 0.0f && green <= 1.0f, "invalid green component")) return 1;
    if (!check(blue >= 0.0f && blue <= 1.0f, "invalid blue component")) return 1;
    if (!check(alpha >= 0.0f && alpha <= 1.0f, "invalid alpha component")) return 1;

    float ratio = 0.0f;
    result = thumbhashsharp_aspect_ratio(hash, hash_length, &ratio);
    if (!check(result == THUMBHASHSHARP_SUCCESS && ratio > 0.0f, "aspect ratio failed")) return 1;

    int32_t invalid_hash_length = 0;
    result = thumbhashsharp_encode(
        2,
        2,
        source,
        (int32_t)sizeof(source),
        99,
        hash,
        (int32_t)sizeof(hash),
        &invalid_hash_length);
    if (!check(result == THUMBHASHSHARP_INVALID_RESIZE_MODE, "invalid resize mode was accepted")) return 1;

    int32_t invalid_width = 0;
    int32_t invalid_height = 0;
    int32_t invalid_rgba_length = 0;
    result = thumbhashsharp_decode(
        hash,
        4,
        decoded,
        (int32_t)sizeof(decoded),
        &invalid_width,
        &invalid_height,
        &invalid_rgba_length);
    if (!check(result == THUMBHASHSHARP_INVALID_HASH, "invalid hash was accepted")) return 1;

    printf(
        "ThumbHashSharp Native smoke test passed: hash=%d bytes, decoded=%dx%d, ratio=%.3f\n",
        encoded_hash_length,
        decoded_width,
        decoded_height,
        ratio);
    return 0;
}
