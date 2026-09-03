using DG.Tweening;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using Z.Animations.Placeholder;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Z.GameClients.Stages.Characters.Actions;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies;
using Z.GameClients.Stages.Characters.Shields;
using Z.GameClients.Stages.Characters.Stats;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.Scenes;
using Z.UIs.Stages.HUDs;
using Z.UnityHelpers;
using Random = UnityEngine.Random;
using Sequence = DG.Tweening.Sequence;

namespace Z.GameClients.Stages.Characters.Monsters
{
    public partial class Monster : Character
    {
        public MonsterStaticData StaticData { get; private set; }
        public bool IsBigSizeMonster => this.ColliderRadius > 1.0f;

        public new MonsterAnimationController AnimationController => (MonsterAnimationController)base._animationController;

        //// 타겟을 공격가능한 거리

        // 몬스터에서 AI관련 이벤트가 발생하면, AIController에 이벤트를 전달해주기 위한 인터페이스
        public IMonsterAIEvent MonsterAIEvent { get; private set; }

        //몬스터가 죽을때 떨어지는 경험치, 골드, 아이템 정보
        public long DropExp { get; private set; }
        public long DropGolds { get; private set; }
        public IReadOnlyList<DropItemType> DropItems { get; private set; }

        // 몬스터의 충돌기반 공격 주기. 이 시간 주기로 공격한다. 
        // 몬스터의 적은 PC하나 (혹은 소환수 몇개)로 그 숫자가 적기 때문에, 시간주기 처리로 충분하다.
        private float _lastCollisionAttackedAt;
        public float CollisionAttackDuration => StaticData.CollisionAttackSpeed <= 0f ? 10f : (1.0f / StaticData.CollisionAttackSpeed);
        public float CollisionAttackPower => _stats.AttackPower.Value;
        public override float RangeAttackPower => SpecialAttackPower;
        public float SpecialAttackPower => _stats.AttackPower.Value * StaticData.SpecialAttack1DamageRate;

        // 뽀잉뽀잉하는 피격 애니메이션
        private Sequence _hittedBodyAnimation;

        private bool _isCheckHitOnCollisionArea;

        private float _attackRange;
        public float AttackRange => _attackRange;
        private int _projectileCount;
        public int ProjectileCount => _projectileCount;
        private float _projectileSpeed;
        public float ProjectileSpeed => _projectileSpeed;

        private EmphasisCircle _emphasisCircle;
        private MonsterNameDisplayer _monsterNameDisplayer;
        private NavigationArrow _navigationArrow;

        public override void AllocateSharedResources(
            CharacterType characterType,
            string skeletonDataPath,
            float colliderRadius,
            Vector2 hitBoxOffset,
            Vector2 hitBoxSize,
            float shadowSize
            )
        {
            base.AllocateSharedResources(characterType, skeletonDataPath, colliderRadius, hitBoxOffset, hitBoxSize, shadowSize);

            _lastCollisionAttackedAt = 0f;
            float bodyLocalScale = _animationController.BodyLocalScale;
            Body.transform.localScale = new Vector3(bodyLocalScale, bodyLocalScale, 1.0f);
            if (!IsBigSizeMonster)
            {
                var sequence = DOTween.Sequence(this.gameObject);
                sequence.Append(Body.transform.DOScale(bodyLocalScale * 1.2f, 0.13f));
                sequence.Append(Body.transform.DOScale(bodyLocalScale * 0.9f, 0.10f));
                sequence.Append(Body.transform.DOScale(bodyLocalScale * 1.15f, 0.10f));
                sequence.Append(Body.transform.DOScale(bodyLocalScale * 1.0f, 0.10f));

                sequence.SetRecyclable(true);
                sequence.SetAutoKill(false);
                sequence.Pause();

                _hittedBodyAnimation = sequence;
            }
            else
            {
                var sequence = DOTween.Sequence(this.gameObject);
                sequence.Append(Body.transform.DOScale(bodyLocalScale * 1.05f, 0.07f));
                sequence.Append(Body.transform.DOScale(bodyLocalScale * 0.95f, 0.05f));
                sequence.Append(Body.transform.DOScale(bodyLocalScale * 1.01f, 0.04f));
                sequence.Append(Body.transform.DOScale(bodyLocalScale * 1.0f, 0.03f));

                sequence.SetRecyclable(true);
                sequence.SetAutoKill(false);
                sequence.Pause();

                _hittedBodyAnimation = sequence;
            }

            this.gameObject.layer = LayerMask.NameToLayer("Monster");

            Vector2 offset = new Vector2(0.0f, _bodyRenderer.bounds.size.y);
        }

