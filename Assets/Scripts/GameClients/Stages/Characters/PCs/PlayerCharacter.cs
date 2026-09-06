using DG.Tweening;
using Shared.GameDataTypes;
using Shared.GameLogics;
using Shared.Localizers;
using Shared.StaticDatas;
using Shared.UserDatas;
using SamMul.Animations.Placeholder;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Heroes;
using SamMul.GameClients.Stages.Characters.Actions;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.ConditionalEffects;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.Characters.PCs.Skills;
using SamMul.GameClients.Stages.Characters.Shields;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.GameClients.Stages.ItemObjects;
using SamMul.Loggers;
using SamMul.ResourcePools;
using SamMul.Scenes;
using SamMul.UIs.Stages.HUDs;
using SamMul.UIs.Stages.Popups;
using SamMul.UnityHelpers;
using Random = UnityEngine.Random;

namespace SamMul.GameClients.Stages.Characters.PCs
{
    public enum EntryStatus { NotEntered, Entered, Exited }

    public readonly struct EquippedEquipmentStatModifiers
    {
        // 장착한 장비들로 추가되는 기본 스탯변화량 (레벨효과, 등급효과)
        public readonly StatModifier MaxHPAddValue;
        public readonly StatModifier MaxHPMultValue;

        public readonly StatModifier AttackPowerAddValue;
        public readonly StatModifier AttackPowerMultValue;

        public EquippedEquipmentStatModifiers(float addMaxHP, float multMaxHP, float addAttackPower, float multAttackPower)
        {
            this.MaxHPAddValue = new StatModifier(addMaxHP, StatModType.Flat);
            this.MaxHPMultValue = new StatModifier(multMaxHP, StatModType.PercentAdd);
            this.AttackPowerAddValue = new StatModifier(addAttackPower, StatModType.Flat);
            this.AttackPowerMultValue = new StatModifier(multAttackPower, StatModType.PercentMult);
        }

        public void Apply(PlayerCharacterStatCalculators stats)
        {
            stats.MaxHP.AddModifier(MaxHPAddValue);
            stats.MaxHP.AddModifier(MaxHPMultValue);

            stats.AttackPower.AddModifier(AttackPowerAddValue);
            stats.AttackPower.AddModifier(AttackPowerMultValue);
        }

        public void Disapply(PlayerCharacterStatCalculators stats)
        {
            stats.MaxHP.RemoveModifier(MaxHPAddValue);
            stats.MaxHP.RemoveModifier(MaxHPMultValue);

            stats.AttackPower.RemoveModifier(AttackPowerAddValue);
            stats.AttackPower.RemoveModifier(AttackPowerMultValue);
        }
    }

    public class PlayerCharacter : Character
    {
        public HeroStaticData StaticData { get; private set; }
        public HeroData HeroData { get; private set; }

        private LevelCalculator _levelCalculator;
        public int Level => _levelCalculator.Level;
        public long CurrentExp => _levelCalculator.CurrentExp;
        public long ExpToNextLevel => _levelCalculator.ExpToNextLevel;

        public long Gold => _gold;
        private long _gold;

        public override float RangeAttackPower => Stats.AttackPower.Value;

        public new PCAnimationController AnimationController => _animationController;
        protected new PCAnimationController _animationController => (PCAnimationController)base._animationController;

        public new PlayerCharacterStatCalculators Stats => _stats;
        protected new PlayerCharacterStatCalculators _stats => (PlayerCharacterStatCalculators)base._stats;

        public ConditionalEffectManager ConditionalEffects => _conditionalEffects;
        protected ConditionalEffectManager _conditionalEffects;

        private SkillSet _skillSet;
        private SkillDeck _skillDeck;

        // 장착한 장비들로 인해 증감된 스탯변화를 관리한다.
        public EquippedEquipmentStatModifiers? EquipmentStatModifiers => _equipmentStatModifiers;
        private EquippedEquipmentStatModifiers? _equipmentStatModifiers;

        public IEnumerable<EquipmentData> EquippedEquipments => _equippedEquipments;
        private IEnumerable<EquipmentData> _equippedEquipments;

        public CustomParameterManager CustomParameters => _customParameters;
        private CustomParameterManager _customParameters;

        // 무적이 되어 모든 피격을 무효화한다.
        public bool IsInvincible => _isInvincible;
        private bool _isInvincible;

        // 에이밍중인 타겟. 없으면 null
        private Character _aimTarget;

        //아직 스킬 선택창 새로고침 가능한지
        public bool IsCanSelectSkillRefesh => _leftSkillRefreshCount > 0;

        // 스킬 새로고침 개수 (남은갯수)
        private int _leftSkillRefreshCount;

        // 스킬 후보지 갱신 찬스 갯수 (이번 챕터의 최대 횟수)
        public int LeftSkillRefreshCount => _leftSkillRefreshCount + 1;

