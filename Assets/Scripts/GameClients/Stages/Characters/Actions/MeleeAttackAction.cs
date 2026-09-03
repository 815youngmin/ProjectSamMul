using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;

namespace Z.GameClients.Stages.Characters.Actions
{
    public sealed class MeleeAttackAction : ActionBase
    {
        private readonly Character _target;
        private readonly Monster _owner;

        // 애니메이션의 시작부터 히트프레임이 재생되기 까지의 시간
        private readonly float _hitTimeOnAnimation;
        // 이 액션에서 히트를 처리할 시각
        private float _hitAt;
        // 이 액션에서 히트를 처리했는지 여부
        private bool _isHitFired;

        public MeleeAttackAction(
            Character target,
            Monster owner,
            MonsterAnimationController animationController) :
            base(ActionType.Attack, animationController.AttackAnimationDuration, animationController)
        {
            Debug.Assert(target != null);
            this._target = target;
            this._owner = owner;

            this._hitTimeOnAnimation = animationController.FindHitTimeOnAttackAnimation();
            this._isHitFired = false;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            this._animationController.SkipHitAnimation(true);

            this._hitAt = now + this._hitTimeOnAnimation;

            if (this._owner.MoveDir == Vector2.zero)
            {
                // 이동하지 않고 있을 경우, 공격 방향을 바라보도록 한다.
                var attackDirection = this._target.CenterPos - this._owner.CenterPos;
                this._animationController.UpdateBodyDirectionByMoveDirection(attackDirection);
            }
            else
            {
                this._animationController.UpdateBodyDirectionByMoveDirection(this._owner.MoveDir);
            }
            this._animationController.PlayAttackForce(this.Duration);
        }

        public override void Update(Stage stage, float deltaTime, float now)
        {
            if (this._isHitFired)
            {
                return;
            }

            if (this._hitAt <= now)
            {
                this._isHitFired = true;
                this.DoHit(stage);
            }
        }

        private void DoHit(Stage stage)
        {
            if (this._target.IsImmuneToHit)
            {
                return;
            }

            float damage = _owner.Stats.AttackPower.Value;
            Vector2 hitVector = (this._target.CenterPos - _owner.CenterPos).normalized;

            _target.Hitted(stage, attacker: _owner, damage, hitVector, this._target.UIPos, hitSoundPrefabPath: string.Empty);
        }

        public override ActionBase End(Stage stage)
        {
            this._animationController.SkipHitAnimation(false);
            return null;
        }

        public override bool Cancel(Stage stage)
        {
            if (!base.Cancel(stage))
            {
                return false;
            }

            this._animationController.SkipHitAnimation(false);
            this._animationController.StopAttack();

            return true;
        }
    }



}
