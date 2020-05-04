// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

Shader "MapsSDK/ClippingDistanceShader"
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

            float3 _CameraPosition;
            float3 _CameraNormal;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 cameraPosition : POSITION1;
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
                o.cameraPosition = UnityObjectToViewPos(v.vertex).xyz;

                return o;
            }
#if SHADER_API_GLES
            // Cannot return float directly in GLES API because float to fixed4 conversion fails.
            fixed4 frag(v2f i) : SV_Target
            {
                // Calculate distance by using camera position/plane and the input worldSpacePosition.
                return -i.cameraPosition.zzzz;
            }
#else
            float frag(v2f i) : SV_Target
            {
                // Calculate distance by using camera position/plane and the input worldSpacePosition.
                return -i.cameraPosition.z;
            }
#endif
            ENDCG
        }
    }
}