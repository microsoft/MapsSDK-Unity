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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using System.Linq;

/// <summary>
/// Supports Virtual Keyboard integration by providing the implementation to necessary common patterns
/// </summary>
public class OVRVirtualKeyboard : MonoBehaviour
{
    public enum KeyboardInputMode
    {
        Far = 0,
        Direct = 1,
        Max = 2,
    }

    public abstract class OVRVirtualKeyboardInput : MonoBehaviour
    {
        public OVRInput.Controller InteractionDevice;

        public abstract bool PositionValid { get; }
        public abstract bool IsPressed { get; }
        public abstract OVRPlugin.Posef InputPose { get; }
        public abstract OVRPlugin.Posef InteractorRootPose { get; }

        public virtual void ModifyInteractorRoot(OVRPlugin.Posef interactorRootPose)
        {
        }

        // Conversion helpers
        public Vector3 InputPosePosition => InputPose.Position.FromFlippedZVector3f();
        public Quaternion InputPoseRotation => InputPose.Orientation.FromFlippedZQuatf();
    }

    public static class Events
    {
        public static void Init()
        {
            eventHandler_ = new VirtualKeyboardEventHandler();
            OVRManager.instance.RegisterEventListener(eventHandler_);
        }

        public static void Deinit()
        {
            if (OVRManager.instance != null)
            {
                OVRManager.instance.DeregisterEventListener(eventHandler_);
            }
        }

        /// <summary>
        /// Occurs when text has been committed
        /// @params (string text)
        /// </summary>
        public static event Action<string> CommitText;

        /// <summary>
        /// Occurs when a backspace is pressed
        /// </summary>
        public static event Action Backspace;

        /// <summary>
        /// Occurs when a return key is pressed
        /// </summary>
        public static event Action Enter;

        /// <summary>
        /// Occurs when keyboard is shown
        /// </summary>
        public static event Action KeyboardShown;

        /// <summary>
        /// Occurs when keyboard is hidden
        /// </summary>
        public static event Action KeyboardHidden;

        private static VirtualKeyboardEventHandler eventHandler_;

