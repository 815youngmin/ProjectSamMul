using Shared.GameDataTypes;
using Shared.Localizers;
using Shared.StaticDatas;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Z.GameClients.Stages;
using Z.GameClients.Stages.Characters.PCs;
using Z.ResourcePools;
using Z.UnityHelpers;
using Random = UnityEngine.Random;

namespace Z.UIs.Stages.Popups
{
    public class SkillSelectorPopup : BasePopup
    {
        public enum RewardType { Gold, Meat }

        public static readonly string PREFAB_PATH = "Stages/UIs/Popups/SkillSelectorPopup/SkillSelectorPopup.prefab";

        [SerializeField] private RectTransform _skillButtonGroup;
        [SerializeField] private List<SkillSelectorButton> _skillButtons;

        [SerializeField] private AcquiredSkillGroup _acquiredSkillGroup;
        [SerializeField] private ZButton _skillRefreshButton;
        [SerializeField] private RectTransform _titleStickerTransform;
        //스티커 이미지 생성위치
        [SerializeField] private RectTransform _backgroundStickerTransform;

        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _levelShadowText;
        [SerializeField] private TextMeshProUGUI _levelupText;
        [SerializeField] private TextMeshProUGUI _selectSkilllText;
        [SerializeField] private TextMeshProUGUI _refeshButtonText;
        [SerializeField] private TextMeshProUGUI _autoPlayMessageText;
        [SerializeField] private Slider _autoPlayLeftTimeSlider;

        [SerializeField] private BanSkillGroup _banSkillGroup;

        private GameObject _backgroundStickerPrefab;

        private List<Sprite> _stickerSprites;
        private List<Image> _dropStickerObjects;
        private List<Image> _pool;
        private bool _isInitialize;

        private float _stickerDownSpeed = 15;
        private float _stickerScaleZeroPositionY;
        private float _stickerCreatedAt;

        private List<SkillKey[]> _skillCandidates;
        private int _currentRefreshCount;

        private bool _isAutoPlay;
        private float _createdAt;

        private static readonly float AUTO_SELECT_WAIT_DURATION_MAX = 15.0f;
        private static readonly float AUTO_SELECT_WAIT_DURATION_MIN = 5f;

        // 전역변수입니다. 계속 유지됩니다. 조심하세요.
        // 지난번 선택이 자동선택이었는지 여부.
        private static bool _didAutoSelectedPreviousTime = false;

        // 이번 스킬팝업의 자동 선택대기시간
        private static float _autoSelectWaitDuration = AUTO_SELECT_WAIT_DURATION_MAX;

        private void Initialize(HeroType heroType, int level, bool isAutoPlay, Action<bool> closeRequester)
        {
            Debug.Assert(_skillButtons.Count == 3);
            base.InitializeBase(closeRequester);

            _isAutoPlay = isAutoPlay;
            _createdAt = Time.unscaledTime;

            _autoPlayMessageText.gameObject.SetActive(_isAutoPlay);
            _autoPlayLeftTimeSlider.gameObject.SetActive(_isAutoPlay);

            if (_isAutoPlay)
            {
                _autoPlayMessageText.text = string.Format(Localizer.Instance.GetText("UI_SKILL_AUTO_SELECT_MESSAGE"), (int)(_autoSelectWaitDuration + 0.99f));
                _autoPlayLeftTimeSlider.value = 1f;
            }

            if (_didAutoSelectedPreviousTime)
            {
                _autoSelectWaitDuration = math.max(AUTO_SELECT_WAIT_DURATION_MIN, _autoSelectWaitDuration * 0.5f);
            }
            else
            {
                _autoSelectWaitDuration = AUTO_SELECT_WAIT_DURATION_MAX;
            }
            _didAutoSelectedPreviousTime = false;

            _stickerSprites = new List<Sprite>();
            var characterImageStaticData = StaticDataRepository.Instance.CharacterImagePaths.CharacterImagePathStaticDatas[heroType];
            foreach (var path in characterImageStaticData.SkillSelectorBackParticleImagePaths)
            {
                var sprite = ResourcePool.Instance.LoadResource<Sprite>(path);
                _stickerSprites.Add(sprite);
            }

            var stickerPrefab = new GameObject("sticker");
            stickerPrefab.transform.SetParent(_backgroundStickerTransform);
            stickerPrefab.transform.localScale = Vector3.one;
            stickerPrefab.transform.localPosition = Vector3.zero;
            stickerPrefab.AddComponent<Image>();
            stickerPrefab.SetActive(false);
            _backgroundStickerPrefab = stickerPrefab;

            //스티커 사라질 위치
            _stickerScaleZeroPositionY = this.GetComponent<RectTransform>().rect.height * -0.65f;
            _stickerDownSpeed = 15;

            GameObject stickerGroup = ResourcePool.Instance.LoadResource<GameObject>(characterImageStaticData.SkillSelectorTitleStickerGroupPath);
            Instantiate(stickerGroup, _titleStickerTransform);

            _dropStickerObjects = new List<Image>();
            _pool = new List<Image>();
            _isInitialize = true;

            _levelText.gameObject.SetActive(true);
            _levelText.text = level.ToString();
            _levelShadowText.gameObject.SetActive(true);
            _levelShadowText.text = level.ToString();
            _levelupText.gameObject.SetActive(level > 1);
            _levelupText.text = Localizer.Instance.GetText("UI_SKILL_SELECT_LEVELUP");
        }

