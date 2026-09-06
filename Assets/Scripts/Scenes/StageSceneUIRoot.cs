#nullable enable
using DG.Tweening;
using Shared.CSProtocols.ReqResData.Chapters;
using Shared.GameDataTypes;
using Shared.Localizers;
using Shared.StaticDatas;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SamMul.GameClients;
using SamMul.GameClients.Stages;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using SamMul.UIs;
using SamMul.UIs.Lobbies;
using SamMul.UIs.Stages.HUDs;
using SamMul.UIs.Stages.Popups;

namespace SamMul.Scenes
{
    public class StageSceneUIRoot : BaseSceneUIRoot
    {
        [SerializeField] private VariableJoystick _joystick;
        public VariableJoystick Joystick => _joystick;

        [SerializeField] private StageSceneWalletBarGroup _walletBarGroup;
        public StageSceneWalletBarGroup WalletBarGroup => _walletBarGroup;

        [SerializeField] private ExpBarGroup _expBarGroup;
        public ExpBarGroup ExpBarGroup => _expBarGroup;

        [SerializeField] private StagePCStatGroup _pcStatGroup;
        public StagePCStatGroup PCStatGroup => _pcStatGroup;

        [SerializeField] private StageTimer _stageTimer;
        public StageTimer StageTimer => _stageTimer;

        [SerializeField] private BossHPBarGroup _bossHpBarGroup;
        public BossHPBarGroup BossHPBarGroup => _bossHpBarGroup;

        [SerializeField] private KillCount _killCount;
        public KillCount KillCount => _killCount;

        [SerializeField] private WarningPopup _warningPopup;
        public WarningPopup WarningPopup => _warningPopup;

        [SerializeField] private ZButton _pauseButton;
        [SerializeField] private StageNameUI _stageNameUI;

        [SerializeField] private TopSpace _topSpace;
        public TopSpace TopSpace => _topSpace;

        [SerializeField] private AccelerationButton _accelerationButton;

        [SerializeField] private RectTransform _allyCharacterDisplayerGroup;

        public bool IsSkillSelectorPopupOpened => _skillSelectorPopup != null;
        private SkillSelectorPopup? _skillSelectorPopup;

        public bool IsSkillBoxPopupOpened => _skillBoxPopup != null;
        private SkillBoxPopup? _skillBoxPopup;

        private StageResultPopup? _stageResultPopup;

        private ResurrectionPopup? _resurrectionPopup;

        private PauseMenuPopup? _pauseMenuPopup;

        private BossVSSequencePopup? _bossVSSequencePopup;

        private List<NavigationArrow> _navigationArrows = null!;

        public void Initialize(SceneType sceneType, HeroType heroType, int? chapterNumber)
        {
            this.InitializeBase(sceneType);

            _joystick.gameObject.SetActive(false);
            _joystick.Initialize();

            _walletBarGroup.gameObject.SetActive(false);
            _expBarGroup.gameObject.SetActive(false);
            _expBarGroup.Initialize(heroType);
            _pcStatGroup.gameObject.SetActive(false);
            _stageTimer.gameObject.SetActive(false);
            _bossHpBarGroup.gameObject.SetActive(false);
            _killCount.gameObject.SetActive(false);

            _warningPopup.gameObject.SetActive(false);

            _stageNameUI.gameObject.SetActive(false);
            _stageNameUI.Initialize();

            _skillSelectorPopup = null;
            _skillBoxPopup = null;

            _walletBarGroup.UpdateGoldAmount(0);
            _stageTimer.Initialize();
            _stageTimer.UpdateTimer(0f, 1f);
            _killCount.UpdateKillCount(0);
            _warningPopup.Initialize();
            _topSpace.Initialize(heroType);

            _pauseButton.onClick.RemoveAllListeners();
            _pauseButton.onClick.AddListener(() =>
            {
                this.AddPausePopup();
            });

            _accelerationButton.Initialize(chapterNumber);
            _navigationArrows = new List<NavigationArrow>();

            {
                // 스킬선택 팝업을 캐싱해둔다. 처음 등장시점에 로딩딜레이가 크기 떄문에 캐싱이 필요하다.
                var blocker = ResourcePool.Instance.InstantiateFromResource<UIBlocker>(UI_BLOCKER_RESOURCE_PATH);
                ResourcePool.Instance.PutBackInstance(UI_BLOCKER_RESOURCE_PATH, blocker.gameObject);

                var skillSelectorPopup = ResourcePool.Instance.InstantiateFromResource<SkillSelectorPopup>(SkillSelectorPopup.PREFAB_PATH);
                ResourcePool.Instance.PutBackInstance(SkillSelectorPopup.PREFAB_PATH, skillSelectorPopup.gameObject);
            }
            {
                // 스킬박스 팝업도 캐싱해둔다.
                var skillBoxPopup = ResourcePool.Instance.InstantiateFromResource<SkillBoxPopup>(SkillBoxPopup.PREFAB_PATH);
                ResourcePool.Instance.PutBackInstance(SkillBoxPopup.PREFAB_PATH, skillBoxPopup.gameObject);
            }
        }

