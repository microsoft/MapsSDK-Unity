/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

float inverseLerp(float t, float a, float b) {
  return (t - a)/(b - a);
}

float sinEasing(float value) {
  return sin((value - 0.5) * 3.14) * 0.5 + 0.5;
}

float3 lineDistance(float2 uv, float4 linePoints) {
  float2 start = linePoints.xy;
  float2 end = linePoints.zw;
  float2 startToUv = uv - start;
  float2 lineVector = end - start;
  float lineSqrLength = dot(lineVector, lineVector);
  float projectedValue = dot(lineVector, startToUv) / lineSqrLength;
  float projectedValueClamped = saturate(projectedValue);
  float2 positionOnLine = start + lineVector * projectedValueClamped;
  float distanceToLine = length(uv - positionOnLine);

  return float3(distanceToLine, projectedValue, length(lineVector) * projectedValueClamped);
}

float4 resizeLine(float4 linePoints, float param) {
  float2 lStart = linePoints.xy;
  float2 lEnd = linePoints.zw;
  float2 lEndCorrected = lStart + (lEnd - lStart) * param;
  return float4(lStart, lEndCorrected);
}

float computeRadiusMovingMask(float2 radiusMinMax, float param, float sizeParm) {
  float radParam = saturate(saturate(param - 0.1) / 0.9);
  float radScaleParam = saturate(param / 0.1);
  float maxRadCorrected = lerp(radiusMinMax.x, radiusMinMax.y, radParam);
  return lerp(radiusMinMax.x, maxRadCorrected, sizeParm) * radScaleParam;
}

void getPalmFingerRadius(out float2 lineRadius[5]) {
  float2 palmFingerRadius[5] = {
    float2(0.070, 0.14),
    float2(0.06, 0.06),
    float2(0.06, 0.06),
    float2(0.06, 0.06),
    float2(0.06, 0.06)
  };
  lineRadius = palmFingerRadius;
}

void getFingerRadius(out float2 lineRadius[5]) {
  float2 fingerRadius[5] = {
    float2(0.2, 0.2),
    float2(0.065, 0.055),
    float2(0.055, 0.055),
    float2(0.050, 0.045),
    float2(0.045, 0.05)
  };
  lineRadius = fingerRadius;
}

float fingerLineGlow(float2 handUV, float strengthValues[5], float distanceScale, float4 lines[5], float2 linesRadius[5]) {
  float2 uv = handUV;
  float distValue = 2.0;
  for(int index = 0; index < 5; index++) {
    
    float param = saturate(strengthValues[index]);
    float4 linePoints = resizeLine(lines[index], param);
    float2 lDist = lineDistance(uv, linePoints);
    float radius = computeRadiusMovingMask(linesRadius[index], param, saturate(lDist.y));

    float dist = lDist.x - radius;
    distValue = min(dist, distValue);
  }
  
  float invDist = distValue * -1.0;
  float glowMask = saturate(invDist * distanceScale);
  return sinEasing(glowMask);
}

float movingFingerGradient(float2 handUV, float strengthValues[5], float gradientLength, float4 lines[5], float2 linesRadius[5]) {
  float2 uv = handUV;
  float distValue = 0.0;
  float value = 0.0;
  for(int index = 0; index < 5; index++) {
    float strength = saturate(strengthValues[index]);
    float2 lDist = lineDistance(uv, lines[index]);
    float radius = lerp(linesRadius[index].x, linesRadius[index].y, saturate(lDist.y));
    if (lDist.x < radius) {
      float lValue = max(0.0, (lDist.x - radius) * -1.0);
      float hDist = lDist.y;
      float param = saturate(inverseLerp(hDist, strength - gradientLength, strength));
      if ( lValue > distValue) {
        value = sinEasing(1.0 - param);
        distValue = lValue;
      }
    }
  }
  return value;
}

float invertedSphereGradient(float3 gradientCenter, float3 worldPosition, float gradientLength) {
  float distance = length(worldPosition - gradientCenter);
  float gradient = 1.0 - saturate(distance / gradientLength);
  return sinEasing(gradient);
}

float movingSphereGradient(float3 gradientCenter, float3 worldPosition, float gradientLength, float offset, float gradientMultiplier) {
  float normalizedDistance = length(worldPosition - gradientCenter) / gradientLength;
  float gradient = saturate((normalizedDistance - offset) * gradientMultiplier);
  return sinEasing(gradient);
}
