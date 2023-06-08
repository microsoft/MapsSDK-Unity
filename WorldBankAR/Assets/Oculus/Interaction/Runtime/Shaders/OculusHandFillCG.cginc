﻿/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight

#pragma prefer_hlslcc gles
#pragma exclude_renderers d3d11_9x
#pragma target 2.0

//

struct VertexInput {
  float4 vertex : POSITION;
  half3 normal : NORMAL;
  half4 vertexColor : COLOR;
  float4 texcoord : TEXCOORD0;
  float4 texcoord1 : TEXCOORD1;
  UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput {
  float4 vertex : SV_POSITION;
  float3 worldPos : TEXCOORD1;
  float3 worldNormal : TEXCOORD2;
  half4 glowColor : COLOR;
  float4 texcoord1 : TEXCOORD3;
  UNITY_VERTEX_INPUT_INSTANCE_ID
  UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput baseVertex(VertexInput v) {
  VertexOutput o;
  UNITY_SETUP_INSTANCE_ID(v);
  UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
  UNITY_TRANSFER_INSTANCE_ID(v, o);

  o.worldPos = mul(unity_ObjectToWorld, v.vertex);
  o.worldNormal = UnityObjectToWorldNormal(v.normal);
  o.vertex = UnityObjectToClipPos(v.vertex);
  o.texcoord1 = v.texcoord1;
  half4 maskPixelColor = tex2Dlod(
      _FingerGlowMask,
      float4(v.texcoord.xy, 0.0, 0.0));
  o.glowColor.rgb = float3(0.0, 0.0, 0.0);
  o.glowColor.a = saturate(maskPixelColor.a + _WristFade) * _Opacity;
  return o;
}

#include "GlowFunctions.cginc"

void getFingerStrength(out float fingerStrength[5]) {
  float strengthValuesUniforms[5] = {
      _ThumbGlowValue,
      _IndexGlowValue,
      _MiddleGlowValue,
      _RingGlowValue,
      _PinkyGlowValue
  };
  fingerStrength = strengthValuesUniforms;
}

void getFillFingerLines(out float4 lines[5]) {
  float4 _lines[5] = {
      _ThumbLine,
      _IndexLine,
      _MiddleLine,
      _RingLine,
      _PinkyLine
  };
  lines = _lines;
}

half4 applyGlow(int glowType, float3 color, float alpha, float2 texCoord, float3 worldPosition) {
  if (glowType == 27 || glowType == 29) {
    float fingerStrength[5];
    getFingerStrength(fingerStrength);
    float4 lines[5];
    getFillFingerLines(lines);
    float2 fingerRadius[5];
    getFingerRadius(fingerRadius);
    float glowValue = movingFingerGradient(texCoord, fingerStrength, _GlowParameter, lines, fingerRadius);
    return half4(color + _GlowColor * glowValue, alpha);
  }
  if (glowType == 16 || glowType == 17) {
    float gradient = invertedSphereGradient(_GlowPosition, worldPosition, _GlowMaxLength);
    float3 glowColor = _GlowColor * gradient * _GlowParameter;
    return float4(saturate(color + glowColor), alpha);
  }
  if (glowType == 11 || glowType == 15) {
    float gradient = movingSphereGradient(_GlowPosition, worldPosition, _GlowMaxLength, _GlowParameter, 1.5);
    float3 glowColor = _GlowColor * gradient;
    return float4(saturate(color + glowColor), alpha);
  }
  return half4(color, alpha);
}

half4 baseFragment(VertexOutput i) : SV_Target {
  float3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
  float fresnelNdot = dot(i.worldNormal, worldViewDir);
  float fresnel = 1.0 * pow(1.0 - fresnelNdot, _FresnelPower);
  float4 color = lerp(_ColorTop, _ColorBottom, fresnel);

  if (_GenerateGlow == 1) {
    return applyGlow(_GlowType, color.rgb, i.glowColor.a, i.texcoord1, i.worldPos);
  } else {
    return half4(color.rgb, i.glowColor.a);
  }
}
