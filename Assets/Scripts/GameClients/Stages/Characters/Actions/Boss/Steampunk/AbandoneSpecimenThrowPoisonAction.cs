using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Steampunk
{
    public class AbandoneSpecimenThrowPoisonAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string ReadyAnimationName = "PrepareSingleExecutionBegin";
        private static readonly string WaitAnimationName = "PrepareSingleExecutionRepeat";
        private static readonly string AttackAnimationName = "SingleExecutionAction";
        private static readonly string EndAnimationName = "SingleExecutionEnd";

        private static readonly float DAMAGE_COEFFICIENT = 1.0f;

        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _waitAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;

        private static readonly float AreaEffectDuration = 5f;
        private static readonly float AreaEffectRadius = 2.5f;
        private static readonly int AreaEffectAmount = 6;
        private static readonly float ThrowDistance = 10f;
        private static readonly float ThrowDelay = 0.075f;

        private List<Vector2> _poisonThrowPositions;

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
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

            _poisonThrowPositions = new List<Vector2>();
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _waitAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, false, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, 0f);

            float hitTimeOnAnimation = _readyAnimation.Duration + _waitAnimation.Duration + AnimationController.FindHitTime(_attackAnimation);
            this.GetNonOverlappingPositions();
            for (int i = 0; i < _poisonThrowPositions.Count; i++)
            {
                stage.CreatePoisonBall(_owner, _owner.CenterPos, _poisonThrowPositions[i], hitTimeOnAnimation + ThrowDelay * i, AreaEffectRadius, _owner.SpecialAttackPower * DAMAGE_COEFFICIENT, AreaEffectDuration);
            }
        }


        public override ActionBase End(Stage stage)
        {
            return null;
        }

        bool IsOverlapping(Vector2 newPos)
        {
            foreach (Vector2 pos in _poisonThrowPositions)
            {
                float distance = Vector2.Distance(newPos, pos);
                if (distance < 1.5f * AreaEffectRadius)    //약간의 겹침은 허용한다.
                {
                    return true; // 겹침
                }
            }
            return false; // 겹치지 않음
        }
        void GetNonOverlappingPositions()
        {
            for (int i = 0; i < AreaEffectAmount; i++)
            {
                Vector2 newPos;
                int attempt = 0;
                do
                {
                    newPos = _owner.CenterPos + Random.insideUnitCircle * ThrowDistance;
                    attempt++;

                    // 무한 루프 방지 (50회 시도 후 강제 중단)
                    if (attempt > 50)
                    {
                        break;
                    }

                } while (IsOverlapping(newPos)); // 겹치면 다시 위치 생성

                // 겹치지 않는 위치가 확인되면 장판 위치 지정
                _poisonThrowPositions.Add(newPos);
            }
        }
    }
}

