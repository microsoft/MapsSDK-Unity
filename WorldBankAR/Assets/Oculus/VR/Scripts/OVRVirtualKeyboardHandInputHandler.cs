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

using System.Collections;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(OVRHand))]
[RequireComponent(typeof(OVRSkeleton))]
public class OVRVirtualKeyboardHandInputHandler : OVRVirtualKeyboard.OVRVirtualKeyboardInput
{
    public override bool PositionValid =>
        OVRInput.IsControllerConnected(InteractionDevice) &&
        hand.IsTracked &&
        (OVRVirtualKeyboard.InputMode == OVRVirtualKeyboard.KeyboardInputMode.Direct
            ? skeleton.IsDataValid && handIndexTip != null
            : hand.IsPointerPoseValid);

    public override bool IsPressed => OVRInput.Get(
        OVRInput.Button.One, // hand pinch
        InteractionDevice);

    public override OVRPlugin.Posef InputPose
    {
        get
        {
            Transform inputTransform =
                OVRVirtualKeyboard.InputMode == OVRVirtualKeyboard.KeyboardInputMode.Direct
                    ? handIndexTip.Transform
                    : hand.PointerPose;
            return new OVRPlugin.Posef()
            {
                Position = inputTransform.position.ToFlippedZVector3f(),
                // Rotation on the finger tip transform is sideways, use this instead
                Orientation = hand.PointerPose.rotation.ToFlippedZQuatf(),
            };
        }
    }

    public OVRVirtualKeyboard OVRVirtualKeyboard;

    private OVRHand hand;
    private OVRSkeleton skeleton;

    private OVRBone handIndexTip;

    // Poke limiting state
    private bool pendingApply;
    private bool pendingRevert;
    private OVRPlugin.Posef originalInteractorRootPose;
    private OVRPlugin.Posef newInteractorRootPose;
    private OVRPlugin.Posef lastGivenWristRootPose;

    private void Start()
    {
        hand = GetComponent<OVRHand>();
        skeleton = GetComponent<OVRSkeleton>();
    }

    private void Update()
    {
        if (handIndexTip == null && skeleton.IsDataValid)
        {
            handIndexTip = GetSkeletonIndexTip(skeleton);
        }
    }

    private static OVRBone GetSkeletonIndexTip(OVRSkeleton skeleton)
    {
        return skeleton.Bones.First(b => b.Id == OVRSkeleton.BoneId.Hand_IndexTip);
    }

    //
    // Poke limiting logic
    //

    private void LateUpdate()
    {
        ApplyPendingWristRoot();
        if (pendingRevert)
        {
            StartCoroutine(RevertWristRoot());
        }
    }

    private OVRBone wristRoot
    {
        get => limitingReady ? skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_WristRoot] : null;
    }

    private bool limitingReady
    {
        get => hand.IsTracked && hand.IsDataValid && skeleton.IsDataValid;
    }

    public override OVRPlugin.Posef InteractorRootPose
    {
        get
        {
            if (limitingReady)
            {
                lastGivenWristRootPose =  new OVRPlugin.Posef()
                {
                    Position = wristRoot.Transform.position.ToFlippedZVector3f(),
                    Orientation = wristRoot.Transform.rotation.ToFlippedZQuatf(),
                };
                return lastGivenWristRootPose;
            }

            return OVRPlugin.Posef.identity;
        }
    }

    public override void ModifyInteractorRoot(OVRPlugin.Posef interactorRootPose)
    {
        if (!limitingReady ||
            PoseEqualsWristRoot(interactorRootPose))
        {
            // hands are not tracking or if the position or rotation has not actually changed do nothing
            return;
        }

        // if the new pose has changed, mark the apply as pending.
        pendingApply = true;
        newInteractorRootPose = interactorRootPose;
    }

    private void ApplyPendingWristRoot()
    {
        if (!pendingApply)
        {
            return;
        }

        // If another script has also modified the wristRoot since sending to the vk, then abandon updating it
        if (!PoseEqualsWristRoot(lastGivenWristRootPose))
        {
            pendingApply = false;
            return;
        }

        pendingApply = false;
        pendingRevert = true;
        originalInteractorRootPose = InteractorRootPose;
        wristRoot.Transform.position = newInteractorRootPose.Position.FromFlippedZVector3f();
        wristRoot.Transform.rotation = newInteractorRootPose.Orientation.FromFlippedZQuatf();
    }

    private IEnumerator RevertWristRoot()
    {
        if (!pendingRevert)
        {
            yield break;
        }
        yield return new WaitForEndOfFrame();
        // If another script has also modified the wristRoot, then abandon restoring it
        if (!PoseEqualsWristRoot(newInteractorRootPose))
        {
            pendingRevert = false;
            yield break;
        }
        pendingRevert = false;
        wristRoot.Transform.position = originalInteractorRootPose.Position.FromFlippedZVector3f();
        wristRoot.Transform.rotation = originalInteractorRootPose.Orientation.FromFlippedZQuatf();
    }

    private bool PoseEqualsWristRoot(OVRPlugin.Posef pose, float epsilon = 0.0001f)
    {
        var position = pose.Position.FromFlippedZVector3f();
        var rotation = pose.Orientation.FromFlippedZQuatf();
        if ((wristRoot.Transform.position - position).sqrMagnitude > epsilon * epsilon)
        {
            return false;
        }
        if (Quaternion.Angle(wristRoot.Transform.rotation, rotation) > epsilon)
        {
            return false;
        }
        return true;
    }
}
