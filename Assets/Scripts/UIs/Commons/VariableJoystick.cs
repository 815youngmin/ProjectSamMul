#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 화면 터치 위치에 나타나는 플로팅 가상 조이스틱.
/// 기존에 쓰던 외부 조이스틱 에셋을 대체하는 최소 구현으로, <see cref="Direction"/>만 게임 로직에서 읽는다.
/// </summary>
public class VariableJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform _background = null!;
    [SerializeField] private RectTransform _handle = null!;
    [SerializeField] private float _handleRange = 1f;
    [SerializeField] private float _deadZone = 0.1f;

    private Canvas? _canvas;
    private Camera? _uiCamera;
    private Vector2 _input;

    /// <summary>정규화된 입력 방향. 입력이 없으면 <see cref="Vector2.zero"/>.</summary>
    public Vector2 Direction => _input;

    public void Initialize()
    {
        _canvas = GetComponentInParent<Canvas>();
        _uiCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay ? _canvas.worldCamera : null;
        _background.gameObject.SetActive(false);
        _input = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _background.anchoredPosition = ScreenToAnchoredPosition(eventData.position);
        _background.gameObject.SetActive(true);
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 center = RectTransformUtility.WorldToScreenPoint(_uiCamera, _background.position);
        float radius = _background.sizeDelta.x * 0.5f * (_canvas != null ? _canvas.scaleFactor : 1f);
        Vector2 delta = (eventData.position - center) / Mathf.Max(radius, 1f);

        float magnitude = delta.magnitude;
        if (magnitude < _deadZone)
        {
            _input = Vector2.zero;
        }
        else
        {
            _input = magnitude > 1f ? delta / magnitude : delta;
        }

        _handle.anchoredPosition = _input * _background.sizeDelta * 0.5f * _handleRange;
    }

    public void OnPointerUp(PointerEventData? eventData)
    {
        _input = Vector2.zero;
        _handle.anchoredPosition = Vector2.zero;
        _background.gameObject.SetActive(false);
    }

    private Vector2 ScreenToAnchoredPosition(Vector2 screenPosition)
    {
        var parent = (RectTransform)transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPosition, _uiCamera, out Vector2 local);
        return local;
    }
}