        public void Clear()
        {
        }

        public void UpdateLogic()
        {
#if UNITY_ANDROID
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // 백버튼 누른 경우임. 게임 일시정지팝업 띄운다.
                if (!IsPausePopupOpened)
                {
                    this.AddPausePopup();
                }
                else
                {
                    this.ClosePauseMenuPopup(skipAnimation: false);
                }
            }
#endif // UNITY_ANDROID

            for (int i = 0; i < _navigationArrows.Count; ++i)
            {
                _navigationArrows[i].UpdateLogic();
            }

        }

        public void OnPlayerAppearingEnd(PlayerCharacter pc)
        {
            _joystick.gameObject.SetActive(true);
            _walletBarGroup.gameObject.SetActive(true);
            _expBarGroup.gameObject.SetActive(true);
            _stageTimer.gameObject.SetActive(true);
            _pcStatGroup.Initialize((int)0, (int)0, (int)0, pc.LeftResurrectCount);
            _pcStatGroup.gameObject.SetActive(true);
            _killCount.gameObject.SetActive(true);

            this.UpdateExpBar(pc.Level, pc.CurrentExp, pc.ExpToNextLevel);
        }

        public void UpdatePCHP(int currentHp, int maxHp)
        {
            _pcStatGroup.UpdateHP(currentHp, maxHp);
        }
        public void UpdatePCAttackPower(int attackPower)
        {
            _pcStatGroup.UpdateAttackPower(attackPower);
        }
        public void UpdatePCResurrectCount(int currentResurrectCount)
        {
            _pcStatGroup.UpdateResurrectCount(currentResurrectCount);
        }

        public void PlayResurrectSequence(PlayerCharacter pc)
        {
            _pcStatGroup.PlayResurrectSequence(pc);
        }

        public void OnBossStageEventBegin(Stage stage, MonsterStaticData bossStaticData, bool showBossAppearingPopup)
        {
            _stageTimer.DisplayBossName(bossStaticData);
            _expBarGroup.gameObject.SetActive(false);
            _bossHpBarGroup.gameObject.SetActive(true);

            if (showBossAppearingPopup)
            {
                _warningPopup.EndWarning();
                DOTween.Sequence(this).
                    AppendInterval(0.21f).
                    AppendCallback(() =>
                    {
                        this.AddBossVSSequencePopup(bossStaticData.MonsterType, stage.PC.StaticData.HeroType);
                    });
            }
            else
            {
                this.ShowBossNameUI(bossStaticData.MonsterName);
            }
        }

        public void OnBossStageEventEnd()
        {
            _stageTimer.HideBossName();
            _expBarGroup.gameObject.SetActive(true);
            _bossHpBarGroup.gameObject.SetActive(false);

        }

