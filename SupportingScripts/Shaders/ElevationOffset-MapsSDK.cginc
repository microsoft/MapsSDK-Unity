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

    // Elevation texture's origin is flipped. Fix it here.
    scaledAndOffsetUv.y = 1.0 - scaledAndOffsetUv.y;

    float elevation = UnpackElevation(scaledAndOffsetUv);
    return float2(elevation * elevationScale, elevation);
}

float3 FilterNormal(float2 uv, float scale, float2 offset, float elevationScale, float texelSize)
{
    // Elevation texture's origin is flipped. Fix it here.
    float2 scaledAndOffsetUv = (uv * scale) + offset;
    scaledAndOffsetUv.y = 1.0 - scaledAndOffsetUv.y;

    float minusX = UnpackElevation(scaledAndOffsetUv + float2(-texelSize, 0));
    float plusX = UnpackElevation(scaledAndOffsetUv + float2(texelSize, 0));
    float minusY = UnpackElevation(scaledAndOffsetUv + float2(0, -texelSize));
    float plusY = UnpackElevation(scaledAndOffsetUv + float2(0, texelSize));

    float averageX = 0.5 * (plusX - minusX);
    float averageY = 0.5 * (plusY - minusY);

    return normalize(float3(-averageX, _ZComponent, averageY));
}
#endif

#endif