        // NOTE: 룬폭탄(함정폭탄 궁극진화) 전용 변수 입니다!!!! 해당 변수 값은 RuneTrapAreaEffectObject에서 변경하고 있습니다. 사용 주의!
        public bool IsRuneTrapBombLayOnSide = false;

        // 진화, 등급효과 등으로 다시 부활할때 사용한다
        // 쓰러지는 연출 이후 다시 살아나는 연출을 해야한다. 다른곳에서 진짜 죽었다 판단하지 않게 확인 가능하도록 사용하는 변수다.
        public bool IsFakeDead => _isFakeDead;
        private bool _isFakeDead;

        //부활 연출 중인 경우
        private bool _isResurrecting;
        // 남은 부활 가능 횟수
        private int _leftResurrectCount;
        public int LeftResurrectCount => _leftResurrectCount;

        public bool IsHPBufferActive => _hpBuffer?.IsActive ?? false;
        public HPBuffer HPBuffer => _hpBuffer;
        private HPBuffer _hpBuffer;

        private StarCoreDisplayer _starCoreDisplayer;

        public EntryStatus EntryStatus => _entryStatus;
        private EntryStatus _entryStatus;

        public float LastDamageTakenAt => _lastDamageTakenAt;
        private float _lastDamageTakenAt;

        public override void AllocateSharedResources(CharacterType characterType, string skeletonDataPath, float colliderRadius, Vector2 hitBoxOffset, Vector2 hitBoxSize, float shadowSize)
        {
            this.gameObject.layer = LayerMask.NameToLayer("Player");

            Debug.Assert(characterType.IsHeroType());
            base.AllocateSharedResources(characterType, skeletonDataPath, colliderRadius, hitBoxOffset, hitBoxSize, shadowSize);

            // NOTE: 초기화 코드 + 리소스 풀링 다시 구현필요하다.
            // AllocateSharedResources 와 Initialize도 다시 나눌 필요가 있다.
            // 리소스 풀링하게되면 몬스터 단위로 리소스풀링될 것. key 에 따라 공유되는 것과 아닌 것으로 분리하자.

            _aimTarget = null;

            _skillSet = new SkillSet();
            _skillDeck = new SkillDeck();

            _equipmentStatModifiers = null;

            _conditionalEffects = new ConditionalEffectManager();
            _customParameters = new CustomParameterManager();
        }

        private void InitializeCommonResources(Stage stage, IReadOnlyList<SkillId> userSkillDeck, HeroStaticData heroStaticData, HeroData heroData)
        {
            this.StaticData = heroStaticData;
            this.HeroData = heroData;

            this.InitializeCharacter(
                AllianceType.Players,
                BaseStats.FromHeroData(this.StaticData, this.HeroData),
                mass: 20f,
                drag: 25f);

            _skillDeck.Initialize(HeroData.HeroType, userSkillDeck);

            _conditionalEffects.Clear();
            if (_equipmentStatModifiers != null)
            {
                Log.I.Warn($"{this.HeroData.HeroType} PC 초기화하려는데 {nameof(_equipmentStatModifiers)}가 null이 아님. 초기화 잘못된듯.");
            }
            _equipmentStatModifiers = null;

            _gold = 0;

            _isInvincible = false;
            _isFakeDead = false;

            _animationController.PlayIdleAttackActionInfinitely();

            _aimTarget = null;
            _animationController.EndAiming();


            _isResurrecting = false;
            _leftResurrectCount = 0;
            _entryStatus = EntryStatus.NotEntered;
            _lastDamageTakenAt = Time.time;

            // 플레이어는 그냥 넉백 없도록 한다
            this.SetImmuneToKnockBack();
        }

        // 리소스 풀에서 꺼내져서, 실제 사용측에서 호출한다.
        // 인스턴스의 속성을 초기화한다.
        public void InitializePlayerCharacter(
            IReadOnlyList<SkillId> userSkillDeck,
            HeroStaticData heroStaticData,
            HeroData heroData,
            IEnumerable<EquipmentData> equippedEquipments,
            float elementBonusRate,
            Stage stage)
        {
            _levelCalculator = new LevelCalculator();

            this.InitializeCommonResources(stage, userSkillDeck, heroStaticData, heroData);

            this.InitializeHeroGradeEffects(heroData);
            this.InitializeEquipmentEffects(equippedEquipments);

            this.ApplyElementBonusToStats(elementBonusRate);
        }

