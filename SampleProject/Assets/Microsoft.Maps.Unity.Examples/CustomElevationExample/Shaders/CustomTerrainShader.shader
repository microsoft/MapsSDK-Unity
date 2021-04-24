// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This shader is based on StandardTerrainShader-MapsSDK.shader, but instead of using explicit vertex and fragment definitions,
// this shader uses the surface shader approach and adds normal data to elevation sources-- it does not work in areas with 3D model data!

Shader "Maps SDK/Custom Terrain Shader"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        [MaterialToggle] _UseSolidColor("Use Solid Color", Float) = 0
    }

    SubShader
    {
        Tags{ "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM

        // These are the maps specific keywords...
        #pragma multi_compile __ ENABLE_ELEVATION_TEXTURE
        #pragma multi_compile __ USE_R16_FOR_ELEVATION_TEXTURE
        #pragma multi_compile __ ENABLE_CONTOUR_LINES
        #pragma multi_compile __ ENABLE_MRTK_INTEGRATION
        #pragma multi_compile __ ENABLE_CIRCULAR_CLIPPING

        #include "Packages/com.microsoft.maps.unity/Runtime/Shaders/ClippingVolume-MapsSDK.cginc"
        #include "Packages/com.microsoft.maps.unity/Runtime/Shaders/ContourLines-MapsSDK.cginc"
        #include "Packages/com.microsoft.maps.unity/Runtime/Shaders/ElevationOffset-MapsSDK.cginc"

        #pragma surface surf StandardSpecular vertex:vert addshadow

        #define _SPECULARHIGHLIGHTS_OFF 1
        #define _GLOSSYREFLECTIONS_OFF 1

        #pragma target 3.0
        //#pragma enable_d3d11_debug_symbols

        fixed4 _Color;
        int _MainTexCount;
        sampler2D _MainTex0;
        sampler2D _MainTex1;
        sampler2D _MainTex2;
        sampler2D _MainTex3;
        float4 _TexScaleAndOffset[4];
        float _UseSolidColor;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_MainTex1;
            float3 worldPos;
            float3 worldNormal;
            float isSkirt;
#if ENABLE_ELEVATION_TEXTURE
            float2 elevationTex;
            float elevation;
            float elevationNorm;
#endif
            INTERNAL_DATA
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

#if ENABLE_ELEVATION_TEXTURE
            float2 elevationOffset =
                CalculateElevationOffset(
                    v.texcoord,
                    _ElevationTexScaleAndOffset.x,
                    _ElevationTexScaleAndOffset.yz,
                    _ElevationTexScaleAndOffset.w);
            v.vertex.y += elevationOffset.x;

            o.elevationTex = v.texcoord;
            o.elevation = elevationOffset.y;
#endif

            o.isSkirt = v.texcoord.z;
            v.texcoord = float4(_TexScaleAndOffset[0].x * v.texcoord + _TexScaleAndOffset[0].yz, 0, 0);
            if (_MainTexCount > 1)
            {
                v.texcoord1 = float4(_TexScaleAndOffset[1].x * v.texcoord + _TexScaleAndOffset[1].yz, 0, 0);
            }

            v.normal = float3(0, 1, 0);
            v.tangent = float4(1, 0, 0, -1);
        }

        fixed4 blend(fixed4 dst, fixed4 src)
        {
            return fixed4(src.rgb * src.a + dst.rgb * (1.0 - src.a), 1);
        }

        void surf(Input IN, inout SurfaceOutputStandardSpecular o)
        {
            ClipToVolume(IN.worldPos, IN.isSkirt);

            // Switch between solid color or textures.
            fixed4 color = _Color;
            if (_UseSolidColor < 0.5)
            {
                color = tex2D(_MainTex0, IN.uv_MainTex);
                if (_MainTexCount > 1)
                {
                    color = blend(color, tex2D(_MainTex1, IN.uv_MainTex1));
                }
            }

            // Apply contours.
#if ENABLE_ELEVATION_TEXTURE && ENABLE_CONTOUR_LINES
            float2 elevationOffset =
                CalculateElevationOffset(
                    IN.elevationTex,
                    _ElevationTexScaleAndOffset.x,
                    _ElevationTexScaleAndOffset.yz,
                    _ElevationTexScaleAndOffset.w);
            color = ApplyContourLines(color, elevationOffset.y);
#endif

            o.Albedo = color;
            o.Specular = 0;

#if ENABLE_ELEVATION_TEXTURE
            float3 localNormal =
                FilterNormal(
                    IN.elevationTex,
                    _ElevationTexScaleAndOffset.x,
                    _ElevationTexScaleAndOffset.yz,
                    1.0 / 257.0,
                    1.0);
            float3 worldNormal = UnityObjectToWorldNormal(localNormal);

            // Construct world space to tangent space matrix.
            half3 worldT = WorldNormalVector(IN, half3(1, 0, 0));
            half3 worldB = WorldNormalVector(IN, half3(0, 1, 0));
            half3 worldN = WorldNormalVector(IN, half3(0, 0, 1));
            half3x3 tbn = half3x3(worldT, worldB, worldN);

            // Convert world space normal to tangent space.
            o.Normal= mul(tbn, worldNormal);
#endif
        }

        ENDCG
    }
}
