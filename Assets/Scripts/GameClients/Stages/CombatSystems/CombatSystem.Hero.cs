using UnityEngine;
using Z.GameClients.Stages.Characters.Stats;

namespace Z.GameClients.Stages.CombatSystems
{
    public static partial class CombatSystem
    {
        public static float CalculateCharacterBasicAttackDamage(PlayerCharacterStatCalculators stats, float characterAttackPowerRate)
        {
            return stats.AttackPower.Value * (1.0f + characterAttackPowerRate);
        }

        public static float CalculateCharacterAttackKnockBackPower(PlayerCharacterStatCalculators stats, float characterAttackKnockBackPowerRate)
        {
            return stats.CharacterAttackKnockBackPower.Value * characterAttackKnockBackPowerRate;
        }


        //캐릭터가 최종적으로 입는 데미지를 계산해줍니다. (데미지 감소율, 받는 피해량 증가 처리)
        public static (bool isCritical,float finalDamage) CalculateCharacterHittedDamage(CharacterStatCalculators stats, float damage, float attackerCriticalChance, float attackerCriticalCoefficient)
        {
            float resultDamageReduction = (stats.DamageReduction.Value - stats.ReceivedDamageIncrease.Value);
            float finalDamage = damage * (1.0f - resultDamageReduction);

            bool isCritical = false;
            //크리티컬 확률 처리 1은 포함되지 않게 한다
            if (Random.Range(minInclusive: 0f, maxInclusive: 0.9999999f) < attackerCriticalChance)
            {
                finalDamage *= attackerCriticalCoefficient;
                isCritical = true;
            }

            if (finalDamage < 1)
            {
                finalDamage = 1;
            }
            return (isCritical, finalDamage);
        }
    }
}
