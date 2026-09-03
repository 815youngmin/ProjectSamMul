namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.PoisonousAreaAI
{
    public abstract class PoisonousAreaBaseAIStrategy : MonsterAIStrategyBase
    {
        protected void CreatePoisonousAreaEffect(Stage stage, Monster owner)
        {
            stage.CreatePoisonousAreaEffect(
                owner: owner,
                position: owner.Pos,
                lifeTime: 1.9f,
                tickPeriod: 1.0f / owner.StaticData.SpecialAttack1AttackSpeed,
                damage: owner.SpecialAttackPower,
                radius: owner.StaticData.SpecialAttack1EffectiveRange);
        }
    }
}
