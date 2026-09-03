#nullable enable
using System.Collections.Generic;
using Shared.GameDataTypes;

namespace Shared.StaticDatas
{
    /// <summary>One playable floor. Table "Stages". The floor is a Width x Height rectangle centred on the origin.</summary>
    public class StageStaticData
    {
        public int StageNumber { get; set; }
        public StageFormType StageFormType { get; set; }
        public SpawnLogicType SpawnLogicType { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float TotalStageTimeSec { get; set; }
        public float ItemBoxSpawnPeriodSec { get; set; }
        public bool IsResurrectable { get; set; }
        public string FloorTileResourcePath { get; set; } = "";
        public string FenceResourcePath { get; set; } = "";
        public string BGMPath { get; set; } = "";
    }

    /// <summary>Timed script entry of a stage. Table "StageEvents".</summary>
    public class StageEventStaticData
    {
        public int StageNumber { get; set; }
        public StageEventType EventType { get; set; }
        public float BeginAt { get; set; }
        public float EndAt { get; set; }
        public CharacterType MonsterType { get; set; }
        public int Amount { get; set; }
        public float TickPeriod { get; set; }
        public float RespawnPhasePeriod { get; set; }
        public float MonsterHPWeight { get; set; }
        public float MonsterAttackPowerWeight { get; set; }
        public int TotalExp { get; set; }
        public List<DropItemType> DropItems { get; set; } = new List<DropItemType>();
        public float CameraSize { get; set; }
        public float Param1 { get; set; }
    }

    public class StageStaticDataRepository
    {
        private readonly Dictionary<int, StageStaticData> _stages = new Dictionary<int, StageStaticData>();
        private readonly Dictionary<int, IReadOnlyList<StageEventStaticData>> _eventsByStage = new Dictionary<int, IReadOnlyList<StageEventStaticData>>();

        public IReadOnlyDictionary<int, StageStaticData> StageStaticDatas => _stages;

        public StageStaticDataRepository(IReadOnlyList<StageStaticData> stages, IReadOnlyList<StageEventStaticData> events)
        {
            foreach (var stage in stages)
            {
                if (_stages.ContainsKey(stage.StageNumber))
                {
                    throw new StaticDataValidationError($"Duplicate stage number {stage.StageNumber}.");
                }
                _stages.Add(stage.StageNumber, stage);
            }

            var grouped = new Dictionary<int, List<StageEventStaticData>>();
            foreach (var stageEvent in events)
            {
                if (!grouped.TryGetValue(stageEvent.StageNumber, out var list))
                {
                    list = new List<StageEventStaticData>();
                    grouped.Add(stageEvent.StageNumber, list);
                }
                list.Add(stageEvent);
            }
            foreach (var pair in grouped)
            {
                _eventsByStage.Add(pair.Key, pair.Value);
            }
        }

        public StageStaticData Get(int stageNumber)
            => Find(stageNumber) ?? throw new StaticDataValidationError($"Stage {stageNumber} is not defined.");

        public StageStaticData? Find(int stageNumber)
            => _stages.TryGetValue(stageNumber, out var stage) ? stage : null;

        /// <summary>Events of a stage in table order; null when the stage has no events.</summary>
        public IReadOnlyList<StageEventStaticData>? FindStageEvents(int stageNumber)
            => _eventsByStage.TryGetValue(stageNumber, out var events) ? events : null;
    }
}
