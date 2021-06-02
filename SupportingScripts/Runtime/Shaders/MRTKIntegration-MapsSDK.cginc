// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Includes shader code to support MRTK features like hover light.

#if ENABLE_MRTK_INTEGRATION

#define HOVER_LIGHT_COUNT 2
#define HOVER_LIGHT_DATA_SIZE 2
float4 _HoverLightData[HOVER_LIGHT_COUNT * HOVER_LIGHT_DATA_SIZE];

inline float HoverLight(float4 hoverLight, float inverseRadius, float3 worldPosition)
{
    return (1.0 - saturate(length(hoverLight.xyz - worldPosition) * inverseRadius)) * hoverLight.w;
}

inline half4 ApplyHoverLight(float3 worldPosition, half4 color)
{
    half pointToLight = 0.0;
    half3 fluentLightColor = half3(0.0, 0.0, 0.0);

    [unroll]
    for (int hoverLightIndex = 0; hoverLightIndex < HOVER_LIGHT_COUNT; ++hoverLightIndex)
    {
        int dataIndex = hoverLightIndex * HOVER_LIGHT_DATA_SIZE;
        half hoverValue = HoverLight(_HoverLightData[dataIndex], _HoverLightData[dataIndex + 1].w, worldPosition);
        pointToLight += hoverValue;
        fluentLightColor += lerp(half3(0.0, 0.0, 0.0), _HoverLightData[dataIndex + 1].rgb, hoverValue);
    }

    color.rgb += fluentLightColor * pointToLight;
    return color;
}

#else

inline half4 ApplyHoverLight(float3 worldPosition, half4 color)
{
    return color;
}

#endif
