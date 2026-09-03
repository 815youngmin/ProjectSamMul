using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;
using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.ResourcePools;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Alibaba
{
    public class BlackCyclopsDashAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string DashEffectPath = "Stages/Characters/SpineSkeletons/Arabian/09.Arabian_Boss_BlackCyclops/Arabian_Boss_BlackCyclops_eff.prefab";

        private static readonly string ReadyAnimationName = "DashBegin";
        private static readonly string DashAnimationName = "DashRepeat";
        private static readonly string EndAnimationName = "DashEnd";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private static readonly float SectorAngle = 150f;
        private static readonly int ProjectileAmount = 7;
        private static readonly string ProjectileBodyPath = "Stages/Projectiles/StoneSmall_Radius0_3.prefab";
        private static readonly float ProjectileSpeed = 15f;
        private static readonly float ProjectileAcceleration = 1f;
        private static readonly float ProjectileRadius = 0.3f;
        private static readonly float ProjectileAliveDistance = 45f;
        private static readonly float ProjectileKnobackPower = 0.1f;

        private static readonly float DashDuration = 1f;
        private static readonly float DashSpeed = 15.0f;

        private Monster _owner;
        private Character _target;
        private Vector2 _dashDirection;

        private Animation _readyAnimation;
        private Animation _dashAnimation;
        private Animation _endAnimation;

        private SkeletonAnimation _effect;

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                DashDuration +
                animationController.FindAnimation(EndAnimationName).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _dashAnimation = AnimationController.FindAnimation(DashAnimationName);
            _endAnimation = AnimationController.FindAnimation(EndAnimationName);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            _effect = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(DashEffectPath);
            _effect.transform.SetParent(_owner.transform);
            _effect.transform.localScale = Vector3.one;
            _effect.transform.localPosition = Vector3.zero;
            _effect.gameObject.SetActive(false);

            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _dashAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, DashDuration);


            float indicatorAt = now;
            float dashAt = indicatorAt + _readyAnimation.Duration;

            base.AddOneOffSubAction(indicatorAt, (Stage stage, float deltaTime, float now) =>
            {
                stage.CreateDashAttackRangeIndicator(_owner, _target, _owner.CollisionAttackRadius + 2.0f, DashSpeed * DashDuration, _readyAnimation.Duration);
                AnimationController.UpdateBodyDirectionByMoveDirection(_target.Pos - _owner.Pos);
                _dashDirection = Vector2.zero;
            });

            base.AddDurationalSubAction(dashAt, DashDuration, (Stage stage, float deltaTime, float now) =>
            {
                if (_dashDirection == Vector2.zero)
                {
                    _dashDirection = (_target.Pos - _owner.Pos).normalized;
                    AnimationController.UpdateBodyDirectionByMoveDirection(_dashDirection);

                    _effect.gameObject.SetActive(true);
                    _effect.AnimationState.SetAnimation(0, "DashRepeat_eff", true);
                    _effect.AnimationState.AddEmptyAnimation(0, 0f, DashDuration);
                    _effect.Skeleton.ScaleX = AnimationController.Body.skeleton.GetLocalScale().x;
                }
                _owner.transform.Translate(deltaTime * DashSpeed * _dashDirection, Space.World);
            });

            base.AddOneOffSubAction(dashAt + DashDuration, (Stage stage, float deltaTime, float now) =>
            {
                Vector2 fireDirection = _target.Pos - _owner.CenterPos;
                this.TryMultipleDirectionFiring(stage, _owner.CenterPos, fireDirection);
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
                ResourcePool.Instance.PutBackInstance(DashEffectPath, _effect.gameObject);
                _effect = null;
            }
            return null;
        }


        private void TryMultipleDirectionFiring(Stage stage, Vector2 firePosition, Vector2 fireDirection)
        {
            for (int i = -(ProjectileAmount - 1); i <= ProjectileAmount - 1; i += 2)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 0.5f * (float)i * SectorAngle / ProjectileAmount) * fireDirection;
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
                     isRemovableBySpinBladeObject: true,
                     hitSoundPrefabPath: string.Empty
                     );
            }
        }

    }
}