        private void ApplyElementBonusToStats(float elementBonusRate)
        {
            // 원소 보너스는 곱연산으로 적용
            float elementBonusMultiplier = CustomParameters.GetParameterValue(StaticData.ElementType switch
            {
                ElementType.Water => CustomParameterType.WaterElementBonusMultiplier,
                ElementType.Wind => CustomParameterType.WindElementBonusMultiplier,
                ElementType.Earth => CustomParameterType.EarthElementBonusMultiplier,
                ElementType.Fire => CustomParameterType.FireElementBonusMultiplier,
                _ => throw new NotImplementedException($"원소 종류 {StaticData.ElementType}에 대한 처리가 구현되지 않았습니다."),
            });
            float resultElementBonusRate = elementBonusMultiplier * elementBonusRate;
            Stats.AttackPower.AddModifier(new StatModifier(resultElementBonusRate, StatModType.PercentAdd));
        }

        // Hud, Shadow등 효과를 키기 위한 함수.
        public override void InitializeEffects()
        {
            base.InitializeEffects();
            this.ShowHPBar(new Vector2(0f, -0.4f), Vector3.one, HPBar.PC_COLOR);
            _animationController.SetToInitialState();
        }

        protected override CharacterActionController CreateCharacterActionController()
        {
            return new PCActionController(_animationController);
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();

            this.DestroyEquipmentEffects();
            _skillSet.Clear(this);
            _conditionalEffects.Clear();
            _customParameters.Clear();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _conditionalEffects.Clear();
        }

        public override void OnEnterredIntoStage(Stage stage)
        {
            _entryStatus = EntryStatus.Entered;
            base.OnEnterredIntoStage(stage);

            // 시작하면 히어로 기본스킬 레벨1을 들고 시작한다.
            _conditionalEffects.EnterredIntoStage(stage, this);
            _skillSet.Initialize(this, stage);
            _conditionalEffects.OnSkillSetInitialized(stage, this);

            // 등급효과로 MaxHP가 증가하는 경우가 있기 때문에, 여기에서 최종 MaxHP에 맞춰 만피를 채워준다.
            float gap = this.MaxHP - this.CurrentHP;
            if (gap > 0f)
            {
                this.RecoverHP(stage, gap);
            }

            this.SetResurrectCount((int)this.Stats.MaxResurrectCount.Value);

            //스킬선택창 새로고침 카운트 설정
            //초기화 단계에서 처리하고 싶지만 ConditionalEffectBase의 초기화는 이 단계에서 처리한다
            bool isTutorial = stage.StageType == StageType.Chapter &&
                stage.ChapterStaticData.ChapterNumber == 0;
            if (isTutorial)
            {
                _leftSkillRefreshCount = 0;
            }
            else
            {
                _leftSkillRefreshCount = (int)_customParameters.GetParameterValue(CustomParameterType.AdditionalSkillRefreshCount);
            }
        }

        public override void OnExitingFromStage(Stage stage)
        {
            base.OnExitingFromStage(stage);
            _conditionalEffects.ExitingFromStage(stage, this);

            _skillSet.ClearBeforeExitFromStage(this, stage);
            this.DestroyEquipmentEffects();

            _animationController.SetToInitialState();
        }

        public void OnEnterredIntoBossStageEvent(Stage stage, MonsterStaticData bossStaticData)
        {
            _conditionalEffects.EnterredIntoBossStageEvent(stage, owner: this, bossStaticData);
        }

        protected override CharacterStatCalculators CreateStatCalculators()
        {
            return new PlayerCharacterStatCalculators();
        }

        public override void UpdateLogic(Stage stage)
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            // 보스전에서는 플레이어 캐릭터를 강제로 울타리 안에 가둠.
            if (stage.FenceRect != null)
            {
                stage.ForceClampCharacterToCurrentRect(this);
            }

#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("PlayerCharacter.BaseUpdateLogic"))
#endif
            {
                base.UpdateLogic(stage);
            }

            float now = Time.time;

#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("PlayerCharacter.UpdateLogic.SkillSetUpdate"))
#endif
            {
                if (!_action.IsDead)
                {
                    _skillSet.Update(this, stage, now);
                }
            }

#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("PlayerCharacter.UpdateLogic.UpdateConditionalEffects"))
#endif
            {
                _conditionalEffects.UpdateConditionalEffects(stage, this);
            }

            if (_action.IsStunned ||
                _action.IsDead ||
                _action.IsAppearing)
            {
                return;
            }

            if (_aimTarget != null)
            {
                _animationController.UpdateAimBoneToTargetWorldPosition(_aimTarget.Pos);
            }

#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("PlayerCharacter.UpdateLogic.TakeAcquirableItemObjectsInDistance"))
#endif
            {
                float acquisitionDistance = Stats.AcquisitionDistance.Value;
                foreach (var acquirableItem in stage.TakeAcquirableItemObjectsInDistance(this.Pos, acquisitionDistance))
                {
                    acquirableItem.OnAcquired(this, stage);
                    // NOTE: OnAcquired 호출즉시 경험치가 오르지는 않는다. 획득 애니메이션 재생 다 끝나야 들어온다.
                }
            }

#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("PlayerCharacter.UpdateLogic.UpdateLevelExp"))
#endif
            {
                this.UpdateLevelExp(stage);
            }

#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("PlayerCharacter.UpdateLogic.UpdateShieldAndHPBuffer"))
#endif
            {
                Shield?.UpdateLogic();
                HPBuffer?.UpdateLogic();
            }
        }