        public virtual void InitializeMonster(
            AllianceType alliance,
            MonsterStaticData staticData,
            MonsterInstanceInitialData initialData,
            Stage stage,
            bool isBoss,
            bool isElite,
            MonsterAIBlackboardBase? aiBlackboard)
        {
            StaticData = staticData;

            float hpWeight = initialData.hpWeight + stage.AdditionalMaxHPWeight;
            float attackPowerWeight = initialData.attackPowerWeight + stage.AdditionalAttackPowerWeight;
            float moveSpeedWeight;

            //이동속도 증가 가중치는 기본 이동속도 5아래에만 적용된다.
            if (StaticData.MoveSpeed > 5)
            {
                moveSpeedWeight = 1f;
            }
            else
            {
                if (isBoss || isElite)
                {
                    moveSpeedWeight = 1f + stage.AdditionalEliteBossMonsterMoveSpeedWeight;
                }
                else
                {
                    moveSpeedWeight = 1f + stage.AdditionalBasicMonsterMoveSpeedWeight;
                }
            }

            var baseStats = BaseStats.FromMonsterStaticData(
                staticData,
                hpWeight,
                attackPowerWeight,
                moveSpeedWeight
                );

            this.InitializeCharacter(alliance, baseStats, staticData.Mass, staticData.Drag);

            if (stage.IsMonsterCCImmune)
            {
                this.SetImmuneToKnockBack();
            }

            var aiController = new MonsterAIController();
            MonsterAIEvent = (IMonsterAIEvent)aiController;
            aiController.InitializeAIStrategy(owner: this, stage, aiBlackboard);

            DropExp = initialData.dropExp;
            DropGolds = initialData.dropGolds;
            DropItems = initialData.dropItems;

            _lastCollisionAttackedAt = Time.time - CollisionAttackDuration + Random.Range(-0.05f, 0.05f);
            _isCheckHitOnCollisionArea = initialData.isHitOnCollision;

            _projectileCount = initialData.projectileCount;
            _projectileSpeed = initialData.projectileSpeed;
            _attackRange = initialData.attackRange;

            _isBoss = isBoss;
            _isElite = isElite;
        }

        //소환 전용 초기화 함수
        //기존 초기 Action에 SummonAction을 넣어 소환중 무적시간을 갖는다.
        //소환된 몬스터는 SummonedMonsterAIStrategy AI를 사용한다.
        public virtual ISummonedMonsterCommandSender InitializeSummonMonster(AllianceType alliance, MonsterStaticData staticData, MonsterInstanceInitialData initialData, Stage stage, float summonTime, bool isBoss)
        {
            StaticData = staticData;

            var baseStats = BaseStats.FromMonsterStaticData(
                staticData,
                initialData.hpWeight + stage.AdditionalMaxHPWeight,
                initialData.attackPowerWeight + stage.AdditionalAttackPowerWeight,
                1 + stage.AdditionalBasicMonsterMoveSpeedWeight);

            this.InitializeCharacter(alliance, baseStats, staticData.Mass, staticData.Drag);

            var aiController = new MonsterAIController();
            MonsterAIEvent = (IMonsterAIEvent)aiController;
            ISummonedMonsterCommandSender commandSender = aiController.InitializeSummonMonsterAIStrategy(owner: this, stage);

            Action.ChangeTo(stage, new SummonedAction(AnimationController, owner: this, summonTime));

            DropExp = initialData.dropExp;
            DropGolds = initialData.dropGolds;
            DropItems = initialData.dropItems;

            _lastCollisionAttackedAt = Time.time - CollisionAttackDuration + Random.Range(-0.05f, 0.05f);
            _isCheckHitOnCollisionArea = initialData.isHitOnCollision;

            _projectileCount = initialData.projectileCount;
            _projectileSpeed = initialData.projectileSpeed;
            _attackRange = initialData.attackRange;
            _isBoss = isBoss;
            _isElite = false;
            return commandSender;
        }

