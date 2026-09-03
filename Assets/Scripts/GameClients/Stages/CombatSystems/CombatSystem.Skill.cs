using Z.GameClients.Stages.Characters.Stats;

namespace Z.GameClients.Stages.CombatSystems
{
    public static partial class CombatSystem
    {
        public static float CalculateSkillAttackDamage(CharacterStatCalculators stats, float skillAttackPowerRate)
        {
            return stats.AttackPower.Value * (1.0f + skillAttackPowerRate);
        }

        // 점화 효과의 1회당 대미지
        public static float CalculateBurnAttackDamage(CharacterStatCalculators stats, float burnDamageRate)
        {
            return stats.AttackPower.Value * burnDamageRate;
        }
        public static float CalculateSkillAttackKnockBackPower(PlayerCharacterStatCalculators stats, float skillAttackKnockBackPowerRate)
        {
            return stats.SkillAttackKnockBackPower.Value * skillAttackKnockBackPowerRate;
        }
    }
}