        protected override CharacterAnimationController CreateCharacterAnimationController(GameObject bodyObject, string bodyDataPath)
        {
            // 몬스터와 같은 규칙: .asset 은 스켈레톤(스파인), 그 외(.controller)는 스프라이트 + 애니메이터.
            bool isSpriteBody = Path.GetExtension(bodyDataPath) != ".asset";

            var body = bodyObject.AddComponent<SkeletonAnimation>();
            if (!isSpriteBody)
            {
                body.skeletonDataAsset = ResourcePool.Instance.LoadResource<SkeletonDataAsset>(bodyDataPath);
            }
            body.Initialize(true);

            PCSpriteBody? spriteBody = null;
            if (isSpriteBody)
            {
                var animatorController = ResourcePool.Instance.LoadResource<RuntimeAnimatorController>(bodyDataPath);
                if (animatorController == null)
                {
                    throw new InvalidOperationException($"플레이어 애니메이션 컨트롤러가 없습니다. Path[{bodyDataPath}]");
                }
                spriteBody = PCSpriteBody.Create(bodyObject, animatorController);
                // 스켈레톤 트랙은 계속 돌리되 플레이스홀더 몸통은 그리지 않는다.
                bodyObject.GetComponent<MeshRenderer>().enabled = false;
                _bodyRenderer = spriteBody.Renderer;
            }
            else
            {
                _bodyRenderer = bodyObject.GetComponent<Renderer>();
            }

            var controller = this.CreatePCAnimationController(body);
            if (spriteBody != null)
            {
                controller.AttachSpriteBody(spriteBody);
            }
            return controller;
        }

        private PCAnimationController CreatePCAnimationController(SkeletonAnimation body)
        {
            switch (CharacterType)
            {
                default:
                    return new PCAnimationController(
                        body,
                        _bodyRenderer,
                        isPlayerOrBoss: true,
                        idleActionAnimationName: null,
                        idleMovementAnimationName: "idle",
                        runAnimationName: "run",
                        runBackwardAnimationName: "run",
                        hittedAnimationName: null,
                        attackAnimationNames: new List<string>() { "attack" },
                        specialAttackAnimationName: "attack",
                        targetingAnimationName: null,
                        deadAnimationName: "die",
                        faceAnimationName: "blink",
                        appearAnimationName: "appear",
                        disappearAnimationName: "disappear",
                        pointAnimationName: "point",
                        healAnimationName: "heal"
                    );
            }
        }

        public override float Hitted(Stage stage, Character attacker, float damage, Vector2 hitVector, Vector2 hitPosition, string hitSoundPrefabPath)
        {
            if (_isInvincible)
            {
                return 0f;
            }

            // 방어막이 활성화되어 있으면 방어막 개수를 하나 줄이고 대미지를 무시한다.
            if (IsShieldActive)
            {
                _shield.DecreaseShieldAmount(1);
                return 0.0f;
            }

            //데미지 무시, 데미지 감소에 대해 처리한다.
            var beingHittedByEnemyResultData = _conditionalEffects.BeingHittedByEnemy(stage, owner: this, enemy: attacker, damage);

            //데미지 무시 효과 적용
            if (beingHittedByEnemyResultData._isEnemyAttackIgnored)
            {
                return 0f;
            }
            else
            {
                //데미지 감소처리한 데미지를 넣어준다.
                damage = beingHittedByEnemyResultData._calculatedDamage;
            }

            _lastDamageTakenAt = Time.time;

            // 체력 버퍼가 활성화되어 있으면 체력 버퍼가 대미지를 받아 완충하고 남은 대미지만 받는다.
            if (IsHPBufferActive)
            {
                damage = _hpBuffer.TakeDamage(damage);
            }

            if (damage <= 0.0f)
            {
                return 0.0f;
            }

            //회피율 계산은 플레이어만 해준다.
            if (Random.value <= Stats.DodgeRate.Value)
            {
                //회피 관련된 이펙트 있으면 여기서 연출
                stage.DamagePopups.CreateAvoidPopup(attacker, this.CenterPos, new Vector2(0f, 0.1f));
                return 0f;
            }

            float finalDamage = base.Hitted(stage, attacker, damage, hitVector, hitPosition, hitSoundPrefabPath);
            _conditionalEffects.HittedByEnemy(stage, owner: this, enemy: (Monster)attacker, finalDamage);

            if (stage.PC == this)
            {
                var uiRoot = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
                uiRoot.UpdatePCHP((int)this.CurrentHP, (int)this.MaxHP);
            }

            if (UnityGlobal.Sounds.IsVibrationActivated)
            {
                // 진동 옵션이 켜져있다면 햅틱 진동을 준다. 
            }

            return finalDamage;
        }