        /// <summary>
        /// 몬스터의 주변에 특별한 몬스터임을 강조하는 강조 원을 추가합니다.
        /// </summary>
        /// <param name="color">
        /// 강조 원의 색깔입니다.
        /// </param>
        public void AddEmphasisCircle(EmphasisCircle.Color color)
        {
            if (_emphasisCircle != null)
            {
                return;
            }

            _emphasisCircle = ResourcePool.Instance.InstantiateFromResource<EmphasisCircle>(EmphasisCircle.PREFAB_PATH);
            _emphasisCircle.Initialize(this, color);
        }

        /// <summary>
        /// 몬스터의 주변에 특별한 몬스터임을 강조하는 강조 원을 제거합니다.
        /// </summary>
        private void RemoveEmphasisCircle()
        {
            if (_emphasisCircle == null)
            {
                return;
            }

            ResourcePool.Instance.PutBackInstance(EmphasisCircle.PREFAB_PATH, _emphasisCircle.gameObject);
            _emphasisCircle = null;
        }

        /// <summary>
        /// 몬스터의 이름을 보여주는 디스플레이어를 추가합니다.
        /// </summary>
        /// <param name="color">
        /// 몬스터 이름 디스플레이어의 색깔입니다.
        /// </param>
        public void AddMonsterNameDisplayer(MonsterNameDisplayer.Color color)
        {
            if (_monsterNameDisplayer != null)
            {
                return;
            }

            _monsterNameDisplayer = ResourcePool.Instance.InstantiateFromResource<MonsterNameDisplayer>(MonsterNameDisplayer.PREFAB_PATH);
            _monsterNameDisplayer.Initialize(this, color);
        }

        /// <summary>
        /// 몬스터의 이름을 보여주는 디스플레이어를 제거합니다.
        /// </summary>
        private void RemoveMonsterNameDisplayer()
        {
            if (_monsterNameDisplayer == null)
            {
                return;
            }

            ResourcePool.Instance.PutBackInstance(MonsterNameDisplayer.PREFAB_PATH, _monsterNameDisplayer.gameObject);
            _monsterNameDisplayer = null;
        }

        /// <summary>
        /// 몬스터를 가리키는 내비게이션 화살표를 추가합니다.
        /// </summary>
        /// <param name="stage">
        /// 스테이지.
        /// </param>
        /// <param name="showAlways">
        /// 내비게이션 화살표를 항상 보여줄 것인가?
        /// </param>
        /// <param name="prefabPath">
        /// 내비게이션 화살표의 프리팹 경로.
        /// </param>
        /// <param name="iconText">
        /// 내비게이션 화살표에 보여줄 텍스트.
        /// </param>
        public void AddNavigationArrow(Stage stage, bool showAlways, string prefabPath, string iconText)
        {
            if (_navigationArrow != null)
            {
                return;
            }

            _navigationArrow = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>().AddNavigationArrow(stage, transform, showAlways, prefabPath, iconText);
        }

        /// <summary>
        /// 몬스터를 가리키는 내비게이션 화살표를 제거합니다.
        /// </summary>
        private void RemoveNavigationArrow()
        {
            if (_navigationArrow == null)
            {
                return;
            }

            _navigationArrow.RemoveNavigationArrow();
        }