        public void PauseAccelerationButon()
        {
            _accelerationButton.PauseAccelerationButton();
        }
        public void ResumeAccelerationButton()
        {
            _accelerationButton.ResumeAccelerationButton();
        }

        public void AddBossVSSequencePopup(CharacterType bossType, HeroType heroType)
        {
            _bossVSSequencePopup = this.CreateAndAddPopup<BossVSSequencePopup>(BossVSSequencePopup.PREFAB_PATH, true);
            this.PauseResumeOnPopup();
            _bossVSSequencePopup.Initialize(bossType, heroType, CloseBossVSSequencePopup);
        }

        public void CloseBossVSSequencePopup(bool skipAnimation)
        {
            if (_bossVSSequencePopup == null)
            {
                Debug.LogWarning("보스 연출팝업이 없는데 팝업 닫으려했음. 로직 잘못됨 수정하세요.");
                return;
            }

            this.CloseAndDestroyPopup(_bossVSSequencePopup, () =>
            {
                _bossVSSequencePopup = null;
                this.PauseResumeOnPopup();
            }, skipAnimation);
        }
        public void AddSkillSelectorPopup(Stage stage, PlayerCharacter owner, List<SkillKey[]> skillCandidates, SkillKey[] acquiredSkills, SkillId[] banSkillIds)
        {
            if (_skillSelectorPopup != null)
            {
                Debug.LogWarning("스킬선택팝업이 이미 있는데 스킬선택팝업 또 열려고 했음. 로직 잘못됨. 수정하세요.");
                return;
            }

            _skillSelectorPopup = this.CreateAndAddPopup<SkillSelectorPopup>(SkillSelectorPopup.PREFAB_PATH, playSound: true);
            _skillSelectorPopup.InitializeForSkills(stage, owner, skillCandidates, acquiredSkills, banSkillIds, closeRequester: CloseSkillSelectorPopup);
            this.PauseResumeOnPopup();
        }
        public void CloseSkillSelectorPopup(bool skipAnimation)
        {
            if (_skillSelectorPopup == null)
            {
                Debug.LogWarning("스킬팝업이 없는데 스킬팝업 닫으려했음. 로직 잘못됨 수정하세요.");
                return;
            }

            this.CloseAndDestroyPopup(_skillSelectorPopup, () =>
            {
                _skillSelectorPopup = null;
                this.PauseResumeOnPopup();
            }, skipAnimation);
        }

        public void AddSkillBoxPopup(PlayerCharacter owner, int learnSkillCount)
        {
            if (_skillBoxPopup != null)
            {
                Debug.LogWarning("스킬박스팝업이 이미 있는데 스킬박스팝업 또 열려고 했음. 로직 잘못됨. 수정하세요.");
                return;
            }

            var learnedSkills = owner.SelectSkillsToLearnBySkillBox(learnSkillCount);
            var candidateSkills = owner.GetCandidateSkillsToLearnBySkillBox();

            _skillBoxPopup = this.CreateAndAddPopup<SkillBoxPopup>(SkillBoxPopup.PREFAB_PATH, playSound: true);
            _skillBoxPopup.Initialize(owner, learnedSkills, candidateSkills, closeRequester: CloseSkillBoxPopup);
            this.PauseResumeOnPopup();
        }

        public void CloseSkillBoxPopup(bool skipAnimation)
        {
            if (_skillBoxPopup == null)
            {
                Debug.LogWarning("스킬박스팝업이 없는데 스킬박스팝업 닫으려했음. 로직 잘못됨 수정하세요.");
                return;
            }

            this.CloseAndDestroyPopup(_skillBoxPopup, () =>
            {
                _skillBoxPopup = null;
                this.PauseResumeOnPopup();
            }, skipAnimation);
        }

