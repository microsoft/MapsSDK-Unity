/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// ProgressCurve provides a helper for creating curves for easing.
    /// In some respects it works like an AnimationCurve except that ProgressCurve
    /// always takes in a normalized AnimationCurve and a second parameter
    /// defines the length of the animation.
    ///
    /// A few helper methods are provided to track progress through the animation.
    /// </summary>
    [Serializable]
    public class ProgressCurve
    {
        [SerializeField]
        private AnimationCurve _animationCurve;
        public AnimationCurve AnimationCurve
        {
            get
            {
                return _animationCurve;
            }
            set
            {
                _animationCurve = value;
            }
        }

        [SerializeField]
        private float _animationLength;
        public float AnimationLength
        {
            get
            {
                return _animationLength;
            }
            set
            {
                _animationLength = value;
            }
        }

        private Func<float> _timeProvider = () => Time.time;
        public Func<float> TimeProvider
        {
            get
            {
                return _timeProvider;
            }
            set
            {
                _timeProvider = value;
            }
        }

        private float _animationStartTime;

        public ProgressCurve()
        {
            _animationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            _animationLength = 1.0f;
        }

        public ProgressCurve(AnimationCurve animationCurve, float animationLength)
        {
            _animationCurve = animationCurve;
            _animationLength = animationLength;
        }

        public ProgressCurve(ProgressCurve other)
        {
            Copy(other);
        }

        public void Copy(ProgressCurve other)
        {
            _animationCurve = other._animationCurve;
            _animationLength = other._animationLength;
            _animationStartTime = other._animationStartTime;
            _timeProvider = other._timeProvider;
        }

        public void Start()
        {
            _animationStartTime = _timeProvider();
        }

        public float Progress()
        {
            if (_animationLength <= 0f)
            {
                return _animationCurve.Evaluate(1.0f);
            }

            float normalizedTimeProgress = Mathf.Clamp01(ProgressTime() / _animationLength);
            return _animationCurve.Evaluate(normalizedTimeProgress);
        }

        public float ProgressIn(float time)
        {
            if (_animationLength <= 0f)
            {
                return _animationCurve.Evaluate(1.0f);
            }

            float normalizedTimeProgress = Mathf.Clamp01((ProgressTime() + time) / _animationLength);
            return _animationCurve.Evaluate(normalizedTimeProgress);
        }

        public float ProgressTime()
        {
            return Mathf.Clamp(_timeProvider() - _animationStartTime, 0f, _animationLength);
        }

        public void End()
        {
            _animationStartTime = _timeProvider() - _animationLength;
        }

    }
}
