using DG.Tweening;
using Shared.Localizers;
using SamMul.Animations.Placeholder;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.Scenes;
using SamMul.UnityHelpers;

// TODO : PopUpBase 상속받고, RootSceneUI에서 잘 초기화하도록 처리할 것 
public class WarningPopup : MonoBehaviour
{
    public enum WarningType
    {
        None,
        Rush,
        Boss,
        StageEffectMeteor,
        StageEffectIceArea,
        StageEffectSandArea,
        StageEffectLightning,
    }

    [SerializeField] private Image _warningLineUpImage;
    [SerializeField] private Image _warningLineDownImage;
    [SerializeField] private Image _leftBackground;
    [SerializeField] private Image _rightBackground;

    [SerializeField] private SkeletonGraphic _warningBoss;
    [SerializeField] private TextMeshProUGUI _warningBossText;
    [SerializeField] private TextMeshProUGUI _warningRushText;
    [SerializeField] private TextMeshProUGUI _warningStageEffectText;

    private Sequence _beginSequence;
    private Sequence _endSequence;

    private Sequence _repeatLineMoveSequence;
    private Sequence _repeatLineFadeSequence;
    private Sequence _repeatBackgroundSequence;
    private Sequence _repeatTextScalingSequence;

    private bool _isDisplaying;

    public void Initialize()
    {
        Color oldColor = _warningLineDownImage.color;

        _beginSequence = DOTween.Sequence(this.transform);
        _beginSequence.Append(_warningLineUpImage.DOFade(oldColor.a, 0.3f));  
        _beginSequence.Join(_warningLineDownImage.DOFade(oldColor.a, 0.3f));
        _beginSequence.SetRecyclable(true);
        _beginSequence.SetAutoKill(false);
        _beginSequence.Pause();

        _repeatLineMoveSequence = DOTween.Sequence(this.transform);
        _repeatLineMoveSequence.Append(_warningLineUpImage.transform.DOLocalMoveX(-3647, 30.0f));
        _repeatLineMoveSequence.Join(_warningLineDownImage.transform.DOLocalMoveX(-3647, 30.0f));
        _repeatLineMoveSequence.SetLoops(-1);
        _repeatLineMoveSequence.SetRecyclable(true);
        _repeatLineMoveSequence.SetAutoKill(false);
        _repeatLineMoveSequence.Pause();

        _repeatLineFadeSequence = DOTween.Sequence(this.transform);
        _repeatLineFadeSequence.Append(_warningLineUpImage.DOFade(0.4f, 0.3f).From(1f));
        _repeatLineFadeSequence.Join(_warningLineDownImage.DOFade(0.4f, 0.3f).From(1f));
        _repeatLineFadeSequence.Append(_warningLineUpImage.DOFade(1.0f, 0.3f));
        _repeatLineFadeSequence.Join(_warningLineDownImage.DOFade(1.0f, 0.3f));
        _repeatLineFadeSequence.SetLoops(-1);
        _repeatLineFadeSequence.SetRecyclable(true);
        _repeatLineFadeSequence.SetAutoKill(false);
        _repeatLineFadeSequence.Pause();


        _repeatBackgroundSequence = DOTween.Sequence();
        _repeatBackgroundSequence.Append(_leftBackground.DOColor(Color.black, 0.3f).From(new Color(0.427451f, 0.07450981f, 0.03137255f)));
        _repeatBackgroundSequence.Join(_rightBackground.DOColor(Color.black, 0.3f).From(new Color(0.427451f, 0.07450981f, 0.03137255f)));
        _repeatBackgroundSequence.AppendInterval(0.2f);
        _repeatBackgroundSequence.Append(_leftBackground.DOColor(new Color(0.427451f, 0.07450981f, 0.03137255f), 0.3f));
        _repeatBackgroundSequence.Join(_rightBackground.DOColor(new Color(0.427451f, 0.07450981f, 0.03137255f), 0.3f));
        _repeatBackgroundSequence.SetLoops(-1);
        _repeatBackgroundSequence.SetRecyclable(true);
        _repeatBackgroundSequence.SetAutoKill(false);
        _repeatBackgroundSequence.Pause();


        _repeatTextScalingSequence = DOTween.Sequence(this.transform);
        _repeatTextScalingSequence.Append(_warningBossText.transform.DOScale(new Vector2(1.1f, 1.1f), 0.3f));
        _repeatTextScalingSequence.Join(_warningRushText.transform.DOScale(new Vector2(1.1f, 1.1f), 0.3f));
        _repeatTextScalingSequence.Join(_warningStageEffectText.transform.DOScale(new Vector2(1.1f, 1.1f), 0.3f));
        _repeatTextScalingSequence.Join(_warningBossText.DOFade(1.0f, 0.3f).From(0.6f));
        _repeatTextScalingSequence.Join(_warningRushText.DOFade(1.0f, 0.3f).From(0.6f));
        _repeatTextScalingSequence.Join(_warningStageEffectText.DOFade(1.0f, 0.3f).From(0.6f));
        _repeatTextScalingSequence.Append(_warningBossText.transform.DOScale(new Vector2(1.0f, 1.0f), 0.3f));
        _repeatTextScalingSequence.Join(_warningRushText.transform.DOScale(new Vector2(1.0f, 1.0f), 0.3f));
        _repeatTextScalingSequence.Join(_warningStageEffectText.transform.DOScale(new Vector2(1.0f, 1.0f), 0.3f));
        _repeatTextScalingSequence.Join(_warningBossText.DOFade(0.6f, 0.3f));
        _repeatTextScalingSequence.Join(_warningRushText.DOFade(0.6f, 0.3f));
        _repeatTextScalingSequence.Join(_warningStageEffectText.DOFade(0.6f, 0.3f));
        _repeatTextScalingSequence.SetLoops(-1);
        _repeatTextScalingSequence.SetRecyclable(true);
        _repeatTextScalingSequence.SetAutoKill(false);
        _repeatTextScalingSequence.Pause();

        var endSequence = DOTween.Sequence(this.transform);
        endSequence.AppendInterval(0.5f);
        endSequence.OnComplete(() =>
        {
            this.OnCompleteEndSequence();
        });

        endSequence.SetRecyclable(true);
        endSequence.SetAutoKill(false);
        endSequence.Pause();

        _endSequence = endSequence;

        _warningBossText.text = Localizer.Instance.GetText("UI_WARNING_BOSS");
        _warningRushText.text = Localizer.Instance.GetText("UI_WARNING_RUSH_MONSTER");
        _isDisplaying = false;
    }

