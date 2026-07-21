#ifndef THUMBHASHSHARP_H
#define THUMBHASHSHARP_H

#include <stdint.h>

#if defined(_WIN32)
#define THUMBHASHSHARP_API __declspec(dllimport)
#define THUMBHASHSHARP_CALL __cdecl
#else
#define THUMBHASHSHARP_API __attribute__((visibility("default")))
#define THUMBHASHSHARP_CALL
#endif

#ifdef __cplusplus
extern "C" {
#endif

#define THUMBHASHSHARP_VERSION_MAJOR 2
#define THUMBHASHSHARP_VERSION_MINOR 0
#define THUMBHASHSHARP_VERSION_PATCH 0
#define THUMBHASHSHARP_MAX_HASH_LENGTH 25
#define THUMBHASHSHARP_MAX_DECODED_RGBA_LENGTH (32 * 32 * 4)

typedef enum thumbhashsharp_result {
    THUMBHASHSHARP_SUCCESS = 0,
    THUMBHASHSHARP_NULL_POINTER = 1,
    THUMBHASHSHARP_INVALID_ARGUMENT = 2,
    THUMBHASHSHARP_BUFFER_TOO_SMALL = 3,
    THUMBHASHSHARP_INVALID_RESIZE_MODE = 4,
    THUMBHASHSHARP_INVALID_HASH = 5,
    THUMBHASHSHARP_INTERNAL_ERROR = 255
} thumbhashsharp_result;

typedef enum thumbhashsharp_resize_mode {
    THUMBHASHSHARP_RESIZE_AREA = 0,
    THUMBHASHSHARP_RESIZE_BILINEAR = 1,
    THUMBHASHSHARP_RESIZE_NEAREST_NEIGHBOR = 2
} thumbhashsharp_resize_mode;

/* Returns the packed library version as 0x00MMmmpp (major, minor, patch). */
THUMBHASHSHARP_API uint32_t THUMBHASHSHARP_CALL thumbhashsharp_version(void);

/*
 * Encodes unpremultiplied, row-major RGBA8888 pixels.
 * hash may be NULL when hash_capacity is zero to query the required length.
 */
THUMBHASHSHARP_API int32_t THUMBHASHSHARP_CALL thumbhashsharp_encode(
    int32_t width,
    int32_t height,
    const uint8_t* rgba,
    int32_t rgba_length,
    int32_t resize_mode,
    uint8_t* hash,
    int32_t hash_capacity,
    int32_t* hash_length);

/*
 * Decodes a complete ThumbHash into unpremultiplied RGBA8888 pixels.
 * rgba may be NULL when rgba_capacity is zero to query dimensions and length.
 */
THUMBHASHSHARP_API int32_t THUMBHASHSHARP_CALL thumbhashsharp_decode(
    const uint8_t* hash,
    int32_t hash_length,
    uint8_t* rgba,
    int32_t rgba_capacity,
    int32_t* width,
    int32_t* height,
    int32_t* rgba_length);

/* Returns average unpremultiplied RGBA components in the range 0 to 1. */
THUMBHASHSHARP_API int32_t THUMBHASHSHARP_CALL thumbhashsharp_average_color(
    const uint8_t* hash,
    int32_t hash_length,
    float* red,
    float* green,
    float* blue,
    float* alpha);

/* Returns the approximate source width-to-height ratio. */
THUMBHASHSHARP_API int32_t THUMBHASHSHARP_CALL thumbhashsharp_aspect_ratio(
    const uint8_t* hash,
    int32_t hash_length,
    float* ratio);

#ifdef __cplusplus
}
#endif

#endif
