// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

///
/// Basic 3D Text shader with proper z-sorting
///
Shader "Maps SDK/Occludable 3D Text"
{
    Properties
    {
        _MainTex("Alpha (A)", 2D) = "white" {}

        [HideInInspector] _Color("Main Color", Color) = (1,1,1,1)
        [HideInInspector] _StencilComp("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask("Color Mask", Float) = 15
    }

    SubShader
    {
        LOD 200

        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest[unity_GUIZTestMode]
        Offset -1, -1
        Fog { Mode Off }
        Blend One OneMinusSrcAlpha
        ColorMask[_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                half4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : POSITION;
                half4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO

            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(fixed4, _Color)
#define _Color_arr Props
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                o.color.rgb *= o.color.a;
                #ifdef UNITY_HALF_TEXEL_OFFSET
                    o.vertex.xy += (_ScreenParams.zw - 1.0)*float2(-1,1);
                #endif
                return o;
            }

            half4 frag(v2f i) : COLOR
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half4 col = i.color;
                col *= tex2D(_MainTex, i.texcoord).a;
                fixed4 instancedColor = UNITY_ACCESS_INSTANCED_PROP(_Color_arr, _Color);
                instancedColor.rgb *= instancedColor.a;
                col = col * instancedColor;
                clip(col.a - 0.01);
                return col;
            }
            ENDCG
        }
    }
}