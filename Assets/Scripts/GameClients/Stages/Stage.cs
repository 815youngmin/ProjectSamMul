#nullable enable
using DG.Tweening;
using Shared.CSProtocols.ReqResData.Chapters;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using Shared.UserDatas;
using SamMul.Animations.Placeholder;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using SamMul.GameClients.Stages.AreaEffectObjects;
using SamMul.GameClients.Stages.AreaIndicators;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.Actions;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.Characters.StatusEffects;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.GameClients.Stages.DamagePopups;
using SamMul.GameClients.Stages.DeadEffectObjects;
using SamMul.GameClients.Stages.EnvironmentObjects;
using SamMul.GameClients.Stages.IndicatorObjects;
using SamMul.GameClients.Stages.ItemObjects;
using SamMul.GameClients.Stages.Particles;
using SamMul.GameClients.Stages.ProjectileObjects;
using SamMul.GameClients.Stages.StageEvents;
using SamMul.ResourcePools;
using SamMul.Scenes;
using SamMul.UIs.Stages.HUDs;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages
{
    // partial 클래스의 멤버는 생성자가 정의된 기본 코드파일에 정의해주세요. 
    // 이 클래스의 경우, Stage.cs가 멤버를 정의할 기본 코드 파일입니다.
    public partial class Stage
    {
        public readonly StageType StageType;
        // 메인챕터 생성에 필요한 데이터
        // 메인챕터인 경우 ChapterStaticData는 null일 수 없다.
        public readonly ChapterStaticData? ChapterStaticData;

        public readonly ElementType StageElementType;
        public readonly StageStaticData StaticData;
        private readonly StaticDataRepository _staticDatas;

        public float AdditionalMaxHPWeight => _additionalMaxHPWeight;
        private float _additionalMaxHPWeight;
        public float AdditionalAttackPowerWeight => _additionalAttackPowerWeight;
        private float _additionalAttackPowerWeight;

        //일반, 엘리트, 보스 몬스터 이동속도 증가치는 기본 이동속도가 5 미만인 경우에만 적용됩니다.
        public float AdditionalBasicMonsterMoveSpeedWeight => _additionalBasicMonsterMoveSpeedWeight;
        private float _additionalBasicMonsterMoveSpeedWeight;
        public float AdditionalEliteBossMonsterMoveSpeedWeight => _additionalEliteBossMonsterMoveSpeedWeight;
        public float _additionalEliteBossMonsterMoveSpeedWeight;

        public float AdditionalSpawnAmountWeight => _additionalMonsterSpawnWeight;
        private float _additionalMonsterSpawnWeight;

        public float AdditionalExpDropWeight => _additionalExpDropWeight;
        private float _additionalExpDropWeight;

        public float AdditionalItemDropPercent => _additionalItemBoxDropPercent;
        private float _additionalItemBoxDropPercent;

        public bool IsMonsterCCImmune => _isMonsterCCImmune;
        private bool _isMonsterCCImmune;

        public bool IsPlayerResurrectionSkip => _isPlayerResurrectionSkip;
        private bool _isPlayerResurrectionSkip;

        public readonly float DefaultMaxHPWeight;
        public readonly float DefaultAttackPowerWeight;

        public AreaIndicatorManager AreaIndicators => _areaIndicators;
        private readonly AreaIndicatorManager _areaIndicators;

        public ParticleManager Particles => _particles;
        private readonly ParticleManager _particles;

        public DamagePopupManager DamagePopups => _damagePopups;
        private readonly DamagePopupManager _damagePopups;

        public DeadEffectManager DeadEffects => _deadEffects;
        private readonly DeadEffectManager _deadEffects;

        private readonly IndicatorPool _indicatorPool;
        private readonly CharacterPool _characterPool;
        private readonly ProjectilePool _projectilePool;
        private readonly AreaEffectPool _areaEffectPool;
        private readonly ItemObjectPool _itemObjectPool;
        private readonly EnvironmentObjectPool _environmentObjectPool;

        public PlayerCharacter PC => _playerCharacters[0];

        /// <summary>
        /// 플레이어 캐릭터들. 유저가 조종하는 PC와 AI가 조종하는 PC 모두를 포함한다.
        /// 리스트의 첫 번째 원소는 반드시 유저가 조종하는 PC이다.
        /// </summary>
        public IReadOnlyList<PlayerCharacter> PlayerCharacters => _playerCharacters;
        private readonly List<PlayerCharacter> _playerCharacters;

        private readonly List<List<Character>> _allianceCharacters;
        // 매 프레임, 몬스터를 업데이트 하는 중에 추가된 몬스터를 바로 _aliveMonster에 추가하면 컨테이너의 이터레이터 무효화가 일어난다.
        // 이전 프레임에 만들어진 몬스터는 다음 프레임에 aliveMonsters로 옮기고, 업데이트해준다.
        private readonly List<Monster> _monstersCreatedOnThisFrame;

        private readonly List<ProjectileObject> _aliveProjectiles;
        private readonly List<ProjectileObject> _projectilesCreatedOnThisFrame;
        private readonly List<ProjectileObject> _projectilesToRemove;

        private readonly List<AreaEffectObjectBase> _aliveAreaEffects;
        private readonly List<AreaEffectObjectBase> _areaEffectsCreatedOnThisFrame;
        private readonly List<AreaEffectObjectBase> _areaEffectsToRemove;

        private readonly List<IndicatorObjectBase> _aliveIndicators;
        private readonly List<IndicatorObjectBase> _indicatorsCreatedOnThisFrame;
        private readonly List<IndicatorObjectBase> _indicatorsToRemove;

        private readonly List<AcquirableItemObject> _acquirableItemObjects;
        private readonly List<BreakableItemObject> _breakableItemObjects;
        public IReadOnlyList<BreakableItemObject> BreakableItemObjects => _breakableItemObjects;
        private GameObject _expObjectsRoot = null!;

        private StageEventController _stageEventController;
        public float StageRunningTime => _stageEventController.StageRunningTime;
        public float MaxStageTime => _stageEventController.MaxStageTime;

        private List<FenceObject> _fenceObjects;
        private List<Collider2D> _fenceOuterWallColliders;

        public long EliminatedBosses { get; private set; }
        public long EliminatedElites { get; private set; }
        public long EliminatedMonsters { get; private set; }

        private readonly StageDropItemSupplier _dropGoldSupplier;
        private readonly StageDropItemSupplier _dropGemSupplier;
        private readonly StageDropItemSupplier _dropRandomEquipmentElementSupplier;
        //랜덤 장비 강화석 드랍개수 계산할때 필요함
        public long RemainingEquipmentElement => _dropRandomEquipmentElementSupplier.RemainingItmes;

        /// <summary>
        /// 스테이지에서 지급할 만렙 이후 레벨업시 지급할 보너스 골드 중 남은 량
        /// </summary>
        private long _leftLevelUpBonusGoldAmount;
        private readonly long LevelUpBonusGoldPerUnit;

        //보스 패턴등에 사용되는 울타리 위치 및 사이즈
        private Rect? _fenceRect;
        public Rect? FenceRect => _fenceRect;

        // 스테이지 결과. 스테이지 결과가 아직 안 정해졌으면 null
        private StagePlayResult? _stagePlayResult;
        private float _resultPopupOpenAt;
        public bool HasStagePlayResult => _stagePlayResult != null && float.MaxValue == _resultPopupOpenAt;

        private ZoneManager _zoneManager;

        private Stage(
            StageType stageType,
            ChapterStaticData? chapterStaticData,
            StaticDataRepository staticDatas)
        {
            this.StageType = stageType;
            this.ChapterStaticData = null;

            if (StageType == StageType.Chapter)
            {
                this.ChapterStaticData = chapterStaticData;
                this.StaticData = chapterStaticData!.StageStaticData;
                this.StageElementType = chapterStaticData!.ElementType;
            }
            else
            {
                throw new NotImplementedException($"{StageType} 구현 안 됨 구현해주세요");
            }

            _staticDatas = staticDatas;
            _areaIndicators = new AreaIndicatorManager();
            _particles = new ParticleManager();
            _damagePopups = new DamagePopupManager();
            _deadEffects = new DeadEffectManager();
            _characterPool = new CharacterPool(staticDatas);
            _projectilePool = new ProjectilePool(staticDatas);
            _areaEffectPool = new AreaEffectPool(staticDatas);
            _itemObjectPool = new ItemObjectPool();
            _indicatorPool = new IndicatorPool();
            _environmentObjectPool = new EnvironmentObjectPool();

            _zoneManager = new ZoneManager(this.StaticData.Width, this.StaticData.Height);

            _playerCharacters = new List<PlayerCharacter>();
            _allianceCharacters = new List<List<Character>>(2)
            { // enum AllianceType
                new List<Character>(), // 0. PlayerAlliance
                new List<Character>()  // 1. MonsterAlliance
            };

            _monstersCreatedOnThisFrame = new List<Monster>();

            _aliveProjectiles = new List<ProjectileObject>();
            _projectilesCreatedOnThisFrame = new List<ProjectileObject>();
            _projectilesToRemove = new List<ProjectileObject>();

            _aliveAreaEffects = new List<AreaEffectObjectBase>();
            _areaEffectsCreatedOnThisFrame = new List<AreaEffectObjectBase>();
            _areaEffectsToRemove = new List<AreaEffectObjectBase>();

            _aliveIndicators = new List<IndicatorObjectBase>();
            _indicatorsCreatedOnThisFrame = new List<IndicatorObjectBase>();
            _indicatorsToRemove = new List<IndicatorObjectBase>();

            _acquirableItemObjects = new List<AcquirableItemObject>();
            _breakableItemObjects = new List<BreakableItemObject>();

            var stageEventStaticDatas = staticDatas.Stages.FindStageEvents(StaticData.StageNumber);
            if (stageEventStaticDatas == null)
            {
                throw new StaticDataValidationError($"스테이지 이벤트가 없습니다. StageNumber[{StaticData.StageNumber}]");
            }
            var stageEventController = new StageEventController(stageEventStaticDatas, StaticData);
            _stageEventController = stageEventController;

            this.DefaultMaxHPWeight = stageEventStaticDatas.First().MonsterHPWeight;
            this.DefaultAttackPowerWeight = stageEventStaticDatas.First().MonsterAttackPowerWeight;

            _fenceObjects = new List<FenceObject>();
            _fenceOuterWallColliders = new List<Collider2D>();

            _stagePlayResult = null;

            this._isMonsterCCImmune = false;
            this._isPlayerResurrectionSkip = false;

            // 스테이지에서 지급할 아이템(골드 보석 등)을 설정한다
            switch (stageType)
            {
                case StageType.Chapter:
                    _dropGoldSupplier = StageDropItemSupplier.Create(chapterStaticData!.TotalDropGold, stageEventStaticDatas);
                    _leftLevelUpBonusGoldAmount = chapterStaticData.TotalLevelUpBonusGold;
                    this.LevelUpBonusGoldPerUnit = math.max(1, (chapterStaticData.TotalLevelUpBonusGold / 10));
                    _dropGemSupplier = StageDropItemSupplier.Create(chapterStaticData!.TotalDropGem, stageEventStaticDatas);
                    _dropRandomEquipmentElementSupplier = StageDropItemSupplier.Create(chapterStaticData!.TotalRandomEquipmentTicket, stageEventStaticDatas);
                    break;
                default:
                    throw new NotImplementedException($"{stageType} 구현 안 됨. 구현하세요.");
            }
        }

        public static Stage CreateForMainChapter(ChapterStaticData chapterStaticData, StaticDataRepository staticDatas)
            => new Stage(StageType.Chapter, chapterStaticData, staticDatas);

        public void Initialize(
            IReadOnlyList<SkillId> userSkillDeck,
            HeroData userHeroData,
            IEnumerable<EquipmentData> userHeroEquippedEquipments,
            EvolutionData evolutionData)
        {
            _stagePlayResult = null;
            _expObjectsRoot = new GameObject("@ExpObjectsRoot");

            _damagePopups.InitializeForStageScene();
            _deadEffects.InitializeForStageScene();

            var heroStaticData = _staticDatas.Heroes.Get(userHeroData.HeroType);
            this.CreatePlayerCharacter(userSkillDeck, heroStaticData, userHeroData, userHeroEquippedEquipments, evolutionData);

            bool isInitEventSkipped = _stageEventController.StartStageTimer(0f);
            if (!isInitEventSkipped)
            {
                _stageEventController.InitializeStageEnterEvent(this);
            }
            else
            {
                // InitEvent가 스킵된 경우엔 OnEnterredIntoStage를 직접 호출해줘야 한다.
                PC.OnEnterredIntoStage(this);
            }

            EliminatedBosses = 0;
            EliminatedElites = 0;
            EliminatedMonsters = 0;
        }

        public void Update()
        {
#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("Stage.Update"))
#endif
            {
                if (Time.timeScale <= 0f)
                {
                    // 팝업이 띄워져 있다던지 해서 시간이 멈췄다. 업데이트도 멈춘다. 
                    return;
                }

                float deltaTime = Time.deltaTime;
                float now = Time.time;

                {
                    List<Character> allCharacter = new List<Character>();
                    foreach (var charactes in _allianceCharacters)
                    {
                        allCharacter.AddRange(charactes);
                    }
                    _zoneManager.ComputeZone(allCharacter);
                }

                this.UpdateCharacters(now, deltaTime);
                this.UpdateProjectiles(now, deltaTime);
                this.UpdateAreaEffects(now, deltaTime);
                this.UpdateIndicator(now, deltaTime);
                this.UpdateAcquirableItemObjects(now, deltaTime);
                this.UpdateBreakableItemObjects(now, deltaTime);
                this.CheckAndSpawnItemBoxObject(now, deltaTime);

                this.UpdateParticles();
                this.UpdateDeadEffects();

                if (_stagePlayResult != null)
                {
                    var stagePlayResult = _stagePlayResult.Value;
                    if (_resultPopupOpenAt < now)
                    {
                        if (stagePlayResult == StagePlayResult.Failed)
                        {
                            bool hasResurrectCoin = GameClient.CS.UserGameData!.ResurrectionCoin > 0;

                            bool isAbleToResurrect = StaticData.IsResurrectable && // 부활 불가능한 스테이지면 부활 못한다.
                                (GameClient.CS.UserGameData!.StageResurrectCount < 1) && // 이 챕터에서 이미 부활 한번 했으면 부활 더 못한다.
                                !(GameClient.CS.UserGameData.ClearedHighestChapter <= 0 && !hasResurrectCoin) && // 1챕터에서는 부활코인 없으면 부활 불가능   
                                !_isPlayerResurrectionSkip &&
                                PC.Action.IsDead;

                            if (isAbleToResurrect &&
                                GameClient.CS.UserGameData.ClearedHighestChapter <= 0 &&
                                GameClient.CS.UserGameData.HighestStageTimeInSeconds <= 0) // (첫번째시도 == 최고기록이 없는경우)
                            {
                                // 그럼에도 불구하고, 1챕터의 첫번째 시도에서는 부활 불가능
                                isAbleToResurrect = false;
                            }

                            if (isAbleToResurrect)
                            {
                                UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>().AddResurrectionPopup(StaticData.StageNumber, () => this.ShowStageResultPopup(stagePlayResult));
                            }
                            else
                            {
                                this.ShowStageResultPopup(stagePlayResult);
                            }
                        }
                        else if (stagePlayResult == StagePlayResult.Cleared)
                        {
                            this.ShowStageResultPopup(stagePlayResult);
                        }
                        else
                        {
                            throw new NotImplementedException($"[{stagePlayResult}] 구현 안 됨.");
                        }

                        // 한번 오픈후 다시 열리지 않게 한다.
                        _resultPopupOpenAt = float.MaxValue;
                    }
                    return;
                }

                _stageEventController.Update(stage: this);

                _stagePlayResult = this.CheckStagePlayResult();
                if (_stagePlayResult != null)
                {
                    _resultPopupOpenAt = now + 1.85f;
                }
            }
        }

        private void ShowStageResultPopup(StagePlayResult stagePlayResult)
        {
            var stageUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
            stageUI.AddMainChapterResultPopup(this.ChapterStaticData!, PC.Gold, PC.Gem, PC.AcquiredRandomEquipmentElement, StageRunningTime, stagePlayResult, EliminatedBosses, EliminatedElites, EliminatedMonsters);
        }

        // 부활팝업창에서 부활성공했다. 게임플레이 재개할 수 있게 상태를 변경한다.
        public void OnResurrected(PlayerCharacter owner, float resurrectedHP)
        {
            float now = Time.time;

            // 스테이지 진행을 위해, 결정된 결과(실패)를 초기화해준다.
            _stagePlayResult = null;

            owner.Action.ChangeTo(this, new IdleAction(owner.AnimationController));
            owner.RecoverHP(this, resurrectedHP);
            owner.InitializeEffects();

            // 1.5초 동안 무적 버프 준다.
            owner.StatusEffects.AddOrUpdateStatusEffect(
                this, owner,
                StatusEffectType.SuperArmor,
                1.5f,
                now,
                effectParameter1: 0f);

            owner.OnResurrected(this);
            {
                // 폭탄 대미지 효과와 스턴을 준다.
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/12-Bomb_SFX.prefab", owner.Pos);

                {
                    string EFFECT_PATH = "Stages/GradeEffects/StunEnemies.prefab";
                    var floorEffect = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(EFFECT_PATH);
                    floorEffect.transform.SetParent(owner.transform);
                    floorEffect.transform.localPosition = Vector3.zero;
                    floorEffect.transform.localScale = 2.0f * Vector3.one;
                    floorEffect.gameObject.SetActive(false);

                    floorEffect.gameObject.SetActive(true);
                    floorEffect.AnimationState.SetAnimation(0, "animation", false);

                    DOTween.Sequence().AppendInterval(2f)
                        .AppendCallback(() =>
                       {
                           ResourcePool.Instance.PutBackInstance(EFFECT_PATH, floorEffect.gameObject);
                       });
                }

                float damage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, 6f); // 폭탄 데미지 비율
                float effectiveRange = 20f * GameClient.CameraController.OrthographicSize / 18f;
                CircularTargetArea areaAttack = new CircularTargetArea(owner.Pos, effectiveRange); // 폭탄 터지는 범위.

                List<Character> characters = new List<Character>();
                this.FindAliveCharactersInArea(owner.Alliance.ToEnemyAlliance(), areaAttack, characters);
                foreach (Character character in characters)
                {
                    character.Hitted(this, owner, damage, Vector2.zero, character.CenterPos, hitSoundPrefabPath: string.Empty);
                    // 안죽었으면 스턴도 준다.
                    if (!character.Action.IsDead)
                    {
                        character.StatusEffects.AddOrUpdateStatusEffect(this, character, StatusEffectType.Stun, duration: 1.99f, now, effectParameter1: 0.0f);
                    }
                }
            }
        }

        private void UpdateParticles()
        {
#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("Stage.UpdateParticles"))
#endif
            {
                _particles.Update();
            }
        }

        private void UpdateDeadEffects()
        {
#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("Stage.UpdateDeadEffects"))
#endif
            {
                _deadEffects.Update();
            }
        }

        public void ClearBeforeChangingScene(Scene clearingScene)
        {
            if (this.PC != null &&
                this.PC.gameObject.scene == clearingScene)
            {
                var pc = this.PC;
                pc.OnExitingFromStage(this);

                _allianceCharacters[(int)AllianceType.Players].Remove(pc);
                _characterPool.PutBackCharacter(pc);
            }

            var removedMonsters = new List<Character>();
            foreach (var allianceCharacter in _allianceCharacters)
            {
                foreach (var character in allianceCharacter)
                {
                    if (character.gameObject.scene == clearingScene)
                    {
                        _characterPool.PutBackCharacter(character);
                        removedMonsters.Add(character);
                    }
                }
            }

            foreach (var character in removedMonsters)
            {
                _allianceCharacters[(int)character.Alliance].Remove(character);
            }
            removedMonsters.Clear();

            _zoneManager.ClearBeforeChangingScene();

            _characterPool.ClearBeforeChangingScene(clearingScene);
            _projectilePool.ClearBeforeChangingScene(clearingScene);
            _areaEffectPool.ClearBeforeChangingScene(clearingScene);
            _itemObjectPool.ClearBeforeChangingScene(clearingScene);
            _environmentObjectPool.ClearBeforeChangingScene(clearingScene);
            _indicatorPool.ClearBeforeChangingScene(clearingScene);

            _particles.ClearBeforeChangingScene(clearingScene);
            _damagePopups.ClearBeforeChangingScene(clearingScene);
            _deadEffects.ClearBeforeChangingScene(clearingScene);
        }

        public StagePlayResult? CheckStagePlayResult()
        {
            if (PC.Action.IsDead && !PC.IsFakeDead)
            {
                return StagePlayResult.Failed;
            }

            if (_stageEventController.IsStageClear)
            {
                return StagePlayResult.Cleared;
            }

            return null;
        }

        public void GetBossSpawnTimes(in List<float> bossSpawnTimes)
        {
            _stageEventController.GetBossSpawnTimes(in bossSpawnTimes);
        }

        public float GetLastBossSpawnTime() => _stageEventController.GetLastBossSpawnTime();

        public int GetTotalBossAmount() => _stageEventController.GetTotalBossAmount();

        public bool IsAbleToGiveLevelUpBonusGold()
        {
            if (StageType != StageType.Chapter)
            {
                return false;
            }

            if (_leftLevelUpBonusGoldAmount <= 0)
            {
                return false;
            }

            return true;
        }

        public long TakeLevelUpBonusGoldAmount()
        {
            if (!this.IsAbleToGiveLevelUpBonusGold())
            {
                return 0;
            }

            long amount = math.min(LevelUpBonusGoldPerUnit, _leftLevelUpBonusGoldAmount);
            _leftLevelUpBonusGoldAmount -= amount;
            return amount;
        }

        public long TakeRewardMonsterDropGolds(long requestedAmount) => _dropGoldSupplier.TakeItemsToDrop(requestedAmount);

        public bool HasItemBoxDropGolds() => _dropGoldSupplier.HasItemsToDrop;

        public long TakeItemBoxDropGolds(long requestedAmount) => _dropGoldSupplier.TakeItemsToDrop(requestedAmount);

        public long TakeMonsterDropGems(long requestedAmount) => _dropGemSupplier.TakeItemsToDrop(requestedAmount);

        public long TakeMonsterDropRandomEquipmentElement(long requestdAmount) => _dropRandomEquipmentElementSupplier.TakeItemsToDrop(requestdAmount);

    }
}
