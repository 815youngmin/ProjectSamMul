using Shared.StaticDatas;
using UnityEngine;
using UnityEngine.UI;
using Z.GameClients.Stages.Characters.Stats;
using Z.GameClients.Stages.CombatSystems;
using Z.GameClients.Stages.ProjectileObjects;
using Z.ResourcePools;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    public class BubbleGumSkill : SkillBase
    {
        private StatModifier _attackSpeedIncreaser;  //Param1 : 공격속도 
        private float _gumAreaDotDamageRate;            // Param2 : 껌 장판 도트 대미지 계수 (PC공격력 * {1+Param})
        private float _gumAreaDotDamageAttackSpeed;     // Param3 : 껌 장판 도트대미지 공격속도 (초당 공격횟수)
        private float _gumAreaRadius;               // Param4 : 껌 장판 영역 크기 (폭발 범위에도 사용)
        private float _gumAreaExplosionDamageRate;      // Param5 : 껌 장판 폭발 대미지 계수 (PC공격력 * {1+Param})
        private float _gumAreaExplosionProjectileDamageRate;    // 껌 초월시, 장판 폭파하며 추가생성되는 프로젝타일의 대미지 계수 (Param5의 값을 동일하게 사용)
        private float _gumAreaDuration;                 // Param6 : 껌 장판 지속시간 (터질때까지 대기 시간)

        private readonly float _enemyMoveSpeedChangeRatio;      // 이동속도 변화 비율 : 0.35 == 이동속도 -65%
        private readonly float _enemyMoveSpeedChangeDuration;   // 이동속도 감소 지속시간
        private readonly float _bubbleGumDurationRatio;         // 풍선껌 지속 시간

        private static readonly float GUM_PROJECTILE_ATTACK_RANGE = 12f;   // 껌 투사체 날아가는 범위
        private static readonly float GUM_PROJECTILE_SPEED = 20f;

        private static readonly long ATTACK_COUNT_FOR_THROWING_TRANSCENDENT_OBJECT = 1;   // 초월(폭탄껌) 몇번마다 한번씩 생성할껀지에 대한 개수

        private long _throwCount; //스킬 습득 후 현재까지 생성된 풍선껌 개수

        private int _miniGumAmount = 0;     //CleavageGum 등급효과 기능에 사용될 미니껌 개수
        private float _throwAt;

        private Slider _gumSlider;
        private readonly string GUM_SLIDER_PREFAB_PATH = "Stages/UIs/HUDs/Aims/GumSlider.prefab";

        private string _slimeProjectileBodyPath;

        public BubbleGumSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats, IReadOnlyCustomParameters parameters) : base(staticData)
        {
            _attackSpeedIncreaser = new StatModifier(staticData.Parameter1, StatModType.Flat);  //Param1 : 공격속도 
            _gumAreaDotDamageRate = staticData.Parameter2;            // Param2 : 껌 장판 도트 대미지 계수 (PC공격력 * {1+Param})
            _gumAreaDotDamageAttackSpeed = staticData.Parameter3;     // Param3 : 껌 장판 도트대미지 공격속도 (초당 공격횟수)
            if (staticData.Parameter3 <= 0)
            {
                throw new StaticDataValidationError($"Invalid Gum Dotdamage AttackSpeed[{staticData.Parameter3}]. Must be equal or larger than 0.");
            }

            _gumAreaRadius = staticData.Parameter4;     // Param4 : 껌 장판 영역 크기 (폭발 범위에도 사용)
            _gumAreaExplosionDamageRate = staticData.Parameter5;      // Param5 : 껌 장판 폭발 대미지 계수 (PC공격력 * {1+Param})
            _gumAreaExplosionProjectileDamageRate = staticData.Parameter5;    // 껌 초월시, 장판 폭파하며 추가생성되는 프로젝타일의 대미지 계수 (Param5의 값을 동일하게 사용)
            _gumAreaDuration = staticData.Parameter6; // Param6 : 껌 장판 지속 시간

            _enemyMoveSpeedChangeRatio = parameters.GetParameterValue(CustomParameterType.BubbleGumMoveSpeedChangeRatio);
            _enemyMoveSpeedChangeDuration = parameters.GetParameterValue(CustomParameterType.BubbleGumMoveSpeedChangeDuration);
            _bubbleGumDurationRatio = parameters.GetParameterValue(CustomParameterType.BubbleGumDurationRatio);

            _throwAt = 0f;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            owner.Stats.CharacterAttackSpeed.AddModifier(_attackSpeedIncreaser);

            _throwAt = now + 2f;
            _throwCount = 0;

            _miniGumAmount = (int)owner.CustomParameters.GetParameterValue(CustomParameterType.CleavageGum);

            _gumSlider = ResourcePool.Instance.InstantiateFromResource<Slider>(GUM_SLIDER_PREFAB_PATH);
            _gumSlider.transform.SetParent(owner.transform);
            _gumSlider.transform.localPosition = new Vector2(0.0f, -0.63f);
            _gumSlider.transform.localScale = Vector3.one;
            _gumSlider.value = 0.0f;

            _slimeProjectileBodyPath = "Stages/Projectiles/Gum_Projectile/GumProjectile.prefab";

            if (this.IsTranscendent)
            {
                // 초월의 경우 찍자 마자 초월 투사체 하나 생성해준다. (초월을 보여주기 위함)
                var randomNearOwner = owner.Pos + new Vector2(Random.Range(minInclusive: -5f, maxInclusive: 5f), Random.Range(minInclusive: -5f, maxInclusive: 5f));
                this.SpawnGumArea(stage, owner, spawnPosition: randomNearOwner, isSpecialGum: true);
            }
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);

            owner.Stats.CharacterAttackSpeed.RemoveModifier(_attackSpeedIncreaser);
            _miniGumAmount = 0;

            ResourcePool.Instance.PutBackInstance(GUM_SLIDER_PREFAB_PATH, _gumSlider.gameObject);
            _gumSlider = null;
        }

        private static float GetThrowGumPeriod(PlayerCharacter owner)
        {
            var attackSpeed = owner.Stats.CharacterAttackSpeed.Value;
            return attackSpeed <= 0f ? 10f : (1f / attackSpeed);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if (owner.Action.IsDead)
            {
                return;
            }

            float throwPeriod = GetThrowGumPeriod(owner);
            _gumSlider.value = (throwPeriod + now - _throwAt) / (throwPeriod - 0.1f);

            if (now < _throwAt)
            {
                return;
            }

            _throwCount++;
            _throwAt = now + throwPeriod;
            _gumSlider.value = 0.0f;

            bool throwSpecialGum = IsTranscendent &&
                (_throwCount % ATTACK_COUNT_FOR_THROWING_TRANSCENDENT_OBJECT == 0);

            float attackRangeRatio = owner.Stats.AttackRangeDistanceRatio.Value;
            float effectiveRange = GUM_PROJECTILE_ATTACK_RANGE * attackRangeRatio;
            float projectileRadius = 0.85f * attackRangeRatio;

            Vector2 fireDirection = this.GetAimDirection(owner, stage, effectiveRange * 1.3f + 3f);
            this.ThrowGum(stage, owner, fireDirection, aliveDistance: effectiveRange, projectileRadius, throwSpecialGum);
            owner.ConditionalEffects.OnBasicSkillUsed(stage, owner);
        }

        private ProjectileObject ThrowGum(Stage stage, PlayerCharacter owner, Vector2 direction, float aliveDistance, float projectileRadius, bool throwSpecialGum)
        {
            float gumAreaDotDamage = CombatSystem.CalculateCharacterBasicAttackDamage(owner.Stats, _gumAreaDotDamageRate);

            var projectile = stage.CreateProjectile(
                _slimeProjectileBodyPath,
                owner.Alliance,
                owner,
                baseDamage: gumAreaDotDamage,
                knockBackPower: 0f,
                owner.CenterPos,
                direction,
                speed: GUM_PROJECTILE_SPEED,
                acceleration: 0f,
                collidingRadius: projectileRadius,
                aliveDistance,
                hitChances: 1,
                splitCount: 0,
                isRemovableBySpinBladeObject: false,
                hitCharacterHandler: BubbleGumProjectileCharacterHitHandler,
                hitItemHandler: BubbleGumProjectileItemHitHandler,
                onFinishedHandler: (Stage stage, ProjectileObject finishedProjectile, bool isHit) =>
                {
                    SpawnGumArea(stage, owner, spawnPosition: finishedProjectile.transform.position, throwSpecialGum);
                },
                hitSoundPrefabPath: string.Empty
                );

            projectile.transform.localScale = Vector3.one;

            return projectile;
        }

        private static bool BubbleGumProjectileCharacterHitHandler(Stage stage, Character target, Vector2 hitPosition, ProjectileObject attackerProjectile)
        {
            bool isHitted = attackerProjectile.TryHitCharacter(stage, target, hitPosition, attackerProjectile);
            // 껌 투사체는 딱 한번만 맞고 터지도록 한다. 모종의 이유로 HitChance 여러번을 처리하는 버그가 없도록 한다. 
            attackerProjectile.ForceRemoveAllHitChances(stage);
            return isHitted;
        }

        private static bool BubbleGumProjectileItemHitHandler(Stage stage, Collider2D collidedCharacter, ProjectileObject attackerProjectile)
        {
            bool isHitted = attackerProjectile.TryHitObject(stage, collidedCharacter, attackerProjectile);
            // 껌 투사체는 딱 한번만 맞고 터지도록 한다. 모종의 이유로 HitChance 여러번을 처리하는 버그가 없도록 한다. 
            attackerProjectile.ForceRemoveAllHitChances(stage);
            return isHitted;
        }

        private void SpawnGumArea(Stage stage, PlayerCharacter owner, Vector2 spawnPosition, bool isSpecialGum)
        {
            float areaRadius = owner.Stats.AttackRangeDistanceRatio.Value * _gumAreaRadius;
            float dotDamagePeriod = 1f / _gumAreaDotDamageAttackSpeed;

            float dotDamage = CombatSystem.CalculateCharacterBasicAttackDamage(owner.Stats, _gumAreaDotDamageRate);
            float explosionDamage = CombatSystem.CalculateCharacterBasicAttackDamage(owner.Stats, _gumAreaExplosionDamageRate);
            float subProjectileDamage = CombatSystem.CalculateCharacterBasicAttackDamage(owner.Stats, _gumAreaExplosionProjectileDamageRate);

            float lifetime = _gumAreaDuration * owner.Stats.DurationIncreaseRate.Value * _bubbleGumDurationRatio;

            stage.CreateBubbleGumAreaEffectObject(
                owner,
                spawnPosition,
                dotDamage,
                dotDamagePeriod,
                explosionDamage,
                subProjectileDamage,
                areaRadius,
                lifetime,
                _enemyMoveSpeedChangeRatio,
                _enemyMoveSpeedChangeDuration,
                _miniGumAmount,
                isSpecialGum
            );
        }

        private Vector2 GetAimDirection(PlayerCharacter owner, Stage stage, float effectiveRange)
        {
            Vector2? direction = null;

            var closestEnemy = stage.FindClosestCharacter(owner.Alliance.ToEnemyAlliance(),
                owner.CenterPos,
                effectiveRange,
                condition: character => !character.Action.IsDead && !character.IsImmuneToHit);

            if (closestEnemy != null)
            {
                direction = (closestEnemy.Pos - owner.CenterPos).normalized;
            }
            else
            {
                var breakableObject = stage.FindClosestBreakableItemObjectExceptFence(owner.CenterPos);
                if (breakableObject != null)
                {
                    var distance = ((Vector2)breakableObject.transform.position - owner.CenterPos).magnitude;
                    if (distance <= effectiveRange)
                    {
                        direction = ((Vector2)breakableObject.transform.position - owner.CenterPos).normalized;
                    }
                }

            }

            if (direction == null ||
                direction.Value == Vector2.zero)
            {
                direction = owner.MoveDir;
                if (direction.Value == Vector2.zero)
                {
                    direction = Vector2.up;
                }
            }

            return direction.Value;
        }
    }
}
