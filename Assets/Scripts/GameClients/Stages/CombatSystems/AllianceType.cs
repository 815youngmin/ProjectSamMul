using System;

namespace SamMul.GameClients.Stages.CombatSystems
{
    public enum AllianceType
    {
        Players,
        Monsters,
    }

    public static class AllianceTypeExtension
    {
        public static AllianceType ToEnemyAlliance(this AllianceType allianceType)
        {
            switch (allianceType)
            {
                case AllianceType.Players:
                    {
                        return AllianceType.Monsters;
                    }
                    case AllianceType.Monsters:
                    {
                        return AllianceType.Players;
                    }
                default:
                    {
                        throw new NotImplementedException($"{allianceType} to enemytype");
                    }
            }
        }
    }

}
