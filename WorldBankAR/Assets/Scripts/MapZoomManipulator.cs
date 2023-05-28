using System.Collections.Generic;
using Microsoft.Maps.Unity;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Input.UnityInput;
using UnityEngine;

public class MapZoomManipulator : MonoBehaviour, IMixedRealityPointerHandler
{
    [SerializeField] MapRenderer _mapRenderer;
    [SerializeField] MapInteractionController _mapInteractionCtrl;
    private bool _hasFirstPointerDraggedThisFrame = false;
    private Vector3[] _handPositionMap = null;
    private List<PointerData> pointerDataList = new List<PointerData>();
    private MapZoomPinchLogic _mapZoomPinchLogic;

    /// <summary>
    /// Holds the pointer and the initial intersection point of the pointer ray
    /// with the object on pointer down in pointer space
    /// </summary>
    private readonly struct PointerData
    {
        public PointerData(IMixedRealityPointer pointer, Vector3 worldGrabPoint) : this()
        {
            initialGrabPointInPointer = Quaternion.Inverse(pointer.Rotation) * (worldGrabPoint - pointer.Position);
            Pointer = pointer;
            IsNearPointer = pointer is IMixedRealityNearPointer;
        }

        private readonly Vector3 initialGrabPointInPointer;

        public IMixedRealityPointer Pointer { get; }

        public bool IsNearPointer { get; }

        /// <summary>
        /// Returns the grab point on the manipulated object in world space.
        /// </summary>
        public Vector3 GrabPoint => (Pointer.Rotation * initialGrabPointInPointer) + Pointer.Position;
    }

    private void Awake()
    {
        _mapZoomPinchLogic = new MapZoomPinchLogic();
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
        
    }

    public virtual void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        //Debug.Log("+++ OnPointerDown " + eventData.Pointer.PointerId);
        if (//eventData.used ||
            eventData.Pointer == null ||
            eventData.Pointer.Result == null)
        {
            return;
        }

        if (!TryGetPointerDataWithId(eventData.Pointer.PointerId, out _))
        {
            pointerDataList.Add(new PointerData(eventData.Pointer, eventData.Pointer.Result.Details.Point));
            Debug.Log("+++ added pointer ID " + eventData.Pointer.PointerId);
        }

        if (pointerDataList.Count > 0)
        {
            // Always mark the pointer data as used to prevent any other behavior to handle pointer events
            // as long as the MapZoomManipulator is active.
            // This is due to us reacting to both "Select" and "Grip" events.
            eventData.Use();
        }

        Vector3[] handPositionArray = GetHandPositionArray();
        _mapZoomPinchLogic.Setup(handPositionArray, _mapRenderer.ZoomLevel);
    }

    public virtual void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
        Debug.Log("+++ eventData.pointerID " + eventData.Pointer.PointerId);

        if (_hasFirstPointerDraggedThisFrame)
        {
            HandleTwoHandManipulationUpdated();
            _hasFirstPointerDraggedThisFrame = false;
        }
        else
        {
            _hasFirstPointerDraggedThisFrame = true;
        }
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        _hasFirstPointerDraggedThisFrame = false;
        if (TryGetPointerDataWithId(eventData.Pointer.PointerId, out PointerData pointerDataToRemove))
        {
            pointerDataList.Remove(pointerDataToRemove);
        }
    }

    #region private methods

    private bool TryGetPointerDataWithId(uint id, out PointerData pointerData)
    {
        int pointerDataListCount = pointerDataList.Count;
        for (int i = 0; i < pointerDataListCount; i++)
        {
            PointerData data = pointerDataList[i];
            if (data.Pointer.PointerId == id)
            {
                pointerData = data;
                return true;
            }
        }

        pointerData = default(PointerData);
        return false;
    }

    private void HandleTwoHandManipulationUpdated()
    {
        Vector3[] handPositionArray = GetHandPositionArray();
        Debug.Log("+++ handPositionArray length= " + handPositionArray.Length);

        float zoomFactor = _mapZoomPinchLogic.GetZoomFactor(handPositionArray);
        Debug.Log("+++ HandleTwoHandManipulationUpdated() zoom= " + zoomFactor);
        _mapInteractionCtrl.Zoom(zoomFactor);
    }

    private Vector3[] GetHandPositionArray()
    {
        if (_handPositionMap?.Length != pointerDataList.Count)
        {
            _handPositionMap = new Vector3[pointerDataList.Count];
        }

        uint index = 0;
        int pointerDataListCount = pointerDataList.Count;

        for (int i = 0; i < pointerDataListCount; i++)
        {
            _handPositionMap[index++] = pointerDataList[i].Pointer.Position;
        }
        return _handPositionMap;
    }

    #endregion
}
