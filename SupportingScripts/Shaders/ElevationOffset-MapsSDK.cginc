// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#ifndef ELEVATION_OFFSET
#define ELEVATION_OFFSET

#if ENABLE_ELEVATION_TEXTURE

sampler2D _ElevationTex;
float4 _ElevationTexScaleAndOffset;
float _ZComponent;
float _MinElevationInMetersForTile;
float _ElevationRangeInMetersForTile;

float UnpackElevation(float2 scaledAndOffsetUv)
{
#if USE_R16_FOR_ELEVATION_TEXTURE
    float normalizedHeight = tex2Dlod(_ElevationTex, float4(scaledAndOffsetUv, 0, 0)).r;
#else
    float2 rgNormalizedHeight = tex2Dlod(_ElevationTex, float4(scaledAndOffsetUv, 0, 0)).rg;
    float normalizedHeight = (rgNormalizedHeight.r + rgNormalizedHeight.g * 256.0f) / 257.0f;
#endif
    return _MinElevationInMetersForTile + _ElevationRangeInMetersForTile * normalizedHeight;
}

float2 CalculateElevationOffset(float2 uv, float scale, float2 offset, float elevationScale)
{
    float2 scaledAndOffsetUv = (uv * scale) + offset;

    float elevation = UnpackElevation(scaledAndOffsetUv);
    return float2(elevation * elevationScale, elevation);
}

float3 FilterNormal(float2 uv, float scale, float2 offset, float elevationScale, float texelSize, float texelScale)
{
    texelSize *= texelScale;

    float2 scaledAndOffsetUv = (uv * scale) + offset;

#if LOW_QUALITY_ELEVATION_TEXTURE_NORMALS

    float center = UnpackElevation(scaledAndOffsetUv);
    float right = UnpackElevation(scaledAndOffsetUv + float2(texelSize, 0));
    float up = UnpackElevation(scaledAndOffsetUv + float2(0, texelSize));
    float averageX = center - right;
    float averageY = center - up;

#elif HIGH_QUALITY_ELEVATION_TEXTURE_NORMALS

    // Sobel filter.

    float upperLeft = UnpackElevation(scaledAndOffsetUv + float2(-texelSize, texelSize));
    float left = UnpackElevation(scaledAndOffsetUv - float2(texelSize, 0));
    float lowerLeft = UnpackElevation(scaledAndOffsetUv - float2(texelSize, texelSize));
    float upperRight = UnpackElevation(scaledAndOffsetUv + float2(texelSize, texelSize));
    float right = UnpackElevation(scaledAndOffsetUv + float2(texelSize, 0));
    float lowerRight = UnpackElevation(scaledAndOffsetUv + float2(texelSize, -texelSize));
    float down = UnpackElevation(scaledAndOffsetUv - float2(0, texelSize));
    float up = UnpackElevation(scaledAndOffsetUv + float2(0, texelSize));
    float averageX = 0.125 * (upperLeft + 2.0 * left + lowerLeft - upperRight - 2.0 * right - lowerRight);
    float averageY = 0.125 * (lowerLeft + 2.0 * down + lowerRight - upperLeft - 2.0 * up - upperRight);

#else // MEDIUM_QUALITY_ELEVATION_TEXTURE_NORMALS

    float left = UnpackElevation(scaledAndOffsetUv - float2(texelSize, 0));
    float right = UnpackElevation(scaledAndOffsetUv + float2(texelSize, 0));
    float down = UnpackElevation(scaledAndOffsetUv - float2(0, texelSize));
    float up = UnpackElevation(scaledAndOffsetUv + float2(0, texelSize));
    float averageX = 0.5 * (left - right);
    float averageY = 0.5 * (down - up);

#endif

    return normalize(float3(averageX / texelScale, _ZComponent, averageY / texelScale));
}

#endif

#endif