        protected override CharacterAnimationController CreateCharacterAnimationController(GameObject bodyObject, string bodyDataPath)
        {
            CharacterAnimationController characterAnimationController = null;

            if (Path.GetExtension(bodyDataPath) == ".asset")
            {
                // Spine Skeleton 
                SkeletonDataAsset skeletonDataAsset = ResourcePool.Instance.LoadResource<SkeletonDataAsset>(bodyDataPath);
                Debug.Assert(skeletonDataAsset != null);
                var body = bodyObject.AddComponent<SkeletonAnimation>();
                bodyObject.AddComponent<SpriteAnimationHandler>();
                _bodyRenderer = bodyObject.GetComponent<MeshRenderer>();
                body.skeletonDataAsset = ResourcePool.Instance.LoadResource<SkeletonDataAsset>(bodyDataPath);
                body.Initialize(true);
                characterAnimationController = new SpineMonsterAnimationController(
                    body,
                    _bodyRenderer,
                    _isBoss,
                    CharacterType,
                    idleAnimationName: "idle",
                    walkAnimationName: "move",
                    hittedAnimationName: "hitted",
                    attackAnimationName: "attack",
                    deadAnimationName: "die",
                    appearAnimationName: "appear",
                    disappearAnimationName: "disappear",
                    healAnimationName: "heal"
                );
            }
            else
            {
                // Sprite AnimationController
                SpriteRenderer renderer = bodyObject.AddComponent<SpriteRenderer>();
                _bodyRenderer = renderer;
                var body = bodyObject.AddComponent<Animator>();
                body.runtimeAnimatorController = ResourcePool.Instance.LoadResource<RuntimeAnimatorController>(bodyDataPath);
                if (body.runtimeAnimatorController == null)
                {
                    Debug.LogError($"{bodyDataPath}에 해당하는 애니메이션 컨트롤러를 찾을 수 없습니다. 올바로 동작하지 않습니다. 리소스 경로 확인해주세요.");
                }

                characterAnimationController = new SpriteMonsterAnimationController(
                    body,
                    _bodyRenderer,
                    _isBoss,
                    idleAnimationName: "idle",
                    walkAnimationName: "move",
                    hittedAnimationName: "hitted",
                    attackAnimationName: "attack",
                    deadAnimationName: "die",
                    appearAnimationName: "appear",
                    disappearAnimationName: "disappear",
                    healAnimationName: "heal"
                );
            }

            return characterAnimationController;
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_hittedBodyAnimation != null)
            {
                _hittedBodyAnimation.Kill();
                _hittedBodyAnimation = null;
            }
        }

        public override void UpdateLogic(Stage stage)
        {
            float now = Time.time;

#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("Monster.UpdateLogic.BaseUpdateLogic()"))
#endif
            {
                base.UpdateLogic(stage);
            }

            if (this.Action.IsDead)
            {
                return;
            }

#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("Monster.UpdateLogic.HitOnCollisionArea"))
#endif
            {
                if (_isCheckHitOnCollisionArea)
                {
                    this.HitOnCollisionArea(stage, now);
                }
            }

#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("Monster.UpdateLogic.MonsterAIEventUpdate"))
#endif
            {
                MonsterAIEvent.Update(stage);
            }

#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("Monster.UpdateLogic.UpdateShield"))
#endif
            {
                Shield?.UpdateLogic();
            }

        }

        public override void OnEnterredIntoStage(Stage stage)
        {
            base.OnEnterredIntoStage(stage);

            MonsterAIEvent?.OnEnterredIntoStage(stage);
        }

        public void SetCheckHitOnCollisionArea(bool isCheckHitOnCollisionArea)
        {
            _isCheckHitOnCollisionArea = isCheckHitOnCollisionArea;
        }

        private readonly List<Character> v_Targets = new List<Character>();
        private void HitOnCollisionArea(Stage stage, float now)
        {
            if ((_lastCollisionAttackedAt + CollisionAttackDuration) <= now)
            {
                v_Targets.Clear();

                var enemyAlliance = Alliance.ToEnemyAlliance();
                var collisionArea = new CircularTargetArea(Pos, CollisionAttackRadius);
                stage.FindAliveCharactersInArea(enemyAlliance, collisionArea, in v_Targets);

                float collisionDamage = CollisionAttackPower;
                foreach (var target in v_Targets)
                {
                    var delta = (target.Pos - this.Pos);
                    var hitVector = delta.normalized;
                    target.Hitted(stage, this, collisionDamage, hitVector, this.Pos + delta * 0.4f, string.Empty);
                }

                if (v_Targets.Count > 0)
                {
                    _lastCollisionAttackedAt = now;
                }

                v_Targets.Clear();
            }
        }