    public void StartWarning(WarningType warningType)
    {
        switch (warningType)
        {
            case WarningType.Rush:
                {
                    _warningLineUpImage.gameObject.SetActive(true);
                    _warningLineDownImage.gameObject.SetActive(true);
                    _warningBoss.gameObject.SetActive(false);

                    _warningRushText.gameObject.SetActive(true);
                    _warningBossText.gameObject.SetActive(false);
                    _warningStageEffectText.gameObject.SetActive(false);
                }
                break;
            case WarningType.Boss:
                {
                    var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
                    stageSceneUI.TopSpace.OnBossWarning();

                    _warningLineUpImage.gameObject.SetActive(false);
                    _warningLineDownImage.gameObject.SetActive(true);
                    _warningBoss.gameObject.SetActive(true);
                    _warningBoss.AnimationState.ClearTracks();
                    _warningBoss.AnimationState.SetAnimation(0, "boss", true);

                    _warningBossText.gameObject.SetActive(true);
                    _warningRushText.gameObject.SetActive(false);
                    _warningStageEffectText.gameObject.SetActive(false);
                }
                break;
            case WarningType.StageEffectMeteor:
                {
                    _warningLineUpImage.gameObject.SetActive(false);
                    _warningLineDownImage.gameObject.SetActive(false);
                    _warningBoss.gameObject.SetActive(false);

                    _warningBossText.gameObject.SetActive(false);
                    _warningRushText.gameObject.SetActive(false);
                    _warningStageEffectText.gameObject.SetActive(true);

                    _warningStageEffectText.text = Localizer.Instance.GetText("UI_WARNING_STAGE_EFFECT_METEOR");
                }
                break;
            case WarningType.StageEffectIceArea:
                {
                    _warningLineUpImage.gameObject.SetActive(false);
                    _warningLineDownImage.gameObject.SetActive(false);
                    _warningBoss.gameObject.SetActive(false);

                    _warningBossText.gameObject.SetActive(false);
                    _warningRushText.gameObject.SetActive(false);
                    _warningStageEffectText.gameObject.SetActive(true);

                    _warningStageEffectText.text = Localizer.Instance.GetText("UI_WARNING_STAGE_EFFECT_ICE");
                }
                break;
            case WarningType.StageEffectSandArea:
                {
                    _warningLineUpImage.gameObject.SetActive(false);
                    _warningLineDownImage.gameObject.SetActive(false);
                    _warningBoss.gameObject.SetActive(false);

                    _warningBossText.gameObject.SetActive(false);
                    _warningRushText.gameObject.SetActive(false);
                    _warningStageEffectText.gameObject.SetActive(true);

                    _warningStageEffectText.text = Localizer.Instance.GetText("UI_WARNING_STAGE_EFFECT_SAND");
                }
                break;
            case WarningType.StageEffectLightning:
                {
                    _warningLineUpImage.gameObject.SetActive(false);
                    _warningLineDownImage.gameObject.SetActive(false);
                    _warningBoss.gameObject.SetActive(false);

                    _warningBossText.gameObject.SetActive(false);
                    _warningRushText.gameObject.SetActive(false);
                    _warningStageEffectText.gameObject.SetActive(true);

                    _warningStageEffectText.text = Localizer.Instance.GetText("UI_WARNING_STAGE_EFFECT_LIGHTNING");

                }
                break;
            default:
                {
                    throw new InvalidEnumArgumentException();
                }
        }

        _beginSequence.Restart();
        _repeatTextScalingSequence.Restart();
        _repeatLineMoveSequence.Restart();
        _repeatLineFadeSequence.Restart();
        _repeatBackgroundSequence.Restart();

        this.gameObject.SetActive(true);
        _isDisplaying = true;
    }

    public void EndWarning()
    {
        if (_isDisplaying == false)
        {
            return;
        }
        _isDisplaying = true;
        _endSequence.Restart();
    }
    
    private void OnCompleteEndSequence()
    {
        _repeatLineMoveSequence.Pause();
        _repeatTextScalingSequence.Pause();
        _repeatBackgroundSequence.Pause();
        _repeatLineFadeSequence.Pause();
        var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
        stageSceneUI.TopSpace.OffBossWarning();

        this.gameObject.SetActive(false);
    }
}
