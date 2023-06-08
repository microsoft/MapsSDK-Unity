/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Runtime.InteropServices;
using UnityEngine;

public class OVRPassthroughColorLut : System.IDisposable
{
    /// <summary>
    /// System limit on the maximum allowed LUT resolution.
    /// </summary>
    public static int MaxResolution { get; } = 64;

    public uint Resolution { get; private set; }
    public ColorChannels Channels { get; private set; }
    public bool IsInitialized { get; private set; }

    internal ulong _colorLutHandle;
    private GCHandle _allocHandle;
    private OVRPlugin.PassthroughColorLutData _lutData;
    private int _channelCount;
    private byte[] _colorBytes;
    private object _locker = new object();

    /// <summary>
    /// Initialize the color LUT data from a texture. If the texture is opaque, `channels`
    /// can be set to `ColorChannels.Rgb`, otherwise `ColorChannels.Rgba` should be used.
    /// Use `UpdateFrom()` to update LUT data after construction.
    /// </summary>
    /// <param name="initialLutTexture">Texture to initialize the LUT from</param>
    /// <param name="channels">Color channels for one color LUT entry</param>
    /// <param name="flipY">Flag to inform whether the LUT texture should be flipped vertically. This is needed for LUT images which have color (0, 0, 0)
    /// in the top-left corner. Some color grading systems, e.g. Unity post-processing, have color (0, 0, 0) in the bottom-left corner,
    /// in which case flipping is not needed.</param>
    public OVRPassthroughColorLut(Texture2D initialLutTexture, ColorChannels channels = ColorChannels.Rgba,
        bool flipY = true)
        : this(initialLutTexture.width * initialLutTexture.height, channels)
    {
        Create(CreateLutDataFromTexture(initialLutTexture, flipY));
    }

    /// <summary>
    /// Set the color LUT data from an array of `Color`. The resolution is
    /// inferred from the array size, thus the size needs to be a result of
    /// `resolution = size ^ 3 * numColorChannels`, where `numColorChannels` depends
    /// on `channels`.
    /// Use `UpdateFrom()` to update color LUT data after construction.
    /// </summary>
    /// <param name="initialColorLut">Color array to initialize the LUT from</param>
    /// <param name="channels">Color channels for one color LUT entry</param>
    public OVRPassthroughColorLut(Color[] initialColorLut, ColorChannels channels)
        : this(initialColorLut.Length, channels)
    {
        Create(CreateLutDataFromArray(initialColorLut));
    }

    /// <summary>
    /// Update color LUT data from an array of Colors.
    /// Color channels and resolution must match the original.
    /// </summary>
    /// <param name="colors">Color array</param>
    public void UpdateFrom(Color[] colors)
    {
        if (!IsInitialized)
        {
            Debug.LogError("Can not update an uninitialized lut object.");
            return;
        }

        var resolution = GetResolutionFromSize(colors.Length);

        if (resolution != Resolution)
        {
            Debug.LogError($"Can only update with the same resolution of {Resolution}.");
            return;
        }

        WriteColorsAsBytes(colors, _colorBytes);
        OVRPlugin.UpdatePassthroughColorLut(_colorLutHandle, _lutData);
    }

    /// <summary>
    /// Update color LUT data from a texture.
    /// Color channels and resolution must match the original.
    /// </summary>
    /// <param name="lutTexture">Color LUT texture</param>
    /// <param name="flipY">Flag to inform whether the LUT texture should be flipped vertically. This is needed for LUT images which have color (0, 0, 0)
    /// in the top-left corner. Some color grading systems, e.g. Unity post-processing, have color (0, 0, 0) in the bottom-left corner,
    /// in which case flipping is not needed.</param>
    public void UpdateFrom(Texture2D lutTexture, bool flipY = true)
    {
        if (!IsInitialized)
        {
            Debug.LogError("Can not update an uninitialized lut object.");
            return;
        }

        var resolution = GetResolutionFromSize(lutTexture.width * lutTexture.height);

        if (resolution != Resolution)
        {
            Debug.LogError($"Can only update with the same resolution of {Resolution}.");
            return;
        }

        ColorLutTextureConverter.TextureToColorByteMap(lutTexture, _channelCount, _colorBytes, flipY);
        OVRPlugin.UpdatePassthroughColorLut(_colorLutHandle, _lutData);
    }

    public void Dispose()
    {
        if (IsInitialized)
        {
            Destroy();
        }

        if (_allocHandle != null && _allocHandle.IsAllocated)
        {
            _allocHandle.Free();
        }
    }

