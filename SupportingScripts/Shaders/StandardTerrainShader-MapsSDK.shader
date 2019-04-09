// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

Shader "MapsSDK/StandardTerrainShader"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
    }

    SubShader
    {
        Pass
        {
            Tags{ "RenderType" = "Opaque" "LightMode" = "ForwardBase" }

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            // These are the maps specific keywords...
            #pragma multi_compile __ ENABLE_ELEVATION_TEXTURE

            // Support the various Unity keywords...
            #pragma multi_compile_fog
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

            // Use shader model 3.0 target to get nicer looking lighting.
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include "ClippingVolume-MapsSDK.cginc"
            #include "ElevationOffset-MapsSDK.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _TexScaleAndOffset;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPosition : POSITION1;
                float2 texcoord : TEXCOORD0;
                
                SHADOW_COORDS(1)
                UNITY_FOG_COORDS(2)

                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

#if defined(ENABLE_ELEVATION_TEXTURE)
                float elevationOffset =
                    CalculateElevationOffset(
                        _ElevationTex,
                        v.uv,
                        _ElevationTexScaleAndOffset.x,
                        _ElevationTexScaleAndOffset.yz,
                        _ElevationTexScaleAndOffset.w);
                v.vertex.y += elevationOffset;
#endif

                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.texcoord = _TexScaleAndOffset.x * v.uv + _TexScaleAndOffset.yz;

                float3 worldPosition = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
                o.worldPosition = worldPosition;

                TRANSFER_SHADOW(o)
                UNITY_TRANSFER_FOG(o, o.pos);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Because we sample from a fullscreen texture (the shadow map), don't forget to setup the eye index.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float minDistanceToPlane = ClipToVolume(i.worldPosition);

                // Albedo comes from a texture tinted by color
                fixed4 color = tex2D(_MainTex, i.texcoord) * _Color;
    
                float lerpAmount = saturate(1.0 + minDistanceToPlane / _ClippingVolumeFadeDistance);
                color = lerp(color, _ClippingVolumeColor, lerpAmount);

                // Apply shadow.
                fixed shadow = SHADOW_ATTENUATION(i);
                color.rgb *= saturate(shadow + 0.33);

                // Apply fog.
                UNITY_APPLY_FOG(i.fogCoord, color);

                return fixed4(color.r, color.g, color.b, 1.0);
            }
            ENDCG
        }

        // Custom shadow caster rendering pass to handle clipping volume.
        Pass
        {
            Tags {"LightMode" = "ShadowCaster"}

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            // These are the map specific keywords...
            #pragma multi_compile __ ENABLE_ELEVATION_TEXTURE

            #pragma multi_compile_shadowcaster

            #include "UnityCG.cginc"
            #include "ClippingVolume-MapsSDK.cginc"
            #include "ElevationOffset-MapsSDK.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv: TEXCOORD;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
                float3 worldPosition : POSITION1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

#if defined(ENABLE_ELEVATION_TEXTURE)
                float elevationOffset =
                    CalculateElevationOffset(
                        _ElevationTex,
                        v.uv,
                        _ElevationTexScaleAndOffset.x,
                        _ElevationTexScaleAndOffset.yz,
                        _ElevationTexScaleAndOffset.w);
                v.vertex.y += elevationOffset;
#endif

                v2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 worldPosition = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
                o.worldPosition = worldPosition;

                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                ClipToVolume(i.worldPosition);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
