using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ThumbHash
{
    public static class ThumbHashHelper
    {
        /// <summary>
        /// Encodes an RGBA image to a ThumbHash. RGB should not be premultiplied by A.
        /// </summary>
        /// <summary>
        /// Encodes an RGBA image to a ThumbHash. RGB should not be premultiplied by A.
        /// </summary>
        public static string RgbaToThumbHashBase64(int width, int height, ReadOnlySpan<byte> rgba)
            => Convert.ToBase64String(RgbaToThumbHash(width, height, rgba));

        public static byte[] RgbaToThumbHash(int width, int height, ReadOnlySpan<byte> rgba)
        {
            int pixelCount = width * height;

            // Determine the average color
            double avgR = 0, avgG = 0, avgB = 0, avgA = 0;

            // Use a for loop for average color calculation
            for (int i = 0; i < pixelCount; i++)
            {
                int j = i * 4;
                double alpha = rgba[j + 3] / 255.0;
                avgR += alpha * (rgba[j] / 255.0);
                avgG += alpha * (rgba[j + 1] / 255.0);
                avgB += alpha * (rgba[j + 2] / 255.0);
                avgA += alpha;
            }

            if (avgA > 0)
            {
                avgR /= avgA;
                avgG /= avgA;
                avgB /= avgA;
            }

            bool hasAlpha = avgA < pixelCount;
            int lLimit = hasAlpha ? 5 : 7;
            int lx = Math.Max(1, (int)Math.Round((double)(lLimit * width) / Math.Max(width, height)));
            int ly = Math.Max(1, (int)Math.Round((double)(lLimit * height) / Math.Max(width, height)));

            float[] lChannelData = new float[pixelCount];
            float[] pChannelData = new float[pixelCount];
            float[] qChannelData = new float[pixelCount];
            float[] aChannelData = new float[pixelCount];

            // 将 ReadOnlySpan<byte> 转换为数组
            byte[] rgbaArray = rgba.ToArray();

            // Process the image data in parallel
            Parallel.For(0, pixelCount, i =>
            {
                int j = i * 4;
                float alpha = rgbaArray[j + 3] / 255.0f;
                float r = (float)(avgR * (1.0f - alpha) + alpha * (rgbaArray[j] / 255.0f));
                float g = (float)(avgG * (1.0f - alpha) + alpha * (rgbaArray[j + 1] / 255.0f));
                float b = (float)(avgB * (1.0f - alpha) + alpha * (rgbaArray[j + 2] / 255.0f));

                lChannelData[i] = (r + g + b) / 3.0f;
                pChannelData[i] = (r + g) / 2.0f - b;
                qChannelData[i] = r - g;
                aChannelData[i] = alpha;
            });

            // Encode using the DCT into DC and normalized AC terms
            Channel lChannel = new Channel(Math.Max(3, lx), Math.Max(3, ly));
            lChannel.Encode(width, height, lChannelData);

            Channel pChannel = new Channel(3, 3);
            pChannel.Encode(width, height, pChannelData);

            Channel qChannel = new Channel(3, 3);
            qChannel.Encode(width, height, qChannelData);

            Channel aChannel = null;
            if (hasAlpha)
            {
                aChannel = new Channel(5, 5);
                aChannel.Encode(width, height, aChannelData);
            }

            // Write the constants
            bool isLandscape = width > height;
            int header24 = (int)Math.Round(63.0f * lChannel.Dc)
                            | (int)Math.Round(31.5f + 31.5f * pChannel.Dc) << 6
                            | (int)Math.Round(31.5f + 31.5f * qChannel.Dc) << 12
                            | (int)Math.Round(31.0f * lChannel.Scale) << 18
                            | (hasAlpha ? 1 << 23 : 0);

            int header16 = (isLandscape ? ly : lx)
                            | (int)Math.Round(63.0f * pChannel.Scale) << 3
                            | (int)Math.Round(63.0f * qChannel.Scale) << 9
                            | (isLandscape ? 1 << 15 : 0);

            int acStart = hasAlpha ? 6 : 5;
            int acCount = lChannel.Ac.Length + pChannel.Ac.Length + qChannel.Ac.Length
                        + (hasAlpha ? aChannel.Ac.Length : 0);

            byte[] hash = new byte[acStart + (acCount + 1) / 2];
            hash[0] = (byte)(header24 & 0xFF);
            hash[1] = (byte)(header24 >> 8 & 0xFF);
            hash[2] = (byte)(header24 >> 16 & 0xFF);
            hash[3] = (byte)(header16 & 0xFF);
            hash[4] = (byte)(header16 >> 8 & 0xFF);

            if (hasAlpha)
            {
                hash[5] = (byte)((int)Math.Round(15.0f * aChannel.Dc) | (int)Math.Round(15.0f * aChannel.Scale) << 4);
            }

            // Write the varying factors
            int acIndex = 0;
            acIndex = lChannel.WriteTo(hash, acStart, acIndex);
            acIndex = pChannel.WriteTo(hash, acStart, acIndex);
            acIndex = qChannel.WriteTo(hash, acStart, acIndex);
            if (hasAlpha)
            {
                aChannel.WriteTo(hash, acStart, acIndex);
            }

            return hash;
        }

        public static Image ThumbHashToRgba(string base64) => ThumbHashToRgba(Convert.FromBase64String(base64));

        /// <summary>
        /// Decodes a ThumbHash to an RGBA image. RGB is not premultiplied by A.
        /// </summary>
        public static Image ThumbHashToRgba(ReadOnlySpan<byte> hash)
        {
            if (hash.Length < 5)
                throw new ArgumentException("Invalid ThumbHash length.");

            // Read the constants
            int header24 = hash[0] & 0xFF | (hash[1] & 0xFF) << 8 | (hash[2] & 0xFF) << 16;
            int header16 = hash[3] & 0xFF | (hash[4] & 0xFF) << 8;

            float lDc = (header24 & 63) / 63.0f;
            float pDc = (header24 >> 6 & 63) / 31.5f - 1.0f;
            float qDc = (header24 >> 12 & 63) / 31.5f - 1.0f;
            float lScale = (header24 >> 18 & 31) / 31.0f;
            bool hasAlpha = (header24 >> 23 & 1) != 0;

            float pScale = (header16 >> 3 & 63) / 63.0f;
            float qScale = (header16 >> 9 & 63) / 63.0f;
            bool isLandscape = (header16 >> 15 & 1) != 0;

            int lx = Math.Max(3, isLandscape ? hasAlpha ? 5 : 7 : header16 & 7);
            int ly = Math.Max(3, isLandscape ? header16 & 7 : hasAlpha ? 5 : 7);

            float aDc = hasAlpha ? (hash[5] & 15) / 15.0f : 1.0f;
            float aScale = hasAlpha ? (hash[5] >> 4 & 15) / 15.0f : 1.0f;

            // Read the varying factors
            int acStart = hasAlpha ? 6 : 5;
            int acIndex = 0;

            Channel lChannel = new Channel(lx, ly);
            Channel pChannel = new Channel(3, 3);
            Channel qChannel = new Channel(3, 3);
            Channel aChannel = hasAlpha ? new Channel(5, 5) : null;

            acIndex = lChannel.Decode(hash, acStart, acIndex, lScale);
            acIndex = pChannel.Decode(hash, acStart, acIndex, pScale * 1.25f);
            acIndex = qChannel.Decode(hash, acStart, acIndex, qScale * 1.25f);
            if (hasAlpha)
            {
                aChannel.Decode(hash, acStart, acIndex, aScale);
            }

            // Decode using the DCT into RGB
            float ratio = ThumbHashToApproximateAspectRatio(hash);
            int w = (int)Math.Round(ratio > 1.0f ? 32.0f : 32.0f * ratio);
            int h = (int)Math.Round(ratio > 1.0f ? 32.0f / ratio : 32.0f);

            byte[] rgba = new byte[w * h * 4];

            // Precompute cosine values
            float[][] fxCosTable = new float[w][];
            for (int x = 0; x < w; x++)
            {
                fxCosTable[x] = new float[Math.Max(lx, hasAlpha ? 5 : 3)];
                for (int cx = 0; cx < fxCosTable[x].Length; cx++)
                {
                    fxCosTable[x][cx] = (float)Math.Cos(Math.PI / w * (x + 0.5f) * cx);
                }
            }

            float[][] fyCosTable = new float[h][];
            for (int y = 0; y < h; y++)
            {
                fyCosTable[y] = new float[Math.Max(ly, hasAlpha ? 5 : 3)];
                for (int cy = 0; cy < fyCosTable[y].Length; cy++)
                {
                    fyCosTable[y][cy] = (float)Math.Cos(Math.PI / h * (y + 0.5f) * cy);
                }
            }

            // Process the image data in parallel
            Parallel.For(0, h, y =>
            {
                float[] fy = fyCosTable[y];
                for (int x = 0; x < w; x++)
                {
                    int i = (y * w + x) * 4;
                    float l = lDc, p = pDc, q = qDc, a = aDc;

                    float[] fx = fxCosTable[x];

                    // Decode L
                    int j = 0;
                    for (int cy = 0; cy < ly; cy++)
                    {
                        float fy2 = fy[cy] * 2.0f;
                        for (int cx = cy > 0 ? 0 : 1; cx * ly < lx * (ly - cy); cx++, j++)
                        {
                            if (j < lChannel.Ac.Length)
                            {
                                l += lChannel.Ac[j] * fx[cx] * fy2;
                            }
                        }
                    }

                    // Decode P and Q
                    j = 0;
                    for (int cy = 0; cy < 3; cy++)
                    {
                        float fy2 = fy[cy] * 2.0f;
                        for (int cx = cy > 0 ? 0 : 1; cx < 3 - cy; cx++, j++)
                        {
                            if (j < pChannel.Ac.Length)
                            {
                                float f = fx[cx] * fy2;
                                p += pChannel.Ac[j] * f;
                                q += qChannel.Ac[j] * f;
                            }
                        }
                    }

                    // Decode A
                    if (hasAlpha)
                    {
                        j = 0;
                        for (int cy = 0; cy < 5; cy++)
                        {
                            float fy2 = fy[cy] * 2.0f;
                            for (int cx = cy > 0 ? 0 : 1; cx < 5 - cy; cx++, j++)
                            {
                                if (j < aChannel.Ac.Length)
                                {
                                    a += aChannel.Ac[j] * fx[cx] * fy2;
                                }
                            }
                        }
                    }

                    // Convert to RGB
                    float b = l - 2.0f / 3.0f * p;
                    float r = (3.0f * l - b + q) / 2.0f;
                    float g = r - q;

                    // Clamp values between 0 and 1
                    r = Clamp(r, 0, 1);
                    g = Clamp(g, 0, 1);
                    b = Clamp(b, 0, 1);
                    a = Clamp(a, 0, 1);

                    rgba[i] = (byte)(r * 255.0f);
                    rgba[i + 1] = (byte)(g * 255.0f);
                    rgba[i + 2] = (byte)(b * 255.0f);
                    rgba[i + 3] = (byte)(a * 255.0f);
                }
            });

            return new Image(w, h, rgba);
        }

        public static RGBA ThumbHashToAverageRgba(string base64) => ThumbHashToAverageRgba(Convert.FromBase64String(base64));

        /// <summary>
        /// Extracts the average color from a ThumbHash. RGB is not premultiplied by A.
        /// </summary>
        public static RGBA ThumbHashToAverageRgba(ReadOnlySpan<byte> hash)
        {
            if (hash.Length < 6)
                throw new ArgumentException("Invalid ThumbHash length.");

            int header = hash[0] & 0xFF | (hash[1] & 0xFF) << 8 | (hash[2] & 0xFF) << 16;
            float l = (header & 63) / 63.0f;
            float p = (header >> 6 & 63) / 31.5f - 1.0f;
            float q = (header >> 12 & 63) / 31.5f - 1.0f;
            bool hasAlpha = (header >> 23 & 1) != 0;
            float a = hasAlpha ? (hash[5] & 15) / 15.0f : 1.0f;

            float b = l - 2.0f / 3.0f * p;
            float r = (3.0f * l - b + q) / 2.0f;
            float g = r - q;

            return new RGBA(
                Clamp(r, 0, 1),
                Clamp(g, 0, 1),
                Clamp(b, 0, 1),
                a);
        }

        public static float ThumbHashToApproximateAspectRatio(string base64) => ThumbHashToApproximateAspectRatio(Convert.FromBase64String(base64));

        /// <summary>
        /// Extracts the approximate aspect ratio of the original image.
        /// </summary>
        public static float ThumbHashToApproximateAspectRatio(ReadOnlySpan<byte> hash)
        {
            if (hash.Length < 5)
                throw new ArgumentException("Invalid ThumbHash length.");

            int header16 = hash[3] & 0xFF | (hash[4] & 0xFF) << 8;
            bool hasAlpha = (hash[2] & 0x80) != 0;
            bool isLandscape = (header16 >> 15 & 1) != 0;
            int lx = isLandscape ? hasAlpha ? 5 : 7 : header16 & 7;
            int ly = isLandscape ? header16 & 7 : hasAlpha ? 5 : 7;
            return lx / (float)ly;
        }

        /// <summary>
        /// Clamps a value between a minimum and maximum value.
        /// </summary>
        private static float Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        /// <summary>
        /// Represents an RGBA color with components ranging from 0 to 1.
        /// </summary>
        public class RGBA
        {
            public float R { get; }
            public float G { get; }
            public float B { get; }
            public float A { get; }

            public RGBA(float r, float g, float b, float a)
            {
                R = r;
                G = g;
                B = b;
                A = a;
            }
        }

        /// <summary>
        /// Represents an image with width, height, and RGBA pixel data.
        /// </summary>
        public class Image
        {
            public int Width { get; }
            public int Height { get; }
            public byte[] Rgba { get; }

            public Image(int width, int height, byte[] rgba)
            {
                Width = width;
                Height = height;
                Rgba = rgba;
            }
        }

        /// <summary>
        /// Helper class to handle encoding and decoding of individual color channels.
        /// </summary>
        private class Channel
        {
            public int Nx { get; }
            public int Ny { get; }
            public float Dc { get; set; }
            public float[] Ac { get; }
            public float Scale { get; set; }

            public Channel(int nx, int ny)
            {
                Nx = nx;
                Ny = ny;
                List<float> acList = new List<float>();
                for (int cy = 0; cy < Ny; cy++)
                {
                    for (int cx = cy > 0 ? 0 : 1; cx * Ny < Nx * (Ny - cy); cx++)
                    {
                        acList.Add(0.0f);
                    }
                }
                Ac = acList.ToArray();
            }

            /// <summary>
            /// Encodes the channel using the Discrete Cosine Transform (DCT).
            /// </summary>
            public void Encode(int w, int h, float[] channelData)
            {
                int n = 0;
                float[] fx = new float[w];

                for (int cy = 0; cy < Ny; cy++)
                {
                    for (int cx = 0; cx * Ny < Nx * (Ny - cy); cx++)
                    {
                        float f = 0.0f;

                        // Precompute fx for current cx
                        for (int x = 0; x < w; x++)
                            fx[x] = (float)Math.Cos(Math.PI / w * cx * (x + 0.5f));

                        for (int y = 0; y < h; y++)
                        {
                            float fy = (float)Math.Cos(Math.PI / h * cy * (y + 0.5f));
                            for (int x = 0; x < w; x++)
                                f += channelData[x + y * w] * fx[x] * fy;
                        }
                        f /= w * h;

                        if (cx > 0 || cy > 0)
                        {
                            Ac[n++] = f;
                            Scale = Math.Max(Scale, Math.Abs(f));
                        }
                        else
                        {
                            Dc = f;
                        }
                    }
                }

                if (Scale > 0)
                {
                    for (int i = 0; i < Ac.Length; i++)
                        Ac[i] = 0.5f + 0.5f / Scale * Ac[i];
                }
            }

            /// <summary>
            /// Decodes the channel from the ThumbHash byte array.
            /// </summary>
            public int Decode(ReadOnlySpan<byte> hash, int start, int index, float scale)
            {
                for (int i = 0; i < Ac.Length; i++)
                {
                    if (start + index / 2 >= hash.Length)
                        throw new ArgumentException("Invalid ThumbHash data.");

                    int data = hash[start + index / 2] >> (index & 1) * 4 & 0x0F;
                    Ac[i] = (data / 7.5f - 1.0f) * scale;
                    index++;
                }
                return index;
            }

            /// <summary>
            /// Writes the varying factors to the ThumbHash byte array.
            /// </summary>
            public int WriteTo(byte[] hash, int start, int index)
            {
                for (int i = 0; i < Ac.Length; i++)
                {
                    if (start + index / 2 >= hash.Length)
                        throw new ArgumentException("ThumbHash array is too short.");

                    int quantized = (int)Math.Round(Ac[i] * 15.0f);
                    quantized = Math.Clamp(quantized, 0, 15);

                    if ((index & 1) == 0)
                    {
                        hash[start + index / 2] = (byte)quantized;
                    }
                    else
                    {
                        hash[start + index / 2] |= (byte)(quantized << 4);
                    }

                    index++;
                }
                return index;
            }
        }
    }
}