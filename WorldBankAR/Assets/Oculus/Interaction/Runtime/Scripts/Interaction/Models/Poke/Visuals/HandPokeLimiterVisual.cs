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

using UnityEngine;
using Oculus.Interaction.Input;

namespace Oculus.Interaction
{
    /// <summary>
    /// HandPokeInteractorVisual forwards the finger state of an associated
    /// HandPokeInteractor to a HandGrabModifier to lock and unlock
    /// finger joints in the modifier's target hand data.
    /// </summary>
    public class HandPokeLimiterVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        private IHand Hand;

        [SerializeField]
        private PokeInteractor _pokeInteractor;

        [SerializeField]
        private SyntheticHand _syntheticHand;

        private bool _isTouching;

        protected bool _started = false;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hand, nameof(Hand));
            this.AssertField(_pokeInteractor, nameof(_pokeInteractor));
            this.AssertField(_syntheticHand, nameof(_syntheticHand));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _pokeInteractor.WhenStateChanged += HandleStateChanged;
                _pokeInteractor.WhenPassedSurfaceChanged += HandlePassedSurfaceChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                if (_isTouching)
                {
                    UnlockWrist();
                }

                _pokeInteractor.WhenStateChanged -= HandleStateChanged;
                _pokeInteractor.WhenPassedSurfaceChanged -= HandlePassedSurfaceChanged;
            }
        }

        private void HandlePassedSurfaceChanged(bool passed)
        {
            CheckPassedSurface();
        }

        private void HandleStateChanged(InteractorStateChangeArgs args)
        {
            CheckPassedSurface();
        }

        private void CheckPassedSurface()
        {
            if (_pokeInteractor.IsPassedSurface)
            {
                LockWrist();
            }
            else
            {
                UnlockWrist();
            }
        }

        protected virtual void LateUpdate()
        {
            UpdateWrist();
        }

        private void LockWrist()
        {
            _isTouching = true;
        }

        private void UnlockWrist()
        {
            _syntheticHand.FreeWrist();
            _isTouching = false;
        }

        private void UpdateWrist()
        {
            if (!_isTouching) return;

            if (!Hand.GetRootPose(out Pose rootPose))
            {
                return;
            }

            Vector3 positionDelta = rootPose.position - _pokeInteractor.Origin;
            Vector3 targetPosePosition = _pokeInteractor.TouchPoint + positionDelta +
                                         _pokeInteractor.Radius *
                                         _pokeInteractor.TouchNormal;
            Pose wristPoseOverride = new Pose(targetPosePosition, rootPose.rotation);

            _syntheticHand.LockWristPose(wristPoseOverride, 1.0f, SyntheticHand.WristLockMode.Full, true, true);
            _syntheticHand.MarkInputDataRequiresUpdate();
        }

        #region Inject

        public void InjectAllHandPokeLimiterVisual(IHand hand, PokeInteractor pokeInteractor,
            SyntheticHand syntheticHand)
        {
            InjectHand(hand);
            InjectPokeInteractor(pokeInteractor);
            InjectSyntheticHand(syntheticHand);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        public void InjectPokeInteractor(PokeInteractor pokeInteractor)
        {
            _pokeInteractor = pokeInteractor;
        }

        public void InjectSyntheticHand(SyntheticHand syntheticHand)
        {
            _syntheticHand = syntheticHand;
        }

        #endregion
    }
}
