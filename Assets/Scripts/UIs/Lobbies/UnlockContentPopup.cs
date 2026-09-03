using DG.Tweening;
using Shared.GameDataTypes;
using Shared.Localizers;
using Shared.StaticDatas;
using SamMul.Animations.Placeholder;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.ResourcePools;
using Sequence = DG.Tweening.Sequence;

public class UnlockContentPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _messageText;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Button _backgroundButton;
    [SerializeField] private Image _lockerBackground;
    [SerializeField] private SkeletonGraphic _lockerIconSkeletonGraphic;

    [SerializeField] private ParticleSystem _unlockEffect;
    [SerializeField] private ParticleSystem _unlockGlow;

    private Sequence _lockShakingSequence;
    private Sequence _iconMoveSequence;

    private Action _onIconMoveSequenceCompleted;

    /// <summary>
    /// 콘텐트 해금 팝업을 초기화합니다.
    /// </summary>
    /// <param name="contentName">
    /// 해금할 콘텐트의 이름입니다.
    /// </param>
    /// <param name="targetSprite">
    /// 해금할 콘텐트의 스프라이트입니다. 아이콘에 이 스프라이트가 보여집니다.
    /// </param>
    /// <param name="sizeDelta">
    /// 아이콘의 사이즈 델타입니다.
    /// </param>
    /// <param name="initialPosition">
    /// 아이콘의 초기 위치입니다.
    /// </param>
    /// <param name="initialLocalScale">
    /// 아이콘의 초기 로컬 스케일입니다.
    /// </param>
    /// <param name="targetPosition">
    /// 아이콘의 목표 위치입니다.
    /// </param>
    /// <param name="targetLocalScale">
    /// 아이콘이 목표 로컬 스케일입니다.
    /// </param>
    /// <param name="onIconMoveSequenceCompleted">
    /// 아이콘 이동이 끝나고 나서 호출되는 콜백입니다.
    /// </param>
    private void Initialize(
        string contentName,
        Sprite targetSprite,
        Vector2 sizeDelta,
        Vector3 initialPosition,
        Vector3 initialLocalScale,
        Vector3 targetPosition,
        Vector3 targetLocalScale,
        Action onIconMoveSequenceCompleted)
    {
        this.gameObject.SetActive(true);
        this.transform.SetAsLastSibling();

        _iconImage.sprite = targetSprite;
        _iconImage.SetNativeSize();

        _iconImage.rectTransform.sizeDelta = sizeDelta;
        _iconImage.rectTransform.position = initialPosition;
        _iconImage.rectTransform.localScale = initialLocalScale;

        _lockerBackground.sprite = targetSprite;
        _lockerBackground.rectTransform.anchorMax = _iconImage.rectTransform.anchorMin;
        _lockerBackground.rectTransform.sizeDelta = _iconImage.rectTransform.sizeDelta;

        _backgroundButton.enabled = true;
        _backgroundButton.onClick.RemoveAllListeners();
        _backgroundButton.onClick.AddListener(() => this.PlayIconMoveSequence(targetPosition, targetLocalScale));

        _messageText.text = $"{contentName} {Localizer.Instance.GetText("UI_CONTENT_OPEN_POPUP_MESSAGE")}";
        _onIconMoveSequenceCompleted = onIconMoveSequenceCompleted;

        this.PlayLockShakingSequence();
    }

    /// <summary>
    /// 자물쇠가 흔들리는 연출을 재생합니다.
    /// </summary>
    private void PlayLockShakingSequence()
    {
        _unlockGlow.gameObject.SetActive(false);
        _unlockGlow.Stop(true);
        _unlockEffect.gameObject.SetActive(false);
        _unlockEffect.Stop(true);
        _messageText.gameObject.SetActive(false);

        _lockerBackground.color = new Color32(0, 0, 0, 150);
        _lockerIconSkeletonGraphic.gameObject.SetActive(true);
        _lockerIconSkeletonGraphic.Skeleton.SetToSetupPose();
        _lockerIconSkeletonGraphic.color = Color.white;

        TrackEntry trackEntry = _lockerIconSkeletonGraphic.AnimationState.SetAnimation(0, "lock", false);
        _lockerIconSkeletonGraphic.AnimationState.AddEmptyAnimation(0, 0, 0);
        trackEntry.Complete += (TrackEntry trackEntry) =>
        {
            _lockerIconSkeletonGraphic.color = Color.clear;
            _lockerBackground.color = Color.clear;
        };

        _lockShakingSequence = DOTween.Sequence();
        _lockShakingSequence.AppendInterval(trackEntry.Animation.Duration - _unlockEffect.main.duration);
        _lockShakingSequence.AppendCallback(() =>
        {
            _unlockEffect.gameObject.SetActive(true);
            _unlockEffect.Play(true);
        });
        _lockShakingSequence.AppendInterval(_unlockEffect.main.duration);
        _lockShakingSequence.AppendCallback(PlayUnlockGlowAndShowMessageText);
    }

    /// <summary>
    /// 자물쇠가 깨지면 후광을 활성화하고 메시지를 보여줍니다.
    /// </summary>
    private void PlayUnlockGlowAndShowMessageText()
    {
        _unlockGlow.gameObject.SetActive(true);
        _unlockGlow.Play();
        _messageText.gameObject.SetActive(true);
    }

    /// <summary>
    /// 아이콘이 이동하는 연출을 재생합니다.
    /// </summary>
    private void PlayIconMoveSequence(Vector3 targetPosition, Vector3 targetLocalScale)
    {
        _backgroundButton.enabled = false;
        _lockShakingSequence.Kill(false);

        _lockerIconSkeletonGraphic.AnimationState.SetEmptyAnimation(0, 0.0f);
        _lockerIconSkeletonGraphic.color = Color.clear;
        _lockerBackground.color = Color.clear;
        this.PlayUnlockGlowAndShowMessageText();

        _iconMoveSequence = DOTween.Sequence();
        _iconMoveSequence.AppendInterval(0.5f);
        _iconMoveSequence.AppendCallback(() =>
        {
            _unlockGlow.gameObject.SetActive(false);
            _messageText.gameObject.SetActive(false);
        });
        _iconMoveSequence.Append(_iconImage.rectTransform.DOMove(targetPosition, 0.2f));
        _iconMoveSequence.Join(_iconImage.rectTransform.DOScale(targetLocalScale, 0.2f));
        _iconMoveSequence.AppendInterval(0.5f);
        _iconMoveSequence.OnComplete(() =>
        {
            this.gameObject.SetActive(false);
            _onIconMoveSequenceCompleted?.Invoke();
        });
    }
}
