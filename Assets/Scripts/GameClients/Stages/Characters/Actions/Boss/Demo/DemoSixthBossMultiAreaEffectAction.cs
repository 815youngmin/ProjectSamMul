using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Demo
{
    /// <summary>
    /// 제자리에서 special_attack 애니메이션을 AttackCount 번 재생하며, 매번 타겟 주변 임의 위치 여러 곳에 범위를 예고한 뒤 지속 피해 장판을 만든다.
    /// 장판은 PoisonousAreaEffectObject(리소스 없이 붉게 표시되는 장판)를 쓴다.
    /// </summary>
    public class DemoSixthBossMultiAreaEffectAction : SmartAction<SpriteMonsterAnimationController>
    {
        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private static readonly int AttackCount = 3;
        // 한 번의 공격 애니메이션 길이이자 공격 간격. 장판 위치는 애니메이션 중간(HitTime)에 정한다.
        private static readonly float AttackPeriod = 1f;
        private static readonly float AttackHitTime = 0.25f;

        private static readonly float AreaCreateRadius = 8f;
        private static readonly int AreaEffectAmount = 5;
        // 위치를 예고한 뒤 피해가 들어가기 시작할 때까지의 시간.
        private static readonly float AreaIndicatorDuration = 1f;
        // PoisonousAreaEffectObject 가 생성된 뒤 피해 없이 진해지는 시간(APPEAR_DURATION). 이만큼 먼저 만들어 예고 끝에 맞춘다.
        private static readonly float AreaAppearDuration = 0.5f;
        private static readonly float AreaTickPeriod = 0.25f;
        private static readonly float AreaLifeTime = 5f;
        private static readonly float AreaRadius = 2f;

        private Monster _owner;
        private Character _target;
        private Vector2[,] _areaPositions;

        public override void Initialize(Monster owner, Character target, SpriteMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                AttackCount * AttackPeriod + (AreaIndicatorDuration - AreaAppearDuration),
                animationController);

            _owner = owner;
            _target = target;
            _areaPositions = new Vector2[AttackCount, AreaEffectAmount];
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            AnimationController.UpdateBodyDirectionByMoveDirection(_target.Pos - _owner.Pos);

            for (int i = 0; i < AttackCount; i++)
            {
                int attackIndex = i;
                float startAt = now + AttackPeriod * i;
                float indicatorAt = startAt + AttackHitTime;
                float areaAt = indicatorAt + AreaIndicatorDuration - AreaAppearDuration;

                base.AddOneOffSubAction(startAt, (Stage stage, float deltaTime, float now) =>
                {
                    AnimationController.PlaySpecialAttack(AttackPeriod);
                });

                base.AddOneOffSubAction(indicatorAt, (Stage stage, float deltaTime, float now) =>
                {
                    for (int j = 0; j < AreaEffectAmount; j++)
                    {
                        Vector2 position = _target.Pos + Random.insideUnitCircle.normalized * Random.Range(0, AreaCreateRadius);
                        _areaPositions[attackIndex, j] = position;
                        stage.CreateCircularAttackRangeIndicator(position, AreaRadius, AreaIndicatorDuration);
                    }
                });

                base.AddOneOffSubAction(areaAt, (Stage stage, float deltaTime, float now) =>
                {
                    for (int j = 0; j < AreaEffectAmount; j++)
                    {
                        stage.CreatePoisonousAreaEffect(
                            _owner,
                            _areaPositions[attackIndex, j],
                            AreaLifeTime,
                            AreaTickPeriod,
                            _owner.SpecialAttackPower * DAMAGE_COEFFICIENT,
                            AreaRadius);
                    }
                });
            }

            base.AddOneOffSubAction(now + AttackCount * AttackPeriod, (Stage stage, float deltaTime, float now) =>
            {
                AnimationController.PlayIdleAttackActionInfinitely();
            });
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}
