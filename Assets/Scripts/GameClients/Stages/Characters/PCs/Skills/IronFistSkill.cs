using Shared.StaticDatas;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class IronFistSkill : SkillBase
    {
        private static readonly float FINDING_RADIUS = 10.0f;
        private static readonly float KNOCKBACK_POWER = 0.65f;

        private readonly float _attackPowerRate;                        // Parameter1: 공격 추가 대미지 계수.
        private readonly StatModifier _increaseCharacterAttackSpeed;    // Parameter2: 캐릭터 공격 속력 증가.
        private readonly int _hitCount;                                 // Parameter3: 적을 타격할 수 있는 횟수.
        private readonly float _radius;                                 // Parameter4: 투사체 반지름.
        private readonly float _movingSpeed;                            // Parameter5: 투사체 속력.
        private readonly float _lifetime;                               // Parameter6: 투사체 수명.

        private readonly IReadOnlyCharacterStatCalculators _characterStats;
        private readonly bool _withWindField;
        private readonly bool _removePoisons;

        private float _attackAt;

        public IronFistSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats, IReadOnlyCustomParameters parameters) : base(staticData)
        {
            _attackPowerRate = staticData.Parameter1;
            _increaseCharacterAttackSpeed = new StatModifier(staticData.Parameter2, StatModType.Flat);
            _hitCount = (int)staticData.Parameter3;
            _radius = staticData.Parameter4;
            _movingSpeed = staticData.Parameter5;
            _lifetime = staticData.Parameter6;

            _characterStats = characterStats;
            _withWindField = parameters.GetParameterValue(CustomParameterType.IronFistsCreateWindFields) != 0.0f;
            _removePoisons = parameters.GetParameterValue(CustomParameterType.WindFieldsRemovePoisonousAreaEffects) != 0.0f;

            _attackAt = 0.0f;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            owner.Stats.CharacterAttackSpeed.AddModifier(_increaseCharacterAttackSpeed);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_increaseCharacterAttackSpeed);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if (owner.Action.IsDead)
            {
                return;
            }

            if (now < _attackAt)
            {
                return;
            }

            Vector2 targetPos;
            var target = owner.FindBasicAttackTarget(stage, FINDING_RADIUS);
            if (target != null)
            {
                targetPos = target.CenterPos;
            }
            else
            {
                var item = owner.FindClosestBreakableItemObjectExceptFence(stage, FINDING_RADIUS);
                if (item != null)
                {
                    targetPos = item.transform.position;
                }
                else
                {
                    return;
                }
            }

            bool isMultishot = !IsTranscendent && (this.Level >= 3);

            var movingDirection = (targetPos - owner.CenterPos).normalized;
            float movingSpeed = owner.Stats.ProjectileMoveSpeedIncreaseRate.Value * _movingSpeed;
            float attackDamage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, _attackPowerRate);
            float attackRadius = owner.Stats.AttackRangeDistanceRatio.Value * _radius;
            float knockbackPower = CombatSystem.CalculateCharacterAttackKnockBackPower(owner.Stats, KNOCKBACK_POWER);
            float lifetime = owner.Stats.DurationIncreaseRate.Value * _lifetime;
            bool withWindField = IsTranscendent || _withWindField;

            if (!isMultishot)
            {
                stage.CreateIronFistAreaEffect(
                    owner,
                    position: owner.CenterPos,
                    movingDirection,
                    movingSpeed,
                    attackDamage,
                    attackRadius,
                    knockbackPower,
                    lifetime,
                    hitCount: _hitCount,
                    isTranscendent: IsTranscendent,
                    withWindField,
                    removePoisons: _removePoisons);
            }
            else
            {
                var basePosition = owner.CenterPos;
                var leftOffset = movingDirection.GetOrthogonalVector(left: true).normalized * (attackRadius * 0.6f);
                var leftPosition = basePosition + leftOffset;
                var rightPosition = basePosition - leftOffset;

                stage.CreateIronFistAreaEffect(
                    owner,
                    leftPosition,
                    movingDirection,
                    movingSpeed,
                    attackDamage,
                    attackRadius,
                    knockbackPower,
                    lifetime,
                    hitCount: _hitCount,
                    isTranscendent: IsTranscendent,
                    withWindField,
                    removePoisons: _removePoisons);

                stage.CreateIronFistAreaEffect(
                    owner,
                    rightPosition,
                    movingDirection,
                    movingSpeed,
                    attackDamage,
                    attackRadius,
                    knockbackPower,
                    lifetime,
                    hitCount: _hitCount,
                    isTranscendent: IsTranscendent,
                    withWindField,
                    removePoisons: _removePoisons);
            }

            base.PlaySkillSoundEffect(owner.Pos);
            owner.ConditionalEffects.OnBasicSkillUsed(stage, owner);
            _attackAt = now + 1.0f / _characterStats.CharacterAttackSpeedValue;
        }
    }
}
