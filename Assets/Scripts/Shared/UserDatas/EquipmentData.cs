#nullable enable
using System;
using Newtonsoft.Json;
using Shared.GameDataTypes;

namespace Shared.UserDatas
{
    /// <summary>An equipment owned by the user.</summary>
    public class EquipmentData
    {
        public EquipmentInstanceId InstanceId { get; set; }
        public EquipmentId EquipmentId { get; set; }
        public Grade Grade { get; set; }
        public int Level { get; set; }
        public DateTime CreateAt { get; set; }

        [JsonConstructor]
        public EquipmentData(EquipmentInstanceId instanceId, EquipmentId equipmentId, Grade grade, int level, DateTime createAt)
        {
            InstanceId = instanceId;
            EquipmentId = equipmentId;
            Grade = grade;
            Level = level;
            CreateAt = createAt;
        }
    }
}