        public override float Hitted(Stage stage, Character attacker, float damage, Vector2 hitVector, Vector2 hitPosition, string hitSoundPrefabPath)
        {
            // 방어막이 활성화되어 있으면 방어막 개수를 하나 줄이고 대미지를 무시한다.
            if (IsShieldActive)
            {
                _shield.DecreaseShieldAmount(1);
                return 0.0f;
            }
            float finalDamage = base.Hitted(stage, attacker, damage, hitVector, hitPosition, hitSoundPrefabPath);

            MonsterAIEvent.OnHitted(stage, attacker);

            float baseBodyLocalScale = _animationController.BodyLocalScale;
            Body.transform.localScale = new Vector3(baseBodyLocalScale, baseBodyLocalScale, baseBodyLocalScale);

            if (!_action.IsDead)
            {
                _hittedBodyAnimation.Restart();
            }

            return finalDamage;
        }

        public override void Move(Vector2 moveVector)
        {
            base.Move(moveVector);
        }

        public override void StopMovement()
        {
            base.StopMovement();
            _animationController.PlayIdleAttackActionInfinitely();
        }

        protected override void Dead(Stage stage, Vector2 hitVector)
        {
            if (_action.IsDead)
            {
                Debug.LogWarning($"이미 죽엇는데 또 죽으려함. 뭔가 이상합니다. 확인해주세요. {this.name}");
                // 보상 중복해서 떨구지 않도록 바로 리턴한다.
                return;
            }

            base.Dead(stage, hitVector);

            if (DropExp > 0)
            {
                stage.CreateExpObject(DropExp, Pos);
            }

            if (DropGolds > 0)
            {
                Vector2 randomOffset = 2.0f * Random.insideUnitCircle;
                // DropGolds는 몬스터 생성시점에, 스테이지에서 이미 가져온 골드다. stage.TakeDropGolds()를 추가로 진행하지 않는다.
                stage.CreateGoldObject(DropGolds, Pos + randomOffset);
            }

            //보스 처치시 장비 강화석 드랍 코드
            if (IsBoss && stage.StageType == StageType.Chapter)
            {
                long totalRandomEquipmentTicket = stage.ChapterStaticData!.TotalRandomEquipmentTicket;

                //드랍해야되는 강화티켓이 보스 총 수보다 적음
                if (totalRandomEquipmentTicket < stage.GetTotalBossAmount())
                {
                    long remainingBossAmount = stage.GetTotalBossAmount() - (stage.EliminatedBosses + 1);
                    long dropAmount;
                    //남은 개수와 앞으로 드랍해야될 개수 확인
                    if (stage.RemainingEquipmentElement > remainingBossAmount)
                    {
                        //확정으로 하나
                        dropAmount = 1;
                    }
                    else
                    {
                        //랜덤으로 하나
                        dropAmount = Random.Range(0, 2);
                    }
                    long elementToDrop = stage.TakeMonsterDropRandomEquipmentElement(dropAmount);
                    for (int i = 0; i < elementToDrop; i++)
                    {
                        Vector2 randomOffset = 2.0f * Random.insideUnitCircle;
                        stage.CreateRandomEquipmentElementObject(Pos + randomOffset);
                    }
                }
                else
                {
                    //총 보스수 - 죽은 보스수 = 남아있는 보스수 (최소 확보해야될 보석 개수)
                    //나 당사자는 아직 죽은처리가 안되어 있기때문에 +1 해준다.
                    long remainingBossAmount = stage.GetTotalBossAmount() - (stage.EliminatedBosses + 1);

                    //남아있는 아이탬 개수값에서 최소 확보해야될 개수만큼 빼준다.
                    long randomAmount = (long)Random.Range(1, stage.RemainingEquipmentElement - remainingBossAmount + 1);

                    //랜덤 개수만큼 아이템 생성
                    long elementToDrop = stage.TakeMonsterDropRandomEquipmentElement(randomAmount);
                    for (int i = 0; i < elementToDrop; i++)
                    {
                        Vector2 randomOffset = 2.0f * Random.insideUnitCircle;
                        stage.CreateRandomEquipmentElementObject(Pos + randomOffset);
                    }
                }

            }

            for (int index = 0; index < DropItems.Count; ++index)
            {
                this.CreateDropItem(DropItems[index], stage);
            }

            _characterCollider.enabled = false;
            _hitBoxCollider.enabled = false;
            this.RemoveEmphasisCircle();
            this.RemoveMonsterNameDisplayer();
            this.RemoveNavigationArrow();

            MonsterAIEvent.OnDead(stage);

            if (Alliance == AllianceType.Monsters)
            {
                stage.IncreaseMonsterKillCount(_isBoss, _isElite);
            }

            stage.DeadEffects.CreateNormalDeadEffect(owner: this, hitVector);
        }

