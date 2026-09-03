namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies
{
    public abstract class MonsterAIBlackboardBase
    {
        public virtual void OnEnterredIntoStage(Stage stage, Monster owner) { }
    }

    public abstract class MonsterAIStrategyBase
    {
        protected readonly MonsterAIBlackboardBase? _blackboard;
        public MonsterAIBlackboardBase? Blackboard => _blackboard;

        protected MonsterAIStrategyBase()
        {
            _blackboard = null;
        }
        protected MonsterAIStrategyBase(MonsterAIBlackboardBase blackboard)
        {
            _blackboard = blackboard;
        }

        public abstract void Begin(Stage stage, Monster owner);
        /// <summary>
        /// 현재 전략의 상태에 따라 전략을 수행합니다.
        /// AI 전략을 바꾸고 싶을 경우, 바꿀 전략을 리턴해주면 됩니다.
        /// </summary>
        public abstract MonsterAIStrategyBase Update(Stage stage, Monster owner);
        public abstract void End(Stage stage, Monster owner);

        /// <summary>
        /// AI 전략을 바꾸고 싶을 경우, 바꿀 전략을 리턴해주면 됩니다.
        /// </summary>
        public abstract MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker);

        /// <summary>
        /// AI 전략을 바꾸고 싶을 경우, 바꿀 전략을 리턴해주면 됩니다.
        /// </summary>
        public virtual MonsterAIStrategyBase OnDead(Stage stage, Monster owner) => null;

        /// <summary>
        /// AI 전략을 바꾸고 싶을 경우, 바꿀 전략을 리턴해주면 됩니다.
        /// </summary>
        public virtual MonsterAIStrategyBase OnDisappearing(Stage stage, Monster owner) => null;

        /// <summary>
        /// AI 전략을 바꾸고 싶을 경우, 바꿀 전략을 리턴해주면 됩니다.
        /// </summary>
        public virtual MonsterAIStrategyBase OnRePosition(Stage stage, Monster owner) => null;

        public static bool IsEnemyInSearchDistance(Monster owner, Character enemy)
        {
            if (enemy == null)
            {
                return false;
            }

            if (enemy.IsImmuneToHit)
            {
                return false;
            }

            return true;
        }

    }

}
