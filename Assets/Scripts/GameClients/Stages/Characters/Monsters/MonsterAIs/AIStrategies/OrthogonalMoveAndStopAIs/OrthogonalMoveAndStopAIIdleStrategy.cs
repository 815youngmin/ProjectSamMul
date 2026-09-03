
using Shared.DataTables;
using UnityEngine;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.MeleeAIs
{
    /// <summary>
    /// 직교 형태로 움직이는 몬스터AI
    /// 생성 시점에 플레이어 위치를 기준으로 잡고 왼쪽, 오른쪽, 위, 아래 4방향중 하나를 정해 이동하는 AI
    /// </summary>
    public class OrthogonalMoveAndStopAIIdleStrategy : MonsterAIStrategyBase
    {
        Character _target;
        private float _findTargetCheckAt;
        private readonly static float _findTargetCheckDuration = 1.0f;

        public override void Begin(Stage stage, Monster owner)
        {
            owner.StopMovement();
            _findTargetCheckAt = 0.0f;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            if(owner.Action.IsBeingSummoned)
            {
                return null;
            }

            float now = Time.time;
            if(_findTargetCheckAt < now)
            {
                _findTargetCheckAt = now + _findTargetCheckDuration;
                Character enemy = stage.FindClosestCharacter(owner.Alliance.ToEnemyAlliance(), owner.Pos, limitDistance: GameConstants.AI_TARGET_SEARCH_MAX_DISTANCE,
                    condition: character => !character.Action.IsDead);
                _target = enemy;
            }

            if (IsEnemyInSearchDistance(owner, _target))
            {
                Vector2 moveDir = this.GetMoveDirection(_target, owner);
                return new OrthogonalMoveAndStopAICombatStrategy(_target, moveDir);
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

        private Vector2 GetMoveDirection(Character target, Monster owner)
        {
            Vector2 delta = target.Pos - owner.Pos;

            
            if(Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                //수평이동
                if(delta.x < 0)
                {
                    return Vector2.left;
                }
                else
                {
                    return Vector2.right;
                }
            }
            else
            {
                //수직이동
                if (delta.y < 0)
                {
                    return Vector2.down;
                }
                else
                {
                    return Vector2.up;
                }
            }

        }

    }
}