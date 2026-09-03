using DG.Tweening;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using SamMul.Animations.Placeholder;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.ResourcePools;

namespace SamMul.UIs.Stages.Popups
{
    public class BossVSSequencePopup : BasePopup
    {
        public static readonly string PREFAB_PATH = "Stages/UIs/Popups/BossVSSequencePopup/BossVSSequencePopup.prefab";

        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _bossImage;
        [SerializeField] private Image _playerImage;
        [SerializeField] private SkeletonGraphic _vsAnimation;
        [SerializeField] private Image _backgroundPattern1;
        [SerializeField] private Image _backgroundPattern2;
        [SerializeField] private TextMeshProUGUI _bossDescription;
        [SerializeField] private TextMeshProUGUI _bossName;
        [SerializeField] private TextMeshProUGUI _bossLabelText;
        [SerializeField] private TextMeshProUGUI _playerName;

        private Color _transparentColor;


        public void Initialize(CharacterType bossCharacterType, HeroType playerCharacterType, Action<bool> closeRequester)
        {
            base.InitializeBase(closeRequester);
            _transparentColor = new Color(1.0f, 1.0f, 1.0f, 0f);

            this.SetInitialState(bossCharacterType, playerCharacterType);

        }

        public override void OnPopupOpenAnimationFinished()
        {
            this.PlayVSSequence();
        }

        private void SetInitialState(CharacterType bossCharacterType, HeroType playerCharacterType)
        {
            //보스 정보
            string bossIllustPath = StaticDataRepository.Instance.MonsterImagePaths.Get(bossCharacterType).IllustImagePath;
            Sprite bossSprite = ResourcePool.Instance.LoadResource<Sprite>(bossIllustPath);
            if (bossSprite == null)
            {
                Debug.LogError("보스 일러스트 정보가 없습니다. 확인이 필요합니다.");
            }
            _bossImage.gameObject.SetActive(false);
            _bossImage.sprite = bossSprite;
            _bossImage.color = Color.black;

            _bossName.gameObject.SetActive(true);
            _bossName.text = StaticDataRepository.Instance.Monsters.Get(bossCharacterType).MonsterName.ToUpper();
            _bossName.color = _transparentColor;

            _bossLabelText.gameObject.SetActive(true);
            _bossLabelText.text = "BOSS"; //BOSS는 언어 상관없이 영어로 나오도록 아트팀에서 요청
            _bossLabelText.color = _transparentColor;

            //플레이어 정보
            var playerImagePath = StaticDataRepository.Instance.CharacterImagePaths.Get(playerCharacterType).DatachipIconPath;
            Sprite playerSprite = ResourcePool.Instance.LoadResource<Sprite>(playerImagePath);
            if (playerSprite == null)
            {
                Debug.LogError("플레이어 일러스트 정보가 없습니다. 확인이 필요합니다.");
            }
            _playerImage.gameObject.SetActive(false);
            _playerImage.sprite = playerSprite;
            _playerImage.color = Color.black;

            _playerName.gameObject.SetActive(true);
            _playerName.text = StaticDataRepository.Instance.Heroes.Get(playerCharacterType).HeroName.ToUpper();
            _playerName.color = _transparentColor;

            //기타
            _backgroundImage.color = new Color(0, 0, 0, 0);
            _backgroundPattern1.gameObject.SetActive(true);
            _backgroundPattern1.color = _transparentColor;
            _backgroundPattern2.gameObject.SetActive(true);
            _backgroundPattern2.color = _transparentColor;

            _bossDescription.gameObject.SetActive(false);

            _vsAnimation.gameObject.SetActive(false);
            _vsAnimation.UnscaledTime = true;
        }

        private void PlayVSSequence()
        {
            Sequence sequenceAnimation = DOTween.Sequence();

            Vector3 currentBossImageLocalPos = _bossImage.rectTransform.localPosition;
            Vector3 currentPlayerImageLocalPos = _playerImage.rectTransform.localPosition;

            sequenceAnimation.Append(_backgroundImage.DOFade(0.5f, 0.3f));
            sequenceAnimation.AppendCallback(() =>
            {
                _bossImage.gameObject.SetActive(true);
                _playerImage.gameObject.SetActive(true);
            });

            sequenceAnimation.Append(_bossImage.rectTransform.DOLocalMove(currentBossImageLocalPos - new Vector3(100, 0f), 0.2f).SetEase(Ease.OutQuart).
                From(currentBossImageLocalPos - new Vector3(1000, 0f, 0f)));
            sequenceAnimation.Join(_playerImage.rectTransform.DOLocalMove(currentPlayerImageLocalPos + new Vector3(100, 0f), 0.2f).SetEase(Ease.OutQuart).
                From(currentPlayerImageLocalPos + new Vector3(900, 0f, 0)));

            sequenceAnimation.Append(_bossImage.rectTransform.DOLocalMove(currentBossImageLocalPos, 3.0f).SetEase(Ease.Linear).
                From(currentBossImageLocalPos - new Vector3(100, 0f)));
            sequenceAnimation.Join(_playerImage.rectTransform.DOLocalMove(currentPlayerImageLocalPos, 3.0f).SetEase(Ease.Linear).
                From(currentPlayerImageLocalPos + new Vector3(100, 0f)));

            sequenceAnimation.Insert(0.5f, _bossImage.DOColor(Color.white, 0.2f));
            sequenceAnimation.Join(_playerImage.DOColor(Color.white, 0.2f));

            sequenceAnimation.InsertCallback(0.3f, () =>
            {
                _vsAnimation.gameObject.SetActive(true);
                _vsAnimation.AnimationState.SetAnimation(0, "animation", false);
            });
            sequenceAnimation.Insert(0.3f, _backgroundPattern1.DOFade(1.0f, 0.3f));
            sequenceAnimation.Join(_backgroundPattern2.DOFade(1.0f, 0.3f));

            sequenceAnimation.Insert(1f, _bossName.DOFade(1.0f, 1.2f));
            sequenceAnimation.Join(_playerName.DOFade(1.0f, 1.2f));
            sequenceAnimation.Join(_bossLabelText.DOFade(1.0f, 1.2f));


            sequenceAnimation.Insert(3.0f, _backgroundImage.DOFade(0f, 0.4f));
            sequenceAnimation.Join(_bossImage.DOFade(0f, 0.4f));
            sequenceAnimation.Join(_playerImage.DOFade(0f, 0.4f));
            sequenceAnimation.Join(_bossName.DOFade(0f, 0.4f));
            sequenceAnimation.Join(_playerName.DOFade(0f, 0.4f));
            sequenceAnimation.Join(_bossLabelText.DOFade(0f, 0.4f));
            sequenceAnimation.Join(_backgroundPattern1.DOFade(0f, 0.4f));
            sequenceAnimation.Join(_backgroundPattern2.DOFade(0f, 0.4f));

            sequenceAnimation.AppendCallback(() =>
            {
                this.Close(false);
            });
            sequenceAnimation.SetUpdate(isIndependentUpdate: true);
            sequenceAnimation.Play();
        }
    }
}
