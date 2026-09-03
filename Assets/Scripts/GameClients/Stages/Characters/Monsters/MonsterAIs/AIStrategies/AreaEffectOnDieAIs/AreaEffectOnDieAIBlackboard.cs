
using UnityEngine;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.AreaEffectOnDieAIs
{
    public class AreaEffectOnDieAIBlackboard : MonsterAIBlackboardBase
    {
        public readonly float areaEffectCreateDelay;
        public readonly float areaEffectLifeTime;
        public readonly float areaEffectRadius;
        public readonly Vector3 areaEffectPrefabScale;
        public readonly string areaEffectPrefabPath;
        public AreaEffectOnDieAIBlackboard(float areaEffectCreateDelay, float areaEffectLifeTime, float areaEffectRadius, Vector3 areaEffectPrefabScale, string areaEffectPrefabPath)
        {
            this.areaEffectCreateDelay = areaEffectCreateDelay;
            this.areaEffectLifeTime = areaEffectLifeTime;
            this.areaEffectRadius = areaEffectRadius;
            this.areaEffectPrefabScale = areaEffectPrefabScale;
            this.areaEffectPrefabPath = areaEffectPrefabPath;
        }
    }
}
