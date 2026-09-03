using DG.Tweening;
using Shared.GameDataTypes;
using Shared.GameLogics;
using Shared.Localizers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.GameClients;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.UIs.Stages.HUDs
{
    public class StageNameUI : MonoBehaviour
    {
        [Header("Challenge Info Group")]
        [SerializeField] private Image _challengeTypeLabel;
        [SerializeField] private TextMeshProUGUI _challengeTypeText;

        [Header("Element Icon")]
        [SerializeField] private Image _elementBackground;
        [SerializeField] private Image _elementIcon;

        [Header("Stage Info Group")]
        [SerializeField] private Image _stageNameBackground;
        [SerializeField] private TextMeshProUGUI _stageNumberAndNameText;

        [Header("PlayerStat Description")]
        [SerializeField] private Image _playerStatDescriptionLabel;
        [SerializeField] private TextMeshProUGUI _playerStatDescriptionText;

        [Header("Boss Name Group")]
        [SerializeField] private TMP_Text _bossNameText;
        
        private readonly float _duration = 2.5f;

        private Color _transparentWhiteColor;
        private Color _transparentBlackColor;
        public void Initialize()
        {
            _transparentWhiteColor = new Color(1f, 1f, 1f, 0f);
            _transparentBlackColor = new Color(0f, 0f, 0f, 0f);
            var challengeTypeLabelColor = _challengeTypeLabel.color;
            challengeTypeLabelColor.a = 0f;
            _challengeTypeLabel.color = challengeTypeLabelColor;

            var challengeTypeColor = _challengeTypeText.color;
            challengeTypeColor.a = 0f;
            _challengeTypeText.color = challengeTypeColor;

            var elementBackgroundColor = _elementBackground.color;
            elementBackgroundColor.a = 0f;
            _elementBackground.color = elementBackgroundColor;
            _elementIcon.color = _transparentWhiteColor;

            _stageNumberAndNameText.color = _transparentWhiteColor;
            _stageNameBackground.color = _transparentWhiteColor;

            _playerStatDescriptionLabel.color = _transparentBlackColor;
            _playerStatDescriptionText.color = _transparentWhiteColor;

            _bossNameText.gameObject.SetActive(false);
        }

        //왼쪽 텍스트가 챕터로 무조건 들어가고 있음
        //번호는 스테이지 넘버로 사용중 해당 텍스트는 기획측에서 확인이 필요함
        public void ShowStageNameUI(string stageNumberAndName, ElementType stageElement, bool hideElementBonus)
        {
            this.gameObject.SetActive(true);
            this.Initialize();

            _stageNumberAndNameText.text = stageNumberAndName;
            _elementIcon.sprite = ResourcePool.Instance.LoadResource<Sprite>(stageElement.IconPath());

            string bonusStatText = string.Empty;
            var ElementComparisonResult =  ElementLogic.Compare(GameClient.Stage.PC.StaticData.ElementType, stageElement);
            switch (ElementComparisonResult)
            {
                case ElementComparisonResult.Same:
                case ElementComparisonResult.Equal:
                    bonusStatText = $"<color=#7DFE25>(+{AvatarLogic.EQUAL_BONUS_RATE * 100f:F0}%)</color>";
                    break;
                case ElementComparisonResult.Recessive:
                    bonusStatText = $"(+{AvatarLogic.RECESSIVE_BONUS_RATE * 100f:F0}%)";
                    break;
                case ElementComparisonResult.Dominant:
                    bonusStatText = $"<color=#7DFE25>(+{AvatarLogic.DOMINANT_BONUS_RATE * 100f:F0}%)</color>";
                    break;
                default:
                    break;
            }

            var disappearingAnimation = DOTween.Sequence(this)
                .AppendInterval(_duration)
                .Append(_stageNumberAndNameText.DOFade(0.0f, 0.5f))
                .Join(_stageNameBackground.DOFade(0f, 0.5f));

            if (!hideElementBonus)
            {
                _playerStatDescriptionText.text = string.Format(
                    Localizer.Instance.GetText("UI_CHAPTER_CHALLENGE_RECOMMENDED_ELEMENT_DESCRIPTION"),
                    GetDisplayText(GameClient.Stage.PC.StaticData.ElementType),
                    bonusStatText);

                disappearingAnimation
                    .Join(_elementBackground.DOFade(0f, 0.5f))
                    .Join(_elementIcon.DOFade(0f, 0.5f))
                    .Join(_playerStatDescriptionLabel.DOFade(0.0f, 0.5f))
                    .Join(_playerStatDescriptionText.DOFade(0f, 0.5f));
            }

            disappearingAnimation.OnComplete(() => { this.gameObject.SetActive(false); });
            disappearingAnimation.Pause();

            var appearingAnimation = DOTween.Sequence(this);
            appearingAnimation.AppendInterval(0.5f);
            appearingAnimation.Append(_stageNumberAndNameText.DOFade(1.0f, 0.4f));
            appearingAnimation.Join(_stageNameBackground.DOFade(1.0f, 0.4f));

            if (!hideElementBonus)
            {
                appearingAnimation.AppendInterval(0.2f);
                appearingAnimation.Append(_elementBackground.DOFade(1f, 0.3f));
                appearingAnimation.Join(_elementIcon.DOFade(1f, 0.3f));

                appearingAnimation.AppendInterval(0.2f);
                appearingAnimation.Append(_playerStatDescriptionLabel.DOFade(0.5f, 0.3f));
                appearingAnimation.Join(_playerStatDescriptionText.DOFade(1f, 0.3f));
            }

            appearingAnimation.OnComplete(() => { disappearingAnimation.Restart(); });
        }

        public void ShowBossNameUI(string bossName)
        {
            this.gameObject.SetActive(true);
            _bossNameText.gameObject.SetActive(true);
            _bossNameText.text = bossName;
            
            _bossNameText.color = _transparentWhiteColor;

            var disappearingAnimation = DOTween.Sequence(this)
                .AppendInterval(_duration)
                .Append(_bossNameText.DOFade(0.0f, 0.5f))
                .Join(_stageNameBackground.DOFade(0f, 0.5f))
                .OnComplete(() =>
                {
                    this.gameObject.SetActive(false);
                });
            disappearingAnimation.Pause();
            
            DOTween.Sequence(this)
                .AppendInterval(0.5f)
                .Append(_bossNameText.DOFade(1.0f, 0.5f))
                .Join(_stageNameBackground.DOFade(1.0f, 0.5f))
                .OnComplete(() =>
                {
                    disappearingAnimation.Restart();
                });
        }

        private static string GetDisplayText(ElementType element)
        {
            return element switch
            {
                ElementType.Earth => Localizer.Instance.GetText("UI_ELEMENT_EARTH"),
                ElementType.Wind => Localizer.Instance.GetText("UI_ELEMENT_WIND"),
                ElementType.Fire => Localizer.Instance.GetText("UI_ELEMENT_FIRE"),
                ElementType.Water => Localizer.Instance.GetText("UI_ELEMENT_WATER"),
                _ => string.Empty
            };
        }

    }
}
