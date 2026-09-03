#nullable enable
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Z.UnityHelpers;
using static UnityEngine.UI.Button;

/// <summary>
/// 누르면 살짝 줄어들고 어두워지는 UI 버튼. 손을 뗀 위치가 버튼 안일 때만 onClick 이 발생합니다.
/// </summary>
[RequireComponent(typeof(Image))]
public class ZButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    private static readonly string CLICK_SOUND_PATH = "Sounds/SoundEffects/UIs/ButtonLobby_SFX.prefab";

    public ButtonClickedEvent onClick { get; set; } = new ButtonClickedEvent();

    public float ButtonDownScalingRate => 0.96f;

    public Image ButtonImage => _buttonImage != null ? _buttonImage : (_buttonImage = this.GetComponent<Image>());
    private Image? _buttonImage;

    public TextMeshProUGUI ButtonText => _buttonText != null ? _buttonText : (_buttonText = this.GetComponentInChildren<TextMeshProUGUI>());
    private TextMeshProUGUI? _buttonText;

    public RectTransform RectTransform => _rectTransform != null ? _rectTransform : (_rectTransform = this.GetComponent<RectTransform>());
    private RectTransform? _rectTransform;

    private Vector3 _originLocalScale;
    private Color _originColor;
    private bool _interactable = true;
    private bool _isInitialized;

    private void InitializeJustOnce()
    {
        if (_isInitialized)
        {
            return;
        }
        _isInitialized = true;

        _originLocalScale = this.transform.localScale;
        _originColor = this.ButtonImage.color;
    }

    public void SetButtonScale(Vector3 originLocalScale)
    {
        this.InitializeJustOnce();
        this.transform.localScale = originLocalScale;
        _originLocalScale = originLocalScale;
    }

    public void SetColor(Color color)
    {
        this.InitializeJustOnce();
        _originColor = color;
        if (_interactable)
        {
            this.ButtonImage.color = color;
        }
    }

    public void SetInteractable(bool interactable) => this.SetInteractable(interactable, willDarkenIfNotInteractable: true);

    /// <param name="willDarkenIfNotInteractable">true 면 비활성 상태일 때 이미지를 회색으로 표시합니다.</param>
    public void SetInteractable(bool interactable, bool willDarkenIfNotInteractable)
    {
        this.InitializeJustOnce();
        _interactable = interactable;
        this.ButtonImage.color = interactable || !willDarkenIfNotInteractable ? _originColor : Color.gray;
    }

    public bool IsActive()
    {
        return this.gameObject.activeSelf && this.enabled && _interactable;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        this.InitializeJustOnce();
        if (!this.IsActive())
        {
            return;
        }

        this.transform.localScale = _originLocalScale * this.ButtonDownScalingRate;
        this.ButtonImage.color = new Color(_originColor.r * 0.6f, _originColor.g * 0.6f, _originColor.b * 0.6f, _originColor.a);

        UnityGlobal.Sounds.PlayBySoundPrefab(CLICK_SOUND_PATH, Vector3.zero);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        this.InitializeJustOnce();
        if (!this.IsActive())
        {
            return;
        }

        this.transform.localScale = _originLocalScale;
        this.ButtonImage.color = _originColor;

        // 스크롤 등으로 드래그 중이면 클릭으로 보지 않는다.
        if (eventData.dragging)
        {
            return;
        }

        if (RectTransformUtility.RectangleContainsScreenPoint(this.RectTransform, eventData.position, eventData.pressEventCamera))
        {
            this.onClick.Invoke();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // 클릭은 OnPointerUp 에서 처리한다. 다른 곳에서 클릭 이벤트를 가로채지 않도록 구현만 해둔다.
    }
}
