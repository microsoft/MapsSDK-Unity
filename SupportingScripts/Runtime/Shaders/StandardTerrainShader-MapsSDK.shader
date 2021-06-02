// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

Shader "Maps SDK/Standard Terrain"
{
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
            #pragma multi_compile __ USE_R16_FOR_ELEVATION_TEXTURE
            #pragma multi_compile __ ENABLE_CONTOUR_LINES
            #pragma multi_compile __ ENABLE_MRTK_INTEGRATION
            #pragma multi_compile __ ENABLE_CIRCULAR_CLIPPING

            // Support the various Unity keywords...
            #pragma multi_compile_fog
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

            // Use shader model 3.0 target to get nicer looking lighting.
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            #include "ClippingVolume-MapsSDK.cginc"
            #include "ContourLines-MapsSDK.cginc"
            #include "ElevationOffset-MapsSDK.cginc"
            #include "MRTKIntegration-MapsSDK.cginc"

            int _MainTexCount;
            sampler2D _MainTex0;
            sampler2D _MainTex1;
            sampler2D _MainTex2;
            sampler2D _MainTex3;
            float4 _TexScaleAndOffset[4];
            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPosition : POSITION1;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float2 uv3 : TEXCOORD2;
                float2 uv4 : TEXCOORD3;
                float isSkirt : TEXCOORD4;
#if ENABLE_ELEVATION_TEXTURE && ENABLE_CONTOUR_LINES
                float elevation : TEXCOORD5;
#endif
                SHADOW_COORDS(6)
                UNITY_FOG_COORDS(7)

                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Extract skirt indicator from the third item in uv
                o.isSkirt = v.uv.z;

#if ENABLE_ELEVATION_TEXTURE
                float2 elevationOffset =
                    CalculateElevationOffset(
                        v.uv,
                        _ElevationTexScaleAndOffset.x,
                        _ElevationTexScaleAndOffset.yz,
                        _ElevationTexScaleAndOffset.w);
                v.vertex.y += elevationOffset.x;
#if ENABLE_CONTOUR_LINES
                o.elevation = elevationOffset.y;
#endif
#endif

                o.pos = UnityObjectToClipPos(v.vertex);
                if (_MainTexCount > 0) { o.uv = _TexScaleAndOffset[0].x * v.uv.xy + _TexScaleAndOffset[0].yz; }
                if (_MainTexCount > 1) { o.uv2 = _TexScaleAndOffset[1].x * v.uv.xy + _TexScaleAndOffset[1].yz; }
                if (_MainTexCount > 2) { o.uv3 = _TexScaleAndOffset[2].x * v.uv.xy + _TexScaleAndOffset[2].yz; }
                if (_MainTexCount > 3) { o.uv4 = _TexScaleAndOffset[3].x * v.uv.xy + _TexScaleAndOffset[3].yz; }

                float3 worldPosition = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
                o.worldPosition = worldPosition;

                TRANSFER_SHADOW(o)
                UNITY_TRANSFER_FOG(o, o.pos);

                return o;
            }

            fixed4 blend(fixed4 dst, fixed4 src)
            {
                return fixed4(src.rgb * src.a + dst.rgb * (1.0 - src.a), 1);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Because we sample from a fullscreen texture (the shadow map), don't forget to setup the eye index.
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Clip to ClippingVolume
                float minDistanceToPlane = ClipToVolume(i.worldPosition, i.isSkirt);

                // Albedo comes from a texture tinted by color
                fixed4 color = tex2D(_MainTex0, i.uv);
                if (_MainTexCount > 1) { color = blend(color, tex2D(_MainTex1, i.uv2)); }
                if (_MainTexCount > 2) { color = blend(color, tex2D(_MainTex2, i.uv3)); }
                if (_MainTexCount > 3) { color = blend(color, tex2D(_MainTex3, i.uv4)); }

                // Apply contours.
#if ENABLE_ELEVATION_TEXTURE && ENABLE_CONTOUR_LINES
                color = ApplyContourLines(color, i.elevation);
#endif

                float lerpAmount = saturate(1.0 + minDistanceToPlane / _ClippingVolumeFadeDistance);
                color = lerp(color, _ClippingVolumeColor, lerpAmount);

                // Apply shadow.
                fixed shadow = SHADOW_ATTENUATION(i);
                color.rgb *= saturate(shadow + 0.33);

                // MRTK hover light.
                color = ApplyHoverLight(i.worldPosition.xyz, color);

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
            #pragma multi_compile __ USE_R16_FOR_ELEVATION_TEXTURE
            #pragma multi_compile __ ENABLE_CIRCULAR_CLIPPING

            #pragma multi_compile_shadowcaster

            #include "UnityCG.cginc"
            #include "ClippingVolume-MapsSDK.cginc"
            #include "ElevationOffset-MapsSDK.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv: TEXCOORD;
                float3 normal : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                V2F_SHADOW_CASTER;
                float3 worldPosition : POSITION1;
                float isSkirt : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // Extract skirt indicator from the third item in uv
                o.isSkirt = v.uv.z;

#if ENABLE_ELEVATION_TEXTURE
                float elevationOffset =
                    CalculateElevationOffset(
                        v.uv,
                        _ElevationTexScaleAndOffset.x,
                        _ElevationTexScaleAndOffset.yz,
                        _ElevationTexScaleAndOffset.w);
                v.vertex.y += elevationOffset;
#endif

                float3 worldPosition = mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1.0)).xyz;
                o.worldPosition = worldPosition;

                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                ClipToVolume(i.worldPosition, i.isSkirt);
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
}
