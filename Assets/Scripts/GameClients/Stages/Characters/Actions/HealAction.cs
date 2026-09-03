using System.Collections.Generic;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Actions
{
    public class HealAction : ActionBase
    {
        private SpriteMonsterAnimationController AnimationController => (SpriteMonsterAnimationController)base._animationController;

        private readonly Character _owner;
        private readonly Character _target;

        private readonly float _healRange;
        private readonly float _healPercent;
        private readonly float _healDuration;
        private readonly float _healPeriod;

        private float _healEndAt;
        private float _healedClearAt;
        private HashSet<Character> _healedCharacters;

        public HealAction(
            Character owner, 
            Character target,
            float healRange, 
            float healPercent, 
            float healDuration,
            float healPeriod,
            SpriteMonsterAnimationController animationController)
            :base(ActionType.Skill, healDuration, animationController)
        {
            _owner = owner;
            _target = target;
            _healRange = healRange;
            _healPercent = healPercent;
            _healDuration = healDuration;
            _healPeriod = healPeriod;
            _healedCharacters = new HashSet<Character>();
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            _healEndAt = now + _healDuration;
            _healedClearAt = now + _healPeriod;

            AnimationController.PlayHeal(_healDuration);
        }

        public override void Update(Stage stage, float deltaTime, float now)
        {
            if(_healedClearAt <= now)
            {
                _healedClearAt = now + _healPeriod;
                _healedCharacters.Clear();
            }

            if(now < _healEndAt)
            {
                List<Character> characters = new List<Character>();
                CircularTargetArea targetArea = new CircularTargetArea(_owner.Pos, _healRange);
                stage.FindAliveCharactersInArea(_owner.Alliance, targetArea, characters);
                foreach (var character in characters)
                {
                    if (_healedCharacters.Contains(character))
                    {
                        continue;
                    }

                    if(character == _owner)
                    {
                        character.RecoverHP(stage, character.MaxHP * _healPercent * 0.3f);
                    }
                    else
                    {
                        character.RecoverHP(stage, character.MaxHP * _healPercent);
                    }

                    _healedCharacters.Add(character);
                }
            }
        }

        public override ActionBase End(Stage stage)
        {
            _healedCharacters.Clear();
            return null;
        }
    }
}
