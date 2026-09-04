#nullable enable
using System;
using System.Collections.Generic;
using Shared.GameDataTypes;

namespace Shared.UserDatas
{
    /// <summary>
    /// Everything the game knows about the player. In the demo it is created locally and persisted as JSON
    /// (every member is a settable property; the parameterless constructor yields a fresh account).
    /// </summary>
    public class UserGameData
    {
        public long Id { get; set; }
        public long Gold { get; set; }
        public long Gem { get; set; }
        public long ResurrectionCoin { get; set; }

        public int ClearedHighestChapter { get; set; }
        public long HighestStageTimeInSeconds { get; set; }
        public int StageResurrectCount { get; set; }

        /// <summary>로비에서 고른 캐릭터와 장비. 데모에는 보유/성장 개념이 없어 종류만 저장한다.</summary>
        public HeroType SelectedHeroType { get; set; } = HeroType.Invalid;
        public EquipmentId SelectedEquipmentId { get; set; } = EquipmentId.Invalid;

        public UserGameData()
        {
        }

        public long GetTotalGemAmount() => Gem;

        /// <summary>스테이지에 들고 갈 캐릭터. 항상 기본 등급 1레벨로 만든다.</summary>
        public HeroData CreateSelectedHeroData()
            => new HeroData(HeroInstanceId.CreateNew(), SelectedHeroType, Grade.D, promotionPoint: 0, level: 1, DateTime.UtcNow);

        /// <summary>스테이지에 들고 갈 장비. 고른 장비 하나를 기본 등급 1레벨로 만든다.</summary>
        public IEnumerable<EquipmentData> CreateSelectedEquipments()
        {
            if (SelectedEquipmentId != EquipmentId.Invalid)
            {
                yield return new EquipmentData(EquipmentInstanceId.CreateNew(), SelectedEquipmentId, Grade.D, level: 1, DateTime.UtcNow);
            }
        }
    }
}
