// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if ENABLE_CONTOUR_LINES

// Assumes premultiplied alpha for colors.
half4 _MajorContourLineColor;
half4 _MinorContourLineColor;
float _HalfMajorContourLinePixelSize;
float _HalfMinorContourLinePixelSize;
float _NumMinorContourIntervalSections;
float _MinorContourLineIntervalInMeters;

half4 ApplyContourLines(half4 color, float elevation /* in meters */)
{
    // Compute the opacity of the contour line.
    float changeInIntervalForOnePixel = fwidth(elevation / _MinorContourLineIntervalInMeters);

    // Avoids rendering a contour line on a completely flat tile.
    if (changeInIntervalForOnePixel == 0)
    {
        return color;
    }

    // Normalize the range and make it continuous... Instead of going 0... 0.999, 1.0, 0.000 etc., the range goes 0 ... 0.499, 0.5, 0.499 ... 0.0 etc.
    float minorNormalizedFrac = 0.5 - distance(frac(elevation / _MinorContourLineIntervalInMeters), 0.5);

    // Determine if this is a major or minor contour line.
    float majorMinorIntervalInMeters = _NumMinorContourIntervalSections * _MinorContourLineIntervalInMeters;
    float majorNormalizedFrac = 0.5 - distance(frac(elevation / majorMinorIntervalInMeters), 0.5);
    bool isMajor = majorNormalizedFrac < (1.0 / (2.0 * _NumMinorContourIntervalSections));

    // Change pixel width based on major/minor.
    float halfPixelWidth = isMajor ? _HalfMajorContourLinePixelSize : _HalfMinorContourLinePixelSize;

    float lowerSmoothStepBound = changeInIntervalForOnePixel * max(halfPixelWidth - 0.5, 0.0);

    // Adding 'changeInIntervalForOnePixel' ensures the edge AA is at least one pixel.
    float upperSmoothStepBound = lowerSmoothStepBound + changeInIntervalForOnePixel; 
    float alpha = 1.0 - smoothstep(lowerSmoothStepBound, upperSmoothStepBound, minorNormalizedFrac);

    float4 preMultipliedAlphaColor = alpha * (isMajor ? _MajorContourLineColor : _MinorContourLineColor);
    return preMultipliedAlphaColor + (1.0f - preMultipliedAlphaColor.a) * color;
}

#endif
