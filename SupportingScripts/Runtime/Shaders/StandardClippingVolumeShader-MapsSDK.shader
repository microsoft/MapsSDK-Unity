// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

Shader "Maps SDK/Standard Clipping Volume"
{
    SubShader
    {
        // Render the clipping volume walls.
        Pass
        {
            Tags{ "RenderType" = "Opaque" "LightMode" = "ForwardBase" }

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma multi_compile __ ENABLE_MRTK_INTEGRATION
            #pragma multi_compile __ ENABLE_CLIPPING // We'll skip the clipping when rendering the base of the map.

            // Use shader model 3.0 target to get nicer looking lighting.
            #pragma target 3.0

            #include "AutoLight.cginc"
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "ClippingVolume-MapsSDK.cginc"
            #include "MRTKIntegration-MapsSDK.cginc"

            struct appdata
            {
                float3 vertex : POSITION;
                float3 normal : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 cameraPosition : POSITION1;
#if ENABLE_MRTK_INTEGRATION
                float3 worldPosition : POSITION2;
#endif
                float3 normal : NORMAL;

                SHADOW_COORDS(1)
                UNITY_FOG_COORDS(2)

                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _TerrainDistanceTex;
            float2 _MapDimension;
            float4x4 _WorldToDistanceCameraMatrix;
            float _MapCurrentHeight;

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);

                float3 worldPosition = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
#if ENABLE_MRTK_INTEGRATION
                o.worldPosition = worldPosition;
#endif
                o.cameraPosition = mul(_WorldToDistanceCameraMatrix, float4(worldPosition, 1.0)).xyz;

                o.normal = UnityObjectToWorldNormal(v.normal);

                TRANSFER_SHADOW(o);
                UNITY_TRANSFER_FOG(o, o.pos);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Because we sample from a fullscreen texture (the shadow map), don't forget to setup the eye index.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

#if ENABLE_CLIPPING
                // Clip the walls to the terrain.
                float2 distanceToMapUV = i.cameraPosition.xy / _MapDimension + float2(0.5, 0.5);
                float normalizedDistanceToMapSurface  = tex2D(_TerrainDistanceTex, distanceToMapUV).r;
                normalizedDistanceToMapSurface *= _MapCurrentHeight;
                float distanceToCurrentPoint = -i.cameraPosition.z;
                if (distanceToCurrentPoint - 0.001 >= normalizedDistanceToMapSurface )
                {
                    discard;
                }
#endif

                // Add a small amount of shading based on the light direction.
                float shadeFactor = min(0.5 * saturate(dot(normalize(_WorldSpaceLightPos0.xyz), i.normal)) + 0.5, max(SHADOW_ATTENUATION(i), 0.5));
                fixed4 finalColor = fixed4(_ClippingVolumeColor.rgb * shadeFactor, _ClippingVolumeColor.a);

#if ENABLE_MRTK_INTEGRATION
                // MRTK hover light.
                finalColor = ApplyHoverLight(i.worldPosition.xyz, finalColor);
#endif

                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, finalColor);

                return finalColor;
            }
            ENDCG
        }

        // Custom shadow caster rendering pass to handle clipping volume.
        Pass
        {
            Tags { "RenderType" = "Opaque" "LightMode" = "ShadowCaster"}

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_shadowcaster
            #pragma multi_compile __ ENABLE_CLIPPING // We'll skip the clipping when rendering the base of the map.

            #include "UnityCG.cginc"
            #include "ClippingVolume-MapsSDK.cginc"

            sampler2D _TerrainDistanceTex;
            float2 _MapDimension;
            float4x4 _WorldToDistanceCameraMatrix;
            float _MapCurrentHeight;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
                float3 cameraPosition : POSITION1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);
                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.cameraPosition = mul(_WorldToDistanceCameraMatrix, float4(mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz, 1.0));

                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
#if ENABLE_CLIPPING
                // Clip the walls to the terrain
                float2 distanceToMapUV = i.cameraPosition.xy / _MapDimension + float2(0.5, 0.5);
                float normalizedDistanceToMapSurface  = tex2D(_TerrainDistanceTex, distanceToMapUV).r;
                normalizedDistanceToMapSurface  *= _MapCurrentHeight;
                float distanceToCurrentPoint = -i.cameraPosition.z;
                if (distanceToCurrentPoint - 0.001 >= normalizedDistanceToMapSurface )
                {
                    discard;
                }
#endif

                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
