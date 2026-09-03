#nullable enable

namespace Shared.GameDataTypes
{
    public enum StageEventType
    {
        MonsterSpawn = 0,
        EliteSpawn = 1,
        MonsterMassSpawn = 2,
        AutoRespawnMonster = 3,
        BossSpawn = 4,
        RushWarning = 5,
        BossWarning = 6,
        StageEnterInit = 7,
        MonsterSpawnToTop = 12,
        MonsterSpawnToBottom = 13,
        MeteorSpawn = 27,
        IceAreaEffectSpawn = 28,
        SandAreaEffectSpawn = 29,
        LightningSpawn = 30,
        StageEffectMeteorWarning = 31,
        StageEffectIceAreaWarning = 32,
        StageEffectSandAreaWarning = 33,
        StageEffectLightningWarning = 34,
    }
}
