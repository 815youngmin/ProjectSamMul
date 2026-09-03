#nullable enable
using System;
using Newtonsoft.Json;
using Shared.GameDataTypes;

namespace Shared.UserDatas
{
    /// <summary>A hero owned by the user.</summary>
    public class HeroData
    {
        public HeroInstanceId InstanceId { get; set; }
        public HeroType HeroType { get; set; }
        public Grade Grade { get; set; }
        public long PromotionPoint { get; set; }
        public int Level { get; set; }
        public DateTime CreatedAt { get; set; }

        [JsonConstructor]
        public HeroData(HeroInstanceId instanceId, HeroType heroType, Grade grade, long promotionPoint, int level, DateTime createdAt)
        {
            InstanceId = instanceId;
            HeroType = heroType;
            Grade = grade;
            PromotionPoint = promotionPoint;
            Level = level;
            CreatedAt = createdAt;
        }
    }
}
