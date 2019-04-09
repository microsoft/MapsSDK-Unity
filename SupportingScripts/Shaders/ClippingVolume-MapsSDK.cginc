// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Properties for clipping. Set on MaterialPropertyBlock for each draw call.
float4 _ClippingVolumePosition;
float4 _ClippingVolumeNormals[2];
float3 _ClippingVolumeUp;
fixed4 _ClippingVolumeColor;
float3 _ClippingVolumeSize;
float _ClippingVolumeFadeDistance;

/// <summary>
/// If the specified world position is outside the clipping volume, the pixel is discarded.
/// Otherwise, the distance to the edge of the volume is returned.
/// </summary>
float ClipToVolume(float3 worldPosition)
{
    // Clip to volume.
    float minDistanceToPlane = -100000000;
    for (uint planeId = 0; planeId < 2; planeId++)
    {
        float3 planePosition = _ClippingVolumePosition;
        float3 planeNormal = _ClippingVolumeNormals[planeId].xyz;

        float distanceToPlane = abs(dot(planeNormal, worldPosition - planePosition)) - _ClippingVolumeSize[planeId];

        if (distanceToPlane > 0)
        {
            discard;
        }

        minDistanceToPlane = max(minDistanceToPlane, distanceToPlane);
    }

    return minDistanceToPlane;
}