        public void AddMainChapterResultPopup(
            ChapterStaticData chapter,
            long totalGold,
            long totalGem,
            long totalRandomEquipmentElement,
            float stageRunningTime,
            StagePlayResult stagePlayResult,
            long eliminatedBosses,
            long eliminatedElites,
            long eliminatedMonsters)
        {
            if (_stageResultPopup != null)
            {
                Debug.LogError("스테이지 결과창이 이미 있는데 또 열려고 했음. 로직 잘못됨. 수정하세요.");
                return;
            }

            _stageResultPopup = this.CreateAndAddPopup<StageResultPopup>("Stages/UIs/Popups/StageResultPopup/StageResultPopup.prefab", playSound: false);
            _stageResultPopup.InitializeForMainChapterResult(
                chapter,
                stagePlayResult,
                totalGold,
                totalGem,
                totalRandomEquipmentElement,
                stageRunningTime,
                eliminatedBosses,
                eliminatedElites,
                eliminatedMonsters,
                closeRequester: CloseStageResultPopup);

            this.PauseResumeOnPopup();
        }
        public void CloseStageResultPopup(bool skipAnimation)
        {
            if (_stageResultPopup == null)
            {
                Debug.LogWarning("스테이지 결과창이 없는데 닫으려했음. 로직 잘못됨 수정하세요.");
                return;
            }
            this.CloseAndDestroyPopup(_stageResultPopup, () =>
            {
                _stageResultPopup = null;
            }, skipAnimation);
        }
        public void AddResurrectionPopup(int stageNumber, Action addResultPopup)
        {
            if (_resurrectionPopup != null)
            {
                Debug.LogWarning("부활창이 이미 있는데 또 열려고 했음. 로직 잘못됨. 수정하세요.");
                return;
            }

            _resurrectionPopup = this.CreateAndAddPopup<ResurrectionPopup>("Stages/UIs/Popups/ResurrectionPopup/ResurrectionPopup.prefab", playSound: true);
            _resurrectionPopup.Initialize(stageNumber, addResultPopup, closeRequester: CloseResurrectionPopup);
            this.PauseResumeOnPopup();
        }

        public void CloseResurrectionPopup(bool skipAnimation)
        {
            if (_resurrectionPopup == null)
            {
                Debug.LogWarning("부활창이 없는데 닫으려했음. 로직 잘못됨 수정하세요.");
                return;
            }
            this.CloseAndDestroyPopup(_resurrectionPopup, () =>
            {
                _resurrectionPopup = null;
                this.PauseResumeOnPopup();
            }, skipAnimation);
        }
        public bool IsPausePopupOpened => _pauseMenuPopup != null;

        public void AddPausePopup()
        {
            if (_pauseMenuPopup != null)
            {
                Debug.LogWarning("일시정지 메뉴창이 이미 있는데 또 열려고 했음. 로직 잘못됨. 수정하세요.");
                return;
            }

            _pauseMenuPopup = this.CreateAndAddPopup<PauseMenuPopup>("Stages/UIs/Popups/PauseMenuPopup/PauseMenuPopup.prefab", playSound: true);
            _pauseMenuPopup.Initialize(closeRequester: ClosePauseMenuPopup, isChapterZero: GameClient.Stage?.ChapterStaticData?.ChapterNumber == 0);
            this.PauseResumeOnPopup();
        }

        public void ClosePauseMenuPopup(bool skipAnimation)
        {
            if (_pauseMenuPopup == null)
            {
                Debug.LogWarning("일시정지 메뉴창이 없는데 닫으려했음. 로직 잘못됨 수정하세요.");
                return;
            }
            this.CloseAndDestroyPopup(_pauseMenuPopup, () =>
            {
                _pauseMenuPopup = null;
                this.PauseResumeOnPopup();
            }, skipAnimation);
        }

        public void UpdateExpBar(int level, long currentExp, long expToNextLevel)
        {
            _expBarGroup.UpdateLevelExp(level, currentExp, expToNextLevel);
        }