        public override void DisappearFromStage(Stage stage) => this.DisappearFromStage(stage, withDeadEffect: true);

        public void DisappearFromStage(Stage stage, bool withDeadEffect)
        {
            if (_action.IsDead)
            {
                return;
            }

            base.DisappearFromStage(stage);

            _characterCollider.enabled = false;
            _hitBoxCollider.enabled = false;
            this.RemoveEmphasisCircle();
            this.RemoveMonsterNameDisplayer();
            this.RemoveNavigationArrow();

            MonsterAIEvent.OnDisappearing(stage);

            if (withDeadEffect)
            {
                stage.DeadEffects.CreateNormalDeadEffect(owner: this, Vector2.zero);
            }
        }

        public override void CreateDamagePopup(Stage stage, Character? attacker, Vector2 hitPosition, float finalDamage, Vector2 hitVector, bool isAttack)
        {
            if (_alliance != AllianceType.Players)
            {
                var damageFloaterPosition = this.CenterPos + (this.CenterPos - hitPosition) * 0.5f;
                stage.DamagePopups.CreateDamagePopup(attacker, damageFloaterPosition, finalDamage, new Vector2(0.0f, 0.05f), isAttack); //hitVector);
            }
        }

        private void CreateDropItem(DropItemType itemType, Stage stage)
        {
            Vector2 randomOffset = 2.0f * Random.insideUnitCircle;
            Vector2 monsterPosition = this.transform.position;
            Vector2 resultSpawnPosition = monsterPosition + randomOffset;
            switch (itemType)
            {
                case DropItemType.HpResorative:
                    {
                        stage.CreateHpResorativeObject(resultSpawnPosition);
                        return;
                    }
                case DropItemType.Gold:
                    {
                        Debug.LogError(
                            $"스테이지[{stage.StaticData.StageNumber}]의 몬스터[{this.CharacterType}]에서 골드[{itemType}]가 드롭되었습니다.\n" +
                            $"몬스터 한테 골드 드랍 될 시 기획확인이 필요합니다.\n" +
                            $"스테이지마다 드랍 가능한 골드량이 정해져있습니다. 영향이 있는지 없는지 확인하고 수정해주세요.\n");
                        stage.CreateGoldObject(1, resultSpawnPosition);
                        return;
                    }
                case DropItemType.ExpMagnet:
                    {
                        stage.CreateExpMagnetObject(resultSpawnPosition);
                        return;
                    }
                case DropItemType.SkillBox:
                    {
                        stage.CreateSkillBoxObject(resultSpawnPosition);
                        return;
                    }
                case DropItemType.Tutorial3SkillBox:
                    {
                        stage.CreateSkillBoxObject(resultSpawnPosition);
                        return;
                    }
                case DropItemType.Tutorial5SkillBox:
                    {
                        stage.CreateSkillBoxObject(resultSpawnPosition);
                        return;
                    }
                case DropItemType.StarCore:
                    {
                        stage.CreateStarCoreObject(resultSpawnPosition);
                        return;
                    }
                default:
                    {
                        Debug.LogError($"{itemType}은 몬스터에서 스폰할 수 없음");
                        return;
                    }
            }
        }

        public void SetBodyPixelYOffset(float offsetPixel)
        {
            Vector3 pos = Body.transform.localPosition;
            pos.y += (offsetPixel * 0.01f);
            Body.transform.localPosition = pos;
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

            if(IsBoss)
            {
                _shield = new BossShield(this);
            }
            else
            {
                _shield = new MonsterShield(this);
            }
        }

        private void LateUpdate()
        {
            (_animationController as SpriteMonsterAnimationController)?.LateUpdate();
        }
    }

}
