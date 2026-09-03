using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;
using UnityEngine;
using Z.GameClients.Stages.AreaEffectObjects;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Alibaba
{
    public class BlackCyclopsSplitObjectAction : SmartAction<SpineMonsterAnimationController>
    {

        private static readonly string AttackEffectPath = "Stages/Characters/SpineSkeletons/Arabian/09.Arabian_Boss_BlackCyclops/Arabian_Boss_BlackCyclops_eff.prefab";

        private static readonly string ReadyAnimationName = "PrepareSingleExecutionBegind";
        private static readonly string WaitAnimationName = "PrepareSingleExecutionRepeatd";
        private static readonly string AttackAnimationName = "SingleExecutionAction";
        private static readonly string EndAnimationName = "SingleExecutionEnd";

        private static readonly float SPLITOBJECT_DAMAGE_COEFFICIENT = 1.0f;
        private static readonly float SPLITEDOBJECT_DAMAGE_COEFFICIENT = 1.0f;

        private static readonly float WaitAnimationDuration = 1.5f;
        private static readonly float MELEE_DAMAGE_COEFFICIENT = 1.0f;
        private static readonly float MeleeAttackRadius = 7f;

        private static readonly int ProjectileAmount = 6;
        private static readonly float ProjectileSpeed = 10;
        private static readonly float ProjectileAliveDistance = 20f;
        private static readonly float ProjectileRadius = 0.8f;
        private static readonly float ProjectileKnobackPower = 0.1f;

        private static readonly int SplitedProjectileAmount = 6;
        private static readonly float SplitedProjectileRadius = 0.4f;
        private static readonly float SplitedProjectileSpeed = 15f;
        private static readonly float SplitedProjectileAcceleration = 1f;
        private static readonly float SplitedProjectileKnobackPower = 0.1f;
        private static readonly float SplitedProjectileAliveDistance = 45f;
        private static readonly bool IsSpinBladeCollide = true;

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
                WaitAnimationDuration +
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

            _effect = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(AttackEffectPath);
            _effect.transform.SetParent(_owner.transform);
            _effect.transform.localScale = Vector3.one;
            _effect.transform.localPosition = Vector3.zero;
            _effect.gameObject.SetActive(false);


            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _waitAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, false, WaitAnimationDuration);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, 0f);


            base.AddOneOffSubAction(now, (Stage stage, float deltaTime, float now) =>
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
                stage.CreateCircularAttackRangeIndicator(_attackPosition, MeleeAttackRadius, _readyAnimation.Duration + WaitAnimationDuration + AnimationController.FindHitTime(_attackAnimation));
            });

            base.AddOneOffSubAction(now + _readyAnimation.Duration + WaitAnimationDuration, (Stage stage, float deltaTime, float now) =>
            {
                _effect.gameObject.SetActive(true);
                _effect.AnimationState.SetAnimation(0, "SingleExecutionAction_eff", false);
                _effect.Skeleton.ScaleX = AnimationController.Body.skeleton.GetLocalScale().x;
            });

            base.AddOneOffSubAction(now + _readyAnimation.Duration + WaitAnimationDuration + AnimationController.FindHitTime(_attackAnimation), (Stage stage, float deltaTime, float now) =>
            {
                CircularTargetArea attackArea = new CircularTargetArea(_attackPosition, MeleeAttackRadius);
                CombatSystem.HitOnTargetArea(
                    stage, attackArea, _owner, _owner.SpecialAttackPower * MELEE_DAMAGE_COEFFICIENT,
                    CombatSystem.KnockBackType.Pivot, Vector2.zero,
                    knockBackPower: 0, null, null, hitSoundPrefabPath: string.Empty);

                this.TryCircleFiring(stage, attackArea.Center);
            });
        }

        public override bool Cancel(Stage stage)
        {
            this.End(stage);
            return base.Cancel(stage);
        }
        public override ActionBase End(Stage stage)
        {
            if(_effect != null)
            {
                ResourcePool.Instance.PutBackInstance(AttackEffectPath, _effect.gameObject);
                _effect = null;
            }
            return null;
        }



        private void TryCircleFiring(Stage stage, Vector2 firePosition)
        {
            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();

            for (int i = 0; i < ProjectileAmount; i++)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 360f / ProjectileAmount * i) * Vector2.up;
                stage.CreateSplitAreaEffectObject(
                    AreaEffectType.BlackCyclopsSplitObject,
                    _owner,
                    ProjectileRadius,
                    firePosition,
                    dir,
                    ProjectileSpeed,
                    _owner.SpecialAttackPower * SPLITOBJECT_DAMAGE_COEFFICIENT,
                    ProjectileAliveDistance,
                    rect,
                    SplitedProjectileAmount,
                    SplitedProjectileRadius,
                    SplitedProjectileSpeed,
                    SplitedProjectileAcceleration,
                    _owner.SpecialAttackPower * SPLITEDOBJECT_DAMAGE_COEFFICIENT,
                    SplitedProjectileKnobackPower,
                    SplitedProjectileAliveDistance,
                    IsSpinBladeCollide
                    );
            }
        }


    }
}

