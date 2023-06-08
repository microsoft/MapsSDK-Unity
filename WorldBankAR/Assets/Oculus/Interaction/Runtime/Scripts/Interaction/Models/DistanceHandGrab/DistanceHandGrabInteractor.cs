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
using Oculus.Interaction.GrabAPI;
using Oculus.Interaction.Input;
using Oculus.Interaction.Throw;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// The DistanceHandGrabInteractor allows grabbing DistanceHandGrabInteractables at a distance.
    /// It operates with HandGrabPoses to specify the final pose of the hand and manipulate the objects
    /// via IMovements in order to attract them, use them at a distance, etc.
    /// The DistanceHandGrabInteractor uses a IDistantCandidateComputer to detect far-away objects.
    /// </summary>
    public class DistanceHandGrabInteractor :
        PointerInteractor<DistanceHandGrabInteractor, DistanceHandGrabInteractable>
        , IHandGrabState, IHandGrabber, IDistanceInteractor
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        public IHand Hand { get; private set; }

        [SerializeField]
        private HandGrabAPI _handGrabApi;

        [SerializeField]
        private Transform _grabOrigin;

        [Header("Grabbing")]
        [SerializeField]
        private GrabTypeFlags _supportedGrabTypes = GrabTypeFlags.Pinch;

        [SerializeField, Optional]
        private Transform _gripPoint;

        [SerializeField, Optional]
        private Transform _pinchPoint;

        [SerializeField, Interface(typeof(IVelocityCalculator)), Optional]
        private UnityEngine.Object _velocityCalculator;
        public IVelocityCalculator VelocityCalculator { get; set; }

        [SerializeField]
        private DistantCandidateComputer<DistanceHandGrabInteractor, DistanceHandGrabInteractable> _distantCandidateComputer
            = new DistantCandidateComputer<DistanceHandGrabInteractor, DistanceHandGrabInteractable>();

        private HandGrabTarget _currentTarget = new HandGrabTarget();
        private HandGrabResult _cachedResult = new HandGrabResult();

        private IMovement _movement;

        private Pose _wristToGrabAnchorOffset = Pose.identity;
        private Pose _wristPose = Pose.identity;
        private Pose _gripPose = Pose.identity;
        private Pose _pinchPose = Pose.identity;

        private bool _handGrabShouldSelect = false;
        private bool _handGrabShouldUnselect = false;

        private HandGrabbableData _lastInteractableData =
            new HandGrabbableData();

        #region IHandGrabber

        public HandGrabAPI HandGrabApi => _handGrabApi;
        public GrabTypeFlags SupportedGrabTypes => _supportedGrabTypes;
        public IHandGrabbable TargetInteractable => Interactable;

        #endregion

        public Pose Origin => _distantCandidateComputer.Origin;

        public Vector3 HitPoint { get; private set; }

        public IRelativeToRef DistanceInteractable => this.Interactable;

        #region IHandGrabSource

        public virtual bool IsGrabbing => HasSelectedInteractable
            && (_movement == null || _movement.Stopped);

        private float _grabStrength;
        public float FingersStrength => _grabStrength;
        public float WristStrength => _grabStrength;

        public Pose WristToGrabPoseOffset => _wristToGrabAnchorOffset;

        public HandFingerFlags GrabbingFingers() =>
            Grab.HandGrab.GrabbingFingers(this, SelectedInteractable);

        public HandGrabTarget HandGrabTarget { get; private set; }

        #endregion

        #region editor events

        protected virtual void Reset()
        {
            _hand = this.GetComponentInParent<IHand>() as MonoBehaviour;
            _handGrabApi = this.GetComponentInParent<HandGrabAPI>();
        }

        #endregion

        protected override void Awake()
        {
            base.Awake();
            Hand = _hand as IHand;
            VelocityCalculator = _velocityCalculator as IVelocityCalculator;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Hand, nameof(Hand));
            this.AssertField(_handGrabApi, nameof(_handGrabApi));
            this.AssertField(_grabOrigin, nameof(_grabOrigin));
            this.AssertField(_distantCandidateComputer, nameof(_distantCandidateComputer));
            if (_velocityCalculator != null)
            {
                this.AssertField(VelocityCalculator, nameof(VelocityCalculator));
            }

            this.EndStart(ref _started);
        }

        #region life cycle

        protected override void DoPreprocess()
        {
            base.DoPreprocess();

            _wristPose = _grabOrigin.GetPose();

            if (Hand.Handedness == Handedness.Left)
            {
                _wristPose.rotation *= Quaternion.Euler(180f, 0f, 0f);
            }

            if (_gripPoint != null)
            {
                _gripPose = _gripPoint.GetPose();
            }
            if (_pinchPoint != null)
            {
                _pinchPose = _pinchPoint.GetPose();
            }
        }

        protected override void DoHoverUpdate()
        {
            base.DoHoverUpdate();

            _handGrabShouldSelect = false;
            if (Interactable == null)
            {
                HandGrabTarget = null;
                _wristToGrabAnchorOffset = Pose.identity;
                _grabStrength = 0f;
                return;
            }

            _wristToGrabAnchorOffset = GetGrabAnchorOffset(_currentTarget.Anchor, _wristPose);
            _grabStrength = Grab.HandGrab.ComputeHandGrabScore(this, Interactable,
                out GrabTypeFlags hoverGrabTypes);
            HandGrabTarget = _currentTarget;

            if (Interactable != null
                && Grab.HandGrab.ComputeShouldSelect(this, Interactable, out GrabTypeFlags selectingGrabTypes))
            {
                _handGrabShouldSelect = true;
            }
        }

        protected override void DoSelectUpdate()
        {
            DistanceHandGrabInteractable interactable = _selectedInteractable;
            _handGrabShouldUnselect = false;
            if (interactable == null)
            {
                _grabStrength = 0f;
                _currentTarget.Clear();
                _handGrabShouldUnselect = true;
                return;
            }

            _grabStrength = 1f;
            Pose grabPose = PoseUtils.Multiply(_wristPose, _wristToGrabAnchorOffset);
            _movement.UpdateTarget(grabPose);
            _movement.Tick();

            Grab.HandGrab.StoreGrabData(this, interactable, ref _lastInteractableData);
            if (Grab.HandGrab.ComputeShouldUnselect(this, interactable))
            {
                _handGrabShouldUnselect = true;
            }
        }

        protected override void InteractableSelected(DistanceHandGrabInteractable interactable)
        {
            if (interactable == null)
            {
                base.InteractableSelected(interactable);
                return;
            }

            _wristToGrabAnchorOffset = GetGrabAnchorOffset(_currentTarget.Anchor, _wristPose);
            Pose grabPose = PoseUtils.Multiply(_wristPose, _wristToGrabAnchorOffset);
            Pose interactableGrabStartPose = _currentTarget.WorldGrabPose;
            _movement = interactable.GenerateMovement(interactableGrabStartPose, grabPose);
            base.InteractableSelected(interactable);
        }

        protected override void InteractableUnselected(DistanceHandGrabInteractable interactable)
        {
            base.InteractableUnselected(interactable);
            _movement = null;

            ReleaseVelocityInformation throwVelocity = VelocityCalculator != null ?
                VelocityCalculator.CalculateThrowVelocity(interactable.transform) :
                new ReleaseVelocityInformation(Vector3.zero, Vector3.zero, Vector3.zero);
            interactable.ApplyVelocities(throwVelocity.LinearVelocity, throwVelocity.AngularVelocity);
        }

        protected override void HandlePointerEventRaised(PointerEvent evt)
        {
            base.HandlePointerEventRaised(evt);

            if (SelectedInteractable == null)
            {
                return;
            }

            if (evt.Identifier != Identifier &&
                (evt.Type == PointerEventType.Select || evt.Type == PointerEventType.Unselect))
            {
                Pose grabPose = PoseUtils.Multiply(_wristPose, _wristToGrabAnchorOffset);
                if (SelectedInteractable.ResetGrabOnGrabsUpdated)
                {
                    if (SelectedInteractable.CalculateBestPose(grabPose, Hand.Scale, Hand.Handedness,
                        ref _cachedResult))
                    {
                        HandGrabTarget.GrabAnchor anchor = _currentTarget.Anchor;
                        _currentTarget.Set(SelectedInteractable.RelativeTo,
                            SelectedInteractable.HandAlignment, anchor, _cachedResult);
                    }
                }

                Pose fromPose = _currentTarget.WorldGrabPose;
                _movement = SelectedInteractable.GenerateMovement(fromPose, grabPose);
                SelectedInteractable.PointableElement.ProcessPointerEvent(
                    new PointerEvent(Identifier, PointerEventType.Move, fromPose, Data));
            }
        }

        protected override Pose ComputePointerPose()
        {
            if (SelectedInteractable != null)
            {
                return _movement.Pose;
            }

            if (Interactable != null)
            {
                HandGrabTarget.GrabAnchor anchorMode = _currentTarget.Anchor;
                return anchorMode == HandGrabTarget.GrabAnchor.Pinch ? _pinchPose :
                    anchorMode == HandGrabTarget.GrabAnchor.Palm ? _gripPose :
                    _wristPose;
            }

            return _wristPose;
        }
        #endregion


        private Pose GetGrabAnchorPose(DistanceHandGrabInteractable interactable, GrabTypeFlags grabTypes,
            out HandGrabTarget.GrabAnchor anchorMode)
        {
            if (_gripPoint != null && (grabTypes & GrabTypeFlags.Palm) != 0)
            {
                anchorMode = HandGrabTarget.GrabAnchor.Palm;
            }
            else if (_pinchPoint != null && (grabTypes & GrabTypeFlags.Pinch) != 0)
            {
                anchorMode = HandGrabTarget.GrabAnchor.Pinch;
            }
            else
            {
                anchorMode = HandGrabTarget.GrabAnchor.Wrist;
            }

            if (interactable.UsesHandPose())
            {
                return _wristPose;
            }
            else if (anchorMode == HandGrabTarget.GrabAnchor.Pinch)
            {
                return _pinchPose;
            }
            else if (anchorMode == HandGrabTarget.GrabAnchor.Palm)
            {
                return _gripPose;
            }
            else
            {
                return _wristPose;
            }
        }

        private Pose GetGrabAnchorOffset(HandGrabTarget.GrabAnchor anchor, in Pose from)
        {
            if (anchor == HandGrabTarget.GrabAnchor.Pinch)
            {
                return PoseUtils.Delta(from, _pinchPose);
            }
            else if (anchor == HandGrabTarget.GrabAnchor.Palm)
            {
                return PoseUtils.Delta(from, _gripPose);
            }

            return PoseUtils.Delta(from, _wristPose);
        }

        protected override bool ComputeShouldSelect()
        {
            return _handGrabShouldSelect;
        }

        protected override bool ComputeShouldUnselect()
        {
            return _handGrabShouldUnselect;
        }

        public override bool CanSelect(DistanceHandGrabInteractable interactable)
        {
            if (!base.CanSelect(interactable))
            {
                return false;
            }
            if (!interactable.SupportsHandedness(this.Hand.Handedness))
            {
                return false;
            }
            if (!Grab.HandGrab.CouldSelect(this, interactable, out GrabTypeFlags availableGrabTypes))
            {
                return false;
            }

            return true;
        }

        protected override DistanceHandGrabInteractable ComputeCandidate()
        {
            DistanceHandGrabInteractable interactable = _distantCandidateComputer.ComputeCandidate(
               DistanceHandGrabInteractable.Registry, this,out Vector3 bestHitPoint);
            HitPoint = bestHitPoint;

            if (interactable == null)
            {
                return null;
            }

            float fingerScore = 1.0f;
            if (!Grab.HandGrab.ComputeShouldSelect(this, interactable, out GrabTypeFlags selectingGrabTypes))
            {
                fingerScore = Grab.HandGrab.ComputeHandGrabScore(this, interactable, out selectingGrabTypes);
            }

            if (selectingGrabTypes == GrabTypeFlags.None)
            {
                selectingGrabTypes = interactable.SupportedGrabTypes & this.SupportedGrabTypes;
            }

            Pose grabPose = GetGrabAnchorPose(interactable, selectingGrabTypes,
                out HandGrabTarget.GrabAnchor anchorMode);
            Pose worldPose = new Pose(bestHitPoint, grabPose.rotation);
            bool poseFound = interactable.CalculateBestPose(worldPose, Hand.Scale,
                Hand.Handedness,
                ref _cachedResult);

            if (!poseFound)
            {
                return null;
            }

            Pose offset = GetGrabAnchorOffset(anchorMode, grabPose);
            _cachedResult.RelativePose = PoseUtils.Multiply(_cachedResult.RelativePose, offset);
            _currentTarget.Set(interactable.RelativeTo, interactable.HandAlignment, anchorMode, _cachedResult);

            return interactable;
        }

        #region Inject
        public void InjectAllDistanceHandGrabInteractor(HandGrabAPI handGrabApi,
            DistantCandidateComputer<DistanceHandGrabInteractor, DistanceHandGrabInteractable> distantCandidateComputer,
            Transform grabOrigin,
            IHand hand, GrabTypeFlags supportedGrabTypes)
        {
            InjectHandGrabApi(handGrabApi);
            InjectDistantCandidateComputer(distantCandidateComputer);
            InjectGrabOrigin(grabOrigin);
            InjectHand(hand);
            InjectSupportedGrabTypes(supportedGrabTypes);
        }

        public void InjectHandGrabApi(HandGrabAPI handGrabApi)
        {
            _handGrabApi = handGrabApi;
        }

        public void InjectDistantCandidateComputer(
            DistantCandidateComputer<DistanceHandGrabInteractor, DistanceHandGrabInteractable> distantCandidateComputer)
        {
            _distantCandidateComputer = distantCandidateComputer;
        }

        public void InjectGrabOrigin(Transform grabOrigin)
        {
            _grabOrigin = grabOrigin;
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        public void InjectSupportedGrabTypes(GrabTypeFlags supportedGrabTypes)
        {
            _supportedGrabTypes = supportedGrabTypes;
        }

        public void InjectOptionalGripPoint(Transform gripPoint)
        {
            _gripPoint = gripPoint;
        }

        public void InjectOptionalPinchPoint(Transform pinchPoint)
        {
            _pinchPoint = pinchPoint;
        }

        public void InjectOptionalVelocityCalculator(IVelocityCalculator velocityCalculator)
        {
            _velocityCalculator = velocityCalculator as UnityEngine.Object;
            VelocityCalculator = velocityCalculator;
        }
        #endregion
    }
}