        public void InitializeForSkills(Stage stage, PlayerCharacter owner, List<SkillKey[]> skillCandidates, SkillKey[] acquiredSkills, SkillId[] banSkillIds,  bool isAutoPlay, Action<bool> closeRequester)
        {
            this.Initialize(owner.StaticData.HeroType, owner.Level, isAutoPlay, closeRequester);

            _skillCandidates = skillCandidates;
            _currentRefreshCount = 0;
            this.InitializeSkillButtons(stage, owner, _skillCandidates[_currentRefreshCount]);

            _acquiredSkillGroup.Initialize(acquiredSkills);
            _acquiredSkillGroup.PlayAcquiredAnimationRelatedToSkillCandidates(_skillCandidates[_currentRefreshCount]);
            if (owner != null && _skillCandidates[_currentRefreshCount].Length > 2)
            {
                _skillRefreshButton.gameObject.SetActive(owner.IsCanSelectSkillRefesh);
            }
            else
            {
                _skillRefreshButton.gameObject.SetActive(false);
            }

            if(banSkillIds == null || banSkillIds.Length == 0)
            {
                _banSkillGroup.gameObject.SetActive(false);
            }
            else
            {
                _banSkillGroup.gameObject.SetActive(true);
                _banSkillGroup.Initialize(banSkillIds);
            }

            _skillRefreshButton.onClick.RemoveAllListeners();
            _skillRefreshButton.onClick.AddListener(() =>
            {
                if (false == _skillRefreshButton.gameObject.activeSelf)
                {
                    return;
                }
                _currentRefreshCount++;
                this.InitializeSkillButtons(stage, owner, _skillCandidates[_currentRefreshCount]);
                _acquiredSkillGroup.PlayAcquiredAnimationRelatedToSkillCandidates(_skillCandidates[_currentRefreshCount]);
                owner.SetUsedSelectSkillRefesh();
                _skillRefreshButton.gameObject.SetActive(owner.IsCanSelectSkillRefesh);

            });
            _skillRefreshButton.transform.DOButtonInitialTween(delay: 0.11f, scaleUpOffset: new Vector3(0.1f, 0.1f, 0.1f));

            _selectSkilllText.gameObject.SetActive(true);
            _selectSkilllText.text = Localizer.Instance.GetText("UI_SKILL_SELECT");
            _refeshButtonText.gameObject.SetActive(true);
            _refeshButtonText.text = Localizer.Instance.GetText("UI_REFRESH_BUTTON");
        }

        //스티커 생성
        private void CreateDropSticker()
        {
            if (_stickerSprites.Count <= 0)
            {
                Debug.LogWarning("스킬 선택창에 준비된 스티커가 없습니다. 확인이 필요합니다.");
                return;
            }

            GameObject stickerObject;
            if (_pool.Count <= 0)
            {
                stickerObject = Instantiate(_backgroundStickerPrefab, _backgroundStickerTransform);
            }
            else
            {
                stickerObject = _pool[_pool.Count - 1].gameObject;
                _pool.RemoveAt(_pool.Count - 1);
            }
            int randomSpriteIndex = Random.Range(0, _stickerSprites.Count);
            Image image = stickerObject.GetComponent<Image>();
            image.sprite = _stickerSprites[randomSpriteIndex];
            image.rectTransform.sizeDelta = image.sprite.rect.size * Random.Range(0.7f, 1f);
            image.rectTransform.localPosition = _backgroundStickerTransform.rect.position + new Vector2(Random.Range(0, _backgroundStickerTransform.rect.width), Random.Range(0, _backgroundStickerTransform.rect.height));
            image.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-90, 90));
            stickerObject.SetActive(true);

