using Shared.GameDataTypes;
using Shared.StaticDatas;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using Random = UnityEngine.Random;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class AlphaGatlingSkill : SkillBase
    {
        private static readonly int FIRE_TRACK_START_INDEX = 6;
        private static readonly string DEFAULT_PROJECTILE_PREFAB_PATH = "Stages/Projectiles/Alpha_Projectile/AlphaProjectile.prefab";
        private static readonly string ALPHA_AIM_PREFAB_PATH = "Stages/UIs/HUDs/Aims/AlphaAim.prefab";
        private static readonly string[] FIRE_ANIMATION_NAMES = { "fire", "fire_a", "fire_b", "fire_c", "fire_d" };

        private readonly float _attackPowerRate;        // Param1 : 일반 공격 대미지 증가 비율 (피해 계수), 최종대미지 = PC공격력 * (1+Param1)
        private readonly StatModifier _attackSpeedIncreaser;     // Param2 : 공격속도 
        private readonly float _knockBackPower;         // Param3 : 넉백 파워 
        private readonly int _hitChance;                // Param4 : 관통
        private readonly float _homingAttackPowerRate;  // pamra5 : 유도미사일 공격 대미지 증가 비율 (피해 계수), 최종대미지 = PC공격력 * (1+Param1)
        private readonly float _homingAreaEffectRadius; // param6 : 유도미사일 공격 범위

        private readonly float _projectileMoveSpeed;// 발사체 속도
        private readonly float _projectileAliveDistance; //발사체 이동 거리
        private readonly float _fireOffsetDistance;
        private readonly float _homingMissileFindTargetRange;
        private readonly int _homingFireAmount;
        private readonly int _homingFireCountTiming;
        private readonly float _homingMissileAliveTime;
        private readonly float _collidingRadius;

        private float _lastAttackedAt;
        private Vector2 _prevFireDirection;
        private long _currentFireCount;
        private string _projectilePath;

        private AlphaAim _alphaAim;
        private Bone _ownerAimBone;
        private SkeletonAnimation _ownerSpineBody;
        private List<Animation> _fireAnimations;

        private readonly IReadOnlyCharacterStatCalculators _characterStats;
        //UpgradeAlphaGattling 등급 효과
        private readonly float _gradeEffectCorrectionAngle;

        //IncreaseAlphaGatlingDamage 변장 효과
        private readonly float _increaseAlphaGatlingDamagePercente;

        //데미지 감소량
        private readonly float _decreaseAlphaGatlingDamagePercente; 
        //추가 총알 발사 개수
        private readonly int _increaseAlphaGatlingBulletFireAmount;
        //추가 총알 관통 횟수
        private readonly int _increaseAlphaGatlingHitChance;

        public AlphaGatlingSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats, IReadOnlyCustomParameters parameters) : base(staticData)
        {
            _attackPowerRate = staticData.Parameter1;
            _attackSpeedIncreaser = new StatModifier(StaticData.Parameter2, StatModType.Flat);
            _knockBackPower = staticData.Parameter3;
            _hitChance = (int)staticData.Parameter4;
            _homingAttackPowerRate = staticData.Parameter5;
            _homingAreaEffectRadius = staticData.Parameter6;

            _projectileMoveSpeed = 26f;
            _projectileAliveDistance = 100f;
            _fireOffsetDistance = 0.65f;
            _lastAttackedAt = 0.0f;
            _currentFireCount = 0;

            _homingMissileFindTargetRange = 20f;
            _homingFireCountTiming = (int)(parameters.GetParameterValue(CustomParameterType.AlphaHomingMissileAttackPeriodRatio) * (float)40);
            _homingFireAmount = 8 + (int)parameters.GetParameterValue(CustomParameterType.IncreaseAlphaHomingMissileAmount);  //IncreaseAlphaHomingMissileAmount 변장 효과
            _homingMissileAliveTime = 5.0f;

            _collidingRadius = 0.45f;

            _prevFireDirection = Vector2.up;
            _characterStats = characterStats;

            // 좌우로 계산하기 때문에 각도를 반으로 줄여준다.
            _gradeEffectCorrectionAngle = 0.5f * parameters.GetParameterValue(CustomParameterType.UpgradeAlphaGattling_CorrectionAngle);

            //알파 게틀링 (총알만) 데미지 추가 증가비율
            //IncreaseAlphaGatlingDamage 변장 효과
            _increaseAlphaGatlingDamagePercente = parameters.GetParameterValue(CustomParameterType.IncreaseAlphaGatlingDamagePercent);

            _decreaseAlphaGatlingDamagePercente = parameters.GetParameterValue(CustomParameterType.DecreaseAlphaGatlingDamagePercent);
            _increaseAlphaGatlingBulletFireAmount = (int)parameters.GetParameterValue(CustomParameterType.IncreaseAlphaGatlingBulletFireAmount);
            _increaseAlphaGatlingHitChance = (int)parameters.GetParameterValue(CustomParameterType.IncreaseAlphaGatlingHitChance);
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);

            //공격속도 적용
            owner.Stats.CharacterAttackSpeed.AddModifier(_attackSpeedIncreaser);

            // 에임 UI 생성 및 초기화
            _alphaAim = ResourcePool.Instance.InstantiateFromResource<AlphaAim>(ALPHA_AIM_PREFAB_PATH);
            _alphaAim.transform.SetParent(owner.transform);
            _alphaAim.transform.localPosition = owner.CenterPos - owner.Pos;
            _alphaAim.transform.localScale = Vector2.one;
            _alphaAim.Initialize();

            //캐릭터 스파인 에임본 찾기
            _ownerAimBone = owner.AnimationController.Body.skeleton.FindBone("aim");
            _ownerSpineBody = owner.AnimationController.Body;

            _prevFireDirection = owner.MoveDir;

            //공격시 순차적으로 재생되어야 하는 애니메이션
            _fireAnimations = new List<Animation>();
            foreach (var fireAnimationName in FIRE_ANIMATION_NAMES)
            {
                var fireAnimation = owner.AnimationController.Body.Skeleton.Data.FindAnimation(fireAnimationName);
                if (fireAnimation != null)
                {
                    _fireAnimations.Add(fireAnimation);
                }
            }

            //공격시 지속적으로 재생되어야 하는 애니메이션
            var fireHeadAnimation = owner.AnimationController.Body.AnimationState.Data.SkeletonData.FindAnimation("fire_head");
            if (fireHeadAnimation != null)
            {
                owner.AnimationController.Body.AnimationState.SetAnimation(FIRE_TRACK_START_INDEX - 1, "fire_head", true);
            }

            _currentFireCount = _homingFireCountTiming - 4;

            _projectilePath = DEFAULT_PROJECTILE_PREFAB_PATH;
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_attackSpeedIncreaser);

            ResourcePool.Instance.PutBackInstance(ALPHA_AIM_PREFAB_PATH, _alphaAim.gameObject);
            _alphaAim = null;
            _ownerAimBone = null;
            _ownerSpineBody = null;
            _fireAnimations.Clear();
            _currentFireCount = 0;
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if (owner.Action.IsDead)
            {
                return;
            }

            Vector2 fireDirection = this.GetFireDirection(stage, owner);
            float projectileMoveSpeed = _projectileMoveSpeed * _characterStats.ProjectileMoveSpeedIncreaseRateValue;
            _alphaAim.SetArrow(fireDirection);
            float knockbackPower = CombatSystem.CalculateCharacterAttackKnockBackPower(owner.Stats, _knockBackPower);

            this.UpdateOwnerAimBonePosition(owner, fireDirection);

            float attackPeriod = 1.0f / _characterStats.CharacterAttackSpeedValue;
            if (now < _lastAttackedAt + attackPeriod)
            {
                return;
            }

            _currentFireCount++;
            int animationIndex = (int)(_currentFireCount % _fireAnimations.Count);
            _ownerSpineBody.AnimationState.SetAnimation(FIRE_TRACK_START_INDEX + animationIndex, _fireAnimations[animationIndex], false);

            if (IsTranscendent)
            {
                float homingMissileDamage = CombatSystem.CalculateCharacterBasicAttackDamage(owner.Stats, _homingAttackPowerRate);
                if (_currentFireCount % _homingFireCountTiming == 0)
                {
                    for (int i = 0; i < _homingFireAmount; i++)
                    {
                        Vector2 homingFireDirection = Quaternion.AngleAxis(360f / _homingFireAmount * i, Vector3.forward) * Vector2.up;
                        Vector2 homingFirePosition = owner.CenterPos + _fireOffsetDistance * 0.5f * homingFireDirection;

                        PlaySpawnedObjectSoundEffect(owner.Pos);
                        stage.CreateAlphaGatlingHomingAreaEffect(
                            owner,
                            stage,
                            findTargetRange: _homingMissileFindTargetRange,
                            areaEffectRadius: _homingAreaEffectRadius,
                            homingFirePosition,
                            homingFireDirection,
                            movingSpeed: projectileMoveSpeed,
                            damage: homingMissileDamage,
                            knockbackPower,
                            aliveTime: _homingMissileAliveTime,
                            StaticData.SkillHitSFXPath
                            );
                    }
                }
            }

            float resultDamage = CombatSystem.CalculateCharacterBasicAttackDamage(owner.Stats, _attackPowerRate);
            resultDamage = (1.0f + _increaseAlphaGatlingDamagePercente - _decreaseAlphaGatlingDamagePercente) * resultDamage;

            //발사 위치보다 가깝게 있는 적이 있으면 해당적을 공격한다.
            var target = stage.FindClosestCharacter(
                owner.Alliance.ToEnemyAlliance(),
                owner.CenterPos + fireDirection * (_fireOffsetDistance * 0.5f),
                limitDistance: (_fireOffsetDistance * 0.5f),
                condition: character => !character.Action.IsDead && !character.IsImmuneToHit
            );

            PlaySkillSoundEffect(owner.Pos);
            if (target != null)
            {
                Vector2 hitVector = (target.CenterPos - owner.CenterPos).normalized * (knockbackPower);
                target.Hitted(stage, attacker: owner, resultDamage, hitVector, target.CenterPos, StaticData.SkillHitSFXPath);

                if(0 < _increaseAlphaGatlingBulletFireAmount ||  1 < _hitChance + _increaseAlphaGatlingHitChance)
                {
                    FireBullet(stage, owner, fireDirection, _knockBackPower, projectileMoveSpeed);
                }
            }
            else
            {
                FireBullet(stage, owner, fireDirection, _knockBackPower, projectileMoveSpeed);
            }
            owner.ConditionalEffects.OnBasicSkillUsed(stage, owner);
            _lastAttackedAt = now;
        }

        //플레이어 스파인 에임본 위치 조절 (발사 방향으로 10 범위 만큼 앞에 위치한다, 너무 가까우면 스파인 애니메이션 오류 있다 함)
        private void UpdateOwnerAimBonePosition(PlayerCharacter owner, Vector2 fireDirection)
        {
            Vector2 aimWorldPosition = this.GetFirePosition(owner) + fireDirection * 10f;
            Vector2 aimLocalPosition;
            var targetSkeletonSpacePoint = _ownerSpineBody.transform.InverseTransformPoint(aimWorldPosition.x, aimWorldPosition.y, 0f);
            targetSkeletonSpacePoint.x *= _ownerSpineBody.Skeleton.ScaleX;
            targetSkeletonSpacePoint.y *= _ownerSpineBody.Skeleton.ScaleY;
            aimLocalPosition = targetSkeletonSpacePoint;

            _ownerAimBone.SetLocalPosition(aimLocalPosition);
        }

        //발사 위치 전달
        private Vector2 GetFirePosition(PlayerCharacter owner)
        {
            float xVariance = Random.Range(-0.3f, 0.3f);
            float yVariance = Random.Range(-0.3f, 0.3f);

            return owner.CenterPos + owner.MoveDir * _fireOffsetDistance + new Vector2(xVariance, yVariance);
        }

        //발사 방향 전달 (이전 발사 방향 값도 저장한다)
        private Vector2 GetFireDirection(Stage stage, PlayerCharacter owner)
        {
            Vector2 fireDriection;
            if (owner.MoveDir == Vector2.zero)
            {
                if (_prevFireDirection == Vector2.zero)
                {
                    _prevFireDirection = Vector2.up;
                }
                fireDriection = _prevFireDirection;
            }
            else
            {
                _prevFireDirection = owner.MoveDir;
                fireDriection = owner.MoveDir;
            }

            if (_gradeEffectCorrectionAngle <= 0)
            {
                return fireDriection.normalized;
            }

            List<Character> charactersInArea = new List<Character>();

            stage.FindCharactersInArea(owner.Alliance.ToEnemyAlliance(),
                           new CircularTargetArea(owner.Pos, radius: 10f),
                            condition: character => !character.Action.IsDead && !character.IsImmuneToHit, charactersInArea);

            foreach (Character character in charactersInArea)
            {
                Vector2 targetDirection = character.Pos - owner.Pos;
                if (Vector2.Angle(fireDriection, targetDirection) <= _gradeEffectCorrectionAngle)
                {
                    return targetDirection.normalized;
                }
            }

            return fireDriection.normalized;
        }

        private void FireBullet(Stage stage, PlayerCharacter owner, Vector2 fireDirection, float knockbackPower, float projectileMoveSpeed)
        {
            float resultDamage = CombatSystem.CalculateCharacterBasicAttackDamage(owner.Stats, _attackPowerRate);
            resultDamage = (1.0f + _increaseAlphaGatlingDamagePercente - _decreaseAlphaGatlingDamagePercente) * resultDamage;

            int sectorFireAmount = 1 + _increaseAlphaGatlingBulletFireAmount;   //총알 발사 개수
            float sectorAngle = sectorFireAmount * 5f;
            float projectileRadius = owner.Stats.AttackRangeDistanceRatio.Value * _collidingRadius;
            Vector3 projectileScale = Vector3.one * owner.Stats.AttackRangeDistanceRatio.Value;
            Vector2 firePos = this.GetFirePosition(owner);
            for (int i = -(sectorFireAmount - 1); i <= sectorFireAmount - 1; i += 2)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 0.5f * (float)i * sectorAngle / sectorFireAmount) * fireDirection;

                //근접한 적이 없으면 그냥 총알 발사
                var projectile = stage.CreateProjectile(
                                    _projectilePath,
                                    owner.Alliance,
                                    owner,
                                    resultDamage,
                                    knockbackPower,
                                    firePos,
                                    dir,
                                    projectileMoveSpeed,
                                    acceleration: 0f,
                                    projectileRadius,
                                    _projectileAliveDistance,
                                    _hitChance + _increaseAlphaGatlingHitChance,
                                    splitCount: 0,
                                    StaticData.SkillHitSFXPath);
                projectile.transform.localScale = projectileScale;
            }
        }
    }
}
