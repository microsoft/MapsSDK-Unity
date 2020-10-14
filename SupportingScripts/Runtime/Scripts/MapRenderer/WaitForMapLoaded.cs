// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Maps.Unity
{
    using UnityEngine;

    /// <summary>
    /// Yields until <see cref="MapRendererBase.IsLoaded"/> returns true.
    /// </summary>
    public class WaitForMapLoaded : CustomYieldInstruction
    {
        private readonly MapRendererBase _mapRendererBase;
        private readonly float _maxWaitDurationInSeconds;
        private readonly int _startingFrame;
        private readonly float _startingTime;
        private byte? _firstFrameLoadedFlag;

        /// <summary>
        /// Constructor.
        /// </summary>
        public WaitForMapLoaded(MapRendererBase mapRendererBase, float maxWaitDurationInSeconds = 30.0f)
        {
            _mapRendererBase = mapRendererBase;
            _maxWaitDurationInSeconds = Mathf.Max(0.0f, maxWaitDurationInSeconds);
            _startingFrame = Time.frameCount;
            _startingTime = Time.time;
        }

        /// <inheritdoc/>
        public override bool keepWaiting
        {
            get
            {
                // Check if the wait has timed out first.
                if (Time.time - _startingTime > _maxWaitDurationInSeconds)
                {
                    return false;
                }

                // Always wait through the first frame.
                if (Time.frameCount <= _startingFrame)
                {
                    return true;
                }

                // Always wait through the frame where the map is loaded.
                var isLoaded = _mapRendererBase.IsLoaded;
                if (isLoaded && !_firstFrameLoadedFlag.HasValue)
                {
                    _firstFrameLoadedFlag = 0;
                    return true;
                }

                if (_firstFrameLoadedFlag.HasValue && !isLoaded)
                {
                    // Could happen when the map loads for one frame and is immediately dirtied for another reason.
                    // In this case, clear our frame loaded tracking (and keep waiting).
                    _firstFrameLoadedFlag = new byte?();
                    return true;
                }

                // Otherwise, we keep waiting while the map has not loaded.
                return !isLoaded;
            }
        }
    }
}
