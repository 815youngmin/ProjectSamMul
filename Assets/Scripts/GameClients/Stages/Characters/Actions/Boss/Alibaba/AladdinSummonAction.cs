using Shared.GameDataTypes;
using Shared.StaticDatas;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.MeleeAIs;
using SamMul.UnityHelpers;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Alibaba
{
    public class AladdinSummonAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "SummonExecutionBegin";
        private static readonly string SummonAnimationName = "SummonExecutionRepeat";
        private static readonly string EndAnimationName = "SummonExecutionEnd";

        private static readonly string SmmonSmokePath = "Stages/ETCEffects/SummonSmokeChapter3.prefab";
        private static readonly CharacterType SummonCharacterType = CharacterType.Arabian_EliteGuardCaptain;

        private static readonly float SummonTime = 1.0f;
        private static readonly int SummonAmount = 2;

        private static readonly float SummonHPWeight = 311.29f;
        private static readonly float SummonAttackPowerWeight = 415.75f;
        
        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _summonAnimation;
        private Animation _endAnimation;

        private Vector2[] _summonPositions;
        private MonsterStaticData _summonMonsterStaticData;

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
             animationController.FindAnimation(ReadyAnimationName).Duration +
             SummonTime +
             animationController.FindAnimation(EndAnimationName).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _summonAnimation = AnimationController.FindAnimation(SummonAnimationName);
            _endAnimation = AnimationController.FindAnimation(EndAnimationName);

            _summonPositions = new Vector2[SummonAmount];
            _summonMonsterStaticData = StaticDataRepository.Instance.Monsters.Get(SummonCharacterType);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _summonAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, SummonTime);

            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();
            for (int i = 0; i < SummonAmount; ++i)
            {
                float randomX = Random.Range(rect.xMin, rect.xMax);
                float randomY = Random.Range(rect.yMin, rect.yMax);

                _summonPositions[i] = new Vector2(randomX, randomY);
            }

            float summonAt = now + _readyAnimation.Duration + SummonTime;

            base.AddOneOffSubAction(now, (Stage stage, float deltaTime, float now) =>
            {
                for (int i = 0; i < SummonAmount; ++i)
                {
                    stage.CreateCircularAttackRangeIndicator(_summonPositions[i], _summonMonsterStaticData.ColliderRadius * 1.5f, _readyAnimation.Duration + SummonTime);
                }
            });

            base.AddOneOffSubAction(summonAt, (Stage stage, float deltaTime, float now) =>
            {
                for (int i = 0; i < SummonAmount; ++i)
                {
                    var commandSender = stage.SummonMonster(
                        _owner.Alliance,
                        SummonCharacterType,
                      MonsterInstanceInitialData.CreateForSummonedMonsterWithoutSummoner(_summonPositions[i], SummonHPWeight, SummonAttackPowerWeight),
                        _endAnimation.Duration);
                    commandSender.SendChangeMonsterAIStrategyCommand(stage, new MeleeIdleAIStrategy());
                    UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(SmmonSmokePath, _summonPositions[i], Vector2.one * 1.5f, null);

                }
            });

        }


        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}