        public void UpdateGold(long currentGold)
        {
            _walletBarGroup.UpdateGoldAmount(currentGold);
        }


        public void UpdateBossHPBar(float currentHP, float maxHP)
        {
            _bossHpBarGroup.UpdateBossHPBar(currentHP, maxHP);
        }
        public void UpdateBossName(string bossName)
        {
            _bossHpBarGroup.UpdateBossName(bossName);
        }

        public void UpdateKillCount(long currentKillCount)
        {
            _killCount.UpdateKillCount(currentKillCount);
        }

        public void OnBeginWarningEvent(WarningPopup.WarningType warningType)
        {
            _warningPopup.StartWarning(warningType);
        }

        public void OnEndWarningEvent()
        {
            _warningPopup.EndWarning();
        }

        public void ShowChapterInfo(string chapterName, int chapterNumber, ElementType elementType)
        {
            string stageNumberAndName = $"{Localizer.Instance.GetText("UI_CHAPTER")} {chapterNumber}. {chapterName}";
            _stageNameUI.ShowStageNameUI(stageNumberAndName, elementType, hideElementBonus: false);
        }

        public void ShowBossNameUI(string bossName)
        {
            _stageNameUI.ShowBossNameUI(bossName);
        }

        /// <summary>
        /// 플레이어 캐릭터의 스탯을 보여줍니다.
        /// </summary>
        /// <param name="stage">
        /// 현재 플레이하고 있는 스테이지입니다.
        /// </param>
        /// <param name="pc">
        /// 스탯을 보여줄 플레이어 캐릭터입니다.
        /// </param>
        /// <returns>
        /// 캐릭터 스탯 증가 연출 재생시간을 반환합니다.
        /// </returns>
        public float DisplayPlayerCharacterStat(Stage stage, PlayerCharacter pc)
        {
            var playerCharacterStatDisplayer = ResourcePool.Instance.InstantiateFromResource<PlayerCharacterStatDisplayer>(PlayerCharacterStatDisplayer.PREFAB_PATH);
            playerCharacterStatDisplayer.transform.SetParent(this.transform);
            playerCharacterStatDisplayer.transform.localPosition = Vector3.zero;
            playerCharacterStatDisplayer.transform.localScale = 0.9f * Vector3.one;
            return playerCharacterStatDisplayer.DisplayPlayerCharacterStat(stage, pc, this);
        }

        /// <summary>
        /// 플레이어 캐릭터의 버프효과와 관련된 택스트를 보여줍니다.
        /// </summary>
        /// <param name="pc">현재 플레이하고 있는 캐릭터입니다.</param>
        /// <param name="buffInfoText">보여줄 텍스트 문구입니다.</param>
        public void DisplayPlayerCharacterBuff(PlayerCharacter pc, string buffInfoText)
        {
            var displayer= ResourcePool.Instance.InstantiateFromResource<PlayerCharacterBuffDisplayer>(PlayerCharacterBuffDisplayer.PREFAB_PATH);
            displayer.transform.SetParent(this.transform);
            displayer.transform.localPosition = Vector3.zero;
            displayer.transform.localScale = Vector3.one;
            displayer.DisplayPlayerCharacterBuff(pc, this, buffInfoText);
        }

        public NavigationArrow AddNavigationArrow(Stage stage, Transform targetTransform, bool showAlways, string prefabPath, string iconText)
        {
            var navigationArrow = ResourcePool.Instance.InstantiateFromResource<NavigationArrow>(prefabPath);
            navigationArrow.transform.SetParent(this.transform);
            navigationArrow.Initialize(() =>
            {
                _navigationArrows.Remove(navigationArrow);
                ResourcePool.Instance.PutBackInstance(prefabPath, navigationArrow.gameObject);
            }, stage, targetTransform, showAlways, iconText);
            _navigationArrows.Add(navigationArrow);

            return navigationArrow;
        }
    }
}