        public override void AttackedEnemy(Stage stage, Character enemy, float damage)
        {
            base.AttackedEnemy(stage, enemy, damage);

            _conditionalEffects.AttackedEnemy(stage, owner: this, enemy, damage);
            _skillSet.AttackedEnemy(stage, owner: this, enemy, damage);

        }
        public override void KilledEnemy(Stage stage, Character enemy)
        {
            base.KilledEnemy(stage, enemy);

            _conditionalEffects.KilledEnemy(stage, owner: this, enemy);
            if (enemy.IsBoss)
            {
                var monster = (Monster)enemy;
                _conditionalEffects.KilledBoss(stage, owner: this, monster);
            }
        }
        public override void KilledStunnedEnemy(Stage stage, Character enemy)
        {
            base.KilledStunnedEnemy(stage, enemy);
            _conditionalEffects.KilledStunnedEnemy(stage, owner: this, enemy);
        }
        public override void CreateDamagePopup(Stage stage, Character? attacker, Vector2 hitPosition, float finalDamage, Vector2 hitVector, bool isAttack)
        {
            // NOTE : PC는 피격되어도 대미지 안 보여주도록 한다. 대미지 1씩 뜨는게 긴장감이 없어짐.
        }

        public override bool IsAbleToRangeAttackNow(float now)
        {
            if (!base.IsAbleToRangeAttackNow(now))
            {
                return false;
            }

            if (_action.IsReloading)
            {
                return false;
            }

            return true;
        }

        public void SetInvincible(bool invincible)
        {
            _isInvincible = invincible;
            if (invincible)
            {
                _hpBar.PlayInvincibleEffect();
            }
            else
            {
                _hpBar.StopInvincibleEffect();
            }
        }

        private void SetResurrectCount(int resurrectCount)
        {
            _leftResurrectCount = resurrectCount;

            if (GameClient.Stage?.PC == this)
            {
                UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>().UpdatePCResurrectCount(_leftResurrectCount);
            }
        }

        public void IncreaseResurrectCount(int amount)
        {
            this.SetResurrectCount(_leftResurrectCount + amount);
        }

        private void DecreaseResurrectCount()
        {
            this.SetResurrectCount(_leftResurrectCount - 1);
        }

        /// <param name="moveVector">조이스틱이 움직여진 방향과 크기를 가진 벡터가 들어온다. 최대 크기는 1인 벡터.</param>
        public override void Move(Vector2 moveVector)
        {
            base.Move(moveVector);
        }

        public override void StopMovement()
        {
            base.StopMovement();
        }

        protected override void Dead(Stage stage, Vector2 hitVector)
        {
            if (_leftResurrectCount > 0 && !_isResurrecting)
            {
                this.SetFakeDead(true);
                _isResurrecting = true;

                DOTween.Sequence(this)
                .AppendInterval(0.25f)
                .AppendCallback(() =>
                {
                    var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
                    stageSceneUI.PlayResurrectSequence(this);
                })
                .AppendInterval(2f)
                .AppendCallback(() =>
                {
                    var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();

                    this.DecreaseResurrectCount();
                    stage.OnResurrected(owner: this, MaxHP * Stats.ResurrectHpRate.Value);
                    _isResurrecting = false;
                });
            }

            // 플레이어는 죽었을 때 그냥 제자리에서 죽자.
            base.Dead(stage, hitVector: Vector2.zero);

            _skillSet.OnPlayerCharacterDead(this, stage, Time.time);
        }

        public override void DisappearFromStage(Stage stage)
        {
            _entryStatus = EntryStatus.Exited;
            base.DisappearFromStage(stage);
            _skillSet.OnPlayerCharacterDead(this, stage, Time.time);
            this.SetFakeDead(true);
            this.gameObject.SetActive(false);
        }

        public void OnResurrected(Stage stage)
        {
            _skillSet.OnPlayerCharacterResurrected(this, stage, Time.time);
            _isFakeDead = false;
            _conditionalEffects.Resurrected(stage, this);
        }

        public void BeginAiming(Character target)
        {
            if (_aimTarget == target)
            {
                return;
            }
            _aimTarget = target;
            _animationController.BeginAiming(_aimTarget.Pos);

            _animationController.UpdateBodyDirectionByMoveDirection(MoveDir);
        }

        public void BeginAiming(BreakableItemObject target)
        {
            _animationController.BeginAiming(target.transform.position);
            _animationController.UpdateBodyDirectionByMoveDirection(MoveDir);
        }

        public void EndAiming()
        {
            _aimTarget = null;
            _animationController.EndAiming();
        }

        public void ReserveToGainExp(long increment)
        {
            long resultExp = increment + (long)(increment * Stats.ExpIncreaseRate.Value);
            _levelCalculator.ReserveToGainExp(resultExp);
        }