    /// <summary>
    /// Check if texture is in acceptable LUT format
    /// </summary>
    /// <param name="texture">Texture to check</param>
    /// <param name="errorMessage">Error message describing acceptance fail reason</param>
    /// <returns></returns>
    public static bool IsTextureSupported(Texture2D texture, out string errorMessage)
    {
        if (!ColorLutTextureConverter.TryGetTextureLayout(texture.width, texture.height, out _, out _,
                out var layoutMessage))
        {
            errorMessage = layoutMessage;
            return false;
        }

        var size = texture.width * texture.height;
        if (!IsResolutionAccepted(GetResolutionFromSize(size), size, out var resolutionMessage))
        {
            errorMessage = resolutionMessage;
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private OVRPassthroughColorLut(int size, ColorChannels channels)
    {
        Channels = channels;
        Resolution = GetResolutionFromSize(size);
        _channelCount = channels == ColorChannels.Rgb ? 3 : 4;

        if (!IsResolutionAccepted(Resolution, size, out var message))
        {
            throw new System.Exception(message);
        }
    }

    private static bool IsResolutionAccepted(uint resolution, int size, out string errorMessage)
    {
        if (resolution > MaxResolution)
        {
            errorMessage = $"Color LUT texture resolution exceeds {MaxResolution} maximum.";
            return false;
        }

        if (!IsPowerOfTwo(resolution))
        {
            errorMessage = "Color LUT texture resolution should be a power of 2.";
            return false;
        }

        if (resolution * resolution * resolution != size)
        {
            errorMessage = "Unexpected LUT resolution, LUT size should be resolution in a power of 3.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    private static bool IsPowerOfTwo(uint x)
    {
        return (x != 0) && ((x & (x - 1)) == 0);
    }

    private void Create(OVRPlugin.PassthroughColorLutData lutData)
    {
        _lutData = lutData;
        IsInitialized = OVRPlugin.CreatePassthroughColorLut((OVRPlugin.PassthroughColorLutChannels)Channels,
            Resolution, _lutData, out _colorLutHandle);
        if (!IsInitialized)
        {
            Debug.LogError("Failed to create Passthrough Color LUT.");
        }
    }

    private static uint GetResolutionFromSize(int size)
    {
        return (uint)Mathf.Round(Mathf.Pow(size, 1f / 3f));
    }

    private OVRPlugin.PassthroughColorLutData CreateLutData(out byte[] colorBytes)
    {
        OVRPlugin.PassthroughColorLutData lutData = default;
        lutData.BufferSize = (uint)(Resolution * Resolution * Resolution * _channelCount);
        colorBytes = new byte[lutData.BufferSize];
        _allocHandle = GCHandle.Alloc(colorBytes, GCHandleType.Pinned);
        lutData.Buffer = _allocHandle.AddrOfPinnedObject();
        return lutData;
    }

    private OVRPlugin.PassthroughColorLutData CreateLutDataFromTexture(Texture2D lut, bool flipY)
    {
        var lutData = CreateLutData(out _colorBytes);
        ColorLutTextureConverter.TextureToColorByteMap(lut, _channelCount, _colorBytes, flipY);
        return lutData;
    }

    private OVRPlugin.PassthroughColorLutData CreateLutDataFromArray(Color[] colors)
    {
        var lutData = CreateLutData(out _colorBytes);
        WriteColorsAsBytes(colors, _colorBytes);
        return lutData;
    }

    private void WriteColorsAsBytes(Color[] colors, byte[] target)
    {
        for (int i = 0; i < colors.Length; i++)
        {
            ColorLutTextureConverter.WriteColorAsBytes(_channelCount, colors[i], i * _channelCount, target);
        }
    }

    ~OVRPassthroughColorLut()
    {
        Dispose();
    }

    private void Destroy()
    {
        if (IsInitialized)
        {
            lock (_locker)
            {
                OVRPlugin.DestroyPassthroughColorLut(_colorLutHandle);
                IsInitialized = false;
            }
        }
    }

    public enum ColorChannels
    {
        Rgb = OVRPlugin.PassthroughColorLutChannels.Rgb,
        Rgba = OVRPlugin.PassthroughColorLutChannels.Rgba
    }

    private static class ColorLutTextureConverter
    {
        /// <summary>
        /// Read colors from LUT texture and write them in the pre-allocated target byte array
        /// </summary>
        /// <param name="lut">Color LUT texture</param>
        /// <param name="channelCount">{RGB = 3, RGBA = 4}</param>
        /// <param name="target">Pre-allocated byte array that should fit all colors of the texture</param>
        /// <param name="flipY">Flag to inform whether the LUT texture should be flipped vertically</param>
        public static void TextureToColorByteMap(Texture2D lut, int channelCount, byte[] target, bool flipY)
        {
            MapColorValues(GetTextureSettings(lut, channelCount, flipY), lut.GetPixels(0), target);
        }

        private static void MapColorValues(TextureSettings settings, Color[] colors, byte[] target)
        {
            for (int bi = 0; bi < settings.Resolution; bi++)
            {
                int bi_row = bi % settings.SlicesPerRow;
                int bi_col = (int)Mathf.Floor(bi / settings.SlicesPerRow);
                for (int gi = 0; gi < settings.Resolution; gi++)
                {
                    for (int ri = 0; ri < settings.Resolution; ri++)
                    {
                        int sX = ri + bi_row * settings.Resolution;
                        int sY = gi + bi_col * settings.Resolution;
                        int y = settings.FlipY ? settings.Height - sY - 1 : sY;
                        int sourceIndex = sX + y * settings.Width;
                        int targetIndex = bi * settings.Resolution * settings.Resolution +
                                          gi * settings.Resolution + ri;
                        WriteColorAsBytes(settings.ChannelCount, colors[sourceIndex],
                            targetIndex * settings.ChannelCount, target);
                    }
                }
            }
        }

        private static TextureSettings GetTextureSettings(Texture2D lut, int channelCount, bool flipY)
        {
            int resolution, slicesPerRow;
            if (TryGetTextureLayout(lut.width, lut.height, out resolution, out slicesPerRow, out var message))
            {
                return new TextureSettings(lut.width, lut.height, resolution, slicesPerRow, channelCount, flipY);
            }
            else
            {
                throw new System.Exception(message);
            }
        }

        public static void WriteColorAsBytes(int channels, Color color, int index, byte[] target)
        {
            for (int c = 0; c < channels; c++)
            {
                target[index + c] = (byte)Mathf.Min(color[c] * 255.0f, 255.0f);
            }
        }

        // Supports 2 formats:
        // - Square, where the z (blue) planes are arranged in a square (like this https://cdn.streamshark.io/obs-guide/img/neutral-lut.png)
        //   For that, assuming that x is the edge size of the LUT, width and height must be x * sqrt(x) (-> doesn't work for all edge sizes)
        // - Horizontal, where the z (blue) planes are arranged horizontally (like this http://www.thomashourdel.com/lutify/img/tut03.jpg)
        internal static bool TryGetTextureLayout(int width, int height, out int resolution, out int slicesPerRow,
            out string errorMessage)
        {
            resolution = -1;
            slicesPerRow = -1;

            if (width == height)
            {
                float edgeLengthF = Mathf.Pow(width, 2.0f / 3.0f);
                if (Mathf.Abs(edgeLengthF - Mathf.Round(edgeLengthF)) > 0.001)
                {
                    errorMessage = "Texture layout is not compatible for color LUTs: " +
                                   "the dimensions don't result in a power-of-two resolution for the LUT. " +
                                   "Acceptable image sizes are e.g. 64 (for a LUT resolution of 16) or 512 (for a LUT resolution of 64).";
                    return false;
                }

                resolution = (int)Mathf.Round(edgeLengthF);
                slicesPerRow = (int)Mathf.Sqrt(resolution);
                Debug.Assert(width == resolution * slicesPerRow);
            }
            else
            {
                if (width != height * height)
                {
                    errorMessage = "Texture layout is not compatible for color LUTs: for horizontal layouts, " +
                                   "the Width is expected to be equal to Height * Height.";
                    return false;
                }

                resolution = height;
                slicesPerRow = resolution;
            }

            errorMessage = string.Empty;
            return true;
        }

        private struct TextureSettings
        {
            public int Width { get; }
            public int Height { get; }
            public int Resolution { get; }
            public int SlicesPerRow { get; }
            public int ChannelCount { get; }
            public bool FlipY { get; }

            public TextureSettings(int width, int height, int resolution, int slicesPerRow, int channelCount,
                bool flipY)
            {
                Width = width;
                Height = height;
                Resolution = resolution;
                SlicesPerRow = slicesPerRow;
                ChannelCount = channelCount;
                FlipY = flipY;
            }
        }
    }
}
