using Shared.GameDataTypes;
using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies;
using Z.UnityHelpers;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;

namespace Z.GameClients.Stages.Characters.Actions.Boss.Steampunk
{
    public class NikolaTeslaSummonAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly float SummonDelay = 0.2f;

        private static readonly string ReadyAnimationName = "SummonExecutionBegin";
        private static readonly string SummonAnimationName = "SummonExecutionRepeat";
        private static readonly string EndAnimationName = "SummonExecutionEnd";
        private static readonly float SummonAnimationDuration = 0.5f;
        private static readonly float SummonIndicatorRadius = 2f;

        private static readonly float SummonHPWeight = 4830f;
        private static readonly float SummonAttackPowerWeight = 427.5f;
        private static CharacterType SummonCharacterType = CharacterType.Steampunk_EliteTeslaCoil;

        private Monster _owner;

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
                    SummonCharacterType,
                    MonsterInstanceInitialData.CreateForSummonedMonsterWithoutSummoner(_summonPosLeft, SummonHPWeight, SummonAttackPowerWeight),
                    SummonDelay);
                commandSneder1.SendChangeMonsterAIStrategyCommand(stage, new TurretReflectionRangeIdleAIStrategy());

                var commandSneder2 = stage.SummonMonster(
                                    _owner.Alliance,
                                    SummonCharacterType,
                                    MonsterInstanceInitialData.CreateForSummonedMonsterWithoutSummoner(_summonPosRight, SummonHPWeight, SummonAttackPowerWeight),
                                    SummonDelay);
                commandSneder2.SendChangeMonsterAIStrategyCommand(stage, new TurretReflectionRangeIdleAIStrategy());

                //연기 이펙트 추가
                UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(SmmonSmokePath, _summonPosLeft, Vector2.one * 2, null);
                UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(SmmonSmokePath, _summonPosRight, Vector2.one * 2, null);
            });
        }

        private void GetSummonPosition(Stage stage)
        {
            //원형 형태의 울타리라 가정하고 짜여진 코드입니다.
            //울타리 중심부를 기준으로 몬스터 소환 방향을 결정해 울타리 밖에 소환되는 일이 없도록 처리 합니다.
            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();

            Vector2 dir = (rect.center - _owner.Pos).normalized;
            if(dir == Vector2.zero)
            {
                dir = Vector2.up;
            }
            Vector2 leftDir = Quaternion.Euler(0f, 0f, 60f) * dir;
            Vector2 rightDir = Quaternion.Euler(0f, 0f, -60f) * dir;

            _summonPosLeft = _owner.Pos + leftDir * 5f;
            _summonPosRight = _owner.Pos + rightDir * 5f;
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}

