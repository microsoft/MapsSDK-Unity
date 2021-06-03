// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

Shader "Maps SDK/Universal Render Pipeline/Unlit Terrain"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            // These are the maps specific keywords...
            #pragma multi_compile __ ENABLE_ELEVATION_TEXTURE
            #pragma multi_compile __ USE_R16_FOR_ELEVATION_TEXTURE
            #pragma multi_compile __ ENABLE_CONTOUR_LINES
            #pragma multi_compile __ ENABLE_MRTK_INTEGRATION
            #pragma multi_compile __ ENABLE_CIRCULAR_CLIPPING

            // Support the various Unity keywords...
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.microsoft.maps.unity/Runtime/Shaders/ClippingVolume-MapsSDK.cginc"
            #include "Packages/com.microsoft.maps.unity/Runtime/Shaders/ContourLines-MapsSDK.cginc"
            #include "Packages/com.microsoft.maps.unity/Runtime/Shaders/ElevationOffset-MapsSDK.cginc"
            #include "Packages/com.microsoft.maps.unity/Runtime/Shaders/MRTKIntegration-MapsSDK.cginc"

            struct Attributes
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 pos  : SV_POSITION;
                float3 worldPosition : POSITION1;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float2 uv3 : TEXCOORD2;
                float2 uv4 : TEXCOORD3;
                float isSkirt : TEXCOORD4;
#if ENABLE_ELEVATION_TEXTURE && ENABLE_CONTOUR_LINES
                float elevation : TEXCOORD5;
#endif
                float fogFactor : TEXCOORD6;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex0);
            SAMPLER(sampler_MainTex0);
            TEXTURE2D(_MainTex1);
            SAMPLER(sampler_MainTex1);
            TEXTURE2D(_MainTex2);
            SAMPLER(sampler_MainTex2);
            TEXTURE2D(_MainTex3);
            SAMPLER(sampler_MainTex3);

            float _MainTexCount;
            float4 _TexScaleAndOffset[4];

            Varyings vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                Varyings o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Extract skirt indicator from the third item in uv
                o.isSkirt = v.uv.z;

#if ENABLE_ELEVATION_TEXTURE
                float2 elevationOffset =
                    CalculateElevationOffset(
                        v.uv.xy,
                        _ElevationTexScaleAndOffset.x,
                        _ElevationTexScaleAndOffset.yz,
                        _ElevationTexScaleAndOffset.w);
                v.vertex.y += elevationOffset.x;
#if ENABLE_CONTOUR_LINES
                o.elevation = elevationOffset.y;
#endif
#endif

                o.pos = TransformObjectToHClip(v.vertex.xyz);
                if (_MainTexCount > 0) { o.uv = (_TexScaleAndOffset[0].x * v.uv.xy + _TexScaleAndOffset[0].yz); }
                if (_MainTexCount > 1) { o.uv2 = _TexScaleAndOffset[1].x * v.uv.xy + _TexScaleAndOffset[1].yz; }
                if (_MainTexCount > 2) { o.uv3 = _TexScaleAndOffset[2].x * v.uv.xy + _TexScaleAndOffset[2].yz; }
                if (_MainTexCount > 3) { o.uv4 = _TexScaleAndOffset[3].x * v.uv.xy + _TexScaleAndOffset[3].yz; }

                float3 worldPosition = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
                o.worldPosition = worldPosition;

                o.fogFactor = ComputeFogFactor(o.pos.z);

                return o;
            }

            half4 blend(half4 dst, half4 src)
            {
                return half4(src.rgb * src.a + dst.rgb * (1.0 - src.a), 1);
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Clip to ClippingVolume
                float minDistanceToPlane = ClipToVolume(i.worldPosition, i.isSkirt);

                // Albedo comes from a texture tinted by color
                half4 color = SAMPLE_TEXTURE2D(_MainTex0, sampler_MainTex0, i.uv);
                if (_MainTexCount > 1) { color = blend(color, SAMPLE_TEXTURE2D(_MainTex1, sampler_MainTex1, i.uv2)); }
                if (_MainTexCount > 2) { color = blend(color, SAMPLE_TEXTURE2D(_MainTex2, sampler_MainTex2, i.uv3)); }
                if (_MainTexCount > 3) { color = blend(color, SAMPLE_TEXTURE2D(_MainTex3, sampler_MainTex3, i.uv4)); }

                // Apply contours.
#if ENABLE_ELEVATION_TEXTURE && ENABLE_CONTOUR_LINES
                color = ApplyContourLines(color, i.elevation);
#endif

                float lerpAmount = saturate(1.0 + minDistanceToPlane / _ClippingVolumeFadeDistance);
                color = lerp(color, _ClippingVolumeColor, lerpAmount);

                // Apply shadow.
#ifdef _MAIN_LIGHT_SHADOWS
                VertexPositionInputs vertexInput = (VertexPositionInputs)0;
                vertexInput.positionWS = i.worldPosition;
                float4 shadowCoordinate = GetShadowCoord(vertexInput);
                half shadow = MainLightRealtimeShadow(shadowCoordinate);
                color.rgb *= saturate(shadow + 0.33);
#endif

                // MRTK hover light.
                color = ApplyHoverLight(i.worldPosition.xyz, color);

                color.rgb = MixFog(color.rgb, i.fogFactor);

                return half4(color.r, color.g, color.b, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            // These are the maps specific keywords...
            #pragma multi_compile __ ENABLE_ELEVATION_TEXTURE
            #pragma multi_compile __ USE_R16_FOR_ELEVATION_TEXTURE
            #pragma multi_compile __ ENABLE_CIRCULAR_CLIPPING

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.microsoft.maps.unity/Runtime/Shaders/ClippingVolume-MapsSDK.cginc"
            #include "Packages/com.microsoft.maps.unity/Runtime/Shaders/ElevationOffset-MapsSDK.cginc"

            struct Attributes
            {
                float4 vertex : POSITION;
                float3 uv: TEXCOORD;
                float3 normal : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 pos  : SV_POSITION;
                float3 worldPosition : POSITION1;
                float isSkirt : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Needed for ApplyShadowBias.
            float3 _LightDirection;

            Varyings vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                Varyings o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Extract skirt indicator from the third item in uv
                o.isSkirt = v.uv.z;

#if ENABLE_ELEVATION_TEXTURE
                float2 elevationOffset =
                    CalculateElevationOffset(
                        v.uv.xy,
                        _ElevationTexScaleAndOffset.x,
                        _ElevationTexScaleAndOffset.yz,
                        _ElevationTexScaleAndOffset.w);
                v.vertex.y += elevationOffset.x;
#endif

                o.worldPosition = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
                float3 worldNormal = TransformObjectToWorldNormal(v.normal);
                o.pos = TransformWorldToHClip(ApplyShadowBias(o.worldPosition, worldNormal, _LightDirection));

                return o;
            }

            void frag(Varyings i)
            {
                ClipToVolume(i.worldPosition, i.isSkirt);
            }

            ENDHLSL
        }
    }
}
