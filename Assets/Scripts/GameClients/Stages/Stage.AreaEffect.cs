using Shared.GameDataTypes;
using System;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.AreaEffectObjects;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.Characters.PCs;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages
{
    public partial class Stage
    {
        // Partial 클래스의 멤버는 생성자가 정의된 기본 코드파일에 정의해주세요. 
        // 이 클래스의 경우, Stage.cs가 멤버를 정의할 기본 코드 파일입니다.

        private void UpdateAreaEffects(float now, float deltaTime)
        {
#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("Stage.UpdateAreaEffects"))
#endif
            {
                // _aliveAreaEffects를 이터레이팅하면서 추가로 생성/삭제되는 오브젝트를 바로 _aliveAreaEffects에 반영하면
                // 컨테이너의 이터레이터가 무효화되기 때문에, 이를 피하기 위해 코드가 살짝 복잡하다.
                foreach (var areaEffect in _areaEffectsCreatedOnThisFrame)
                {
                    _aliveAreaEffects.Add(areaEffect);
                }
                _areaEffectsCreatedOnThisFrame.Clear();

                this.DoRemoveReservedAreaEffects();

                foreach (var areaEffect in _aliveAreaEffects)
                {
                    if (areaEffect.IsAlive)
                    {
                        areaEffect.UpdateLogic(this, deltaTime);
                    }
                    else
                    {
                        _areaEffectsToRemove.Add(areaEffect);
                    }
                }

                this.DoRemoveReservedAreaEffects();
            }
        }

        public void ReserveToRemoveAreaEffect(AreaEffectObjectBase areaEffect)
        {
            areaEffect.gameObject.SetActive(false);

            if (_areaEffectsToRemove.Contains(areaEffect))
            {
                Debug.LogWarning($"{areaEffect.name}이 중복 삭제 요청됨. 뒤의 요청은 무시합니다.");
                return;
            }
            _areaEffectsToRemove.Add(areaEffect);
        }

        private void DoRemoveReservedAreaEffects()
        {
            foreach (var areaEffect in _areaEffectsToRemove)
            {
                if (!_aliveAreaEffects.Remove(areaEffect))
                {
                    if (!_areaEffectsCreatedOnThisFrame.Remove(areaEffect))
                    {
                        Debug.LogWarning($"장판이 이미 제거된 것 같은데요? {areaEffect.name}");
                        continue;
                    }
                }
                areaEffect.gameObject.SetActive(false);
                Debug.Assert(!_aliveAreaEffects.Contains(areaEffect));
                _areaEffectPool.PutBack(areaEffect);
            }
            _areaEffectsToRemove.Clear();
        }

        /// <summary>
        /// 현재 필드의 모든 AreaEffect를 순회하며 <paramref name="doAction"/>을 실행한다.
        /// 어떤 순서로 처리할지 여부는 보장되지 않는다.
        /// </summary>
        /// <remarks>doAction내부에서 AreaEffect를 추가하거나 삭제해서는 안된다.</remarks>
        /// <param name="doAction">이 false를 리턴하면 순회를 종료한다. true를 리턴하면 남은 순회를 이어서 지속한다.</param>
        public void ForAllAliveAreaEffects(Func<AreaEffectObjectBase, bool> doAction)
        {
            foreach (var areaEffect in _aliveAreaEffects)
            {
                if (!doAction(areaEffect))
                {
                    return;
                }
            }
        }

        public SpinBladeObject CreateSpinBladeObject(
            AllianceType alliance,
            Character owner,
            float objectRadius,
            float movingRadius,
            float damage,
            float knockBackPower,
            float attackPeriod,
            float angleSpeedDegree,
            float startAngleDegree,
            string hitSoundPrefabPath)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SpinBladeObject>(AreaEffectType.SpinBlade);
            areaEffect.Initialize(alliance, owner, objectRadius, movingRadius, damage, knockBackPower, attackPeriod, angleSpeedDegree, startAngleDegree, hitSoundPrefabPath);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public GluttonObject CreateGluttonObject(
            AllianceType alliance,
            Character owner,
            float objectRadius,
            Vector2 movingDirection,
            float movingSpeed,
            float oppositeDirectionAcceleration,
            float damage,
            float knockBackPower,
            float lifeTime,
            bool isTranscend,
            string hitSoundPrefabPath)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<GluttonObject>(AreaEffectType.Glutton);
            areaEffect.Initialize(
                alliance, owner, objectRadius, movingDirection, movingSpeed,
                oppositeDirectionAcceleration, damage, knockBackPower,
                lifeTime, isTranscend, hitSoundPrefabPath);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public PlasmaDrillObject CreatePlasmaDrillObject(
            AllianceType alliance,
            Character owner,
            float objectRadius,
            Vector2 movingDirection,
            float movingSpeed,
            float damage,
            float knockBackPower,
            float attackPeriod,
            float lifeTime,
            bool isTranscend,
            string hitSoundPrefabPath)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<PlasmaDrillObject>(AreaEffectType.PlasmaDrill);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(alliance, owner, objectRadius, movingDirection, movingSpeed, damage, knockBackPower, attackPeriod, lifeTime, isTranscend, hitSoundPrefabPath);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            return areaEffect;
        }

        public TentiSweepAreaEffectObject CreateTentiSweepObject(
           AllianceType alliance,
           PlayerCharacter owner,
           Vector2 attackDirection,
           float damage,
           float areaRatio,
           float knockBackPower,
           float normalMonsterStunDuration,
           string hitSoundPrefabPath)
        {
            var areaEffectType = AreaEffectType.TentiSweep_Default;

            var areaEffect = _areaEffectPool.TakeOneFromPool<TentiSweepAreaEffectObject>(areaEffectType);
            areaEffect.Initialize(alliance, owner, attackDirection, damage, areaRatio, knockBackPower, normalMonsterStunDuration, hitSoundPrefabPath);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public TentiSweepVerticalObject CreateTentiSweepVerticalObject(
            PlayerCharacter owner,
            Vector2 attackPosition,
            float damage,
            float areaRatio,
            float knockBackPower,
            float hpDrainPercent,
            string hitSoundPrefabPath)
        {
            var areaEffectType = AreaEffectType.TentiSweepVertical_Default;

            var areaEffect = _areaEffectPool.TakeOneFromPool<TentiSweepVerticalObject>(areaEffectType);
            areaEffect.Initialize(owner, attackPosition, damage, areaRatio, knockBackPower, hpDrainPercent, hitSoundPrefabPath);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public TentiSweepTranscandentObject CreateTentiSweepTranscendentObject(
           AllianceType alliance,
           PlayerCharacter owner,
           Vector2 imageDirection,
           float damage,
           float areaRatio,
           float knockBackPower,
           List<(float, Vector2)> directions,
           float normalMonsterStunDuration,
           string hitSoundPrefabPath)
        {
            var areaEffectType = AreaEffectType.TentiSweepTranscendent_Default;

            var areaEffect = _areaEffectPool.TakeOneFromPool<TentiSweepTranscandentObject>(areaEffectType);
            areaEffect.Initialize(stage: this, alliance, owner, imageDirection, damage, areaRatio, knockBackPower, directions, normalMonsterStunDuration, hitSoundPrefabPath);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }


        public DeathTouchAreaEffectObject CreateDeathTouchEffectObject(
                  AllianceType alliance,
                  Character owner,
                  float areaEffectRadius,
                  Vector2 createPosition,
                  Vector2 destination,
                  float movingTime,
                  float damage,
                  float knockBackPower,
                  bool isTranscendent,
                  string hitSoundPrefabPath)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<DeathTouchAreaEffectObject>(AreaEffectType.DeathTouch);
            areaEffect.Initialize(alliance, owner, areaEffectRadius, createPosition, destination, movingTime, damage, knockBackPower, isTranscendent, hitSoundPrefabPath);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        //FlashLighterObject는 SpriteAnimationHandler를 사용해 애니메이션을 재생하고 있기 때문에
        //gameobject가 활성화 되어 있어야 초기화가 가능하다. gameobject가 비활성화 되어있으면 애니메이터가 초기화 되지 않는다.
        public AmbushMonsterObject CreateAmbushMonsterObject(
            AllianceType alliance,
            Character owner,
            float areaEffectRadius,
            Vector2 hitPoint,
            float damage,
            bool isTranscendent,
            string hitSoundPrefabPath
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<AmbushMonsterObject>(AreaEffectType.AmbushMonster);
            areaEffect.gameObject.SetActive(true);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.Initialize(stage: this, alliance, owner, areaEffectRadius, hitPoint, damage, isTranscendent, hitSoundPrefabPath);
            return areaEffect;
        }

        public IgnitionWaveTranscendAreaEffectObject CreateIgnitionWaveTranscendAreaEffectObject(
            PlayerCharacter owner, Vector2 firingDirection, float moveSpeed, float attackWidth, float attackRange, float damage, float knockBackPower, float burnDamage, float burnDuration, int burnSpreadAmount, float stunDuration, string hitSoundPrefabPath)
        {
            var areaEffectType = AreaEffectType.IgnitionWaveTranscend_Default;

            var areaEffect = _areaEffectPool.TakeOneFromPool<IgnitionWaveTranscendAreaEffectObject>(areaEffectType);
            areaEffect.gameObject.SetActive(true);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.Initialize(owner, firingDirection, moveSpeed, attackWidth, attackRange, damage, knockBackPower, burnDamage, burnDuration, burnSpreadAmount, stunDuration, hitSoundPrefabPath);
            return areaEffect;
        }

        public RuneTrapBombAreaEffectObject CreateRuneTrapBombAreaEffectObject(PlayerCharacter owner, float damage, float radius, float lifeTme, float knockBackPower, string hitSoundPrefabPath)
        {
            RuneTrapBombAreaEffectObject areaEffect = _areaEffectPool.TakeOneFromPool<RuneTrapBombAreaEffectObject>(AreaEffectType.TrapBomb);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, damage, radius, lifeTme, knockBackPower, hitSoundPrefabPath);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            return areaEffect;
        }

        public RuneTrapTranscendentAreaEffectObject CreateRuneTrapBombTranscendentAreaEffectObject(Stage stage, PlayerCharacter owner, Vector2 position, float damage, float radius, float lifeTme, float knockBackPower, string hitSoundPrefabPath)
        {
            RuneTrapTranscendentAreaEffectObject areaEffect = _areaEffectPool.TakeOneFromPool<RuneTrapTranscendentAreaEffectObject>(AreaEffectType.RuneTrap);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, position, damage, radius, lifeTme, knockBackPower, hitSoundPrefabPath);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            return areaEffect;
        }

        public DefensiveFieldAreaEffectObject CreateDefensiveFieldAreaEffectObject(PlayerCharacter owner, float damage, float radius, float attackInterval, float knockBackPower, bool isTranscend, string hitSoundPrefabPath)
        {
            DefensiveFieldAreaEffectObject areaEffectObject = _areaEffectPool.TakeOneFromPool<DefensiveFieldAreaEffectObject>(AreaEffectType.DefensiveField);
            areaEffectObject.gameObject.SetActive(true);
            areaEffectObject.Initialize(owner, damage, radius, attackInterval, knockBackPower, isTranscend, hitSoundPrefabPath);
            _areaEffectsCreatedOnThisFrame.Add(areaEffectObject);
            return areaEffectObject;
        }

        public GymBallObject CreateGymBallObject(AllianceType alliance, Character owner, Vector2 movingDirection, float movingSpeed, float damage,
            float knockBackPower, float lifeTime, int spliteCount, bool isTranscend, string hitSoundPrefabPath)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<GymBallObject>(AreaEffectType.GymBall);
            areaEffect.Initialize(alliance, owner, movingDirection, movingSpeed, damage, knockBackPower, lifeTime, spliteCount, isTranscend, hitSoundPrefabPath);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public MeteorAreaEffectObject CreateMeteorAreaEffectObject(
            Character owner,
            Vector2 dropPosition,
            float attackDamage,
            float attackRadius,
            float knockbackPower,
            float areaEffectLifetime,
            bool isTranscendent)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<MeteorAreaEffectObject>(AreaEffectType.Meteor);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, dropPosition, attackDamage, attackRadius, knockbackPower, areaEffectLifetime, isTranscendent);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public ReflectionAreaEffectObject CreateReflectionAreaEffectObject(
            Monster owner,
            AreaEffectType areaEffectType,
            float objectRadius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float rotatingSpeed,
            float damage,
            float knockBackPower,
            float lifeTime,
            Rect moveRect)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ReflectionAreaEffectObject>(areaEffectType);
            areaEffect.Initialize(owner, objectRadius, startPosition, movingDirection, movingSpeed, rotatingSpeed, damage, knockBackPower, lifeTime, moveRect);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        //벽, 맵에 반사되는 투사체 
        //투사체 프리팹 경로를 받아와 생성한다.
        public ReflectionAreaEffectObject CreateReflectionAreaEffectObject(
            Monster owner,
            float objectRadius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float rotatingSpeed,
            float damage,
            float knockBackPower,
            float lifeTime,
            Rect moveRect,
            string bodyImagePath)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ReflectionAreaEffectObject>(AreaEffectType.ReflectionObject);
            areaEffect.Initialize(owner, objectRadius, startPosition, movingDirection, movingSpeed, rotatingSpeed, damage, knockBackPower, lifeTime, moveRect, bodyImagePath);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public ReflectionAreaEffectMultiHitObject CreateReflectionAreaEffectMultiHitObject(
            Monster owner,
            float objectRadius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float rotatingSpeed,
            float damage,
            float lifeTime,
            int hitChances,
            Rect moveRect,
            string bodyImagePath)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ReflectionAreaEffectMultiHitObject>(AreaEffectType.ReflectionMultiHitObject);
            areaEffect.Initialize(owner, objectRadius, startPosition, movingDirection, movingSpeed, rotatingSpeed, damage, lifeTime, hitChances, moveRect, bodyImagePath);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }


        public IceThreeLeapsBugPoisonAreaEffect CreateIceThreeLeapsBugPoisonAreaEffect(
            Monster owner,
            Vector2 position,
            float lifeTime,
            float dropTime,
            float damage,
            float radius
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<IceThreeLeapsBugPoisonAreaEffect>(AreaEffectType.IceThreeLeapsBugPoison);
            areaEffect.Initialize(owner, position, lifeTime, dropTime, damage, radius);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public PoisonousAreaEffectObject CreatePoisonousAreaEffect(Character owner, Vector2 position, float lifeTime, float tickPeriod, float damage, float radius)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<PoisonousAreaEffectObject>(AreaEffectType.PoisonousArea);

            // 독장판 애니메이션을 재생하기 위해 gameObject를 먼저 활성화해준다.
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, position, lifeTime, tickPeriod, damage, radius);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public IceGolemShockWaveAreaEffectObject CreateIceGolemShockWaveAreaEffect(
            Monster owner,
            Vector2 position,
            float damage,
            float moveSpeed,
            float distance
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<IceGolemShockWaveAreaEffectObject>(AreaEffectType.IceGolemShockWave);
            areaEffect.Initialize(owner, position, damage, moveSpeed, distance);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public IgnitionWaveAreaEffectObject CreateIgnitionWaveAreaEffectObject(PlayerCharacter owner, Vector2 firingDirection, float damage, float attackDistance, float attackAngle, float knockbackPower, float burnDamage, float burnDuration, int burnSpreadAmount, string hitSoundPrefabPath)
        {
            var areaEffectType = AreaEffectType.IgnitionWave_Default;

            var areaEffect = _areaEffectPool.TakeOneFromPool<IgnitionWaveAreaEffectObject>(areaEffectType);
            areaEffect.Initialize(owner, firingDirection, damage, attackDistance, attackAngle, knockbackPower, burnDamage, burnDuration, burnSpreadAmount, hitSoundPrefabPath);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public EgyptSnakeDruidSnakeReflectionAreaEffect CreateEgyptSnakeDruidSnakeReflectionAreaEffectObject(
            Monster owner,
            AllianceType alliance,
            float objectRadius,
            Vector2 movingDirection,
            float movingSpeed,
            float damage,
            float knockBackPower,
            float lifeTime,
            Rect moveRect)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<EgyptSnakeDruidSnakeReflectionAreaEffect>(AreaEffectType.EgyptSnakeDruidSnakeReflectionObject);
            areaEffect.Initialize(owner, alliance, objectRadius, movingDirection, movingSpeed, damage, knockBackPower, lifeTime, moveRect);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }


        public EgyptSphinxVerticalAreaEffect CreateEgyptSphinxVerticalAreaEffect(
            Monster owner,
            Vector2 startPosition,
            float lifeTime,
            float moveSpeed,
            float radius,
            float damage)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<EgyptSphinxVerticalAreaEffect>(AreaEffectType.EgyptSphinxVerticalObject);
            areaEffect.Initialize(owner, startPosition, lifeTime, moveSpeed, radius, damage);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public AlphaGatlingHomingAreaEffectObject CreateAlphaGatlingHomingAreaEffect(
                    PlayerCharacter owner,
                    Stage stage,
                    float findTargetRange,
                    float areaEffectRadius,
                    Vector2 createPosition,
                    Vector2 startDirection,
                    float movingSpeed,
                    float damage,
                    float knockBackPower,
                    float aliveTime,
                    string soundPrefabPath)
        {
            var areaEffectType = AreaEffectType.AlphaGatlingHoming_Default;

            var areaEffect = _areaEffectPool.TakeOneFromPool<AlphaGatlingHomingAreaEffectObject>(areaEffectType);
            areaEffect.Initialize(owner,
                    stage,
                    findTargetRange,
                    areaEffectRadius,
                    createPosition,
                    startDirection,
                    movingSpeed,
                    damage,
                    knockBackPower,
                    aliveTime,
                    soundPrefabPath);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public SpineBodyAreaEffectObject CreateSandAreaEffectObject(
            Character owner,
            float delay,
            float indicatorDuration,
            float damage,
            float attackPeriod,
            float lifeTime,
            float objectRadius,
            Vector2 position
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SpineBodyAreaEffectObject>(AreaEffectType.RaAreaEffectObject);
            areaEffect.Initialize(owner, delay, indicatorDuration, damage, attackPeriod, lifeTime, objectRadius, position);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;

        }

        public AresSpearAreaEffectObject CreateAresAreaEffectObject(
            Character owner,
            Vector2 startPosition,
            Vector2 targetPosition,
            Vector2 backPosition,
            float splitDistance,
            float targetArrivedAt,
            float splitPositionArrivedAt,
            float waitAt,
            float backArrivedAt,
            float damage)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<AresSpearAreaEffectObject>(AreaEffectType.AresSpear);
            areaEffect.Initialize(owner, this, startPosition, targetPosition, backPosition, splitDistance, targetArrivedAt, splitPositionArrivedAt, waitAt, backArrivedAt, damage);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public ZeusLightningAreaEffectObject CreateZeusLightningAreaEffectObject(
            Character owner,
            Vector3 attackPosition,
            float attackRadius,
            float damage,
            float delay,
            float indicatorTime
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ZeusLightningAreaEffectObject>(AreaEffectType.ZeusLightning);
            areaEffect.Initialize(this, owner, attackPosition, attackRadius, damage, delay, indicatorTime);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public BloodRapierAreaEffectObject CreateBloodRapierAreaEffectObject(
            Character owner,
            Vector2 direction,
            float radius,
            float weekKnockbackPower,
            float mainKnockbackPower,
            float weekDamage,
            float mainDamage,
            float areaRatio,
            float hpDrainPercent,
            float hpDrainAmount,
            string hitSoundPrefabPath
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<BloodRapierAreaEffectObject>(AreaEffectType.BloodRapier);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, this, direction, radius, weekKnockbackPower, mainKnockbackPower, weekDamage, mainDamage, areaRatio, hpDrainPercent, hpDrainAmount, hitSoundPrefabPath);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public BloodRapierTranscendentObject CreateBloodRapierTranscendentAreaEffectObject(
            Character owner,
            Vector2 position,
            Vector2 direction,
            float damage,
            float hpDrainRatio,
            float knockbackPower,
            string hitSoundPrefabPath)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<BloodRapierTranscendentObject>(AreaEffectType.BloodRapierTranscendent);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, position, direction, damage, hpDrainRatio, knockbackPower, hitSoundPrefabPath);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public BattleYoYoAreaEffectObject CreateBattleYoYoAreaEffectObject(
            PlayerCharacter owner,
            Vector2 targetPosition,
            float moveDistance,
            float waitingDuration,
            float acceleration,
            float moveSpeed,
            float attackRadius,
            float knockbackPower,
            float damage,
            float deployYoyoDuration,
            float deployYoyoKnobackPower,
            float deployYoyoDamage,
            string hitSoundPrefabPath)
        {
            var areaEffectType = AreaEffectType.BattleYoYo_Default;

            var areaEffect = _areaEffectPool.TakeOneFromPool<BattleYoYoAreaEffectObject>(areaEffectType);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, targetPosition, moveDistance, waitingDuration, acceleration, moveSpeed, attackRadius, knockbackPower, damage, deployYoyoDuration, deployYoyoKnobackPower, deployYoyoDamage, hitSoundPrefabPath);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public BattleYoYoTranscendentObject CreateBattleYoYoTranscendentObject(
            PlayerCharacter owner,
            float damage,
            Vector2 startPosition,
            float radiusUpSpeed,
            float angleUpSpeed,
            float radiusUpAccelration,
            float angleUpAccelration,
            float maxRotateRadius,
            float attackRadius,
            float knockBackPower,
            Vector2 direction,
            string hitSoundPrefabPath
            )
        {
            var areaEffectType = AreaEffectType.BattleYoYoTranscendent_Default;

            var areaEffect = _areaEffectPool.TakeOneFromPool<BattleYoYoTranscendentObject>(areaEffectType);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, damage, startPosition, radiusUpSpeed, angleUpSpeed, radiusUpAccelration, angleUpAccelration, maxRotateRadius, attackRadius, knockBackPower, direction, hitSoundPrefabPath);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public LaserAreaEffectObject CreateLaserAreaEffectObject(
            Character owner,
            float damage,
            float attackTickInterval,
            float distance,
            float thickness,
            Vector2 startDirection,
            float angle,
            float attackDuration,
            bool isRightRotate
            )
        {
            var areaEffect = this.CreateLaserAreaEffectObject(owner, damage, attackTickInterval, distance, thickness, startDirection, angle, attackDuration, isRightRotate, true);
            return areaEffect;
        }
        public LaserAreaEffectObject CreateLaserAreaEffectObject(
            Character owner,
            float damage,
            float attackTickInterval,
            float distance,
            float thickness,
            Vector2 startDirection,
            float angle,
            float attackDuration,
            bool isRightRotate,
            bool isBodyEnable
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<LaserAreaEffectObject>(AreaEffectType.LaserAreaEffect);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, damage, attackTickInterval, distance, thickness, startDirection, angle, attackDuration, isRightRotate, isBodyEnable);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public LaserAreaEffectObject CreateXxperManLaserAreaEffectObject(
            Character owner,
            float damage,
            float attackTickInterval,
            float distance,
            float thickness,
            Vector2 startDirection,
            float angle,
            float attackDuration,
            bool isRightRotate,
            bool isBodyEnable)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<LaserAreaEffectObject>(AreaEffectType.XxperManLaser);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, damage, attackTickInterval, distance, thickness, startDirection, angle, attackDuration, isRightRotate, isBodyEnable);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public LaserAreaEffectObject CreateSolderManLaserAreaEffectObject(
            Character owner,
            float damage,
            float attackTickInterval,
            float distance,
            float thickness,
            Vector2 startDirection,
            float angle,
            float attackDuration,
            bool isRightRotate,
            bool isBodyEnable)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<LaserAreaEffectObject>(AreaEffectType.SolderManLaser);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, damage, attackTickInterval, distance, thickness, startDirection, angle, attackDuration, isRightRotate, isBodyEnable);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }


        public ZhangjuieLaserAreaEffectObject CreateZhangjueLaserAreaEffectObject(
            Character owner,
            float damage,
            float attackTickInterval,
            Vector2 startPosition,
            Vector2 startDirection,
            float angle,
            float attackDuration,
            float createDuration,
            float waitDuration,
            float endDuration
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ZhangjuieLaserAreaEffectObject>(AreaEffectType.ZhangjueLaser);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, damage, attackTickInterval, startPosition, startDirection, angle, attackDuration, createDuration, waitDuration, endDuration);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public ZhugeliangLaserAreaEffectObject CreateZhugeliangLaserAreaEffectObject(
            Character owner,
            float damage,
            float attackTickInterval,
            Vector2 startPosition,
            Vector2 startDirection,
            float angle,
            float attackDuration,
            float createDuration,
            float waitDuration,
            float endDuration
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ZhugeliangLaserAreaEffectObject>(AreaEffectType.ZhugeliangLaser);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, damage, attackTickInterval, startPosition, startDirection, angle, attackDuration, createDuration, waitDuration, endDuration);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public ThreekingdomArcherAttackObject CreateThreekingdomArcher(
            Character owner,
            Vector2 spawnPosition,
            float bodyDamage,
            float projectileDamage,
            float projectileSpeed,
            float projectileAcceleration,
            float projectileAliveDistance,
            ThreekingdomArcherAttackObject.DirectionType direction)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ThreekingdomArcherAttackObject>(AreaEffectType.ThreekingdomArcher);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, spawnPosition, bodyDamage, projectileDamage, projectileSpeed, projectileAcceleration, projectileAliveDistance, direction);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public AxeBoomerangAreaEffectObject CreateAxeBoomerangAreaEffectObject(
            Character owner,
            Vector2 startPosition,
            Vector2 targetPosition,
            float moveSpeed,
            float damage,
            float attackPeriod,
            Action returnCallBack
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<AxeBoomerangAreaEffectObject>(AreaEffectType.AxeBoomerang);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, startPosition, targetPosition, moveSpeed, damage, attackPeriod, returnCallBack);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public VikingSmallAxeAreaEffectObject CreateVikingSmallAxeAreaEffectObject(
            Character owner, Vector2 direction, float moveSpeed, float damage)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<VikingSmallAxeAreaEffectObject>(AreaEffectType.VikingSmallAxe);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, direction, moveSpeed, damage);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public VikingThunderHammerAreaEffectObject CreateVikingThunderHammerAreaEffectObject(
                Character owenr,
                float duration,
                Vector2 startPosition,
                Vector2 endPosition,
                float hammerDamage,
                float lightningDamage,
                int lightningCount,
                int projectileAmount,
                float projectileSpeed,
                float projectileAcceleration,
                float projectileAliveDistance,
                float projectileKnobackPower)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<VikingThunderHammerAreaEffectObject>(AreaEffectType.VikingThunderHammer);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owenr, duration, startPosition, endPosition, hammerDamage, lightningDamage, lightningCount, projectileAmount, projectileSpeed, projectileAcceleration, projectileAliveDistance, projectileKnobackPower);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public VikingThunderWarriorLightningAreaEffectObject CreateVikingThunderWarriorLightningAreaEffectObject(
            Character owner,
            Vector3 attackPosition,
            float attackRadius,
            float damage,
            float indicatorTime,
            int projectileAmount,
            float projectileSpeed,
            float projectileAcceleration,
            float projectileAliveDistance,
            float projectileKnobackPower
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<VikingThunderWarriorLightningAreaEffectObject>(AreaEffectType.VikingThunderWarriorLightning);
            areaEffect.Initialize(this, owner, attackPosition, attackRadius, damage, indicatorTime, projectileAmount, projectileSpeed, projectileAcceleration, projectileAliveDistance, projectileKnobackPower);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }
        /// <summary>
        /// 인디케이터 없는 버전
        /// 인디케이터가 필요하면 따로 구현해야 된다.
        /// </summary>
        public FallingRockAreaEffectObject CreateIceFallingRockAreaEffect(
            Monster owner,
            Vector2 position,
            float dropTime,
            float damage,
            float radius
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<FallingRockAreaEffectObject>(AreaEffectType.FallingRock);
            areaEffect.Initialize(owner, position, dropTime, damage, radius);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }
        /// <summary>
        /// 인디케이터 있는 버전
        /// IndicatorTime + dropTime 동안 인디케이터가 나온다
        /// </summary>
        public FallingRockAreaEffectObject CreateFallingRockAreaEffect(
            Monster owner,
            Vector2 position,
            float indicatorTime,
            float dropTime,
            float damage,
            float radius
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<FallingRockAreaEffectObject>(AreaEffectType.FallingRock);
            areaEffect.Initialize(this, owner, position, indicatorTime, dropTime, damage, radius);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }


        public FirePillarAreaEffectObject CreateFirePillarAreaEffectObject(
            Character owner,
            Vector2 position,
            float delay,
            float indicatorDuration,
            float radius,
            float damage,
            float burnDamage,
            float burnDuration)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<FirePillarAreaEffectObject>(AreaEffectType.FirePillar);
            areaEffect.Initialize(owner, position, delay, indicatorDuration, radius, damage, burnDamage, burnDuration);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public TrickWarriorSpearAreaEffect CreateTrickWarriorSpearAreaEffectObject(
            Character target,
            Character owner,
            Vector3 attackPosition,
            float damage,
            float speed,
            float moveDuration,
            float waitDuration,
            int moveCount
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<TrickWarriorSpearAreaEffect>(AreaEffectType.TrickWarriorSpear);
            areaEffect.Initialize(target, owner, attackPosition, damage, speed, moveDuration, waitDuration, moveCount);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public LokiSpearAreaEffectObject CreateLokiSpearAreaEffectObject(
            Character target,
            Character owner,
            Vector3 attackPosition,
            float damage,
            float speed,
            float moveDuration,
            float waitDuration,
            int moveCount
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<LokiSpearAreaEffectObject>(AreaEffectType.LokiSpear);
            areaEffect.Initialize(target, owner, attackPosition, damage, speed, moveDuration, waitDuration, moveCount);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public KrakenInkBallAreaEffect CreateKrakenInkBallAreaEffectObject(
            Monster owner,
            Vector2 position,
            float dropTime,
            float damage,
            float radius)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<KrakenInkBallAreaEffect>(AreaEffectType.KrakenInkBall);
            areaEffect.Initialize(owner, position, dropTime, damage, radius);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public MjolnirBoomerangAreaEffectObject CreateMjolnirBoomerangAreaEffectObject(
            Monster owner, Vector2 startPosition, Vector2 targetPosition, float moveSpeed, float damage, float attackPeriod, float projectileDamage, float projectileAnglePeriod, Action returnCallBack)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<MjolnirBoomerangAreaEffectObject>(AreaEffectType.MjolnirBoomerang);
            areaEffect.Initialize(owner, startPosition, targetPosition, moveSpeed, damage, attackPeriod, projectileDamage, projectileAnglePeriod, returnCallBack);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public HomingProjectileAreaEffectObject CreateHomingProjectileAreaEffectObject(string bodyResourcePath, Monster owner, Character target, Vector2 firePos, Vector2 fireDirection, float damage, float projectileRadius, float moveSpeed, float homingPower, float lifeTime, bool willRotateInMoveDirection, int hitChances)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<HomingProjectileAreaEffectObject>(AreaEffectType.HomingProjectile);
            areaEffect.Initialize(bodyResourcePath, owner, target, firePos, fireDirection, damage, projectileRadius, moveSpeed, homingPower, lifeTime, willRotateInMoveDirection, hitChances);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public SpineBodyAreaEffectObject CreateFreyaMagicCircleAreaEffectObject(
            Character owner,
            float delay,
            float indicatorDuration,
            float damage,
            float attackPeriod,
            float lifeTime,
            float objectRadius,
            Vector2 position
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SpineBodyAreaEffectObject>(AreaEffectType.FreyaMagicCircle);
            areaEffect.Initialize(owner, delay, indicatorDuration, damage, attackPeriod, lifeTime, objectRadius, position);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;

        }

        public OdinGungnirAreaEffectObject CreateOdinGungnirAreaEffectObject(
            Character owner,
            Vector2 spawnPosition,
            Vector2 targetPosition,
            float moveTargetDuration,
            float spinDuration,
            float moveBackDuration,
            float damage
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<OdinGungnirAreaEffectObject>(AreaEffectType.OdinGungnir);
            areaEffect.Initialize(owner, spawnPosition, targetPosition, moveTargetDuration, spinDuration, moveBackDuration, damage);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;

        }

        /// <param name="subProjectileDamage">초월오브젝트 폭파시 발사되는 프로젝타일의 대미지</param>
        public BubbleGumAreaEffectObject CreateBubbleGumAreaEffectObject(
            PlayerCharacter owner,
            Vector2 spawnPosition,
            float dotDamage,
            float dotDamagePeriod,
            float explosionDamage,
            float subProjectileDamage,
            float areaRadius,
            float lifetime,
            float moveSpeedChangeRatio,
            float moveSpeedChangeDuration,
            int miniGumAmount,
            bool isTranscendent
            )
        {
            var areaEffectType = AreaEffectType.BubbleGum_Default;

            var areaEffect = _areaEffectPool.TakeOneFromPool<BubbleGumAreaEffectObject>(areaEffectType);
            areaEffect.Initialize(owner, spawnPosition, dotDamage, dotDamagePeriod, explosionDamage, subProjectileDamage, areaRadius, lifetime, moveSpeedChangeRatio, moveSpeedChangeDuration, miniGumAmount, isTranscendent);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public ChewingBagAreaEffectObject CreateChewingBagAreaEffectObject(
            Character owner,
            float damage,
            float moveSpeedChangeRatio,
            float duration,
            bool isLeft
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ChewingBagAreaEffectObject>(AreaEffectType.ChewingBag);
            areaEffect.Initialize(owner, damage, moveSpeedChangeRatio, duration, isLeft);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }
        public KennyoSpinBeads CreateKennyoSpinBeads(
            Character owner,
            float damage,
            Vector2 startPosition,
            float radiusUpSpeed,
            float angleUpSpeed,
            float radiusUpAccelration,
            float angleUpAccelration,
            float maxRotateRadius,
            float attackRadius,
            Vector2 direction
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<KennyoSpinBeads>(AreaEffectType.KennyoSpinBeads);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, damage, startPosition, radiusUpSpeed, angleUpSpeed, radiusUpAccelration, angleUpAccelration, maxRotateRadius, attackRadius, direction);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public ShockBombAreaEffectObject CreateShockBomb(
            Character owner,
            Vector2 arrivalPosition,
            float arrivalTime,
            float damage,
            float radius,
            float knockbackPower,
            float duration,
            float hitPeriod,
            bool isTranscend,
            string hitSoundPrefabPath)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ShockBombAreaEffectObject>(AreaEffectType.ShockBomb);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, arrivalPosition, arrivalTime, damage, radius, knockbackPower, duration, hitPeriod, isTranscend, hitSoundPrefabPath);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public BouncingClawAreaEffectObject CreateBouncingClawAreaEffect(
            Character owner,
            Vector2 movingDirection,
            float damage,
            float chainDamage,
            int chainAmount,
            float chainRadius,
            float chainSpeed,
            float moveSpeed,
            float slowEffectDuration,
            float slowEffectSpeedChangeRate,
            float scale,
            bool isTranscend)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<BouncingClawAreaEffectObject>(AreaEffectType.BouncingClaw);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(
                owner,
                movingDirection,
                damage,
                chainDamage,
                chainAmount,
                chainRadius,
                chainSpeed,
                moveSpeed,
                slowEffectDuration,
                slowEffectSpeedChangeRate,
                scale,
                isTranscend);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public ShootingStarAreaEffectObject CreateShootingStarAreaEffect(
            PlayerCharacter owner,
            Vector2 movingDirection,
            float moveSpeed,
            float acceleration,
            float aliveDistance,
            float collisionDamage,
            float explosionDamage,
            float explosionRadius,
            float explosionDelay,
            float collisionKnockbackPower,
            float explosionKnockbackPower,
            float scaleUp,
            bool isTranscend)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ShootingStarAreaEffectObject>(AreaEffectType.ShootingStar);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, movingDirection, moveSpeed, acceleration, aliveDistance, collisionDamage, explosionDamage, explosionRadius,
                                    explosionDelay, collisionKnockbackPower, explosionKnockbackPower, scaleUp, isTranscend);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public IronFistAreaEffectObject CreateIronFistAreaEffect(
            PlayerCharacter owner,
            Vector2 position,
            Vector2 movingDirection,
            float movingSpeed,
            float attackDamage,
            float attackRadius,
            float knockbackPower,
            float lifetime,
            int hitCount,
            bool isTranscendent,
            bool withWindField,
            bool removePoisons)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<IronFistAreaEffectObject>(isTranscendent ? AreaEffectType.IronFistTranscendent : AreaEffectType.IronFist);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(this, owner, position, movingDirection, movingSpeed, attackDamage, attackRadius, knockbackPower, lifetime, hitCount, isTranscendent, withWindField, removePoisons);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public WindFieldAreaEffectObject CreateWindFieldAreaEffect(
            IronFistAreaEffectObject ironFist,
            PlayerCharacter owner,
            Vector2 position,
            Vector2 movingDirection,
            float movingSpeed,
            float height,
            float damagePerTick,
            float tickPeriod,
            float lifetime,
            bool removePoisons)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<WindFieldAreaEffectObject>(AreaEffectType.WindField);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(ironFist, owner, position, movingDirection, movingSpeed, height, damagePerTick, tickPeriod, lifetime, removePoisons);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public ShootingStarExplosionAreaEffectObject CreateShootingStarExplosionAreaEffect(
            PlayerCharacter owner,
            float damage,
            float knobackPower,
            float radius,
            Vector2 position,
            Vector2 direction,
            bool isTranscend)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ShootingStarExplosionAreaEffectObject>(AreaEffectType.ShootingStarExplosion);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(owner, damage, knobackPower, radius, position, direction, isTranscend);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }

        public SusanooSpinBead CreateSusanooSpinBead(
            Character owner,
            float fadeInDuration,       //등장시 페이드인 연출 시간
            float waitDuration,         //등장후 대기시간
            float fadeOutDuration,      //퇴장시 페이드아웃 연출 시간
            float damage,
            Vector2 center,
            float startAngle,
            float startRadius,
            float radiusUpSpeed,
            float angleUpSpeed,
            float radiusUpAccelration,
            float angleUpAccelration,
            float maxRadius,
            float attackRadius,
            Vector2 direction)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SusanooSpinBead>(AreaEffectType.SusanooSpinBead);
            areaEffect.gameObject.SetActive(true);
            areaEffect.Initialize(
            owner,
            fadeInDuration,       //등장시 페이드인 연출 시간
            waitDuration,         //등장후 대기시간
            fadeOutDuration,      //퇴장시 페이드아웃 연출 시간
            damage,
            center,
            startAngle,
            startRadius,
            radiusUpSpeed,
            angleUpSpeed,
            radiusUpAccelration,
            angleUpAccelration,
            maxRadius,
            attackRadius,
            direction);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);

            return areaEffect;
        }


        public SusanooLightningAreaEffect CreateSusanooLightningAreaEffectObject(
            Character owner,
            float waitTime,         //대기 시간
            float indicatorTime,    //대기 시간 후 인디케이터 시간
            Vector2 lightningPosition,
            float lightningRadius,
            float lightningdamage,
            Vector2 projectileDirection,
            float projectileSpeed,
            float projectileRadius,
            float projectileAliveDistance,
            float projectileDamage
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SusanooLightningAreaEffect>(AreaEffectType.SusanooLightning);
            areaEffect.Initialize(this, owner, waitTime, indicatorTime, lightningPosition, lightningRadius, lightningdamage, projectileDirection, projectileSpeed, projectileRadius, projectileAliveDistance, projectileDamage);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public IceThreeLeapsBugReflectionAreaEffect CreateIceThreeLeapsBugReflectionAreaEffectObject(
            Monster owner,
            Vector2 startPosition,
            Vector2 movingDirection,
            float damage,
            float moveSpeed,
            float areaEffectLifeTime,
            float areaEffectDamage,
            Rect moveRect
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<IceThreeLeapsBugReflectionAreaEffect>(AreaEffectType.IceThreeLeapsBugReflectionObject);
            areaEffect.Initialize(owner, startPosition, movingDirection, damage, moveSpeed, areaEffectLifeTime, areaEffectDamage, moveRect);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public DelayAreaEffectObject CreateDelayAreaEffectObject(
            Character owner,
            float delay,
            float damage,
            float lifeTime,
            float attackRadius,
            Vector2 position,
            string bodyPrefabPath
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<DelayAreaEffectObject>(AreaEffectType.DelayAreaEffectObject);
            areaEffect.Initialize(owner, delay, damage, lifeTime, attackRadius, position, bodyPrefabPath);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public BoomerangAreaEffectObject CreateBoomerangAreaEffectObject(
                AreaEffectType areaEffectType,
                Character owner,
                float duration,
                Vector2 startPosition,
                Vector2 endPosition,
                float projectileRadius,
                float projectileDamage)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<BoomerangAreaEffectObject>(areaEffectType);
            areaEffect.Initialize(owner, duration, startPosition, endPosition, projectileRadius, projectileDamage);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public SplitAreaEffectObject CreateSplitAreaEffectObject(
                AreaEffectType areaEffectType,
                Monster owner,
                float objectRadius,
                Vector2 startPosition,
                Vector2 objectMovingDirection,
                float objectMovingSpeed,
                float objectDamage,
                float objectAliveDistance,
                Rect moveRect,
                int projectileAmount,
                float projectileRadius,
                float projectileSpeed,
                float projectileAcceleration,
                float projectileDamage,
                float projectileKnobackPower,
                float projectileAliveDistance,
                bool isSpinBladeCollide)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SplitAreaEffectObject>(areaEffectType);
            areaEffect.Initialize(owner, objectRadius, startPosition, objectMovingDirection, objectMovingSpeed,
                objectDamage, objectAliveDistance, moveRect, projectileAmount, projectileRadius, projectileSpeed, projectileAcceleration,
                projectileDamage, projectileKnobackPower, projectileAliveDistance, isSpinBladeCollide);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public SplitAreaEffectObject CreateSplitAreaEffectObject(
            Monster owner,
            float objectRadius,
            Vector2 startPosition,
            Vector2 objectMovingDirection,
            float objectMovingSpeed,
            float objectDamage,
            float objectAliveDistance,
            string objectBodyPath,
            Rect moveRect,
            int projectileAmount,
            float projectileRadius,
            float projectileSpeed,
            float projectileAcceleration,
            float projectileDamage,
            float projectileKnobackPower,
            float projectileAliveDistance,
            bool isSpinBladeCollide,
            string projectileBodyPath)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SplitAreaEffectObject>(AreaEffectType.SplitObject);
            areaEffect.Initialize(owner, objectRadius, startPosition, objectMovingDirection, objectMovingSpeed,
                objectDamage, objectAliveDistance, objectBodyPath, moveRect, projectileAmount, projectileRadius, projectileSpeed, projectileAcceleration,
                projectileDamage, projectileKnobackPower, projectileAliveDistance, isSpinBladeCollide, projectileBodyPath);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public ReflectionSplitAreaEffectObject CreateReflectionSplitAreaEffectObject(
            AreaEffectType areaEffectType,
            Monster owner,
            float objectRadius,
            Vector2 startPosition,
            Vector2 objectMovingDirection,
            float objectMovingSpeed,
            float objectRotatingSpeed,
            float objectDamage,
            float objectLifeTime,
            Rect moveRect,
            int projectileAmount,
            float projectileRadius,
            float projectileSpeed,
            float projectileAcceleration,
            float projectileDamage,
            float projectileKnobackPower,
            float projectileAliveDistance,
            bool isSpinBladeCollide)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ReflectionSplitAreaEffectObject>(areaEffectType);
            areaEffect.Initialize(owner, objectRadius, startPosition, objectMovingDirection, objectMovingSpeed, objectRotatingSpeed,
                objectDamage, objectLifeTime, moveRect, projectileAmount, projectileRadius, projectileSpeed, projectileAcceleration,
                projectileDamage, projectileKnobackPower, projectileAliveDistance, isSpinBladeCollide);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public ReflectionSplitAreaEffectObject CreateReflectionSplitAreaEffectObject(
          Monster owner,
          float objectRadius,
          Vector2 startPosition,
          Vector2 objectMovingDirection,
          float objectMovingSpeed,
          float objectRotatingSpeed,
          float objectDamage,
          float objectLifeTime,
          string objectBodyPath,
          Rect moveRect,
          int projectileAmount,
          float projectileRadius,
          float projectileSpeed,
          float projectileAcceleration,
          float projectileDamage,
          float projectileKnobackPower,
          float projectileAliveDistance,
          bool isSpinBladeCollide,
          string projectileBodyPath)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ReflectionSplitAreaEffectObject>(AreaEffectType.ReflectionSplitObject);
            areaEffect.Initialize(owner, objectRadius, startPosition, objectMovingDirection, objectMovingSpeed, objectRotatingSpeed,
                objectDamage, objectLifeTime, objectBodyPath, moveRect, projectileAmount, projectileRadius, projectileSpeed, projectileAcceleration,
                projectileDamage, projectileKnobackPower, projectileAliveDistance, isSpinBladeCollide, projectileBodyPath);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public HomingSplitObject CreateHomingSplitObject(
            Monster owner,
            Transform targetTransform,
            Vector2 startPosition,
            Vector2 startDirection,
            string objectPath,
            float objectRadius,
            float objectMovingSpeed,
            float objectDamage,
            float objectLifeTime,
            float homingPower,
            bool willRotateInMoveDirection,
            string projectilePath,
            int projectileAmount,
            float projectileRadius,
            float projectileSpeed,
            float projectileAcceleration,
            float projectileDamage,
            float projectileKnobackPower,
            float projectileAliveDistance)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<HomingSplitObject>(AreaEffectType.HomingSplitObject);
            areaEffect.Initialize(
                owner, targetTransform, objectPath, startPosition, startDirection,
                objectRadius, objectMovingSpeed, objectDamage, objectLifeTime, homingPower, willRotateInMoveDirection,
                projectilePath, projectileAmount, projectileRadius, projectileSpeed, projectileAcceleration,
                projectileDamage, projectileKnobackPower, projectileAliveDistance);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public ReflectionHomingAreaEffectObject CreateReflectionHomingAreaEffectObject(
            Monster owner,
            Character target,
            AreaEffectType areaEffectType,
            float objectRadius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float damage,
            float lifeTime,
            float attackPeriod,
            Rect moveRect,
            float rotatingSpeed)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ReflectionHomingAreaEffectObject>(areaEffectType);
            areaEffect.Initialize(owner, target, objectRadius, startPosition, movingDirection, movingSpeed, damage, lifeTime, attackPeriod, moveRect, rotatingSpeed);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public ReflectionHomingAreaEffectObject CreateReflectionHomingAreaEffectObject(
            Monster owner,
            Character target,
            string bodyPath,
            float objectRadius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float damage,
            float lifeTime,
            float attackPeriod,
            Rect moveRect,
            float rotatingSpeed)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ReflectionHomingAreaEffectObject>(AreaEffectType.ReflectionHomingObject);
            areaEffect.Initialize(owner, target, bodyPath, objectRadius, startPosition, movingDirection, movingSpeed, damage, lifeTime, attackPeriod, moveRect, rotatingSpeed);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public ReturningAreaEffectObject CreateReturningAreaEffectObject(
            AreaEffectType areaEffectType,
            Monster owner,
            float radius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float rotatingSpeed,
            float damage,
            Rect rect)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ReturningAreaEffectObject>(areaEffectType);
            areaEffect.Initialize(owner, radius, startPosition, movingDirection, movingSpeed, rotatingSpeed, damage, rect);
            areaEffect.gameObject.SetActive(true);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            return areaEffect;
        }

        public DrHomingMissileAreaEffectObject CreateDrHommingMissile(
            Monster owner,
            Character target,
            Vector2 firePos,
            Vector2 fireDirection,
            float damage,
            float projectileRadius,
            float moveSpeed,
            float homingPower,
            float lifeTime)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<DrHomingMissileAreaEffectObject>(AreaEffectType.DrHomingMissileObject);
            areaEffect.Initialize(owner, target, firePos, fireDirection, damage, projectileRadius, moveSpeed, homingPower, lifeTime);
            areaEffect.gameObject.SetActive(true);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            return areaEffect;
        }

        public DrHomingSmallMissileAreaEffectObject CreateDrHommingSmallMissile(
            Monster owner,
            Character target,
            Vector2 firePos,
            Vector2 fireDirection,
            float damage,
            float projectileRadius,
            float moveSpeed,
            float homingPower,
            float lifeTime)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<DrHomingSmallMissileAreaEffectObject>(AreaEffectType.DrHomingSmallMissileObject);
            areaEffect.Initialize(owner, target, firePos, fireDirection, damage, projectileRadius, moveSpeed, homingPower, lifeTime);
            areaEffect.gameObject.SetActive(true);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            return areaEffect;
        }

        public PoisonMachinePoisonousAreaEffect CreatePoisonMachinePoisonousAreaEffect(
            Monster owner,
            Vector2 startPosition,
            Vector2 endPosition,
            float movingTime,
            float lifetime,
            float tickPeriod,
            float damagePerTick,
            float radius)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<PoisonMachinePoisonousAreaEffect>(AreaEffectType.PoisonMachinePoisonousAreaEffect);
            areaEffect.Initialize(this, owner, startPosition, endPosition, movingTime, lifetime, tickPeriod, damagePerTick, radius);
            areaEffect.gameObject.SetActive(true);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            return areaEffect;
        }

        public PoisonMachineHomingProjectile CreatePoisonMachineHomingProjectile(
            Monster owner,
            Character target,
            Vector2 startPosition,
            float projectileSpeed,
            float projectileLifetime,
            float projectileDamage,
            float projectileRadius,
            float areaEffectCreationPeriod,
            float areaEffectLifetime,
            float areaEffectTickPeriod,
            float areaEffectDamagePerTick,
            float areaEffectRadius)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<PoisonMachineHomingProjectile>(AreaEffectType.PoisonMachineHomingProjectile);
            areaEffect.Initialize(owner, target, startPosition, projectileSpeed, projectileLifetime, projectileDamage, projectileRadius, areaEffectCreationPeriod, areaEffectLifetime, areaEffectTickPeriod, areaEffectDamagePerTick, areaEffectRadius);
            areaEffect.gameObject.SetActive(true);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            return areaEffect;
        }

        public SusanooBeadObject CreateSusanooBead(
            Monster owner,
            Vector2 startPosition,
            Vector2 endPosition,
            float waitTime,
            float attackRadius,
            float damage
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SusanooBeadObject>(AreaEffectType.SusanooBeadObject);
            areaEffect.Initialize(this, owner, startPosition, endPosition, waitTime, attackRadius, damage);
            areaEffect.gameObject.SetActive(true);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            return areaEffect;
        }

        public TornadoAreaEffectObject CreateTornadoAreaEffectObject(
            Character owner,
            Vector2 position,
            float attackDamagePerTick,
            float attackRadius,
            float attackDuration,
            float attackTickPeriod,
            float transcendentAttackDamage,
            bool isTranscendent)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<TornadoAreaEffectObject>(AreaEffectType.TornadoObject);
            areaEffect.Initialize(owner, position, attackDamagePerTick, attackRadius, attackDuration, attackTickPeriod, transcendentAttackDamage, isTranscendent);
            areaEffect.gameObject.SetActive(true);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            return areaEffect;
        }

        public PoisonPotion CreatePoisonPotion(
            Monster owner,
            Vector2 startPosition,
            Vector2 endPosition,
            float waitTime,
            float attackRadius,
            float damage,
            float attackDuration)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<PoisonPotion>(AreaEffectType.PoisonPotionObject);
            areaEffect.Initialize(this, owner, startPosition, endPosition, waitTime, attackRadius, damage, attackDuration);
            areaEffect.gameObject.SetActive(true);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            return areaEffect;
        }

        public PoisonBall CreatePoisonBall(
            Monster owner,
            Vector2 startPosition,
            Vector2 endPosition,
            float waitTime,
            float attackRadius,
            float damage,
            float attackDuration)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<PoisonBall>(AreaEffectType.PoisonBall);
            areaEffect.Initialize(this, owner, startPosition, endPosition, waitTime, attackRadius, damage, attackDuration);
            areaEffect.gameObject.SetActive(true);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            return areaEffect;
        }

        public SpineBodyAreaEffectObject CreateHenriIISandAreaEffect(
            Character owner,
            float delay,
            float indicatorDuration,
            float damage,
            float attackPeriod,
            float lifeTime,
            float objectRadius,
            Vector2 position
    )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SpineBodyAreaEffectObject>(AreaEffectType.HenriIISandAreaEffect);
            areaEffect.Initialize(owner, delay, indicatorDuration, damage, attackPeriod, lifeTime, objectRadius, position);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public BoomerangObject CreateBoomerangObject(
                AreaEffectType areaEffectType,
                Character owner,
                float duration,
                Vector2 startPosition,
                Vector2 endPosition,
                float damage,
                float attackRadius,
                float rotatingSpeed)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<BoomerangObject>(areaEffectType);
            areaEffect.Initialize(owner, duration, startPosition, endPosition, damage, attackRadius, rotatingSpeed);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public ExplosionAreaEffect CreateExplosionAreaEffect(
            Character owner,
            float delay,
            float indicatorDuration,
            float damage,
            float attackRadius,
            Vector2 position,
            string explosionParticlePath,
            float explosionParticleScale)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ExplosionAreaEffect>(AreaEffectType.ExplosionAreaEffect);
            areaEffect.Initialize(owner, delay, indicatorDuration, damage, attackRadius, position, explosionParticlePath, explosionParticleScale);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public TupacAmaruSandMakeObject CreateTupacAmaruSandMakeObject(
            Character owner,
            Vector2 startPosition,
            Vector2 direction,
            float projectileDamage,
            float areaEffectDamage)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<TupacAmaruSandMakeObject>(AreaEffectType.TupacAmaruSandMakeObject);
            areaEffect.Initialize(owner, startPosition, direction, projectileDamage, areaEffectDamage);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }
        public IntiTentacleObject CreateIntiTentacleObject(
            Character owner,
            Vector2 position,
            float radius,
            float indicatorDuration,
            float damage)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<IntiTentacleObject>(AreaEffectType.IntiTentacleObject);
            areaEffect.Initialize(this, owner, position, radius, indicatorDuration, damage);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public SpaceShipIceAreaEffectObject CreateSpaceShipIceObject(
            Character owner,
            Vector2 attackPosition,
            float attackRadius,
            float damage,
            float areaEffectDuration,
            float slowDuration,
            float slowParameter
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SpaceShipIceAreaEffectObject>(AreaEffectType.SpaceShipIceObject);
            areaEffect.Initialize(owner, attackPosition, attackRadius, damage, areaEffectDuration, slowDuration, slowParameter);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public SpaceShipTranscendentIceObject CreateSpaceShipTranscendentIceObject(
            Character owner,
            Vector2 attackPosition,
            float attackDelay,
            float stunDuration,
            float damage,
            float scale)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SpaceShipTranscendentIceObject>(AreaEffectType.SpaceShipTranscendentIceObject);
            areaEffect.Initialize(owner, attackPosition, attackDelay, stunDuration, damage, scale);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public SpaceShipIceGroupAreaEffect CreateSpaceShipIceGroup(
            Character owner,
            int dropCount,
            float dropDuration,
            Vector2 dropPositionCenter,
            float dropRadius,
            float attackRadius,
            float slowAreaEffectDuration,
            float slowDuration,
            float slowRatio,
            float damage)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SpaceShipIceGroupAreaEffect>(AreaEffectType.SpaceShipIceGroup);
            areaEffect.Initialize(owner, dropCount, dropDuration, dropPositionCenter, dropRadius, attackRadius, slowAreaEffectDuration, slowDuration, slowRatio, damage);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public GenieSandHomingObject CreateGenieSandHomingObject(
            Monster owner,
            Character target,
            Vector2 startPosition,
            float moveSpeed,
            float lifetime,
            float homingPower,
            float homingObjectDamage,
            float homingObjectRadius,
            float areaEffectCreationPeriod,
            float areaEffectLifetime,
            float areaEffectTickPeriod,
            float areaEffectDamagePerTick,
            float areaEffectRadius,
            float lastAreaEffectRadius)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<GenieSandHomingObject>(AreaEffectType.GenieSandHomingObject);
            areaEffect.Initialize(
                owner,
                target,
                startPosition,
                moveSpeed,
                lifetime,
                homingPower,
                homingObjectDamage,
                homingObjectRadius,
                areaEffectCreationPeriod,
                areaEffectLifetime,
                areaEffectTickPeriod,
                areaEffectDamagePerTick,
                areaEffectRadius,
                lastAreaEffectRadius);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public ExplosiveAreaEffectObject CreateExplosiveAreaEffectObject(
            AreaEffectType areaEffectType,
            Monster owner,
            Vector2 startPosition,
            Vector2 endPosition,
            float waitTime,
            float attackRadius,
            float damage)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ExplosiveAreaEffectObject>(areaEffectType);
            areaEffect.Initialize(this, owner, startPosition, endPosition, waitTime, attackRadius, damage);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public ReflectionHomingSplitReflectionObject CreateReflectionHomingReflectionSplitObject(
            Monster owner,
            Transform targetTransform,
            AllianceType alliance,
            AreaEffectType objectType,
            float objectRadius,
            Vector2 startPosition,
            Vector2 objectMovingDirection,
            float objectMovingSpeed,
            float objectRotatingSpeed,
            float objectDamage,
            float objectLifeTime,
            Rect moveRect,
            AreaEffectType splitObjectType,
            int splitObjectAmount,
            float splitObjectRadius,
            float splitObjectSpeed,
            float splitObjectRotatingSpeed,
            float splitObjectDamage,
            float splitObjectKnobackPower,
            float splitObjectLifeTime)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ReflectionHomingSplitReflectionObject>(objectType);
            areaEffect.Initialize(
                owner, targetTransform, alliance, objectRadius, startPosition, objectMovingDirection, objectMovingSpeed,
                objectRotatingSpeed, objectDamage, objectLifeTime, moveRect,
                splitObjectType, splitObjectAmount, splitObjectRadius, splitObjectSpeed, splitObjectRotatingSpeed, splitObjectDamage, splitObjectKnobackPower, splitObjectLifeTime);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public ReflectionHomingBoomerangObject CreateReflectionHomingBoomerangObject(
            Monster owner,
            Character target,
            AreaEffectType areaEffectType,
            float objectRadius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float rotatingSpeed,
            float damage,
            int reflectionCount,
            float attackPeriod,
            Rect moveRect)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ReflectionHomingBoomerangObject>(areaEffectType);
            areaEffect.Initialize(owner, target, objectRadius, startPosition, movingDirection, movingSpeed, rotatingSpeed, damage, reflectionCount, attackPeriod, moveRect);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public ReflectionHomingBoomerangObject CreateReflectionHomingBoomerangObject(
            Monster owner,
            Character target,
            float objectRadius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float rotatingSpeed,
            float damage,
            int reflectionCount,
            float attackPeriod,
            Rect moveRect,
            string bodyPath)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ReflectionHomingBoomerangObject>(AreaEffectType.ReflectionHomingBoomerangObject);
            areaEffect.Initialize(owner, target, objectRadius, startPosition, movingDirection, movingSpeed, rotatingSpeed, damage, reflectionCount, attackPeriod, moveRect, bodyPath);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }


        public SplitReflectionObject CreateSplitReflectionObject(
                AreaEffectType areaEffectType,
                Monster owner,
                float objectRadius,
                Vector2 startPosition,
                Vector2 objectMovingDirection,
                float objectMovingSpeed,
                float objectDamage,
                float objectAliveDistance,
                Rect moveRect,
                AreaEffectType splitObjectType,
                int splitObjectAmount,
                float splitObjectRadius,
                float splitObjectSpeed,
                float splitObjectRotatingSpeed,
                float splitObjectDamage,
                float splitObjectKnobackPower,
                float splitObjectLifeTime)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SplitReflectionObject>(areaEffectType);
            areaEffect.Initialize(owner, objectRadius, startPosition, objectMovingDirection, objectMovingSpeed, objectDamage, objectAliveDistance, moveRect, splitObjectType, splitObjectAmount, splitObjectRadius, splitObjectSpeed, splitObjectRotatingSpeed, splitObjectDamage, splitObjectKnobackPower, splitObjectLifeTime);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public SplitReflectionObject CreateSplitReflectionObject(
          Monster owner,
          Vector2 startPosition,
          Vector2 objectMovingDirection,
          float projectileRadius,
          float projectileSpeed,
          float projectileDamage,
          float projectileAliveDistance,
          string projectileBodyPath,
          Rect moveRect,
          int splitAmount,
          float reflectionObjectRadius,
          float reflectionObjectMovingSpeed,
          float reflectionObjectRotatingSpeed,
          float reflectionObjectDamage,
          float reflectionObjectKnobackPower,
          float reflectionObjectLifeTime,
          string reflectionObjectBodyPath)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SplitReflectionObject>(AreaEffectType.SplitReflectionObject);
            areaEffect.Initialize(owner, startPosition, objectMovingDirection, projectileRadius, projectileSpeed, projectileDamage,
                projectileAliveDistance, projectileBodyPath, moveRect, splitAmount, reflectionObjectRadius, reflectionObjectMovingSpeed,
                reflectionObjectRotatingSpeed, reflectionObjectDamage, reflectionObjectKnobackPower, reflectionObjectLifeTime, reflectionObjectBodyPath);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        /// <summary>
        /// 왼쪽 방향 (시계 반대방향) 으로 돌아간다. 
        /// 첫 시작은 0도 기준 플레이어 왼쪽
        /// </summary>
        public SpinMoveObject CreateSpinMoveObject(
            Character owner,
            AreaEffectType areaEffectType,
            float damage,
            Vector2 startPosition,
            float lifeTime,
            float startRadius,
            float startAngle,
            float radiusUpSpeed,
            float angleUpSpeed,
            float radiusUpAccelration,
            float angleUpAccelration,
            float attackRadius,
            float bodyRotatingSpeed,
            Vector2 offsetPosition,
            float maxRadius = 9999f,            //해당 수치까기 가기 전 Life타임으로 먼저 사라진다 
            float maxRadiusUpSpeed = 9999f,     //중간에 추가된 파라미터라 이전 오브젝트들에게 문제 생기지 않도록 처리하기 위해 
            float maxAngleUpSpeed = 9999f)      //이렇게 처리했다.
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SpinMoveObject>(areaEffectType);
            areaEffect.Initialize(owner, damage, startPosition, lifeTime, startRadius, startAngle, radiusUpSpeed, angleUpSpeed, radiusUpAccelration, angleUpAccelration, attackRadius, bodyRotatingSpeed, offsetPosition, maxRadius, maxRadiusUpSpeed, maxAngleUpSpeed);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public SpinMoveObject CreateSpinMoveObject(
            Character owner,
            AreaEffectType areaEffectType,
            float damage,
            Vector2 startPosition,
            float lifeTime,
            float startRadius,
            float startAngle,
            float radiusUpSpeed,
            float angleUpSpeed,
            float radiusUpAccelration,
            float angleUpAccelration,
            float attackRadius,
            float bodyRotatingSpeed)
        {
            return this.CreateSpinMoveObject(owner, areaEffectType, damage, startPosition, lifeTime, startRadius, startAngle, radiusUpSpeed, angleUpSpeed, radiusUpAccelration, angleUpAccelration, attackRadius, bodyRotatingSpeed, Vector2.zero);
        }

        /// <summary>
        /// 몸체 이미지를 초기화 단계에서 경로 입력 받아 생성하는 버전
        /// 왼쪽 방향 (시계 반대방향) 으로 돌아간다. 
        /// 첫 시작은 0도 기준 플레이어 왼쪽
        /// </summary>
        public SpinMoveObject CreateSpinMoveObject(
            Character owner,
            float damage,
            Vector2 startPosition,
            string bodyPath,
            float lifeTime,
            float startRadius,
            float startAngle,
            float radiusUpSpeed,
            float angleUpSpeed,
            float radiusUpAccelration,
            float angleUpAccelration,
            float attackRadius,
            float bodyRotatingSpeed,
            Vector2 offsetPosition,
            float maxRadius,
            float maxRadiusUpSpeed,
            float maxAngleUpSpeed)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SpinMoveObject>(AreaEffectType.SpinMoveObject);
            areaEffect.Initialize(owner, damage, startPosition, bodyPath, lifeTime, startRadius, startAngle, radiusUpSpeed, angleUpSpeed, radiusUpAccelration, angleUpAccelration, attackRadius, bodyRotatingSpeed, offsetPosition, maxRadius, maxRadiusUpSpeed, maxAngleUpSpeed);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }


        public CircleBoomerangObject CreateCircleBoomerangObject(
            Character owner,
            AreaEffectType areaEffectType,
            float damage,
            float moveSpeed,
            float bodyRotatingSpeed,
            float attackRadius,
            Vector2 startPosition,
            Vector2 targetPosition,
            bool isMoveRight)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<CircleBoomerangObject>(areaEffectType);
            areaEffect.Initialize(owner, damage, moveSpeed, bodyRotatingSpeed, attackRadius, startPosition, targetPosition, isMoveRight);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public BeggarKingReflectionPoisonousAreaEffectObject CreateBeggarKingReflectionPoisonousAreaEffectObject(
            Monster owner,
            float objectRadius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float rotatingSpeed,
            float damage,
            float knockBackPower,
            float lifeTime,
            Rect moveRect,
            float poisonousAreaEffectDuration,
            float poisonousAreaEffectRadius,
            float poisonousAreaEffectDamage
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<BeggarKingReflectionPoisonousAreaEffectObject>(AreaEffectType.BeggarKingReflectionPoisonousAreaEffectObject);
            areaEffect.Initialize(owner, objectRadius, startPosition, movingDirection, movingSpeed, rotatingSpeed, damage, knockBackPower, lifeTime, moveRect, poisonousAreaEffectDuration, poisonousAreaEffectRadius, poisonousAreaEffectDamage);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public DiagonalProjectileCreateObject CreateDiagonalProjectileCreateObject(
            Character owner,
            int fireCount,
            float fireDuration,
            float horizontalWidth,
            int fireLineCount,
            string bodyProjectileResourcePath,
            float baseDamage,
            float knockBackPower,
            float speed,
            float acceleration,
            float collidingRadius,
            float aliveDistance,
            int hitChances,
            int splitCount,
            bool isRemovableBySpinBladeObject,
            string hitSoundPrefabPath
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<DiagonalProjectileCreateObject>(AreaEffectType.DiagonalProjectileCreateObject);
            areaEffect.Initialize(
                owner,
                this,
                fireCount,
                fireDuration,
                horizontalWidth,
                fireLineCount,
                bodyProjectileResourcePath,
                baseDamage,
                knockBackPower,
                speed,
                acceleration,
                collidingRadius,
                aliveDistance,
                hitChances,
                splitCount,
                isRemovableBySpinBladeObject,
                hitSoundPrefabPath
            );

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }


        public DancingTriangleObject CreateDancingTriangleObject(
            Character owner,
            Vector2 startPosition,
            Vector2 movingDirection,
            float damage,
            float speed,
            float radius,
            float knobackPower,
            float duration,
            float attackPeriod,
            float stunDuration,
            float stunPeriod,
            bool removePoisons)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<DancingTriangleObject>(AreaEffectType.DancingTriangleObject);
            areaEffect.Initialize(
            owner,
            startPosition,
            movingDirection,
            damage,
            speed,
            radius,
            knobackPower,
            duration,
            attackPeriod,
            stunDuration,
            stunPeriod,
            removePoisons);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public EnchantingGlowNormalAreaEffectObject CreateEnchantingGlowNormalAreaEffectObject(
            Character owner,
            int attackAmount,
            float damage,
            Vector2 startPosition,
            Vector2 attackDirection,
            float knobackPower,
            float attackRadius,
            float attackPeriod,
            float attackDuration,
            float stunDuration,
            float stunPeriod,
            bool removePoisons)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<EnchantingGlowNormalAreaEffectObject>(AreaEffectType.EnchantingGlowNormalAreaEffectObject);
            areaEffect.Initialize(
            this,
            owner,
            attackAmount,
            damage,
            startPosition,
            attackDirection,
            knobackPower,
            attackRadius,
            attackPeriod,
            attackDuration,
            stunDuration,
            stunPeriod,
            removePoisons);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public EnchantingGlowBodyObject CreateEnchantingGlowBodyObject(
            Character owner,
            Vector2 startPosition,
            Vector2 direction,
            float attackRadius,
            float duration)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<EnchantingGlowBodyObject>(AreaEffectType.EnchantingGlowBodyObject);
            areaEffect.Initialize(owner, startPosition, direction, attackRadius, duration);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public StageMeteorAreaEffectObject CreateStageMeteorAreaEffectObject(
            AllianceType allianceType,
            float indicatorDuration,
            Vector2 dropPosition,
            float attackDamage,
            float attackRadius,
            float burnDamage,
            float burnDuration)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<StageMeteorAreaEffectObject>(AreaEffectType.StageMeteorAreaEffectObject);
            areaEffect.Initialize(this, allianceType, indicatorDuration, dropPosition, attackDamage, attackRadius, burnDamage, burnDuration);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public StageLightningAreaEffectObject CreateStageLightningAreaEffectObject(
            AllianceType allianceType,
            float indicatorDuration,
            Vector3 attackPosition,
            float attackRadius,
            float damage,
            float stunDuration)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<StageLightningAreaEffectObject>(AreaEffectType.StageLightningAreaEffectObject);
            areaEffect.Initialize(this, allianceType, indicatorDuration, attackPosition, attackRadius, damage, stunDuration);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public StageSandAreaEffectObject CreateStageSandAreaEffectObject(
            AllianceType allianceType,
            float indicatorDuration,
            Vector2 position,
            float objectRadius,
            float objectDuration)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<StageSandAreaEffectObject>(AreaEffectType.StageSandAreaEffectObject);
            areaEffect.Initialize(this, allianceType, indicatorDuration, position, objectRadius, objectDuration);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }


        public StageIceAreaEffectObject CreateStageIceAreaEffectObject(
            AllianceType allianceType,
            float indicatorDuration,
            Vector2 position,
            float objectRadius,
            float objectDuration,
            float slowRate,
            float slowDuration)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<StageIceAreaEffectObject>(AreaEffectType.StageIceAreaEffectObject);
            areaEffect.Initialize(this, allianceType, indicatorDuration, position, objectRadius, objectDuration, slowRate, slowDuration);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }
        public ThreeLeapsBugPoisonHomingObject CreateThreeLeapsBugPoisonHomingObject(
            Monster owner,
            Character target,
            float objectRadius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float damage,
            float lifeTime,
            float poisonousAreaDuration,
            float poisonousAreaRadius,
            float poisonousAreaDamage,
            Rect moveRect)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ThreeLeapsBugPoisonHomingObject>(AreaEffectType.ThreeLeapsBugPoisonHomingObject);
            areaEffect.Initialize(owner, target, objectRadius, startPosition, movingDirection, movingSpeed, damage, lifeTime, poisonousAreaDuration, poisonousAreaRadius, poisonousAreaDamage, moveRect);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public EraMixZeusSpecialReflectionHomingObject CreateEraMixZeusSpecialReflectionHomingObject(
            Monster owner,
            Character target,
            Vector2 startPosition,
            Vector2 movingDirection,
            Rect moveRect)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<EraMixZeusSpecialReflectionHomingObject>(AreaEffectType.EraMixZeusSpecialReflectionHomingObject);
            areaEffect.Initialize(owner, target, startPosition, movingDirection, moveRect);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public LightningMachineHardHomingSplitSpinMoveObject CreateLightningMachineHardHomingSplitSpinMoveObject(
            Monster owner,
            Transform targetTransform,
            Vector2 startPosition,
            Vector2 startDirection)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<LightningMachineHardHomingSplitSpinMoveObject>(AreaEffectType.LightningMachineHardHomingSplitSpinMoveObject);
            areaEffect.Initialize(owner, targetTransform, startPosition, startDirection);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public SpecialOperationsSoldierHardReflectionHomingObject CreateSpecialOperationsSoldierHardReflectionHomingObject(
            Monster owner,
            Character target,
            float objectRadius,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float damage,
            float lifeTime,
            Rect moveRect,
            float rotatingSpeed)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SpecialOperationsSoldierHardReflectionHomingObject>(AreaEffectType.SpecialOperationsSoldierHardReflectionHomingObject);
            areaEffect.Initialize(owner, target, objectRadius, startPosition, movingDirection, movingSpeed, damage, lifeTime, moveRect, rotatingSpeed);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public MonsterInfinitySpinBladeObject CreateMonsterInfinitySpinBladeObject(
            Character owner,
            string bodyPath,
            float damage,
            Vector2 startLocalPosition,
            float lifeTime,
            float startRadius,
            float startAngle,
            float radiusUpSpeed,
            float angleUpSpeed,
            float radiusUpAccelration,
            float angleUpAccelration,
            float attackRadius,
            float bodyRotatingSpeed,
            Vector2 offsetPosition,
            float maxRadius,
            float maxRadiusUpSpeed,
            float maxAngleUpSpeed)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<MonsterInfinitySpinBladeObject>(AreaEffectType.MonsterInfinitySpinBladeObject);
            areaEffect.Initialize(
                owner,
                bodyPath,
                damage,
                startLocalPosition,
                lifeTime,
                startRadius,
                startAngle,
                radiusUpSpeed,
                angleUpSpeed,
                radiusUpAccelration,
                angleUpAccelration,
                attackRadius,
                bodyRotatingSpeed,
                offsetPosition,
                maxRadius,
                maxRadiusUpSpeed,
                maxAngleUpSpeed
                );

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public EraMixGuanyuLightningCreateObject CreateEraMixGuanyuLightningCreateObject(
            Monster owner,
            Vector2 firePosition,
            Vector2 fireDirection)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<EraMixGuanyuLightningCreateObject>(AreaEffectType.EraMixGuanyuLightningCreateObject);
            areaEffect.Initialize(this, owner, firePosition, fireDirection);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public EraMixMileJackReflectionFireSplitObject CreateEraMixMileJackReflectionFireSplitObject(
                Monster owner,
                Vector2 firePosition,
                Vector2 fireDirection,
                Rect moveRect)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<EraMixMileJackReflectionFireSplitObject>(AreaEffectType.EraMixMileJackReflectionFireSplitObject);
            areaEffect.Initialize(owner, firePosition, fireDirection, moveRect);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }
        public ReflectionHomingSplitObject CreateReflectionHomingSplitObject(
            Monster owner,
            Transform targetTransform,
            float objectRadius,
            Vector2 startPosition,
            Vector2 objectMovingDirection,
            float objectMovingSpeed,
            float objectRotatingSpeed,
            float objectDamage,
            float objectLifeTime,
            string objectBodyPath,
            Rect moveRect,
            int splitedObjectAmount,
            float splitedObjectRadius,
            float splitedObjectSpeed,
            float splitedObjectAcceleration,
            float splitedObjectDamage,
            float splitedObjectKnobackPower,
            float splitedObjectAliveDistance,
            bool isSpinBladeCollide,
            string splitedObjectBodyPath
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ReflectionHomingSplitObject>(AreaEffectType.ReflectionHomingSplitObject);
            areaEffect.Initialize(
                owner,
                targetTransform,
                objectRadius,
                startPosition,
                objectMovingDirection,
                objectMovingSpeed,
                objectRotatingSpeed,
                objectDamage,
                objectLifeTime,
                objectBodyPath,
                moveRect,
                splitedObjectAmount,
                splitedObjectRadius,
                splitedObjectSpeed,
                splitedObjectAcceleration,
                splitedObjectDamage,
                splitedObjectKnobackPower,
                splitedObjectAliveDistance,
                isSpinBladeCollide,
                splitedObjectBodyPath
                );

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public LandWhaleBasicWhaleObject CreateLandWhaleBasicObject(
            Character owner,
            float damage,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float knobackPower,
            float objectRadius,
            float lifeTime,
            string hitSoundPrefabPath
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<LandWhaleBasicWhaleObject>(AreaEffectType.LandWhaleBasicWhaleObject);
            areaEffect.Initialize(owner, damage, startPosition, movingDirection, movingSpeed, knobackPower, objectRadius, lifeTime, hitSoundPrefabPath);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public LandWhaleTranscendentWhaleObject CreateLandWhaleTranscendentWhaleObject(
            Character owner,
            float damage,
            Vector2 startPosition,
            Vector2 movingDirection,
            float movingSpeed,
            float knobackPower,
            float objectRadius,
            float lifeTime,
            string hitSoundPrefabPath,
            float fireDamage,
            float fireRadius,
            float fireLifeTime,
            float fireCreatePeriod)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<LandWhaleTranscendentWhaleObject>(AreaEffectType.LandWhaleTranscendentWhaleObject);
            areaEffect.Initialize(owner, damage, startPosition, movingDirection, movingSpeed, knobackPower, objectRadius, lifeTime, hitSoundPrefabPath, fireDamage, fireRadius, fireLifeTime, fireCreatePeriod);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public LandWhaleTranscendentFireObject CreateLandWhaleTranscendentFireObject(
            Character owner,
            float damage,
            float lifeTime,
            float objectRadius,
            Vector2 position
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<LandWhaleTranscendentFireObject>(AreaEffectType.LandWhaleTranscendentFireObject);
            areaEffect.Initialize(owner, damage, lifeTime, objectRadius, position);

            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public WoodTentacleObject CreateWoodTentacleObject(
            Character owner,
            Vector2 position,
            float radius,
            float delay,
            float indicatorDuration,
            float damage)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<WoodTentacleObject>(AreaEffectType.WoodTentacleObject);
            areaEffect.Initialize(owner, position, radius, delay, indicatorDuration, damage);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public EraMixKojiroSplitToReflectionObject CreateEraMixKojiroSplitToReflectionObject(
            Monster owner,
            Vector2 startPosition,
            Vector2 startDirection)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<EraMixKojiroSplitToReflectionObject>(AreaEffectType.EraMixKojiroSplitToReflectionObject);
            areaEffect.Initialize(this, owner, startPosition, startDirection);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public StickySlimeNormalObject CreateStickySlimeNormalObject(
            PlayerCharacter owner,
            float damage,
            float objectRadius,
            Vector2 startPosition,
            Vector2 forwardDirection,
            float forwardMoveSpeed,
            float forwardMoveDuration,
            float backwardMoveSpeed,
            float itemAcquireAdditionalRange,
            float dropChance,
            float goldChance,
            long dropGoldAmount
            )
        {
            var areaEffectType = AreaEffectType.StickySlimeNormalObject_Default;

            var areaEffect = _areaEffectPool.TakeOneFromPool<StickySlimeNormalObject>(areaEffectType);
            areaEffect.Initialize(owner, damage, objectRadius, startPosition, forwardDirection, forwardMoveSpeed, forwardMoveDuration, backwardMoveSpeed, itemAcquireAdditionalRange, dropChance, goldChance, dropGoldAmount);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public StickySlimeTranscendentObject CreateStickySlimeTranscendentObject(
            PlayerCharacter owner,
            float damage,
            float objectRadius,
            Vector2 startPosition,
            Vector2 forwardDirection,
            float forwardMoveSpeed,
            float forwardMoveDuration,
            float backwardMoveSpeed,
            float itemAcquireAdditionalRange,
            float dropChance,
            float goldChance,
            long dropGoldAmount
            )
        {
            var areaEffectType = AreaEffectType.StickySlimeTranscendentObject_Default;

            var areaEffect = _areaEffectPool.TakeOneFromPool<StickySlimeTranscendentObject>(areaEffectType);
            areaEffect.Initialize(owner, damage, objectRadius, startPosition, forwardDirection, forwardMoveSpeed, forwardMoveDuration, backwardMoveSpeed, itemAcquireAdditionalRange, dropChance, goldChance, dropGoldAmount);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public ReflectionSplitSpinMoveObjectAreaEffectObject CreateReflectionSplitSpinMoveObjectAreaEffectObject(
           Monster owner,
           float reflectionObjectRadius,
           Vector2 startPosition,
           Vector2 reflectionObjectMovingDirection,
           float reflectionObjectMovingSpeed,
           float reflectionObjectRotatingSpeed,
           float reflectionObjectDamage,
           float reflectionObjectLifeTime,
           string reflectionObjectBodyPath,
           Rect moveRect,
           int spinMoveObjectAmount,
           float spinMoveObjectDamage,
           float spinMoveObjectCreateDistance,
           float spinMoveObjectRadius,
           float spinMoveObjectLifeTime,
           float spinMoveObjectRadiusUpSpeed,
           float spinMoveObjectAngleUpSpeed,
           float spinMoveObjectRadiusUpAcceleration,
           float spinMoveObjectAngleUpAcceleration,
           float spinMoveObjectBodyRotationSpeed,
           float spinMoveObjectMaxRadius,
           float spinMoveObjectMaxRadiusUpSpeed,
           float spinMoveObjectMaxAngleUpSpeed,
           bool isSpinMoveObjectLeftRotate,
           string spinMoveObjectBodyPath)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<ReflectionSplitSpinMoveObjectAreaEffectObject>(AreaEffectType.ReflectionSplitSpinMoveObjectAreaEffectObject);
            areaEffect.Initialize(
            owner,
            reflectionObjectRadius,
            startPosition,
            reflectionObjectMovingDirection,
            reflectionObjectMovingSpeed,
            reflectionObjectRotatingSpeed,
            reflectionObjectDamage,
            reflectionObjectLifeTime,
            reflectionObjectBodyPath,
            moveRect,
            spinMoveObjectAmount,
            spinMoveObjectDamage,
            spinMoveObjectCreateDistance,
            spinMoveObjectRadius,
            spinMoveObjectLifeTime,
            spinMoveObjectRadiusUpSpeed,
            spinMoveObjectAngleUpSpeed,
            spinMoveObjectRadiusUpAcceleration,
            spinMoveObjectAngleUpAcceleration,
            spinMoveObjectBodyRotationSpeed,
            spinMoveObjectMaxRadius,
            spinMoveObjectMaxRadiusUpSpeed,
            spinMoveObjectMaxAngleUpSpeed,
            isSpinMoveObjectLeftRotate,
            spinMoveObjectBodyPath);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);

            return areaEffect;
        }

        public SpinMoveMultiHitObject CreateSpinMoveMultiHitObject(
            Character owner,
            float damage,
            Vector2 startPosition,
            string bodyPath,
            float lifeTime,
            float startRadius,
            float startAngle,
            float radiusUpSpeed,
            float angleUpSpeed,
            float radiusUpAccelration,
            float angleUpAccelration,
            float attackRadius,
            float bodyRotatingSpeed,
            Vector2 offsetPosition,
            float maxRadius,
            float maxRadiusUpSpeed,
            float maxAngleUpSpeed)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<SpinMoveMultiHitObject>(AreaEffectType.SpinMoveMultiHitObject);
            areaEffect.Initialize(owner, damage, startPosition, bodyPath, lifeTime, startRadius, startAngle, radiusUpSpeed, angleUpSpeed, radiusUpAccelration, angleUpAccelration, attackRadius, bodyRotatingSpeed, offsetPosition, maxRadius, maxRadiusUpSpeed, maxAngleUpSpeed);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public IntiTentacleCreateObject CreateIntiTentacleCreateObject(
            Character owner,
            float damage,
            float attackRadius,
            float indicatorDuration,
            float createInterval)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<IntiTentacleCreateObject>(AreaEffectType.IntiTentacleCreateObject);
            areaEffect.Initialize(owner, damage, attackRadius, indicatorDuration, createInterval);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public WoodTentacleCreateObject CreateWoodTentacleCreateObject(
            Character owner,
            float damage,
            float attackRadius,
            float indicatorDuration,
            float createInterval)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<WoodTentacleCreateObject>(AreaEffectType.WoodTentacleCreateObject);
            areaEffect.Initialize(owner, damage, attackRadius, indicatorDuration, createInterval);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public DropStoneCreateObject CreateDropStoneCreateObject(
            Monster owner,
            float damage,
            float attackRadius,
            float indicatorDuration,
            float createInterval)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<DropStoneCreateObject>(AreaEffectType.DropStoneCreateObject);
            areaEffect.Initialize(owner, damage, attackRadius, indicatorDuration, createInterval);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            areaEffect.gameObject.SetActive(true);
            return areaEffect;
        }

        public DeployYoyoAreaEffectObject CreateDeployYoyoAreaEffectObject(
            PlayerCharacter owner,
            Vector2 position,
            Vector2 direction,
            float duration,
            float attackRadius,
            float knockbackPower,
            float damage)
        {
            var areaEffectType = AreaEffectType.DeployBattleYoYo_Default;

            var areaEffect = _areaEffectPool.TakeOneFromPool<DeployYoyoAreaEffectObject>(areaEffectType);
            areaEffect.Initialize(owner, position, direction, duration, attackRadius, knockbackPower, damage);
            areaEffect.gameObject.SetActive(true);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            return areaEffect;
        }

        public DiesWithOwnerAreaEffectObject CreateDiesWithOnwerAreaEffectObject(
            Character owner)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<DiesWithOwnerAreaEffectObject>(AreaEffectType.DiesWithOwnerAreaEffectObject);
            areaEffect.Initialize(owner);
            areaEffect.gameObject.SetActive(true);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            return areaEffect;
        }

        public LightningCreateObject CreateLightningCreateObject(
            Character owner,
            float damage,
            float attackRadius,
            float indicatorDuration,
            float createInterval
            ) 
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<LightningCreateObject>(AreaEffectType.LightningCreateObject);
            areaEffect.Initialize(owner, damage, attackRadius, indicatorDuration, createInterval);
            areaEffect.gameObject.SetActive(true);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            return areaEffect;
        }

        public EraMixGenieReflectionFollowUpObject CreateEraMixGenieReflectionFollowUpObject(
            Monster owner,
            Vector2 startPosition,
            Vector2 movingDirection,
            Rect moveRect
            )
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<EraMixGenieReflectionFollowUpObject>(AreaEffectType.EraMixGenieReflectionFollowUpObject);
            areaEffect.Initialize(owner, startPosition, movingDirection, moveRect);
            areaEffect.gameObject.SetActive(true);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            return areaEffect;
        }

        public GumSpecialMineSlowAreaEffect CreateGumSpecialMineSlowAreaEffect(
            PlayerCharacter owner,
            Vector2 position,
            float duration,
            float damage,
            float radius,
            float slowRate)
        {
            var areaEffect = _areaEffectPool.TakeOneFromPool<GumSpecialMineSlowAreaEffect>(AreaEffectType.GumSpecialMineSlowAreaEffect);
            areaEffect.Initialize(owner, position, duration, damage, radius, slowRate);
            areaEffect.gameObject.SetActive(true);
            _areaEffectsCreatedOnThisFrame.Add(areaEffect);
            return areaEffect;
        }

    }
}