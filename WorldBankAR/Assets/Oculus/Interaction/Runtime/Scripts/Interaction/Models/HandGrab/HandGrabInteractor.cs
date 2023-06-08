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
using System;
using UnityEngine;

namespace Oculus.Interaction.HandGrab
{
    /// <summary>
    /// The HandGrabInteractor allows grabbing objects while having the hands snap to them
    /// adopting a previously authored HandPose.
    /// There are different snapping techniques available, and when None is selected it will
    /// behave as a normal GrabInteractor.
    /// </summary>
    public class HandGrabInteractor : PointerInteractor<HandGrabInteractor, HandGrabInteractable>,
        IHandGrabState, IRigidbodyRef, IHandGrabber
    {
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        public IHand Hand { get; private set; }

        [SerializeField]
        private Rigidbody _rigidbody;

        [SerializeField]
        private HandGrabAPI _handGrabApi;

        [SerializeField]
        private GrabTypeFlags _supportedGrabTypes = GrabTypeFlags.All;

        [SerializeField]
        private bool _hoverOnZeroStrength = false;
        public bool HoverOnZeroStrength
        {
            get
            {
                return _hoverOnZeroStrength;
            }
            set
            {
                _hoverOnZeroStrength = value;
            }
        }

        [SerializeField, Optional]
        private Transform _gripPoint;
        [SerializeField, Optional]
        private SphereCollider _gripCollider;
        [SerializeField, Optional]
        private Transform _pinchPoint;
        [SerializeField, Optional]
        private SphereCollider _pinchCollider;

        [SerializeField, Interface(typeof(IVelocityCalculator)), Optional]
        private UnityEngine.Object _velocityCalculator;
        public IVelocityCalculator VelocityCalculator { get; set; }

        private HandGrabTarget _currentTarget = new HandGrabTarget();
        private HandGrabResult _cachedResult = new HandGrabResult();
        private IMovement _movement;
        private Pose _wristToGrabAnchorOffset = Pose.identity;
        private Pose _wristPose = Pose.identity;
        private Pose _gripPose = Pose.identity;
        private Pose _pinchPose = Pose.identity;

        private HandGrabbableData _lastInteractableData = new HandGrabbableData();
        private bool _handGrabShouldSelect = false;
        private bool _handGrabShouldUnselect = false;

        private HandGrabInteractable _selectedInteractableOverride;

        #region IHandGrabber
        public HandGrabAPI HandGrabApi => _handGrabApi;
        public GrabTypeFlags SupportedGrabTypes => _supportedGrabTypes;
        public IHandGrabbable TargetInteractable => Interactable;
        #endregion

        #region IHandGrabSource

        public virtual bool IsGrabbing => HasSelectedInteractable
            && (_movement == null || _movement.Stopped);

        private float _grabStrength;
        public float FingersStrength => _grabStrength;
        public float WristStrength => _grabStrength;

        public Pose WristToGrabPoseOffset => _wristToGrabAnchorOffset;

        public HandFingerFlags GrabbingFingers()
        {
            return Grab.HandGrab.GrabbingFingers(this, SelectedInteractable);
        }

        public HandGrabTarget HandGrabTarget { get; private set; }
        #endregion

        #region IRigidbodyRef
        public Rigidbody Rigidbody => _rigidbody;
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

            this.AssertField(Rigidbody, nameof(Rigidbody));
            Collider[] colliders = Rigidbody.GetComponentsInChildren<Collider>();

            this.AssertCollectionField(colliders, nameof(colliders),
                $"The associated {AssertUtils.Nicify(nameof(Rigidbody))} must have at least one Collider.");

            foreach (Collider collider in colliders)
            {
                this.AssertIsTrue(collider.isTrigger,
                    $"Associated Colliders in the {AssertUtils.Nicify(nameof(Rigidbody))} must be marked as Triggers.");
            }
            if (_pinchPoint != null)
            {
                this.AssertField(_pinchCollider, nameof(_pinchCollider),
                    whyItFailed: "When using Pinch Point a Pinch Collider is required.");
            }
            if (_gripPoint != null)
            {
                this.AssertField(_gripCollider, nameof(_gripCollider),
                    whyItFailed: "When using Grip Point a Grip Collider is required.");
            }

            this.AssertField(_handGrabApi, nameof(_handGrabApi));
            this.AssertField(Hand, nameof(Hand));

            if (_velocityCalculator != null)
            {
                this.AssertField(VelocityCalculator, nameof(VelocityCalculator));
            }

            this.EndStart(ref _started);
        }

        #region life cycle

