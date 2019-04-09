using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using UnityEngine;

/// <summary>
/// Animates the color and scale of the target MeshRenderer as focus enters and exits.
/// </summary>
public class FocusAnimation : MonoBehaviour, IMixedRealityFocusHandler
{
    [SerializeField]
    private MeshRenderer _targetMeshRenderer = null;

    [SerializeField]
    private float _animationDuration = 0.24f;

    [SerializeField]
    private Color _focusColor = Color.blue;

    private float _targetAnimation = 0.0f;
    private float _currentAnimation = 0.0f;

    private Vector3 _startingLocalScale;
    private Color _startingColor;

    private void Awake()
    {
        Debug.Assert(_targetMeshRenderer != null);
        _startingLocalScale = _targetMeshRenderer.transform.localScale;
        if (_targetMeshRenderer.material.HasProperty("_Color"))
        {
            _startingColor = _targetMeshRenderer.material.color;
        }

        if (_targetMeshRenderer.material.HasProperty("_FaceColor"))
        {
            _targetMeshRenderer.material.SetColor("_FaceColor", new Color(1, 1, 1, 0));
        }
    }

    public void OnBeforeFocusChange(FocusEventData eventData)
    {
    }

    public void OnFocusChanged(FocusEventData eventData)
    {
    }

    public void OnFocusEnter(FocusEventData eventData)
    {
        _targetAnimation = 1.0f;
        StopAllCoroutines();
        StartCoroutine(Animate());
    }

    public void OnFocusExit(FocusEventData eventData)
    {
        _targetAnimation = 0.0f;
        StopAllCoroutines();
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        while(_currentAnimation != _targetAnimation)
        {
            var direction = Mathf.Sign(_targetAnimation - _currentAnimation);
            _currentAnimation += direction * Time.deltaTime / Mathf.Max(_animationDuration, 0.001f);
            _currentAnimation = Mathf.Clamp01(_currentAnimation);

            var localScale = _startingLocalScale * Mathf.Lerp(1.0f, 1.1f, _currentAnimation);
            _targetMeshRenderer.transform.localScale = localScale;

            if (_targetMeshRenderer.material.HasProperty("_Color"))
            {
                var color = Color.Lerp(_startingColor, _focusColor, _currentAnimation);
                _targetMeshRenderer.material.SetColor("_Color", color);
            }

            if (_targetMeshRenderer.material.HasProperty("_FaceColor"))
            {
                var color = Color.Lerp(new Color(1, 1, 1, 0), Color.white, _currentAnimation);
                _targetMeshRenderer.material.SetColor("_FaceColor", color);
            }

            yield return null;
        }
    }
}
