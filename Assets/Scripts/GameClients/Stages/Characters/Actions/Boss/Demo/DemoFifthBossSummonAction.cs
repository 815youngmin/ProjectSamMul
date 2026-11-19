using Shared.GameDataTypes;
using Shared.StaticDatas;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.MeleeAIs;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Demo
{
    /// <summary>제자리에서 special_attack 애니메이션을 재생하며 울타리 안 임의 위치에 소환 위치를 예고한 뒤, 그 자리에 일반 몬스터 여러 마리를 소환한다.</summary>
    public class DemoFifthBossSummonAction : SmartAction<SpriteMonsterAnimationController>
    {
        private static readonly CharacterType SummonCharacterType = CharacterType.Egypt_Warrior;

        private static readonly int SummonAmount = 8;
        // 스테이지 3에서 같은 몬스터가 스폰될 때 쓰는 가중치와 같다.
        private static readonly float SummonHPWeight = 18.0f;
        private static readonly float SummonAttackPowerWeight = 10.0f;

        // 예고 표시와 애니메이션이 재생되는 시간. 끝나는 시점에 소환한다.
        private static readonly float SummonTime = 1.0f;
        // 소환된 몬스터가 등장 연출(무적)을 하는 시간.
        private static readonly float SummonAppearDuration = 0.5f;

        private Monster _owner;
        private Character _target;

        private Vector2[] _summonPositions;
        private MonsterStaticData _summonMonsterStaticData;

        public override void Initialize(Monster owner, Character target, SpriteMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                SummonTime + SummonAppearDuration,
                animationController);

            _owner = owner;
            _target = target;

            _summonPositions = new Vector2[SummonAmount];
            _summonMonsterStaticData = StaticDataRepository.Instance.Monsters.Get(SummonCharacterType);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            AnimationController.UpdateBodyDirectionByMoveDirection(_target.Pos - _owner.Pos);
            AnimationController.PlaySpecialAttack(SummonTime);

            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();
            for (int i = 0; i < SummonAmount; ++i)
            {
                float randomX = Random.Range(rect.xMin, rect.xMax);
                float randomY = Random.Range(rect.yMin, rect.yMax);

                _summonPositions[i] = new Vector2(randomX, randomY);
            }

            base.AddOneOffSubAction(now, (Stage stage, float deltaTime, float now) =>
            {
                for (int i = 0; i < SummonAmount; ++i)
                {
                    stage.CreateCircularAttackRangeIndicator(_summonPositions[i], _summonMonsterStaticData.ColliderRadius * 1.5f, SummonTime);
                }
            });

            base.AddOneOffSubAction(now + SummonTime, (Stage stage, float deltaTime, float now) =>
            {
                for (int i = 0; i < SummonAmount; ++i)
                {
                    var commandSender = stage.SummonMonster(
                        _owner.Alliance,
                        SummonCharacterType,
                        MonsterInstanceInitialData.CreateForSummonedMonsterWithoutSummoner(_summonPositions[i], SummonHPWeight, SummonAttackPowerWeight),
                        SummonAppearDuration);
                    commandSender.SendChangeMonsterAIStrategyCommand(stage, new MeleeIdleAIStrategy());
                }
            });
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}