        /// <summary>
        /// During the update event, move the current interactor (containing also the
        /// trigger for detecting nearby interactableS) to the tracked position of the grip.
        ///
        /// That is the tracked wrist plus a pregenerated position and rotation offset.
        /// </summary>
        protected override void DoPreprocess()
        {
            base.DoPreprocess();

            Hand.GetRootPose(out _wristPose);
            if (_gripPoint != null)
            {
                _gripPose = _gripPoint.GetPose();
            }
            if (_pinchPoint != null)
            {
                _pinchPose = _pinchPoint.GetPose();
            }

            this.transform.SetPose(_wristPose);
        }

        /// <summary>
        /// Each call while the interactor is hovering, it checks whether there is an interaction
        /// being hovered and sets the target snap pose to it. In the HandToObject snapping
        /// behaviors this is relevant as the hand can approach the object progressively even before
        /// a true grab starts.
        /// </summary>
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


        /// <summary>
        /// Each call while the hand is selecting/grabbing an interactable, it moves the item to the
        /// new position while also attracting it towards the hand if the snapping mode requires it.
        ///
        /// In some cases the parameter can be null, for example if the selection was interrupted
        /// by another hand grabbing the object. In those cases it will come out of the release
        /// state once the grabbing gesture properly finishes.
        /// </summary>
        /// <param name="interactable">The selected item</param>
        protected override void DoSelectUpdate()
        {
            HandGrabInteractable interactable = _selectedInteractable;
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

        /// <summary>
        /// When a new interactable is selected, start the grab at the ideal point. When snapping is
        /// involved that can be a point in the interactable offset from the hand
        /// which will be stored to progressively reduced it in the next updates,
        /// effectively attracting the object towards the hand.
        /// When no snapping is involved the point will be the grip point of the hand directly.
        /// Note: ideally this code would be in InteractableSelected but it needs
        /// to be called before the object is marked as active.
        /// </summary>
        /// <param name="interactable">The selected interactable</param>
        protected override void InteractableSelected(HandGrabInteractable interactable)
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

        /// <summary>
        /// When releasing an active interactable, calculate the releasing point in similar
        /// fashion to  InteractableSelected
        /// </summary>
        /// <param name="interactable">The released interactable</param>
        protected override void InteractableUnselected(HandGrabInteractable interactable)
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
                    if (SelectedInteractable.CalculateBestPose(grabPose, Hand.Scale,
                        Hand.Handedness, ref _cachedResult))
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

        private Pose GetGrabAnchorPose(HandGrabInteractable interactable, GrabTypeFlags grabTypes,
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

        public override bool CanSelect(HandGrabInteractable interactable)
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

        /// <summary>
        /// Compute the best interactable to snap to. In order to do it the method measures
        /// the score from the current grip pose to the closes pose in the surfaces
        /// of each one of the interactables in the registry.
        /// Even though it returns the best interactable, it also saves the entire Snap pose to
        /// it in which the exact pose within the surface is already recorded to avoid recalculations
        /// within the same frame.
        /// </summary>
        /// <returns>The best interactable to snap the hand to.</returns>
        protected override HandGrabInteractable ComputeCandidate()
        {
            var interactables = HandGrabInteractable.Registry.List(this);
            float bestFingerScore = float.NegativeInfinity;
            GrabPoseScore bestPoseScore = GrabPoseScore.Max;
            HandGrabInteractable bestInteractable = null;

            foreach (HandGrabInteractable interactable in interactables)
            {
                float fingerScore = 1.0f;
                if (!Grab.HandGrab.ComputeShouldSelect(this, interactable, out GrabTypeFlags selectingGrabTypes))
                {
                    fingerScore = Grab.HandGrab.ComputeHandGrabScore(this, interactable, out selectingGrabTypes);
                }

                if (fingerScore < bestFingerScore)
                {
                    continue;
                }

                if (selectingGrabTypes == GrabTypeFlags.None)
                {
                    if (_hoverOnZeroStrength)
                    {
                        selectingGrabTypes = interactable.SupportedGrabTypes & this.SupportedGrabTypes;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (_gripCollider != null
                    && (selectingGrabTypes & GrabTypeFlags.Palm) != 0
                    && !OverlapsSphere(interactable, _gripCollider))
                {
                    selectingGrabTypes &= ~GrabTypeFlags.Palm;
                }

                if (_pinchCollider != null
                    && (selectingGrabTypes & GrabTypeFlags.Pinch) != 0
                    && !OverlapsSphere(interactable, _pinchCollider))
                {
                    selectingGrabTypes &= ~GrabTypeFlags.Pinch;
                }

                if (selectingGrabTypes == GrabTypeFlags.None)
                {
                    continue;
                }

                Pose grabPose = GetGrabAnchorPose(interactable, selectingGrabTypes,
                    out HandGrabTarget.GrabAnchor anchorMode);
                bool poseFound = interactable.CalculateBestPose(grabPose, Hand.Scale,
                    Hand.Handedness, ref _cachedResult);

                if (!poseFound)
                {
                    continue;
                }

                GrabPoseScore poseScore = _cachedResult.Score;
                if (fingerScore > bestFingerScore
                    || poseScore.IsBetterThan(bestPoseScore))
                {
                    Pose offset = GetGrabAnchorOffset(anchorMode, grabPose);
                    _cachedResult.RelativePose = PoseUtils.Multiply(_cachedResult.RelativePose, offset);
                    _currentTarget.Set(interactable.RelativeTo, interactable.HandAlignment, anchorMode, _cachedResult);

                    bestFingerScore = fingerScore;
                    bestPoseScore = poseScore;
                    bestInteractable = interactable;
                }
            }

            if (bestFingerScore < 0)
            {
                bestInteractable = null;
                _currentTarget.Clear();
            }

            return bestInteractable;
        }

        public void ForceSelect(HandGrabInteractable interactable)
        {
            _selectedInteractableOverride = interactable;
            SetComputeCandidateOverride(() => interactable);
            SetComputeShouldSelectOverride(() => ReferenceEquals(interactable, Interactable));
            SetComputeShouldUnselectOverride(() => !ReferenceEquals(interactable, SelectedInteractable), false);
        }

        public override void SetComputeCandidateOverride(Func<HandGrabInteractable> computeCandidate, bool shouldClearOverrideOnSelect = true)
        {
            base.SetComputeCandidateOverride(() =>
            {
                HandGrabInteractable interactable = computeCandidate.Invoke();
                ComputeTargetForInteractable(interactable);
                return interactable;
            },
            shouldClearOverrideOnSelect);
        }

        public void ForceRelease()
        {
            _selectedInteractableOverride = null;
            ClearComputeCandidateOverride();
            ClearComputeShouldSelectOverride();

            if (State == InteractorState.Select)
            {
                SetComputeShouldUnselectOverride(() => true);
            }
            else
            {
                ClearComputeShouldUnselectOverride();
            }
        }

        public override void Unselect()
        {
            if (State == InteractorState.Select
                && _selectedInteractableOverride != null
                && (SelectedInteractable == _selectedInteractableOverride
                    || SelectedInteractable == null))
            {
                _selectedInteractableOverride = null;
                ClearComputeShouldUnselectOverride();
            }
            base.Unselect();
        }

        private void ComputeTargetForInteractable(HandGrabInteractable interactable, GrabTypeFlags grabType = GrabTypeFlags.All)
        {
            Pose grabPose = GetGrabAnchorPose(interactable,
                interactable.SupportedGrabTypes & this.SupportedGrabTypes & grabType,
                out HandGrabTarget.GrabAnchor anchorMode);

            interactable.CalculateBestPose(grabPose,
                Hand.Scale, Hand.Handedness, ref _cachedResult);
            Pose offset = GetGrabAnchorOffset(anchorMode, grabPose);
            _cachedResult.RelativePose = PoseUtils.Multiply(_cachedResult.RelativePose, offset);
            _currentTarget.Set(interactable.RelativeTo, interactable.HandAlignment, anchorMode, _cachedResult);
        }

        private bool OverlapsSphere(HandGrabInteractable interactable, SphereCollider sphere)
        {
            Vector3 point = sphere.transform.position;
            float radius = sphere.bounds.extents.x;

            foreach (Collider collider in interactable.Colliders)
            {
                if (collider.enabled &&
                    Collisions.IsSphereWithinCollider(point, radius, collider))
                {
                    return true;
                }
            }

            return false;
        }

        #region Inject

        public void InjectAllHandGrabInteractor(HandGrabAPI handGrabApi,
            IHand hand, Rigidbody rigidbody, GrabTypeFlags supportedGrabTypes)
        {
            InjectHandGrabApi(handGrabApi);
            InjectHand(hand);
            InjectRigidbody(rigidbody);
            InjectSupportedGrabTypes(supportedGrabTypes);
        }

        public void InjectHandGrabApi(HandGrabAPI handGrabAPI)
        {
            _handGrabApi = handGrabAPI;
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        public void InjectRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        public void InjectSupportedGrabTypes(GrabTypeFlags supportedGrabTypes)
        {
            _supportedGrabTypes = supportedGrabTypes;
        }

        public void InjectOptionalGripPoint(Transform gripPoint)
        {
            _gripPoint = gripPoint;
        }

        public void InjectOptionalGripCollider(SphereCollider gripCollider)
        {
            _gripCollider = gripCollider;
        }

        public void InjectOptionalPinchPoint(Transform pinchPoint)
        {
            _pinchPoint = pinchPoint;
        }

        public void InjectOptionalPinchCollider(SphereCollider pinchCollider)
        {
            _pinchCollider = pinchCollider;
        }

        public void InjectOptionalVelocityCalculator(IVelocityCalculator velocityCalculator)
        {
            _velocityCalculator = velocityCalculator as UnityEngine.Object;
            VelocityCalculator = velocityCalculator;
        }

        #endregion
    }
}