        private void UpdateLevelExp(Stage stage)
        {
            var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
            Debug.Assert(stageSceneUI != null);

            _levelCalculator.CalculateAndConveyExp();
            stageSceneUI.UpdateExpBar(_levelCalculator.Level, _levelCalculator.CurrentExp, _levelCalculator.ExpToNextLevel);

            while (!stageSceneUI.IsSkillSelectorPopupOpened &&// 스킬선택 팝업이 이미 떠있으면 진행 멈춤
                _levelCalculator.CheckAndIncreaseOneLevel())
            {
                _conditionalEffects.OnLevelUp(stage, this);

                // 튜토리얼에서는 획득가능한 스킬을 상황에 따라 동적으로 결정한다.
                if (stage.StageType == StageType.Chapter &&
                    stage.ChapterStaticData.ChapterNumber == 0)
                {
                    var acquiredSkills = _skillSet.GetAcquiredSkillKeys();
                    var skills = new List<SkillKey[]> { _skillDeck.SelectSkillsForTutorialChapter(_skillSet, this.Level, this.StaticData) };

                    stageSceneUI.AddSkillSelectorPopup(stage, this, skills, acquiredSkills);
                    stageSceneUI.UpdateExpBar(_levelCalculator.Level, _levelCalculator.CurrentExp, _levelCalculator.ExpToNextLevel);
                }
                else
                {
                    stageSceneUI.AddSkillSelectorPopup(stage, this, this.GetSkillCandidates(), this.GetAcquiredSkillKeys());
                    stageSceneUI.UpdateExpBar(_levelCalculator.Level, _levelCalculator.CurrentExp, _levelCalculator.ExpToNextLevel);
                }
            }
        }

        public void AcquireOrUpgradeSkill(SkillId skillId, PlayerCharacter owner, Stage stage)
        {
            if (!_skillDeck.ContainsSkill(skillId))
            {
                string message = $"StageType[{stage.StageType}], owner.HeroType[{owner.HeroData.HeroType}], this.HeroType[{this.HeroData.HeroType}], RequestedSkillId[{skillId}], AcquiredSkills[{_skillSet.AcquiredSkills}], GlobalUserSkillDeck[{string.Join(", ", GameClient.CS.UserSkillDeck)}], AvailableSkillDeck[{_skillDeck.AvailableSkillDeck}]";
                Debug.LogError(message);
                throw new LogicErrorException($"스킬덱에 없는 스킬을 획득하려 함. 무시합니다. Message[{message}]");
            }

            _skillSet.AcquireOrUpgradeSkill(skillId, owner, stage);
        }

        public SkillKey[] GetAcquiredSkillKeys()
        {
            return _skillSet.GetAcquiredSkillKeys();
        }

        public SkillId[] GetAcquiredSkillIds()
        {
            return _skillSet.GetAcquiredSkillIds();
        }

        public bool IsSkillDeckContainsSkill(SkillId skillId)
        {
            return _skillDeck.ContainsSkill(skillId);
        }

        public SkillKey[] SelectSkillsToLearnBySkillBox(int selectSkillCount)
        {
            return _skillDeck.SelectSkillsToLearnBySkillBox(_skillSet, selectSkillCount);
        }

        public SkillKey[] SelectSkillsToLearnByTutorialSkillBox(int selectSkillCount)
        {
            return _skillDeck.SelectSkillsToLearnByTutorialSkillBox(_skillSet, selectSkillCount, StaticData.BasicSkill);
        }

        public List<SkillKey[]> GetSkillCandidates()
        {
            int activeCount;
            int passiveCount;

            if (Level <= 5) //3,4,5
            {
                activeCount = 3;
                passiveCount = 0;
            }
            else if (Level <= 10) //6 ~ 10
            {
                activeCount = 2;
                passiveCount = 1;
            }
            else if (Level <= 13) //11 ~ 13
            {
                activeCount = 1;
                passiveCount = 2;
            }
            else
            {
                //=> 값이 0,0 이면 획득 가능한 스킬중 랜덤으로 3개를 반환해준다.
                activeCount = 0;
                passiveCount = 0;
            }

            var skillCandidates = new List<SkillKey[]>();
            for (int i = 0; i < LeftSkillRefreshCount; i++)
            {
                skillCandidates.Add(_skillDeck.Select3SkillsToLearn(_skillSet, activeCount, passiveCount));
            }
            return skillCandidates;
        }

        public void ReApplyStatsToAcquiredSkills(Stage stage, PlayerCharacterStatCalculators ownerStats)
        {
            _skillSet.ReApplyStatsToAcquiredSkills(ownerStats);

            if (stage.PC == this)
            {
                var stageUIRoot = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
                stageUIRoot.UpdatePCAttackPower((int)this.Stats.AttackPower.Value);
                stageUIRoot.UpdatePCHP((int)this.CurrentHP, (int)this.MaxHP);
            }
        }

