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

using Oculus.Interaction.Grab;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace Oculus.Interaction.DistanceReticles
{
    public class ReticleGhostDrawer : InteractorReticle<ReticleDataGhost>
    {
        [SerializeField, Interface(typeof(IHandGrabber), typeof(IHandGrabState), typeof(IInteractorView))]
        private UnityEngine.Object _handGrabber;
        private IHandGrabber HandGrabber { get; set; }
        private IHandGrabState HandGrabSource { get; set; }

        [FormerlySerializedAs("_modifier")]
        [SerializeField]
        private SyntheticHand _syntheticHand;

        [SerializeField, Interface(typeof(IHandVisual))]
        [FormerlySerializedAs("_visualHand")]
        private UnityEngine.Object _handVisual;

        private IHandVisual HandVisual;

        private bool _areFingersFree = true;
        private bool _isWristFree = true;

        protected override IInteractorView Interactor { get; set; }
        protected override Component InteractableComponent => HandGrabber.TargetInteractable as Component;

        private ITrackingToWorldTransformer Transformer;

        protected virtual void Awake()
        {
            HandVisual = _handVisual as IHandVisual;
            HandGrabber = _handGrabber as IHandGrabber;
            HandGrabSource = _handGrabber as IHandGrabState;
            Interactor = _handGrabber as IInteractorView;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(HandGrabber, nameof(HandGrabber));
            this.AssertField(Interactor, nameof(Interactor));
            this.AssertField(HandGrabSource, nameof(HandGrabSource));
            this.AssertField(HandVisual, nameof(HandVisual));
            this.AssertField(_syntheticHand, nameof(_syntheticHand));
            Transformer = _syntheticHand.GetData().Config.TrackingToWorldTransformer;
            this.EndStart(ref _started);
        }

        private void UpdateHandPose(IHandGrabState snapper)
        {
            HandGrabTarget snap = snapper.HandGrabTarget;

            if (snap == null)
            {
                FreeFingers();
                FreeWrist();
                return;
            }

            if (snap.HandPose != null)
            {
                UpdateFingers(snap.HandPose, snapper.GrabbingFingers());
                _areFingersFree = false;
            }
            else
            {
                FreeFingers();
            }

            Pose wristLocalPose = GetWristPose(snap.WorldGrabPose, snapper.WristToGrabPoseOffset);
            Pose wristPose = Transformer != null
                ? Transformer.ToTrackingPose(wristLocalPose)
                : wristLocalPose;
            _syntheticHand.LockWristPose(wristPose, 1f);
            _isWristFree = false;
        }

        private void UpdateFingers(HandPose handPose, HandFingerFlags grabbingFingers)
        {
            Quaternion[] desiredRotations = handPose.JointRotations;
            _syntheticHand.OverrideAllJoints(desiredRotations, 1f);

            for (int fingerIndex = 0; fingerIndex < Constants.NUM_FINGERS; fingerIndex++)
            {
                int fingerFlag = 1 << fingerIndex;
                JointFreedom fingerFreedom = handPose.FingersFreedom[fingerIndex];
                if (fingerFreedom == JointFreedom.Constrained
                    && ((int)grabbingFingers & fingerFlag) != 0)
                {
                    fingerFreedom = JointFreedom.Locked;
                }
                _syntheticHand.SetFingerFreedom((HandFinger)fingerIndex, fingerFreedom);
            }
        }

        private Pose GetWristPose(Pose gripPoint, Pose offset)
        {
            offset.Invert();
            gripPoint.Premultiply(offset);
            return gripPoint;
        }

        private bool FreeFingers()
        {
            if (!_areFingersFree)
            {
                _syntheticHand.FreeAllJoints();
                _areFingersFree = true;
                return true;
            }
            return false;
        }

        private bool FreeWrist()
        {
            if (!_isWristFree)
            {
                _syntheticHand.FreeWrist();
                _isWristFree = true;
                return true;
            }
            return false;
        }

        protected override void Align(ReticleDataGhost data)
        {
            UpdateHandPose(HandGrabSource);
            _syntheticHand.MarkInputDataRequiresUpdate();
        }

        protected override void Draw(ReticleDataGhost data)
        {
            HandVisual.ForceOffVisibility = false;
        }

        protected override void Hide()
        {
            HandVisual.ForceOffVisibility = true;
        }

        #region Inject

        public void InjectAllReticleGhostDrawer(IHandGrabber handGrabber,
            SyntheticHand syntheticHand, IHandVisual visualHand)
        {
            InjectHandGrabber(handGrabber);
            InjectSyntheticHand(syntheticHand);
            InjectVisualHand(visualHand);
        }

        public void InjectHandGrabber(IHandGrabber handGrabber)
        {
            _handGrabber = handGrabber as UnityEngine.Object;
            HandGrabber = handGrabber;
            Interactor = handGrabber as IInteractorView;
            HandGrabSource = handGrabber as IHandGrabState;
        }

        public void InjectSyntheticHand(SyntheticHand syntheticHand)
        {
            _syntheticHand = syntheticHand;
        }

        public void InjectVisualHand(IHandVisual visualHand)
        {
            _handVisual = visualHand as UnityEngine.Object;
            HandVisual = visualHand;
        }
        #endregion
    }
}
