using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.HealAIs;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.AreaEffectOnDieAIs
{
    public abstract class AreaEffectOnDieBaseAIStrategy : MonsterAIStrategyBase
    {
        public AreaEffectOnDieBaseAIStrategy() : base()
        {
        }

        protected void CreateAreaEffect(Stage stage, Monster owner)
        {
            float lifeTime = owner.StaticData.Param1;
            float areaEffectRadius = owner.StaticData.Param2;
            float delay = 0.5f;
            string areaEffectPath = owner.StaticData.SpecialAttack1ResourcePath;
        
            stage.AreaIndicators.CreateBlinkCircularAttackRangeIndicator(owner.Pos, areaEffectRadius, delay);

            stage.CreateDelayAreaEffectObject(
                owner: owner,
                delay: delay,
                damage: owner.SpecialAttackPower,
                lifeTime: lifeTime,
                attackRadius: areaEffectRadius,
                position: owner.Pos,
                areaEffectPath
                );
        }
    }
}