        private class VirtualKeyboardEventHandler : OVRManager.EventListener
        {
            public void OnEvent(OVRPlugin.EventDataBuffer eventDataBuffer)
            {
                switch (eventDataBuffer.EventType)
                {
                    case OVRPlugin.EventType.VirtualKeyboardCommitText:
                    {
                        CommitText?.Invoke(
                            Encoding.UTF8.GetString(eventDataBuffer.EventData)
                                .Replace("\0", "")
                        );
                        break;
                    }
                    case OVRPlugin.EventType.VirtualKeyboardBackspace:
                    {
                        Backspace?.Invoke();
                        break;
                    }
                    case OVRPlugin.EventType.VirtualKeyboardEnter:
                    {
                        Enter?.Invoke();
                        break;
                    }
                    case OVRPlugin.EventType.VirtualKeyboardShown:
                    {
                        KeyboardShown?.Invoke();
                        break;
                    }
                    case OVRPlugin.EventType.VirtualKeyboardHidden:
                    {
                        KeyboardHidden?.Invoke();
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Unity UI field to automatically commit text into. (optional)
    /// </summary>
    [SerializeField]
    public InputField TextCommitField;

    /// <summary>
    /// Input handlers, which provide pose and other data for each
    /// input device (hand or controller).
    /// </summary>
    [SerializeField]
    public OVRVirtualKeyboardInput[] InputHandlers;

    public Collider Collider { get; private set; }

    /// <summary>
    /// (Internal) Controls which style of input used for interracting with the keyboard
    /// </summary>
    [SerializeField]
    public KeyboardInputMode InputMode = KeyboardInputMode.Far;

    [SerializeField]
    private Shader keyboardModelShader;

    [SerializeField]
    private Shader keyboardModelAlphaBlendShader;

    private bool isKeyboardCreated_ = false;

    private UInt64 keyboardSpace_;
    private float scale_ = 1.0f;

    private Dictionary<ulong, List<Material>> virtualKeyboardTextures_ = new Dictionary<ulong, List<Material>>();
    private OVRGLTFScene virtualKeyboardScene_;
    private ulong virtualKeyboardModelKey_;
    private bool modelInitialized_ = false;
    private bool modelAvailable_ = false;
    private bool keyboardVisible_ = false;

    // Unity event functions

    void Awake()
    {
        Events.Init();

        // Register for events
        Events.CommitText += OnCommitText;
        Events.Backspace += OnBackspace;
        Events.Enter += OnEnter;
        Events.KeyboardShown += OnKeyboardShown;
        Events.KeyboardHidden += OnKeyboardHidden;
    }

    void OnDestroy()
    {
        Events.CommitText -= OnCommitText;
        Events.Backspace -= OnBackspace;
        Events.Enter -= OnEnter;
        Events.KeyboardShown -= OnKeyboardShown;
        Events.KeyboardHidden -= OnKeyboardHidden;
        Events.Deinit();
        DestroyKeyboard();
    }

    void OnEnable()
    {
        ShowKeyboard();
    }

    void OnDisable()
    {
        HideKeyboard();
    }

    public void SuggestVirtualKeyboardLocationForInputMode(KeyboardInputMode inputMode)
    {
        OVRPlugin.VirtualKeyboardLocationInfo locationInfo = new OVRPlugin.VirtualKeyboardLocationInfo();
        switch (inputMode)
        {
            case KeyboardInputMode.Direct:
                locationInfo.locationType = OVRPlugin.VirtualKeyboardLocationType.Direct;
                break;
            case KeyboardInputMode.Far:
                locationInfo.locationType = OVRPlugin.VirtualKeyboardLocationType.Far;
                break;
            default:
                Debug.LogError("Unknown KeyboardInputMode: " + inputMode);
                break;
        }

        var result = OVRPlugin.SuggestVirtualKeyboardLocation(locationInfo);
        if (result != OVRPlugin.Result.Success)
        {
            Debug.LogError("SuggestVirtualKeyboardLocation failed: " + result);
        }
    }


    // Private methods
    private GameObject LoadRuntimeVirtualKeyboardMesh()
    {
        Debug.Log("LoadRuntimeVirtualKeyboardMesh");
        string[] modelPaths = OVRPlugin.GetRenderModelPaths();

        var keyboardPath = modelPaths?.FirstOrDefault(p => p.Equals("/model_fb/virtual_keyboard")
                                                           || p.Equals("/model_meta/keyboard/virtual"));

        if (String.IsNullOrEmpty(keyboardPath))
        {
            Debug.LogError("Failed to find keyboard model.");
            return null;
        }

        OVRPlugin.RenderModelProperties modelProps = new OVRPlugin.RenderModelProperties();
        if (OVRPlugin.GetRenderModelProperties(keyboardPath, ref modelProps))
        {
            if (modelProps.ModelKey != OVRPlugin.RENDER_MODEL_NULL_KEY)
            {
                virtualKeyboardModelKey_ = modelProps.ModelKey;
                byte[] data = OVRPlugin.LoadRenderModel(modelProps.ModelKey);
                if (data != null)
                {
                    OVRGLTFLoader gltfLoader = new OVRGLTFLoader(data);
                    gltfLoader.textureUriHandler = (string rawUri, Material mat) =>
                    {
                        var uri = new Uri(rawUri);
                        // metaVirtualKeyboard://texture/{id}?w={width}&h={height}&ft=RGBA32
                        if (uri.Scheme != "metaVirtualKeyboard" && uri.Host != "texture")
                        {
                            return null;
                        }

                        var textureId = ulong.Parse(uri.LocalPath.Substring(1));
                        if (virtualKeyboardTextures_.ContainsKey(textureId) == false)
                        {
                            virtualKeyboardTextures_[textureId] = new List<Material>();
                        }

                        virtualKeyboardTextures_[textureId].Add(mat);
                        return null; // defer texture data loading
                    };
                    gltfLoader.SetModelShader(keyboardModelShader);
                    gltfLoader.SetModelAlphaBlendShader(keyboardModelAlphaBlendShader);
                    virtualKeyboardScene_ = gltfLoader.LoadGLB(supportAnimation: true, loadMips: true);
                    virtualKeyboardScene_.root.gameObject.name = "OVRVirtualKeyboardModel";
                    PopulateCollision();
                    modelAvailable_ = virtualKeyboardScene_.root != null;
                    return virtualKeyboardScene_.root;
                }
            }
        }

        Debug.LogError("Failed to load model.");
        return null;
    }

    private void PopulateCollision()
    {
        var childrenMeshes = virtualKeyboardScene_.root.GetComponentsInChildren<MeshFilter>();
        GameObject targetGo = virtualKeyboardScene_.root;
        foreach (var mesh in childrenMeshes)
        {
            // prefer collision mesh
            if (mesh.gameObject.name == "collision")
            {
                targetGo = mesh.gameObject;
                break;
            }

            // Fallback to background mesh
            if (mesh.gameObject.name == "background")
            {
                targetGo = mesh.gameObject;
            }
        }

        if (targetGo != virtualKeyboardScene_.root)
        {
            var meshCollider = targetGo.AddComponent<MeshCollider>();
            Collider = meshCollider;
        }
    }

    private void ShowKeyboard()
    {
        if (!isKeyboardCreated_)
        {
            var createInfo = new OVRPlugin.VirtualKeyboardCreateInfo();

            var result = OVRPlugin.CreateVirtualKeyboard(createInfo);
            if (result != OVRPlugin.Result.Success)
            {
                Debug.LogError("Create failed: " + result);
                return;
            }

            // Once created the keyboard should be positioned
            // instead of using a default location, initially use with the unity keyboard root transform
            var locationInfo = ComputeLocation(transform);

            var createSpaceInfo = new OVRPlugin.VirtualKeyboardSpaceCreateInfo();
            createSpaceInfo.pose = OVRPlugin.Posef.identity;
            result = OVRPlugin.CreateVirtualKeyboardSpace(createSpaceInfo, out keyboardSpace_);
            if (result != OVRPlugin.Result.Success)
            {
                Debug.LogError("Create failed to create keyboard space: " + result);
                return;
            }

            result = OVRPlugin.SuggestVirtualKeyboardLocation(locationInfo);
            if (result != OVRPlugin.Result.Success)
            {
                Debug.LogError("Create failed to position keyboard: " + result);
                return;
            }

            // Initialize the keyboard model
            if (modelInitialized_ != true)
            {
                modelInitialized_ = true;
                LoadRuntimeVirtualKeyboardMesh();
                UpdateVisibleState();
            }

            // Should call this whenever the keyboard is created or when the text focus changes
            if (TextCommitField != null)
            {
                result = OVRPlugin.ChangeVirtualKeyboardTextContext(TextCommitField.text);
                if (result != OVRPlugin.Result.Success)
                {
                    Debug.LogError("Failed to set keyboard text context");
                    return;
                }
            }
        }

        try
        {
            SetKeyboardVisibility(true);
            UpdateKeyboardLocation();
            isKeyboardCreated_ = true;
        }
        catch
        {
            DestroyKeyboard();
            throw;
        }
    }

    private void SetKeyboardVisibility(bool visible)
    {
        if (!modelInitialized_)
        {
            // Set active was called before the model was even attempted to be loaded
            return;
        }

        if (!modelAvailable_)
        {
            Debug.LogError("Failed to set visibility. Keyboard model unavailable.");
            return;
        }

        var visibility = new OVRPlugin.VirtualKeyboardModelVisibility();
        visibility.ModelKey = virtualKeyboardModelKey_;
        visibility.Visible = visible;
        var res = OVRPlugin.SetVirtualKeyboardModelVisibility(ref visibility);
        if (res != OVRPlugin.Result.Success)
        {
            Debug.Log("SetVirtualKeyboardModelVisibility failed: " + res);
        }
    }

    private void HideKeyboard()
    {
        if (!modelAvailable_)
        {
            // If model has not been loaded, completely uninitialize
            DestroyKeyboard();
            return;
        }

        SetKeyboardVisibility(false);
    }

    private void DestroyKeyboard()
    {
        if (isKeyboardCreated_)
        {
            if (modelAvailable_)
            {
                GameObject.Destroy(virtualKeyboardScene_.root);
                modelAvailable_ = false;
                modelInitialized_ = false;
            }

            var result = OVRPlugin.DestroyVirtualKeyboard();
            if (result != OVRPlugin.Result.Success)
            {
                Debug.LogError("Destroy failed");
                return;
            }

            Debug.Log("Destroy success");
        }

        isKeyboardCreated_ = false;
    }

    private float MaxElement(Vector3 vec)
    {
        return Mathf.Max(Mathf.Max(vec.x, vec.y), vec.z);
    }

    private OVRPlugin.VirtualKeyboardLocationInfo ComputeLocation(Transform transform)
    {
        OVRPlugin.VirtualKeyboardLocationInfo location = new OVRPlugin.VirtualKeyboardLocationInfo();

        location.locationType = OVRPlugin.VirtualKeyboardLocationType.Custom;
        // Plane in Unity has its normal facing towards camera by default, in runtime it's facing away,
        // so to compensate, flip z for both position and rotation, for both plane and pointer pose.
        location.pose.Position = transform.position.ToFlippedZVector3f();
        location.pose.Orientation = transform.rotation.ToFlippedZQuatf();
        location.scale = MaxElement(transform.localScale);
        return location;
    }

    private void UpdateKeyboardLocation()
    {
        var locationInfo = ComputeLocation(transform);
        var result = OVRPlugin.SuggestVirtualKeyboardLocation(locationInfo);
        if (result != OVRPlugin.Result.Success)
        {
            Debug.LogError("Failed to update keyboard location: " + result);
        }
    }

    void Update()
    {
        if (!isKeyboardCreated_)
        {
            return;
        }

        UpdateInputs();
        SyncKeyboardLocation();
        UpdateAnimationState();
    }

    private void UpdateInputs()
    {
        foreach (OVRVirtualKeyboardInput inputHandler in InputHandlers)
        {
            if (inputHandler.PositionValid)
            {
                var inputInfo = new OVRPlugin.VirtualKeyboardInputInfo();
                switch (inputHandler.InteractionDevice)
                {
                    case OVRInput.Controller.LHand:
                        inputInfo.inputSource = InputMode == KeyboardInputMode.Far
                            ? OVRPlugin.VirtualKeyboardInputSource.HandRayLeft
                            : OVRPlugin.VirtualKeyboardInputSource.HandDirectIndexTipLeft;
                        break;
                    case OVRInput.Controller.LTouch:
                        inputInfo.inputSource = InputMode == KeyboardInputMode.Far
                            ? OVRPlugin.VirtualKeyboardInputSource.ControllerRayLeft
                            : OVRPlugin.VirtualKeyboardInputSource.ControllerDirectLeft;
                        break;
                    case OVRInput.Controller.RHand:
                        inputInfo.inputSource = InputMode == KeyboardInputMode.Far
                            ? OVRPlugin.VirtualKeyboardInputSource.HandRayRight
                            : OVRPlugin.VirtualKeyboardInputSource.HandDirectIndexTipRight;
                        break;
                    case OVRInput.Controller.RTouch:
                        inputInfo.inputSource = InputMode == KeyboardInputMode.Far
                            ? OVRPlugin.VirtualKeyboardInputSource.ControllerRayRight
                            : OVRPlugin.VirtualKeyboardInputSource.ControllerDirectRight;
                        break;
                    default:
                        inputInfo.inputSource = OVRPlugin.VirtualKeyboardInputSource.Invalid;
                        break;
                }

                inputInfo.inputPose = inputHandler.InputPose;
                inputInfo.inputState = 0;
                if (inputHandler.IsPressed)
                {
                    inputInfo.inputState |= OVRPlugin.VirtualKeyboardInputStateFlags.IsPressed;
                }

                var interactorRootPose = inputHandler.InteractorRootPose;
                var result = OVRPlugin.SendVirtualKeyboardInput(inputInfo, ref interactorRootPose);
                inputHandler.ModifyInteractorRoot(interactorRootPose);
            }
        }
    }

    private void SyncKeyboardLocation()
    {
        // If unity transform has updated, sync with runtime
        if (transform.hasChanged)
        {
            // ensure scale uniformity
            var scale = MaxElement(transform.localScale);
            var maxScale = Vector3.one * scale;
            transform.localScale = maxScale;
            UpdateKeyboardLocation();
        }

        // query the runtime for the true position
        if (!OVRPlugin.TryLocateSpace(keyboardSpace_, OVRPlugin.GetTrackingOriginType(), out var keyboardPose))
        {
            Debug.LogError("Failed to locate the virtual keyboard space.");
            return;
        }

        var result = OVRPlugin.GetVirtualKeyboardScale(out var keyboardScale);
        if (result != OVRPlugin.Result.Success)
        {
            Debug.LogError("Failed to get virtual keyboard scale.");
            return;
        }

        transform.SetPositionAndRotation(keyboardPose.Position.FromFlippedZVector3f(), keyboardPose.Orientation.FromFlippedZQuatf());
        transform.localScale = new Vector3(keyboardScale, keyboardScale, keyboardScale);

        if (modelAvailable_)
        {
            virtualKeyboardScene_.root.transform.position = keyboardPose.Position.FromFlippedZVector3f();
            // Rotate to face user
            virtualKeyboardScene_.root.transform.rotation =
                keyboardPose.Orientation.FromFlippedZQuatf() * Quaternion.Euler(0, 180f, 0);
            virtualKeyboardScene_.root.transform.localScale = transform.localScale;
        }
        transform.hasChanged = false;
    }

    private void UpdateAnimationState()
    {
        if (!modelAvailable_)
        {
            return;
        }

        OVRPlugin.GetVirtualKeyboardDirtyTextures(out var dirtyTextures);
        foreach (var textureId in dirtyTextures.TextureIds)
        {
            if (!virtualKeyboardTextures_.TryGetValue(textureId, out var textureMaterials))
            {
                continue;
            }

            var textureData = new OVRPlugin.VirtualKeyboardTextureData();
            OVRPlugin.GetVirtualKeyboardTextureData(textureId, ref textureData);
            if (textureData.TextureByteCountOutput > 0)
            {
                try
                {
                    textureData.TextureBytes = Marshal.AllocHGlobal((int)textureData.TextureByteCountOutput);
                    textureData.TextureByteCapacityInput = textureData.TextureByteCountOutput;
                    OVRPlugin.GetVirtualKeyboardTextureData(textureId, ref textureData);

                    var texBytes = new byte[textureData.TextureByteCountOutput];
                    Marshal.Copy(textureData.TextureBytes, texBytes, 0, (int)textureData.TextureByteCountOutput);

                    var tex = new Texture2D((int)textureData.TextureWidth, (int)textureData.TextureHeight,
                        TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Trilinear;
                    tex.SetPixelData(texBytes, 0);
                    tex.Apply(true /*updateMipmaps*/, true /*makeNoLongerReadable*/);
                    foreach (var material in textureMaterials)
                    {
                        material.mainTexture = tex;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(textureData.TextureBytes);
                }
            }
        }

        var result =
            OVRPlugin.GetVirtualKeyboardModelAnimationStates(virtualKeyboardModelKey_, out var animationStates);
        if (result == OVRPlugin.Result.Success)
        {
            for (var i = 0; i < animationStates.States.Length; i++)
            {
                if (!virtualKeyboardScene_.animationNodeLookup.ContainsKey(animationStates.States[i].AnimationIndex))
                {
                    Debug.LogWarning($"Unknown Animation State Index {animationStates.States[i].AnimationIndex}");
                    continue;
                }

                var animationNodes =
                    virtualKeyboardScene_.animationNodeLookup[animationStates.States[i].AnimationIndex];
                foreach(var animationNode in animationNodes)
                {
                    animationNode.UpdatePose(animationStates.States[i].Fraction, false);
                }
            }
            if (animationStates.States.Length > 0)
            {
                foreach (var morphTargets in virtualKeyboardScene_.morphTargetHandlers)
                {
                    morphTargets.Update();
                }
            }
        }
    }

    private void OnCommitText(string text)
    {
        // TODO: take caret and selection position into account T127712980
        if (TextCommitField != null)
        {
            TextCommitField.text += text;
        }
    }

    private void OnBackspace()
    {
        // TODO: take caret and selection position into account T127712980
        if (TextCommitField == null || TextCommitField.text == String.Empty)
        {
            return;
        }

        string text = TextCommitField.text;
        TextCommitField.text = text.Substring(0, text.Length - 1);
    }

    private void OnEnter()
    {
        // TODO: take caret and selection position into account T127712980
        if (TextCommitField != null && TextCommitField.multiLine)
        {
            TextCommitField.text += "\n";
        }
    }

    private void OnKeyboardShown()
    {
        if (!keyboardVisible_)
        {
            keyboardVisible_ = true;
            UpdateVisibleState();
        }
    }

    private void OnKeyboardHidden()
    {
        if (keyboardVisible_)
        {
            keyboardVisible_ = false;
            UpdateVisibleState();
        }
    }

    private void UpdateVisibleState()
    {
        gameObject.SetActive(keyboardVisible_);
        virtualKeyboardScene_.root.gameObject.SetActive(keyboardVisible_);
    }
}
