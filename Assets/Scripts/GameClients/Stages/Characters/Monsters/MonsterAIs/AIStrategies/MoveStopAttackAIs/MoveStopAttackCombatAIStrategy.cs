using Shared.DataTables;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.MoveStopAttackAIs
{
    public class MoveStopAttackCombatAIStrategy : MonsterAIStrategyBase
    {
        private Character _target;
        private float _findTargetAt;
        private float _moveAt;
        private float _stopAt;
        private float _attackAt;
        private float _attackHitAt;
        private float _hitTimeOnAttackAnimation;
        private bool _isMoving;
        private Vector2 _attackDirection;

        private static readonly float TARGET_CHECK_PERIOD = 1.0f;
        private static readonly float MOVE_TIME = 2f;
        private static readonly float STOP_TIME = 2f;
        // 정지했을 때, 공격하기까지 대기하는 시간
        private static readonly float ATTACK_WAIT_TIME = STOP_TIME/5f;

        public MoveStopAttackCombatAIStrategy(Character target)
        {
            _target = target;
        }

        public override void Begin(Stage stage, Monster owner)
        {
            _findTargetAt = 0.0f;
            _moveAt = Time.time;
            _stopAt = _moveAt + MOVE_TIME;
            _attackAt = _stopAt + ATTACK_WAIT_TIME;
            _attackHitAt = float.MaxValue;

            var spriteAnimator = owner.AnimationController as SpriteMonsterAnimationController;
            _hitTimeOnAttackAnimation = spriteAnimator?.FindHitTimeOnAttackAnimation() ?? 0f;

            if (_hitTimeOnAttackAnimation >= (STOP_TIME - ATTACK_WAIT_TIME))
            {
                Debug.LogWarning($"[{owner.CharacterType}]의 Attack 애니메이션의 히트프레임이 공격시간[{STOP_TIME - ATTACK_WAIT_TIME}] 보다 큽니다. 히트가 안일어납니다. 히트프레임을 조정해주세요.");
            }
            
            _attackDirection = owner.MoveDir;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            var now = Time.time;

            if (_findTargetAt < now)
            {
                _findTargetAt = now + TARGET_CHECK_PERIOD;
                _target = stage.FindClosestCharacter(
                    owner.Alliance.ToEnemyAlliance(),
                    owner.Pos,
                    limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead);
            }

            if (_target == null || _target.IsImmuneToHit)
            {
                return new MoveStopAttackIdleAIStrategy();
            }

            // 움직이고 있는데 멈출 시간이 된 경우
            if (_isMoving && _stopAt < now)
            {
                _attackDirection = owner.MoveDir.x <= 0? Vector2.left : Vector2.right;

                owner.StopMovement();
                _isMoving = false;
                _moveAt = now + STOP_TIME;
                _attackAt = now + ATTACK_WAIT_TIME;
                _attackHitAt = float.MaxValue;
            }
            // 멈춰 있는데 움직일 시간이 된 경우
            else if (!_isMoving && _moveAt < now)
            {
                _isMoving = true;
                _stopAt = now + MOVE_TIME;
                _attackAt = _stopAt + ATTACK_WAIT_TIME;
                _attackHitAt = float.MaxValue;
            }

            if (_attackAt < now)
            {
                // 바라보는 방향 공격하고, 대기 
                owner.AnimationController.PlayAttackForce(owner.AnimationController.AttackAnimationDuration);

                _attackHitAt = _attackAt + _hitTimeOnAttackAnimation;
                _attackAt = float.MaxValue;
            }

            if (_attackHitAt < now)
            {
                var center = owner.Pos;
                var direction = _attackDirection;
                float radius = owner.StaticData.SpecialAttack1EffectiveRange;

                UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(
                    owner.StaticData.SpecialAttack1ResourcePath,
                     center + direction * (0.5f * radius),
                     direction, 
                     flipX: false, 
                     flipY: direction.x < 0.0f, 
                     null, null, null);

                var targetArea = new CircularSectorTargetArea(center, direction, radius, angle: 120f);
                CombatSystem.HitOnTargetArea(
                    stage,
                    targetArea,
                    attacker: owner,
                    owner.SpecialAttackPower,
                    CombatSystem.KnockBackType.Direction,
                    knockBackPivot: owner.MoveDir,
                    knockBackPower: 0.1f,
                    hittedCharacterCollector: null,
                    exceptedCharacters: null,
                    hitSoundPrefabPath: string.Empty);

                _attackHitAt = float.MaxValue;
            }

            if (_isMoving)
            {
                owner.Move(_target.Pos - owner.Pos);
            }

            return null;
        }

        public override void End(Stage stage, Monster owner)
        {
        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            return null;
        }
    }
}
