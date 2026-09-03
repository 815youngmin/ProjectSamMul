
using UnityEngine;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.RangeAIs
{
    public class EliteReflectionRangeAttackAIBlackboard : MonsterAIBlackboardBase
    {
        public readonly int projectileAmount;
        public readonly float projectileSpeed;
        public readonly float projectileLifeTime;
        public readonly float projectileRadius;
        public readonly float projectileRotateSpeed;
        public readonly string projectilePrefabPath;
        public EliteReflectionRangeAttackAIBlackboard(int projectileAmount, float projectileSpeed, float projectileLifeTime, float projectileRadius, float projectileRotateSpeed,  string projectilePrefabPath)
        {
            this.projectileAmount = projectileAmount;
            this.projectileSpeed = projectileSpeed;
            this.projectileLifeTime = projectileLifeTime;
            this.projectileRadius = projectileRadius;
            this.projectileRotateSpeed = projectileRotateSpeed;
            this.projectilePrefabPath = projectilePrefabPath;
        }
    }
}