            _dropStickerObjects.Add(image);
        }

        //스티커들 이동, 스케일 처리
        private void MoveDownAndScaleChangeStickers()
        {
            for (int i = 0; i < _dropStickerObjects.Count; i++)
            {
                float stickerY = _dropStickerObjects[i].rectTransform.anchoredPosition.y > 0 ? 0 : _dropStickerObjects[i].rectTransform.anchoredPosition.y;
                float scale = Mathf.Lerp(0f, 1f, 1 - stickerY / _stickerScaleZeroPositionY);

                _dropStickerObjects[i].rectTransform.position -= new Vector3(0, _stickerDownSpeed * Time.unscaledDeltaTime, 0);
                _dropStickerObjects[i].rectTransform.localScale = Vector3.one * scale;
            }
        }

        //제거해야되는 스티커 pool에 넣는작업
        private void RemoveDropStickerPushPool()
        {
            for (int i = 0; i < _dropStickerObjects.Count; i++)
            {
                if (_dropStickerObjects[i].rectTransform.anchoredPosition.y < _stickerScaleZeroPositionY)
                {
                    _dropStickerObjects[i].gameObject.SetActive(false);
                    _pool.Add(_dropStickerObjects[i]);
                    _dropStickerObjects.RemoveAt(i);
                    i--;
                }
            }
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            if (!_isInitialize)
            {
                return;
            }

            if (now > _stickerCreatedAt + 0.1f)
            {
                for (int i = 0; i < 3; i++)
                {
                    this.CreateDropSticker();
                }
                _stickerCreatedAt = now;
            }

            this.MoveDownAndScaleChangeStickers();
            this.RemoveDropStickerPushPool();

            if (_isAutoPlay)
            {
                float leftTime = _autoSelectWaitDuration - (now - _createdAt);

                if (leftTime <= 0f)
                {
                    int activeSkillButtons = _skillButtons.Count(x => x.isActiveAndEnabled);
                    if (activeSkillButtons <= 0)
                    {
                        return;
                    }

                    int randomIndex = Random.Range(minInclusive: 0, maxExclusive: activeSkillButtons);
                    var selectedButton = _skillButtons[randomIndex];
                    if (!selectedButton.isActiveAndEnabled)
                    {
                        selectedButton = _skillButtons.FirstOrDefault(x => x.isActiveAndEnabled);
                    }

                    selectedButton.InvokeSelectEvent();
                    _didAutoSelectedPreviousTime = true;
                }
                else
                {
                    _autoPlayMessageText.text = string.Format(Localizer.Instance.GetText("UI_SKILL_AUTO_SELECT_MESSAGE"), (int)(leftTime + 0.99f));
                    _autoPlayLeftTimeSlider.value = 1.0f - (leftTime / _autoSelectWaitDuration);
                }
            }
        }

        public override void Close(bool skipAnimation)
        {
            foreach (var skillButton in _skillButtons)
            {
                skillButton.gameObject.SetActive(false);
            }
            base.Close(skipAnimation);
        }

        private void InitializeSkillButtons(Stage stage, PlayerCharacter owner, SkillKey[] skillCandidates)
        {
            // 획득 가능한 스킬이 없으면 골드와 고기를 띄워준다.
            if (skillCandidates.Length <= 0)
            {
                _skillButtons[0].Initialize(owner, RewardType.Meat, parentCloser: this.Close);
                _skillButtons[0].gameObject.SetActive(true);

                if (stage.IsAbleToGiveLevelUpBonusGold())
                {
                    _skillButtons[1].Initialize(owner, RewardType.Gold, parentCloser: this.Close);
                    _skillButtons[1].gameObject.SetActive(true);
                }
                else
                {
                    _skillButtons[1].gameObject.SetActive(false);
                }

                _skillButtons[2].gameObject.SetActive(false);
                return;
            }

            for (int i = 0; i < _skillButtons.Count; ++i)
            {
                if (i < skillCandidates.Length)
                {
                    var skillKey = skillCandidates[i];
                    var skillStaticData = StaticDataRepository.Instance.Skills.Get(skillKey);
                    _skillButtons[i].Initialize(owner, skillStaticData, parentCloser: this.Close);
                    _skillButtons[i].gameObject.SetActive(true);
                }
                else
                {
                    _skillButtons[i].gameObject.SetActive(false);
                }
            }

            _skillButtons[0].transform.DOButtonInitialTween(delay: 0.21f, scaleUpOffset: new Vector3(0.06f, 0.06f, 0.06f));
            _skillButtons[1].transform.DOButtonInitialTween(delay: 0.24f, scaleUpOffset: new Vector3(0.06f, 0.06f, 0.06f));
            _skillButtons[2].transform.DOButtonInitialTween(delay: 0.27f, scaleUpOffset: new Vector3(0.06f, 0.06f, 0.06f));
        }

    }
}
