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

using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// This interactor allows sending use-strength values to an interactable to create use interactions.
    /// e.g. Pressing a trigger, squeezing a ball, etc.
    /// In order to calculate the usage strength of a finger it uses a IFingerUseAPI.
    /// This class is also an IHandGrabState, so it can be attached to a SyntheticHand to drive the fingers rotations, it will
    /// lerp between the RelaxedHandGrabPose and TightHandGrabPose provided by the interactable depending on the progress of the action.
    /// </summary>
    public class HandGrabUseInteractor : Interactor<HandGrabUseInteractor, HandGrabUseInteractable>
        , IHandGrabState
    {
        [SerializeField, Optional, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        public IHand Hand { get; private set; }

        [SerializeField, Interface(typeof(IFingerUseAPI))]
        private UnityEngine.Object _useAPI;
        public IFingerUseAPI UseAPI { get; private set; }

        private HandGrabTarget _currentTarget = new HandGrabTarget();
        private HandPose _relaxedHandPose = new HandPose();
        private HandPose _tightHandPose = new HandPose();

        private HandPose _cachedRelaxedHandPose = new HandPose();
        private HandPose _cachedTightHandPose = new HandPose();

        private HandFingerFlags _fingersInUse = HandFingerFlags.None;
        private float[] _fingerUseStrength = new float[Constants.NUM_FINGERS];
        private bool _usesHandPose;
        private bool _handUseShouldSelect;
        private bool _handUseShouldUnselect;

        public HandGrabTarget HandGrabTarget => _currentTarget;
        public bool IsGrabbing => SelectedInteractable != null;
        public float WristStrength => 0f;
        public float FingersStrength => IsGrabbing ? 1f : 0f;
        public Pose WristToGrabPoseOffset => Pose.identity;

        public Action<IHandGrabState> WhenHandGrabStarted { get; set; } = delegate { };
        public Action<IHandGrabState> WhenHandGrabEnded { get; set; } = delegate { };

        protected override bool ComputeShouldSelect()
        {
            return _handUseShouldSelect;
        }

        protected override bool ComputeShouldUnselect()
        {
            return _handUseShouldUnselect || SelectedInteractable == null;
        }

        protected override void Awake()
        {
            base.Awake();
            Hand = _hand as IHand;
            UseAPI = _useAPI as IFingerUseAPI;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(UseAPI, nameof(UseAPI));
            this.EndStart(ref _started);
        }

        protected override void InteractableSelected(HandGrabUseInteractable interactable)
        {
            base.InteractableSelected(interactable);
            StartUsing();
        }

        protected override void InteractableUnselected(HandGrabUseInteractable interactable)
        {
            base.InteractableUnselected(interactable);

            _currentTarget.Clear();
            _fingersInUse = HandFingerFlags.None;
        }

        private void StartUsing()
        {
            HandGrabResult result = new HandGrabResult()
            {
                HasHandPose = true,
                HandPose = _relaxedHandPose
            };

            _currentTarget.Set(SelectedInteractable.transform,
                HandAlignType.AlignOnGrab, HandGrabTarget.GrabAnchor.Wrist, result);
        }

        protected override void DoHoverUpdate()
        {
            base.DoHoverUpdate();
            _handUseShouldSelect = IsUsingInteractable(Interactable);
        }

        protected override void DoSelectUpdate()
        {
            base.DoSelectUpdate();

            if (SelectedInteractable == null)
            {
                return;
            }

            float useStrength = CalculateUseStrength(ref _fingerUseStrength);
            float progress = SelectedInteractable.ComputeUseStrength(useStrength);
            _handUseShouldUnselect = !IsUsingInteractable(Interactable);
            if (_usesHandPose && !_handUseShouldUnselect)
            {
                MoveFingers(ref _fingerUseStrength, progress);
            }
        }

        private bool IsUsingInteractable(HandGrabUseInteractable interactable)
        {
            if (interactable == null)
            {
                return false;
            }

            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;
                if (interactable.UseFingers[finger] == FingerRequirement.Ignored)
                {
                    continue;
                }
                float strength = UseAPI.GetFingerUseStrength(finger);
                if (strength > interactable.StrengthDeadzone)
                {
                    return true;
                }
            }
            return false;
        }

        private float CalculateUseStrength(ref float[] fingerUseStrength)
        {
            float requiredStrength = 1f;
            float optionalStrength = 0;
            bool requiredSet = false;

            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;

                if (SelectedInteractable.UseFingers[finger] == FingerRequirement.Ignored)
                {
                    fingerUseStrength[i] = 0f;
                    continue;
                }

                float strength = UseAPI.GetFingerUseStrength(finger);
                fingerUseStrength[i] = Mathf.Clamp01((strength - SelectedInteractable.UseStrengthDeadZone) / (1f - SelectedInteractable.UseStrengthDeadZone));

                if (SelectedInteractable.UseFingers[finger] == FingerRequirement.Required)
                {
                    requiredSet = true;
                    requiredStrength = Mathf.Min(requiredStrength, fingerUseStrength[i]);
                }
                else if (SelectedInteractable.UseFingers[finger] == FingerRequirement.Optional)
                {
                    optionalStrength = Mathf.Max(optionalStrength, fingerUseStrength[i]);
                }

                if (fingerUseStrength[i] > 0)
                {
                    MarkFingerInUse(finger);
                }
                else
                {
                    UnmarkFingerInUse(finger);
                }
            }

            return requiredSet ? requiredStrength : optionalStrength;
        }

        private void MoveFingers(ref float[] fingerUseProgress, float useProgress)
        {
            for (int i = 0; i < Constants.NUM_FINGERS; i++)
            {
                HandFinger finger = (HandFinger)i;
                float progress = Mathf.Min(useProgress, fingerUseProgress[i]);

                LerpFingerRotation(_relaxedHandPose.JointRotations,
                  _tightHandPose.JointRotations,
                  _currentTarget.HandPose.JointRotations,
                  finger, progress);
            }
        }

        private void MarkFingerInUse(HandFinger finger)
        {
            _fingersInUse = (HandFingerFlags)(((int)_fingersInUse) | (1 << (int)finger));
        }

        private void UnmarkFingerInUse(HandFinger finger)
        {
            _fingersInUse = (HandFingerFlags)(((int)_fingersInUse) & ~(1 << (int)finger));
        }

        private void LerpFingerRotation(Quaternion[] from, Quaternion[] to, Quaternion[] result, HandFinger finger, float t)
        {
            int[] joints = FingersMetadata.FINGER_TO_JOINT_INDEX[(int)finger];
            for (int i = 0; i < joints.Length; i++)
            {
                int jointIndex = joints[i];
                result[jointIndex] = Quaternion.Slerp(from[jointIndex], to[jointIndex], t);
            }
        }

        public HandFingerFlags GrabbingFingers()
        {
            return _fingersInUse;
        }

        protected override HandGrabUseInteractable ComputeCandidate()
        {
            float bestScore = float.NegativeInfinity;
            HandGrabUseInteractable bestCandidate = null;

            _usesHandPose = false;
            var candidates = HandGrabUseInteractable.Registry.List(this);
            foreach (HandGrabUseInteractable candidate in candidates)
            {
                candidate.FindBestHandPoses(Hand != null ? Hand.Scale : 1f,
                    ref _cachedRelaxedHandPose, ref _cachedTightHandPose,
                    out float score);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCandidate = candidate;
                    _relaxedHandPose.CopyFrom(_cachedRelaxedHandPose);
                    _tightHandPose.CopyFrom(_cachedTightHandPose);
                    _usesHandPose = true;
                }
            }

            return bestCandidate;
        }

        #region Inject

        public void InjectAllHandGrabUseInteractor(IFingerUseAPI useApi)
        {
            InjectUseApi(useApi);
        }

        public void InjectUseApi(IFingerUseAPI useApi)
        {
            _useAPI = useApi as UnityEngine.Object;
            UseAPI = useApi;
        }

        public void InjectOptionalHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }


        #endregion
    }
}