        public void GainGold(long incrementGold)
        {
            if (incrementGold <= 0)
            {
                Debug.LogError($"골드 증가량({incrementGold})이 비정상입니다. 무시합니다. 확인해주세요.");
                return;
            }

            long resultIncrementGold = incrementGold + (long)(incrementGold * Stats.GoldIncreaseRate.Value);
            _gold += resultIncrementGold;

            var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
            stageSceneUI.UpdateGold(_gold);
        }

        public int GetSkillLevel(SkillId skillId)
        {
            return _skillSet.GetSkillLevel(skillId);
        }

        public bool HasSkill(SkillId skillId)
        {
            return _skillSet.HasSkill(skillId);
        }

        public void SetUsedSelectSkillRefesh()
        {
            if (_leftSkillRefreshCount > 0)
            {
                _leftSkillRefreshCount--;
            }
        }

        public void SetFakeDead(bool isFakeDead)
        {
            _isFakeDead = isFakeDead;
        }

        //기본 공격을 대신하는 스킬의 타겟은 플레이어 캐릭터가 전달한다.
        private Character _previousTarget = null;
        private float targetChangedAt = 0f;

        /// <summary>
        /// 기본 공격스킬에 타겟을 찾는 인터페이스 입니다.
        /// 이전 플레이어 공격에서 타겟을 찾는 로직만 가져와 제작한 인터페이스 입니다.
        /// </summary>
        /// <param name="effectiveRange">사정거리</param>
        /// <returns></returns>
        public Character FindBasicAttackTarget(Stage stage, float effectiveRange)
        {
            float targetRecognizingRange = effectiveRange;
            float targetSwitchingDelay = 0.95f; // 기존 타겟이 죽지 않았다면, 최소 0.95초 동안 타겟을 변경하지 않고 연사한다.
            var now = Time.time;

            Character target = _previousTarget;
            if ((target == null || target.Action.IsDead || target.IsImmuneToHit || !target.gameObject.activeSelf) || (now - this.targetChangedAt) > targetSwitchingDelay)
            {
                target = this.FindAndUpdateNewTarget(stage, targetRecognizingRange);
                this.targetChangedAt = now;
            }

            if (target == null || target.Action.IsDead ||
                (target.Pos - Pos).sqrMagnitude > ((targetRecognizingRange + target.ColliderRadius) * (targetRecognizingRange + target.ColliderRadius)))
            {
                if (_previousTarget != null)
                {
                    _previousTarget = null;
                    return null;
                }
            }
            _previousTarget = target;
            return target;
        }

        /// <summary>
        /// 가까운 아이템 오브젝트를 찾아 전달해준다(울타리 제외)
        /// </summary>
        /// <param name="effectiveRange">사정거리</param>
        /// <returns></returns>
        public BreakableItemObject FindClosestBreakableItemObjectExceptFence(Stage stage, float effectiveRange)
        {
            float targetRecognizingRange = effectiveRange + 1f;
            var target = stage.FindClosestBreakableItemObjectExceptFence(Pos);

            if (target == null ||
                ((Vector2)target.transform.position - Pos).sqrMagnitude > (targetRecognizingRange * targetRecognizingRange))
            {
                return null;
            }

            return target;
        }

        private Character FindAndUpdateNewTarget(Stage stage, float targetRecognizingRange)
        {
            var target = stage.FindClosestCharacter(
                Alliance.ToEnemyAlliance(),
                Pos,
                // 대부분의 경우엔 사정거리 내에 적이 잇을 것이므로, 좁은 영역으로 먼저 탐색한다.
                limitDistance: targetRecognizingRange,
                condition: character => !character.Action.IsDead && !character.IsImmuneToHit
            );

            if (target != null)
            {
                return target;
            }

            // 타겟의 충돌체가 더 넓게 걸쳐있을 수 있기 때문에, 추가 검색한다.
            target = stage.FindClosestCharacter(
                Alliance.ToEnemyAlliance(),
                Pos,
                limitDistance: targetRecognizingRange + 6f,
                condition: character => !character.Action.IsDead && !character.IsImmuneToHit
            );

            if (target == null)
            {
                return null;
            }

            // 인식거리에 상대방의 콜라이더가 겹쳐있으면 공격가능하다.
            float effectiveRange = targetRecognizingRange + target.ColliderRadius;
            if ((target.Pos - Pos).sqrMagnitude > (effectiveRange * effectiveRange))
            {
                return null;
            }

            return target;
        }

        private void InitializeHeroGradeEffects(HeroData heroData)
        {
            var gradeEffects = StaticDataRepository.Instance.Heroes.GetHeroGradeEffects(heroData.HeroType);
            foreach (var kvp in gradeEffects)
            {
                if (kvp.Key > heroData.Grade)
                {
                    continue;
                }

                var heroGradeEffect = kvp.Value;
                _conditionalEffects.AddConditionalEffectByGradeEffect(
                    heroGradeEffect.GradeEffectType,
                    heroGradeEffect.Parameter1,
                    heroGradeEffect.Parameter2);
            }
        }

