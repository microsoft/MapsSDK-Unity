// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if defined (ENABLE_ELEVATION_TEXTURE)

sampler2D _ElevationTex;
float4 _ElevationTexScaleAndOffset;

float3 CalculateElevationOffset(sampler2D elevationTex, float2 uv, float scale, float2 offset, float elevationScale)
{
    float2 scaledAndOffsetUv = (uv * scale) + offset;

    // Elevation texture's origin is flipped. Fix it here.
    scaledAndOffsetUv.y = 1.0 - scaledAndOffsetUv.y;

    float elevation = tex2Dlod(elevationTex, float4(scaledAndOffsetUv, 0, 0)).r;
    return elevation * elevationScale;
}

float3 FilterNormal(sampler2D elevationTex, float2 uv, float scale, float2 offset, float elevationScale, float texelSize)
{
    float2 scaledAndOffsetUv = (uv * scale) + offset;

    // Elevation texture's origin is flipped. Fix it here.
    scaledAndOffsetUv.y = 1.0 - scaledAndOffsetUv.y;

    float h0 = tex2Dlod(elevationTex, float4(scaledAndOffsetUv + float2(0, -texelSize), 0, 0)).r;
    float h1 = tex2Dlod(elevationTex, float4(scaledAndOffsetUv + float2(-texelSize, 0), 0, 0)).r;
    float h2 = tex2Dlod(elevationTex, float4(scaledAndOffsetUv + float2(texelSize,  0), 0, 0)).r;
    float h3 = tex2Dlod(elevationTex, float4(scaledAndOffsetUv + float2(0,  texelSize), 0, 0)).r;

    return normalize(float3(h1 - h2, 2, h3 - h0));
}

#endif
