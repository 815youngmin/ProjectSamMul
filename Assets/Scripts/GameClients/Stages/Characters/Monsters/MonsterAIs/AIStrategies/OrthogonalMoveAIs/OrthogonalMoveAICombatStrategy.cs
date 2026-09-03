using Shared.GameDataTypes;
using UnityEngine;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.MeleeAIs
{
    public class OrthogonalMoveAICombatStrategy : MonsterAIStrategyBase
    {
        //이동 방향
        private Vector2 _moveDir;
        private Character target;

        public OrthogonalMoveAICombatStrategy(Character target, Vector2 moveDir)
        {
            this.target = target;
            _moveDir = moveDir;
        }

        public override void Begin(Stage stage, Monster owner)
        {
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        { 
            if (this.target.IsImmuneToHit ||
                this.target.Action.IsDead)
            {
                owner.StopMovement();
                return null;
            }

            owner.Move(_moveDir);
            return null;
        }

        public override void End(Stage stage, Monster owner)
        {

        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            return null;
        }

        //재배치 되었으면 사라지도록 처리한다.
        public override MonsterAIStrategyBase OnRePosition(Stage stage, Monster owner)
        {
            owner.DisappearFromStage(stage, withDeadEffect: false);
            return null;
        }

    }
}