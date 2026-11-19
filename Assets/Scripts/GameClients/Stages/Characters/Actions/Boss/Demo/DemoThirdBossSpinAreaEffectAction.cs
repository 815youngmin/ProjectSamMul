using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Demo
{
    /// <summary>제자리에서 special_attack 애니메이션을 FireCount 번 재생하며, 매번 몸 주위에 나선으로 퍼져 나가는 오브젝트를 만든다. 회전 방향은 발사마다 번갈아 바뀐다.</summary>
    public class DemoThirdBossSpinAreaEffectAction : SmartAction<SpriteMonsterAnimationController>
    {
        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private const string SpinObjectBodyPath = "Stage/Common/Projectiles/MonsterProjectile_Orb.prefab";
        private static readonly int FireCount = 3;
        private static readonly int FireAmount = 6;
        private static readonly float CreateRadius = 1f;
        private static readonly float AttackRadius = 0.75f;
        private static readonly float AreaEffectLifeTime = 10f;
        private static readonly float RadiusUpSpeed = 3.0f;
        private static readonly float AngleUpSpeed = 45f;
        private static readonly float RadiusUpAcceleration = 1.0f;
        private static readonly float AngleUpAcceleration = 1.0f;
        private static readonly float BodyRotatingSpeed = 0.0f;
        private static readonly float MaxValue = 9999f;

        private static readonly float FireAnimationDuration = 1f;
        private static readonly float FireAnimationHitTime = 0.5f;

        private Monster _owner;
        private Character _target;
        private int _currentFireCount;

        public override void Initialize(Monster owner, Character target, SpriteMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                FireAnimationDuration * FireCount,
                animationController);

            _owner = owner;
            _target = target;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            _currentFireCount = 0;

            for (int i = 0; i < FireCount; i++)
            {
                float startAt = now + FireAnimationDuration * i;

                base.AddOneOffSubAction(startAt, (Stage stage, float deltaTime, float now) =>
                {
                    AnimationController.UpdateBodyDirectionByMoveDirection(_target.Pos - _owner.Pos);
                    AnimationController.PlaySpecialAttack(FireAnimationDuration);
                });

                base.AddOneOffSubAction(startAt + FireAnimationHitTime, (Stage stage, float deltaTime, float now) =>
                {
                    float angleDirection = _currentFireCount++ % 2 == 0 ? 1 : -1;
                    for (int j = 0; j < FireAmount; j++)
                    {
                        stage.CreateSpinMoveObject(
                            _owner,
                            _owner.SpecialAttackPower * DAMAGE_COEFFICIENT,
                            _owner.CenterPos,
                            SpinObjectBodyPath,
                            AreaEffectLifeTime,
                            CreateRadius * angleDirection,
                            360f / FireAmount * j * angleDirection,
                            RadiusUpSpeed * angleDirection,
                            AngleUpSpeed * angleDirection,
                            RadiusUpAcceleration * angleDirection,
                            AngleUpAcceleration * angleDirection,
                            AttackRadius,
                            BodyRotatingSpeed,
                            Vector2.zero,
                            MaxValue,
                            MaxValue,
                            MaxValue);
                    }
                });
            }
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}
