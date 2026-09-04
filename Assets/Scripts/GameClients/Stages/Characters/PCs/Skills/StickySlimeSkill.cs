using Shared.GameDataTypes;
using Shared.StaticDatas;
using System;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.GameClients.Stages.ProjectileObjects;
using SamMul.UnityHelpers;
using Random = UnityEngine.Random;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class StickySlimeSkill : SkillBase
    {
        private IReadOnlyCharacterStatCalculators _characterStats;
        
        private readonly float _attackPowerRate;     //Param1 : 데미지 계수
        private StatModifier _attackSpeedIncreaser;  //Param2 : 공격속도 
        private readonly int _slimeAmount;           //Param3 : 분열 개수
        private readonly float _slimeSectorAngle;    //Param4 : 분열 각도
        private readonly float _slimeForwardMoveDuration; //Param5 : 슬라임 전진 시간
        private readonly float _slimeRadius;         //Param6 : 슬라임의 크기

        private static readonly float NormalSplitObjectRadius = 0.35f;
        private static readonly float NormalSplitObjectSpeed = 12f;
        private static readonly float NormalSlimeForwardMoveSpeed = 15f;
        private static readonly float NormalSlimeBackwardMoveSpeed = 12f;
        private static readonly float NormalSplitObjectAcceleration = 0.1f;
        private static readonly float NormalSplitObjectAliveDistance = 16;
        private static readonly float NormalSplitObjectKnobackPower = 0.15f;

        private static readonly float TranscendentSplitObjectRadius = 0.45f;
        private static readonly float TranscendentSplitObjectSpeed = 12f;
        private static readonly float TranscendentSlimeForwardMoveSpeed = 15f;
        private static readonly float TranscendentSlimeBackwardMoveSpeed = 12f;
        private static readonly float TranscendentSplitObjectAcceleration = 0.1f;
        private static readonly float TranscendentSplitObjectAliveDistance = 16;
        private static readonly float TranscendentSplitObjectKnobackPower = 0.3f;

        private static readonly bool IsSpinBladeCollide = false;
        private static readonly float BasicFindingRadius = 10f;

        private float _throwAt;
        private PlayerCharacter _owner;

        private string _slimeProjectileBodyPath;
        private string _slimeCollideAnimationPath;
        private string _slimeBoomAnimationPath;

        private float _dropChance; //재화 드랍 확률
        private long _dropGoldAmount; //골드 드랍량
        private float _dropTranscendentAdditionalChance; //초월 재화 드랍 확률
        private float _totalDropChance; //최종 재화 드랍 확률
        private float _itemAcquireAdditionalRangeGrade;  //등급효과 아이템 획득 추가 범위
        private static readonly float BasicItemAcquireAdditionalRange = 0.6f; //기본 아이템 획득 추가 범위 
        private static readonly float DropGoldChance = 0.9995f;
        //보석 드랍 확률 = 0.0005f; 1 - dropGolPercentage 의 확률로 보석을 드랍한다.
        private int _disguiseEffectIncreaseSlimeAmount; //IncreaseStickySlimeSplitAmount 변장효과로 획득하는 추가 슬라임 분열 개수

        private string _collidePath;
        private string _boomPath;

        //변장 효과 MultiCircleFireStickySlime
        private int _multipleCircleFireStickySlimeStackCount;
        private int _multipleCircleFireStickySlimeStack;
        private static readonly int MultipleCircleFireAmount = 6;

        public StickySlimeSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats, IReadOnlyCustomParameters parameters) 
            : base(staticData)
        {
            _characterStats = characterStats;
            _dropChance = parameters.GetParameterValue(CustomParameterType.UndineBonusItemDropChance);
            _dropTranscendentAdditionalChance = parameters.GetParameterValue(CustomParameterType.UndineTranscendentBonusItemDropAdditionalChance);
            _itemAcquireAdditionalRangeGrade = parameters.GetParameterValue(CustomParameterType.UndineSkillItemAcquireAdditionalRange);

            _multipleCircleFireStickySlimeStackCount = (int)parameters.GetParameterValue(CustomParameterType.MultipleCircleFireStickySlimeStackCount);
            _multipleCircleFireStickySlimeStack = 0;

            _disguiseEffectIncreaseSlimeAmount = (int)parameters.GetParameterValue(CustomParameterType.IncreaseStickySlimeSplitAmount);

            _attackPowerRate = staticData.Parameter1;
            _attackSpeedIncreaser = new StatModifier(staticData.Parameter2, StatModType.Flat);
            _slimeAmount = (int)staticData.Parameter3 + _disguiseEffectIncreaseSlimeAmount;
            _slimeSectorAngle = staticData.Parameter4;
            _slimeForwardMoveDuration = staticData.Parameter5;
            _slimeRadius = staticData.Parameter6;

        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            owner.Stats.CharacterAttackSpeed.AddModifier(_attackSpeedIncreaser);

            _owner = owner;
            _throwAt = now;

            long chapterRewardGold = 0;
            switch (stage.StageType)
            {
                case StageType.Chapter: 
                    {
                        chapterRewardGold = stage.ChapterStaticData.RewardGold;
                    }
                    break;
            }
            // 골드 드랍량은 최소 10부터 시작한다.
            _dropGoldAmount = Math.Max((long)(chapterRewardGold * 0.0001f), 10);

            if (IsTranscendent)
            {
                _totalDropChance = _dropChance + _dropTranscendentAdditionalChance;
            }
            else
            {
                _totalDropChance = _dropChance;
            }

            _slimeProjectileBodyPath = "Stages/AreaEffects/StickySlime/Basic/waterIdle.prefab";
            _slimeCollideAnimationPath = "Stages/AreaEffects/StickySlime/Basic/waterCollide.prefab";
            _slimeBoomAnimationPath = "Stages/AreaEffects/StickySlime/Basic/waterBoom.prefab";
            _collidePath = _slimeCollideAnimationPath;
            _boomPath = _slimeBoomAnimationPath;
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_attackSpeedIncreaser);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if(owner.Action.IsDead)
            {
                return;
            }

            if (now < _throwAt)
            {
                return;
            }

            Vector2 targetPos;
            float findingRadius = owner.Stats.AttackRangeDistanceRatio.Value * BasicFindingRadius;
            var target = owner.FindBasicAttackTarget(stage, findingRadius);
            if (target != null)
            {
                targetPos = target.CenterPos;
            }
            else
            {
                var item = owner.FindClosestBreakableItemObjectExceptFence(stage, findingRadius);
                if (item != null)
                {
                    targetPos = item.transform.position;
                }
                else
                {
                    return;
                }
            }

            if (_multipleCircleFireStickySlimeStackCount > 0)
            {
                _multipleCircleFireStickySlimeStack++;
                if (_multipleCircleFireStickySlimeStack >= _multipleCircleFireStickySlimeStackCount)
                {
                    _multipleCircleFireStickySlimeStack = 0;

                    FireCircleShape(owner, stage, owner.CenterPos);

                    PlaySkillSoundEffect(owner.CenterPos);
                    _throwAt = now + GetThrowPeriod(owner);
                    return;
                }
            }

            Vector2 targetDir = (targetPos - owner.CenterPos).normalized;
            if(IsTranscendent)
            {
                FireTranscendentProjectile(owner, stage, owner.CenterPos, targetDir);
            }
            else
            {
                FireNormalProjectile(owner, stage, owner.CenterPos, targetDir);
            }
            PlaySkillSoundEffect(owner.CenterPos);
            _throwAt = now + GetThrowPeriod(owner);
        }



        private void FireCircleShape(PlayerCharacter owner, Stage stage, Vector2 firePosition)
        {
            for (int i = 0; i < MultipleCircleFireAmount; i++)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 360f / MultipleCircleFireAmount * i) * Vector2.up;
                if(IsTranscendent)
                {
                    FireTranscendentProjectile(owner, stage, firePosition, dir);
                }
                else
                {
                    FireNormalProjectile(owner, stage, firePosition, dir);
                }
            }
        }


        private void FireNormalProjectile(PlayerCharacter owner, Stage stage, Vector2 firePosition, Vector2 fireDirection)
        {
            float damage = CombatSystem.CalculateCharacterBasicAttackDamage(owner.Stats, _attackPowerRate);
            float speed = owner.Stats.ProjectileMoveSpeedIncreaseRate.Value * NormalSplitObjectSpeed;
            float radius = owner.Stats.AttackRangeDistanceRatio.Value * NormalSplitObjectRadius;
            float knockbackPower = CombatSystem.CalculateCharacterAttackKnockBackPower(owner.Stats, NormalSplitObjectKnobackPower);

            var projectile = stage.CreateProjectile(
                    _slimeProjectileBodyPath,
                    owner.Alliance,
                    owner,
                    damage,
                    knockbackPower,
                    firePosition,
                    fireDirection,
                    speed,
                    NormalSplitObjectAcceleration,
                    radius,
                    NormalSplitObjectAliveDistance,
                    hitChances: 1,
                    splitCount: 0,
                    IsSpinBladeCollide,
                    OnHitCharacterHandler,
                    OnHitItemObjectHandler,
                    null,
                    hitSoundPrefabPath: string.Empty
                    );
            projectile.transform.localScale = radius / 0.25f * Vector3.one;
            
        }

        private void FireTranscendentProjectile(PlayerCharacter owner, Stage stage, Vector2 firePosition, Vector2 fireDirection)
        {
            float damage = CombatSystem.CalculateCharacterBasicAttackDamage(owner.Stats, _attackPowerRate);
            float speed = owner.Stats.ProjectileMoveSpeedIncreaseRate.Value * TranscendentSplitObjectSpeed;
            float radius = owner.Stats.AttackRangeDistanceRatio.Value * TranscendentSplitObjectRadius;
            float knockbackPower = CombatSystem.CalculateCharacterAttackKnockBackPower(owner.Stats, TranscendentSplitObjectKnobackPower);

            var projectile = stage.CreateProjectile(
                    _slimeProjectileBodyPath,
                    owner.Alliance,
                    owner,
                    damage,
                    knockbackPower,
                    firePosition,
                    fireDirection,
                    speed,
                    TranscendentSplitObjectAcceleration,
                    radius,
                    TranscendentSplitObjectAliveDistance,
                    hitChances: 1,
                    splitCount: 0,
                    IsSpinBladeCollide,
                    OnHitCharacterHandler,
                    OnHitItemObjectHandler,
                    null,
                    hitSoundPrefabPath: string.Empty
                    );
            projectile.transform.localScale = radius / 0.25f * Vector3.one;
        }

        private bool OnHitCharacterHandler(Stage stage, Character target, Vector2 hitPosition, ProjectileObject attackerProjectile)
        {
            bool isHitted = attackerProjectile.TryHitCharacter(stage, target, hitPosition, attackerProjectile);
            //투사체는 딱 한번만 맞고 터지도록 한다. 모종의 이유로 HitChance 여러번을 처리하는 버그가 없도록 한다. 
            attackerProjectile.ForceRemoveAllHitChances(stage);
            PlaySkillHitSoundEffect(attackerProjectile.transform.position);

            if (IsTranscendent)
            {
                FireTranscendentSectorShape(stage, attackerProjectile.transform.position, attackerProjectile.Direction);
            }
            else
            {
                FireNormalSectorShape(stage, attackerProjectile.transform.position, attackerProjectile.Direction);
            }

            if(isHitted && target.Action.IsDead)
            {
                DropCheckGoldOrGem(stage, _totalDropChance, DropGoldChance, _dropGoldAmount, attackerProjectile.transform.position);
            }

            return isHitted;
        }
        private bool OnHitItemObjectHandler(Stage stage, Collider2D collidedCharacter, ProjectileObject attackerProjectile)
        {
            bool isHitted = attackerProjectile.TryHitObject(stage, collidedCharacter, attackerProjectile);
            //투사체는 딱 한번만 맞고 터지도록 한다. 모종의 이유로 HitChance 여러번을 처리하는 버그가 없도록 한다. 
            attackerProjectile.ForceRemoveAllHitChances(stage);
            PlaySkillHitSoundEffect(attackerProjectile.transform.position);
            if (IsTranscendent)
            {
                FireTranscendentSectorShape(stage, attackerProjectile.transform.position, attackerProjectile.Direction);
            }
            else
            {
                FireNormalSectorShape(stage, attackerProjectile.transform.position, attackerProjectile.Direction);
            }

            return isHitted;
        }

        private void FireNormalSectorShape(Stage stage, Vector2 firePos, Vector2 fireDirection)
        {
            UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(_collidePath, firePos, fireDirection, NormalSplitObjectRadius/ 0.25f * Vector3.one, null,
            () =>
            {
                UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(_boomPath, firePos, Vector2.zero , NormalSplitObjectRadius / 0.5f * Vector3.one, null,
                () =>
                {
                    float damage = CombatSystem.CalculateCharacterBasicAttackDamage(_owner.Stats, _attackPowerRate) * 0.5f; //슬라임 공격력은 기본 공격력에 절반
                    float forwardMoveSpeed = _owner.Stats.ProjectileMoveSpeedIncreaseRate.Value * NormalSlimeForwardMoveSpeed;
                    float backwardMoveSpeed = _owner.Stats.ProjectileMoveSpeedIncreaseRate.Value * NormalSlimeBackwardMoveSpeed;
                    float radius = _owner.Stats.AttackRangeDistanceRatio.Value * _slimeRadius;
                    float forwardMoveDuration = _owner.Stats.DurationIncreaseRate.Value * _slimeForwardMoveDuration;

                    for (int i = -(_slimeAmount - 1); i <= _slimeAmount - 1; i += 2)
                    {
                        Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 0.5f * (float)i * _slimeSectorAngle / _slimeAmount) * fireDirection;
                        stage.CreateStickySlimeNormalObject(_owner, damage, radius, firePos, dir, forwardMoveSpeed, forwardMoveDuration, backwardMoveSpeed, _itemAcquireAdditionalRangeGrade + BasicItemAcquireAdditionalRange, _totalDropChance, DropGoldChance, _dropGoldAmount);
                    }
                }, null);
            }, null);
        }

        private void FireTranscendentSectorShape(Stage stage, Vector2 firePos, Vector2 fireDirection)
        {
            UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(_collidePath, firePos, fireDirection, TranscendentSplitObjectRadius / 0.25f * Vector3.one, null,
            () =>
            {
                UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(_boomPath, firePos, Vector2.zero, TranscendentSplitObjectRadius / 0.5f * Vector3.one, null,
                () =>
                {
                    float damage = CombatSystem.CalculateCharacterBasicAttackDamage(_owner.Stats, _attackPowerRate) * 0.5f; //슬라임 공격력은 기본 공격력에 절반
                    float forwardMoveSpeed = _owner.Stats.ProjectileMoveSpeedIncreaseRate.Value * TranscendentSlimeForwardMoveSpeed;
                    float backwardMoveSpeed = _owner.Stats.ProjectileMoveSpeedIncreaseRate.Value * TranscendentSlimeBackwardMoveSpeed;
                    float radius = _owner.Stats.AttackRangeDistanceRatio.Value * _slimeRadius;
                    float forwardMoveDuration = _owner.Stats.DurationIncreaseRate.Value * _slimeForwardMoveDuration;

                    for (int i = -(_slimeAmount - 1); i <= _slimeAmount - 1; i += 2)
                    {
                        Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 0.5f * (float)i * _slimeSectorAngle / _slimeAmount) * fireDirection;
                        stage.CreateStickySlimeTranscendentObject(_owner, damage, radius, firePos, dir, forwardMoveSpeed, forwardMoveDuration, backwardMoveSpeed, _itemAcquireAdditionalRangeGrade + BasicItemAcquireAdditionalRange, _totalDropChance, DropGoldChance, _dropGoldAmount);
                    }
                }, null);
            }, null);
        }

        private float GetThrowPeriod(PlayerCharacter owner)
        {
            var attackSpeed = owner.Stats.CharacterAttackSpeed.Value;
            return attackSpeed <= 0f ? 10f : (1f / attackSpeed);
        }

        private void DropCheckGoldOrGem(Stage stage, float dropChance, float goldChance, long dropGoldAmount, Vector2 createPos)
        {
            float dropRandom = Random.Range(minInclusive: 0f, maxInclusive: 0.9999999f);
            if(dropChance < dropRandom)
            {
                return;
            }

            float goldRandom = Random.Range(minInclusive: 0f, maxInclusive: 0.9999999f);
            if(goldChance >= goldRandom)
            {
                long actualGoldAmount = stage.TakeItemBoxDropGolds(dropGoldAmount);
                if (actualGoldAmount > 0)
                {
                    stage.CreateGoldObject(actualGoldAmount, createPos);
                }
            }
        }
    }
}

