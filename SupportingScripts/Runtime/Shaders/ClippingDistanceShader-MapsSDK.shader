// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

Shader "Maps SDK/Clipping Distance"
{
    SubShader
    {
        Pass
        {
            Tags{ "RenderType" = "Opaque" }
            Cull front

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            // These are the maps specific keywords...
            #pragma multi_compile __ ENABLE_ELEVATION_TEXTURE
            #pragma multi_compile __ USE_R16_FOR_ELEVATION_TEXTURE

            #include "UnityCG.cginc"
            #include "ClippingVolume-MapsSDK.cginc"
            #include "ElevationOffset-MapsSDK.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
#if ENABLE_ELEVATION_TEXTURE
                float elevationOffset =
                    CalculateElevationOffset(
                        v.uv,
                        _ElevationTexScaleAndOffset.x,
                        _ElevationTexScaleAndOffset.yz,
                        _ElevationTexScaleAndOffset.w).x;
                v.vertex.y += elevationOffset;
#endif
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);

                o.pos = UnityObjectToClipPos(v.vertex);

                return o;
            }

            // Writes the linear, normalized depth of the pixel to the render texture.
#if SHADER_API_GLES || SHADER_API_METAL
            // Cannot return float directly in GLES API because float to float4 conversion fails.
            float4 frag(v2f i) : SV_Target
            {
#if UNITY_REVERSED_Z
                return float4(1, 1, 1, 1) - i.pos.zzzz;
#else
                return i.pos.zzzz;
#endif
            }
#else
            float frag(v2f i) : SV_Target
            {
#if UNITY_REVERSED_Z
                return 1 - i.pos.z;
#else
                return i.pos.z;
#endif
            }
#endif
            ENDCG
        }
    }
}
