using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;
using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Alibaba
{
    public class BlackCyclopsCircleProjectileAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string AttackEffectPath = "Stages/Characters/SpineSkeletons/Arabian/09.Arabian_Boss_BlackCyclops/Arabian_Boss_BlackCyclops_eff.prefab";

        private static readonly string ReadyAnimationName = "PrepareSingleExecutionBegind";
        private static readonly string WaitAnimationName = "PrepareSingleExecutionRepeatd";
        private static readonly string AttackAnimationName = "SingleExecutionAction";
        private static readonly string EndAnimationName = "SingleExecutionEnd";

        private static readonly float MELEE_DAMAGE_COEFFICIENT = 1.0f;
        private static readonly float MeleeAttackRadius = 5f;

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;
        private static readonly int ProjectileAmount = 8;
        private static readonly string ProjectileBodyPath = "Stages/Projectiles/RedSlashObject_Radius0_75.prefab";
        private static readonly float ProjectileSpeed = 15f;
        private static readonly float ProjectileAcceleration = 1f;
        private static readonly float ProjectileRadius = 0.75f;
        private static readonly float ProjectileAliveDistance = 50;
        private static readonly float ProjectileKnobackPower = 0.1f;

        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _waitAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;
        private Vector2 _attackPosition;

        private SkeletonAnimation _effect;

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                animationController.FindAnimation(WaitAnimationName).Duration +
                animationController.FindAnimation(AttackAnimationName).Duration +
                animationController.FindAnimation(ReadyAnimationName).Duration +
                animationController.FindAnimation(WaitAnimationName).Duration +
                animationController.FindAnimation(AttackAnimationName).Duration +
                animationController.FindAnimation(EndAnimationName).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _waitAnimation = AnimationController.FindAnimation(WaitAnimationName);
            _attackAnimation = AnimationController.FindAnimation(AttackAnimationName);
            _endAnimation = AnimationController.FindAnimation(EndAnimationName);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _waitAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _waitAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, 0f);

            _effect = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(AttackEffectPath);
            _effect.transform.SetParent(_owner.transform);
            _effect.transform.localScale = Vector3.one;
            _effect.transform.localPosition = Vector3.zero;
            _effect.gameObject.SetActive(false);

            float time = now;
            float indicatorAt = now;
            float effectAt = now + _readyAnimation.Duration + _waitAnimation.Duration;
            float attackAt = effectAt + AnimationController.FindHitTime(_attackAnimation);

            base.AddOneOffSubAction(indicatorAt, (Stage stage, float deltaTime, float now) =>
            {
                bool isMovingToLeft = _owner.MoveDir.x <= 0;
                if (isMovingToLeft)
                {
                    _attackPosition = _owner.CenterPos + new Vector2(-6f, -3f);
                }
                else
                {
                    _attackPosition = _owner.CenterPos + new Vector2(6f, -3f);
                }
                stage.CreateCircularAttackRangeIndicator(_attackPosition, MeleeAttackRadius, _readyAnimation.Duration + _waitAnimation.Duration + AnimationController.FindHitTime(_attackAnimation));

            });

            base.AddOneOffSubAction(effectAt, (Stage stage, float deltaTime, float now) =>
            {
                _effect.gameObject.SetActive(true);
                _effect.AnimationState.SetAnimation(0, "SingleExecutionAction_eff", false);
                _effect.Skeleton.ScaleX = AnimationController.Body.skeleton.GetLocalScale().x;

            });

            base.AddOneOffSubAction(attackAt, (Stage stage, float deltaTime, float now) =>
            {
                CircularTargetArea attackArea = new CircularTargetArea(_attackPosition, MeleeAttackRadius);
                CombatSystem.HitOnTargetArea(
                    stage, attackArea, _owner, _owner.SpecialAttackPower * MELEE_DAMAGE_COEFFICIENT,
                    CombatSystem.KnockBackType.Pivot, Vector2.zero,
                    knockBackPower: 0, null, null, hitSoundPrefabPath: string.Empty);

                this.TryCircleFiring(stage, attackArea.Center, 0f);

            });

            indicatorAt = now + _readyAnimation.Duration + _waitAnimation.Duration + _attackAnimation.Duration;

            effectAt = now + _readyAnimation.Duration + _waitAnimation.Duration + _attackAnimation.Duration +
                            _readyAnimation.Duration + _waitAnimation.Duration;

            attackAt = effectAt+ AnimationController.FindHitTime(_attackAnimation);

            base.AddOneOffSubAction(indicatorAt, (Stage stage, float deltaTime, float now) =>
            {
                bool isMovingToLeft = _owner.MoveDir.x <= 0;
                if (isMovingToLeft)
                {
                    _attackPosition = _owner.CenterPos + new Vector2(-6f, -3f);
                }
                else
                {
                    _attackPosition = _owner.CenterPos + new Vector2(6f, -3f);
                }
                stage.CreateCircularAttackRangeIndicator(_attackPosition, MeleeAttackRadius, _readyAnimation.Duration + _waitAnimation.Duration + AnimationController.FindHitTime(_attackAnimation));

            });

            base.AddOneOffSubAction(effectAt, (Stage stage, float deltaTime, float now) =>
            {
                _effect.gameObject.SetActive(true);
                _effect.AnimationState.SetAnimation(0, "SingleExecutionAction_eff", false);
                _effect.Skeleton.ScaleX = AnimationController.Body.skeleton.GetLocalScale().x;

            });

            base.AddOneOffSubAction(attackAt, (Stage stage, float deltaTime, float now) =>
            {
                CircularTargetArea attackArea = new CircularTargetArea(_attackPosition, MeleeAttackRadius);
                CombatSystem.HitOnTargetArea(
                    stage, attackArea, _owner, _owner.SpecialAttackPower * MELEE_DAMAGE_COEFFICIENT,
                    CombatSystem.KnockBackType.Pivot, Vector2.zero,
                    knockBackPower: 0, null, null, hitSoundPrefabPath: string.Empty);

                this.TryCircleFiring(stage, attackArea.Center, 360f / ProjectileAmount * 0.5f);
            });
        }


        public override ActionBase End(Stage stage)
        {
            return null;
        }



        private void TryCircleFiring(Stage stage, Vector2 firePosition, float angleOffset)
        {
            for (int i = 0; i < ProjectileAmount; i++)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 360f / ProjectileAmount * i + angleOffset) * Vector2.up;
                stage.CreateProjectile(
                     ProjectileBodyPath,
                     _owner.Alliance,
                     _owner,
                     _owner.RangeAttackPower * DAMAGE_COEFFICIENT,
                     ProjectileKnobackPower,
                     firePosition,
                     dir,
                     ProjectileSpeed,
                     ProjectileAcceleration,
                     ProjectileRadius,
                     ProjectileAliveDistance,
                     hitChances: 1,
                     splitCount: 0,
                     isRemovableBySpinBladeObject: false,
                     hitSoundPrefabPath: string.Empty
                     );
            }
        }

    }
}

