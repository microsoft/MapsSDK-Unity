// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

Shader "Maps SDK/Universal Render Pipeline/Clipping Volume"
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
            #pragma multi_compile __ ENABLE_MRTK_INTEGRATION
            #pragma multi_compile __ ENABLE_CLIPPING // We'll skip the clipping when rendering the base of the map.

            // Support the various Unity keywords...
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.microsoft.maps.unity/Runtime/Shaders/ClippingVolume-MapsSDK.cginc"
            #include "Packages/com.microsoft.maps.unity/Runtime/Shaders/MRTKIntegration-MapsSDK.cginc"

            struct Attributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 pos  : SV_POSITION;
                float3 cameraPosition : POSITION1;
                float3 worldPosition : POSITION2;
                float3 normal : NORMAL;
                float fogFactor : TEXCOORD0;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_TerrainDistanceTex);
            SAMPLER(sampler_TerrainDistanceTex);
            float2 _MapDimension;
            float4x4 _WorldToDistanceCameraMatrix;
            float _MapCurrentHeight;

            Varyings vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                Varyings o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = TransformObjectToHClip(v.vertex.xyz);

                float3 worldPosition = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
                o.worldPosition = worldPosition;

                o.cameraPosition = mul(_WorldToDistanceCameraMatrix, float4(worldPosition, 1.0)).xyz;

                o.normal = TransformObjectToWorldNormal(v.normal);

                o.fogFactor = ComputeFogFactor(o.pos.z);

                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // Because we sample from a fullscreen texture (the shadow map), don't forget to setup the eye index.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

#if ENABLE_CLIPPING
                // Clip the walls to the terrain.
                float2 distanceToMapUV = i.cameraPosition.xy / _MapDimension + float2(0.5, 0.5);
                float normalizedDistanceToMapSurface = SAMPLE_TEXTURE2D(_TerrainDistanceTex, sampler_TerrainDistanceTex, distanceToMapUV).r;
                normalizedDistanceToMapSurface *= _MapCurrentHeight;
                float distanceToCurrentPoint = -i.cameraPosition.z;
                if (distanceToCurrentPoint - 0.001 >= normalizedDistanceToMapSurface)
                {
                    discard;
                }
#endif
                // Apply shadow.
                half shadow = 1.0;
#ifdef _MAIN_LIGHT_SHADOWS
                VertexPositionInputs vertexInput = (VertexPositionInputs)0;
                vertexInput.positionWS = i.worldPosition;
                float4 shadowCoordinate = GetShadowCoord(vertexInput);
                shadow = MainLightRealtimeShadow(shadowCoordinate);
#endif

                // Add a small amount of shading based on the light direction.
                Light mainLight = GetMainLight();
                float shadeFactor = min(0.5 * saturate(dot(normalize(mainLight.direction), i.normal)) + 0.5, max(shadow, 0.5));
                half4 finalColor = half4(_ClippingVolumeColor.rgb * shadeFactor, _ClippingVolumeColor.a);

#if ENABLE_MRTK_INTEGRATION
                // MRTK hover light.
                finalColor = ApplyHoverLight(i.worldPosition.xyz, finalColor);
#endif

                finalColor.rgb = MixFog(finalColor.rgb, i.fogFactor);

                return finalColor;
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

            #pragma multi_compile __ ENABLE_CLIPPING // We'll skip the clipping when rendering the base of the map.

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 pos  : SV_POSITION;
                float3 cameraPosition : POSITION1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Needed for ApplyShadowBias.
            float3 _LightDirection;

            TEXTURE2D(_TerrainDistanceTex);
            SAMPLER(sampler_TerrainDistanceTex);
            float2 _MapDimension;
            float4x4 _WorldToDistanceCameraMatrix;
            float _MapCurrentHeight;

            Varyings vert(Attributes v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                Varyings o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // worldPosition and worldNormal used to apply shadow bias.
                float3 worldPosition = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
                float3 worldNormal = TransformObjectToWorldNormal(v.normal);
                o.pos = TransformWorldToHClip(ApplyShadowBias(worldPosition, worldNormal, _LightDirection));

                // Camera position needed to sample from clipping distance texture.
                o.cameraPosition = mul(_WorldToDistanceCameraMatrix, float4(worldPosition, 1.0)).xyz;

                return o;
            }

            void frag(Varyings i)
            {
#if ENABLE_CLIPPING
                // Clip the walls to the terrain.
                float2 distanceToMapUV = i.cameraPosition.xy / _MapDimension + float2(0.5, 0.5);
                float normalizedDistanceToMapSurface = SAMPLE_TEXTURE2D(_TerrainDistanceTex, sampler_TerrainDistanceTex, distanceToMapUV).r;
                normalizedDistanceToMapSurface *= _MapCurrentHeight;
                float distanceToCurrentPoint = -i.cameraPosition.z;
                if (distanceToCurrentPoint - 0.001 >= normalizedDistanceToMapSurface)
                {
                    discard;
                }
#endif
            }

            ENDHLSL
        }
    }
}