        private void InitializeEquipmentEffects(IEnumerable<EquipmentData> equippedEquipments)
        {
            _equippedEquipments = equippedEquipments;

            float calculatedAttackPower = 0f;
            float calculatedMaxHp = 0f;

            foreach (var equippedEquipment in _equippedEquipments)
            {
                var equipmentStat = AvatarLogic.CalculateEquipmentStat(equippedEquipment);
                switch (equipmentStat.StatType)
                {
                    case StatType.AttackPower:
                        calculatedAttackPower += equipmentStat.CalculatedValue;
                        break;
                    case StatType.MaxHP:
                        calculatedMaxHp += equipmentStat.CalculatedValue;
                        break;
                    default:
                        throw new NotImplementedException($"{equipmentStat.StatType} 구현 안 됨");
                }

                AddConditionalEffectFromEquipmentGradeEffect(equippedEquipment);
            }

            _equipmentStatModifiers = new EquippedEquipmentStatModifiers(
                addMaxHP: calculatedMaxHp, multMaxHP: 0f,
                addAttackPower: calculatedAttackPower, multAttackPower: 0f);
            _equipmentStatModifiers.Value.Apply(this.Stats);

            this.InitializeHp();
        }

        private void DestroyEquipmentEffects()
        {
            _equipmentStatModifiers?.Disapply(this.Stats);
            _equipmentStatModifiers = null;
        }

        private void AddConditionalEffectFromEquipmentGradeEffect(EquipmentData equipmentData)
        {
            var equipmentGradeEffects = StaticDataRepository.Instance.Equipments.GetEquipmentGradeEffects(equipmentData.EquipmentId);
            foreach (var kvp in equipmentGradeEffects)
            {
                if (kvp.Key > equipmentData.Grade)
                {
                    continue;
                }

                var equipmentGradeEffect = kvp.Value;
                _conditionalEffects.AddConditionalEffectByGradeEffect(
                    equipmentGradeEffect.GradeEffectType,
                    equipmentGradeEffect.Parameter1,
                    equipmentGradeEffect.Parameter2);
            }
        }

        public void RecoverHPFromResorativeObject(Stage stage)
        {
            this.RecoverHP(stage, 0.3f * Stats.EatingHPRecoveryRate.Value * MaxHP);
            _conditionalEffects.AcquiredHeart(stage, this);
        }

        public override void RecoverHP(Stage stage, float increment)
        {
            //모든 회복량 증가 스탯을 적용한 최종 회복량 계산을 해준다.
            //이후 과정은 기본 회복 과정과 동일
            float applyRecoveryStatIncrement = increment * Stats.HPRecoveryRate.Value;
            base.RecoverHP(stage, applyRecoveryStatIncrement);

            if (stage.PC == this)
            {
                var stageUIRoot = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
                stageUIRoot.UpdatePCHP((int)this.CurrentHP, (int)this.MaxHP);
            }
        }

        /// <summary>
        /// 방어막을 생성합니다. 피해를 받으면 방어막 개수를 1개 줄이고 피해를 무효화합니다.
        /// </summary>
        public override void CreateShield()
        {
            if (_shield != null)
            {
                Debug.LogWarning("방어막이 이미 생성되어 있습니다. 방어막은 여러 번 생성할 수 없습니다. 등급 효과를 확인해주세요.");
                return;
            }
            _shield = new PlayerShield(this);
        }

        /// <summary>
        /// 체력 버퍼를 생성합니다. 피해를 받으면 체력 버퍼가 대신 피해를 받고, 미처 완충하지 못하고 남은 피해만 받습니다.
        /// </summary>
        /// <param name="maxHpPercentage">
        /// 체력 버퍼의 최대 체력으로 설정할 플레이어 캐릭터의 기본 최대 체력의 비율.
        /// </param>
        public void CreateHPBuffer(float maxHpPercentage)
        {
            if (_hpBuffer != null)
            {
                Debug.LogWarning("체력 버퍼가 이미 생성되어 있습니다. 체력 버퍼는 여러 번 생성할 수 없습니다. 등급 효과를 확인해주세요.");
                return;
            }

            _hpBuffer = new HPBuffer(this, maxHpPercentage);
        }

        /// <summary>
        /// 체력 버퍼를 파괴합니다.
        /// </summary>
        public void DestroyHPBuffer()
        {
            if (_hpBuffer == null)
            {
                Debug.LogWarning("체력 버퍼가 null인데 파괴하려고 했습니다.");
                return;
            }

            _hpBuffer.Destroy();
            _hpBuffer = null;
        }
    }
}
