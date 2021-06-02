// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Properties for clipping. Set on MaterialPropertyBlock for each draw call.
float3 _ClippingVolumePosition;
float4 _ClippingVolumeNormals[2];
float3 _ClippingVolumeUp;
float4 _ClippingVolumeColor;
float3 _ClippingVolumeSize;
float _ClippingVolumeFadeDistance;

/// <summary>
/// If the specified world position is outside the clipping volume, the pixel is discarded.
/// Otherwise, the distance to the edge of the volume is returned.
/// </summary>
float ClipToVolume(float3 worldPosition, float isSkirt)
{
    float3 planePosition = _ClippingVolumePosition;
    float3 planePositionToWorldPosition = worldPosition - planePosition;

    // Clip against the base of the map.
    {
        if (dot(_ClippingVolumeUp, planePositionToWorldPosition) < 0)
        {
            discard;
        }
    }

#if defined(ENABLE_CIRCULAR_CLIPPING)

    // Clip to cylindrical volume
    float currentPixelDistanceToPlane = dot(_ClippingVolumeUp, planePositionToWorldPosition);
    float3 worldPositionOnPlane = worldPosition - _ClippingVolumeUp * currentPixelDistanceToPlane;
    float radius = _ClippingVolumeSize.x - 0.005 * isSkirt * _ClippingVolumeSize.x;
    float distanceToEdge = distance(worldPositionOnPlane, planePosition);
    if (distanceToEdge > radius)
    {
        discard;
    }
    return distanceToEdge - radius;

#else

    // Clip to block volume.
    float minDistanceToPlane = -100000000;
    for (uint planeId = 0; planeId < 2; planeId++)
    {
        float3 planeNormal = _ClippingVolumeNormals[planeId].xyz;
        float distanceToPlane = abs(dot(planeNormal, planePositionToWorldPosition)) - _ClippingVolumeSize[planeId] + 0.005 * isSkirt * _ClippingVolumeSize[planeId];
        if (distanceToPlane > 0)
        {
            discard;
        }
        minDistanceToPlane = max(minDistanceToPlane, distanceToPlane);
    }
    return minDistanceToPlane;

#endif
}
