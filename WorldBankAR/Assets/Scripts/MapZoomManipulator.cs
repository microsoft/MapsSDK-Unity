using System.Collections.Generic;
using Microsoft.Maps.Unity;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Input.UnityInput;
using UnityEngine;

public class MapZoomManipulator : MonoBehaviour
{
    [SerializeField] MapRenderer _mapRenderer;
    [SerializeField] MapInteractionController _mapInteractionCtrl;
    private bool _hasFirstPointerDraggedThisFrame = false;
    private MapZoomPinchLogic _mapZoomPinchLogic;
    private Vector3[] handPositionArray = new Vector3[2];
    private bool isPinching;

    private void OnEnable()
    {
        if(_mapZoomPinchLogic == null)
            _mapZoomPinchLogic = new MapZoomPinchLogic();
    }

    public void ZoomByPinch()
    {
        if(Input.touchCount >= 2)
        {
            handPositionArray[0] = Input.touches[0].position;
            handPositionArray[1] = Input.touches[1].position;
            isPinching = true;
            _mapZoomPinchLogic.Setup(handPositionArray, _mapRenderer.ZoomLevel);
        }
    }

    private void Update()
    {
        if (Input.touchCount >= 2 && isPinching)
        {
            HandleTwoHandManipulationUpdated();
        }
        else
            isPinching = false;
    }

    #region private methods

    private void HandleTwoHandManipulationUpdated()
    {
        float zoomFactor = _mapZoomPinchLogic.GetZoomFactor(handPositionArray);
        Debug.Log("+++ HandleTwoHandManipulationUpdated() zoom= " + zoomFactor);
        _mapInteractionCtrl.Zoom(zoomFactor);
    }



    #endregion
}
