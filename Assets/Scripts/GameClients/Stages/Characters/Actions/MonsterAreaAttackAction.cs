using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.UnityHelpers;
using UnityEngine;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;

namespace Z.GameClients.Stages.Characters.Actions
{

    public class MonsterAreaAttackAction : ActionBase
    {
        private readonly Monster _owner;
        private readonly float _hitTimeOnAnimation;

        public Animation _attackAnimation;
        private float _hitAt;
        private readonly CircularSectorTargetArea _attackArea;
        private readonly float _animationDuration;
        private SpineMonsterAnimationController AnimationController => base._animationController as SpineMonsterAnimationController;

        public MonsterAreaAttackAction(Monster owner, SpineMonsterAnimationController animationController, string animationName, float animationDuration, CircularSectorTargetArea targetArea) : base(ActionType.Skill, animationDuration, animationController)
        {
            _owner = owner;
            _attackAnimation = AnimationController.FindAnimation(animationName);
            if (null != _attackAnimation)
            {
                _hitTimeOnAnimation = AnimationController.FindHitTime(_attackAnimation);
                if (0 == _hitTimeOnAnimation)
                {
                    Debug.LogWarning($"{owner.name}의 Hit Event를 찾을 수 없습니다. 추가해주세요");
                }
            }

            _animationDuration = animationDuration;
            _attackArea = targetArea;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            _hitAt = now + _hitTimeOnAnimation;
            if (null != _attackAnimation)
            {
                float timeScale = _attackAnimation.Duration / _animationDuration;
                if (timeScale < 1.0f)
                {
                    AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, false);
                }
                else
                {
                    AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, false, _animationDuration);
                }
            }

            AnimationController.UpdateBodyDirectionByMoveDirection(_attackArea.Direction);
        }

        public override void Update(Stage stage, float deltaTime, float now)
        {
            if (_hitAt <= now)
            {
                DoHit(stage);
                _hitAt = float.MaxValue;
            }
        }

        private void DoHit(Stage stage)
        {
            CombatSystem.HitOnTargetArea(stage, _attackArea, _owner, _owner.Stats.AttackPower.Value, CombatSystem.KnockBackType.Pivot, _attackArea.Center, knockBackPower: 0, hittedCharacterCollector: null, exceptedCharacters: null, hitSoundPrefabPath: string.Empty);
        }

        public override ActionBase End(Stage stage)
        {
            return null;
        }

    }
}

