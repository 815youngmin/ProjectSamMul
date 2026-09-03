using Shared.GameDataTypes;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies;
using SamMul.UnityHelpers;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Western
{
    public class KidEliteSummonAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly float SummonDelay = 0.2f;

        private static readonly string ReadyAnimationName = "SummonExecutionBegin";
        private static readonly string SummonAnimationName = "SummonExecutionRepeat";
        private static readonly string EndAnimationName = "SummonExecutionEnd";
        private static readonly float SummonAnimationDuration = 0.5f;
        private static readonly float SummonIndicatorRadius = 2f;

        private static readonly float SummonLeftTypeHPWeight = 994f;
        private static readonly float SummonLeftTypeAttackPowerWeight = 100.6f;
        private static CharacterType SummonLeftCharacterType = CharacterType.Western_EliteGunslingerRider_B;

        private static readonly float SummonRightTypeHPWeight = 994f;
        private static readonly float SummonRightTypeAttackPowerWeight = 100.6f;
        private static CharacterType SummonRightCharacterType = CharacterType.Western_EliteGunslingerRider;

        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _summonAnimation;
        private Animation _endAnimation;

        private static readonly string SmmonSmokePath = "Stages/ETCEffects/SummonSmokeChapter4.prefab";
        private Vector2 _summonPosLeft;
        private Vector2 _summonPosRight;

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                        animationController.FindAnimation(ReadyAnimationName).Duration +
                        SummonAnimationDuration +
                        animationController.FindAnimation(EndAnimationName).Duration,
                        animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _summonAnimation = AnimationController.FindAnimation(SummonAnimationName);
            _endAnimation = AnimationController.FindAnimation(EndAnimationName);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _summonAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, 0f);

            this.GetSummonPosition(stage);

            float time = now;
            base.AddOneOffSubAction(time, (Stage stage, float deltaTime, float now) =>
            {
                stage.CreateCircularAttackRangeIndicator(_summonPosLeft, SummonIndicatorRadius, _readyAnimation.Duration + SummonAnimationDuration);
                stage.CreateCircularAttackRangeIndicator(_summonPosRight, SummonIndicatorRadius, _readyAnimation.Duration + SummonAnimationDuration);
            });

            time = now + _readyAnimation.Duration + SummonAnimationDuration;
            base.AddOneOffSubAction(time, (Stage stage, float deltaTime, float now) =>
            {
                var commandSneder1 = stage.SummonMonster(
                    _owner.Alliance,
                    SummonLeftCharacterType,
                    MonsterInstanceInitialData.CreateForSummonedMonsterWithoutSummoner(_summonPosLeft, SummonLeftTypeHPWeight, SummonLeftTypeAttackPowerWeight),
                    SummonDelay);
                commandSneder1.SendChangeMonsterAIStrategyCommand(stage, new MultipleHorizontalEliteRangeAttackSummonOnDieCombatAIStrategy(_target));

                var commandSneder2 = stage.SummonMonster(
                                    _owner.Alliance,
                                    SummonRightCharacterType,
                                    MonsterInstanceInitialData.CreateForSummonedMonsterWithoutSummoner(_summonPosRight, SummonRightTypeHPWeight, SummonRightTypeAttackPowerWeight),
                                    SummonDelay);
                commandSneder2.SendChangeMonsterAIStrategyCommand(stage, new MultipleHorizontalEliteRangeAttackSummonOnDieCombatAIStrategy(_target));

                //연기 이펙트 추가
                UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(SmmonSmokePath, _summonPosLeft, Vector2.one * 2, null);
                UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(SmmonSmokePath, _summonPosRight, Vector2.one * 2, null);
            });
        }

        private void GetSummonPosition(Stage stage)
        {
            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();

            _summonPosLeft = _owner.Pos + new Vector2(-5, 0);
            _summonPosRight = _owner.Pos + new Vector2(5, 0);

            if (_summonPosLeft.x < rect.xMin)
            {
                if (_owner.Pos.y > rect.center.y)
                {
                    //아래쪽으로 옮겨줘야됨
                    _summonPosLeft = _owner.Pos + new Vector2(0, -5);
                }
                else
                {
                    //위쪽으로 옮겨줘야됨
                    _summonPosLeft = _owner.Pos + new Vector2(0, 5);
                }
            }

            if (rect.xMax < _summonPosRight.x)
            {
                if (_owner.Pos.y > rect.center.y)
                {
                    //아래쪽으로 옮겨줘야됨
                    _summonPosRight = _owner.Pos + new Vector2(0, -5);
                }
                else
                {
                    //위쪽으로 옮겨줘야됨
                    _summonPosRight = _owner.Pos + new Vector2(0, 5);
                }
            }

        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}

