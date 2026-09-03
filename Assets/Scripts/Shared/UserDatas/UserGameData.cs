#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Shared.GameDataTypes;
using Shared.GameLogics;

namespace Shared.UserDatas
{
    /// <summary>
    /// Everything the game knows about the player. In the demo it is created locally and persisted as JSON
    /// (every member is a settable property; the parameterless constructor yields a fresh account).
    /// </summary>
    public class UserGameData
    {
        public long Id { get; set; }
        public string Nickname { get; set; } = "";
        public int AccountLevel { get; set; } = 1;
        public long AccountExp { get; set; }
        public long Gold { get; set; }
        public long Gem { get; set; }
        public long SpecialDNA { get; set; }
        public long StarCandy { get; set; }
        public long ResurrectionCoin { get; set; }

        public int ClearedHighestChapter { get; set; }
        public long HighestStageTimeInSeconds { get; set; }
        public int StageResurrectCount { get; set; }

        public HeroInstanceId SelectedHero { get; set; } = HeroInstanceId.Invalid;
        public Dictionary<HeroInstanceId, HeroData> Heroes { get; set; } = new Dictionary<HeroInstanceId, HeroData>();
        /// <summary>Equipment worn by the selected hero, by slot.</summary>
        public Dictionary<EquipmentSlot, EquipmentInstanceId> EquipmentSlots { get; set; } = new Dictionary<EquipmentSlot, EquipmentInstanceId>();
        public Dictionary<EquipmentInstanceId, EquipmentData> Equipments { get; set; } = new Dictionary<EquipmentInstanceId, EquipmentData>();

        public int HighestBasicEvolutionID { get; set; }
        public int HighestSpecialEvolutionID { get; set; }

        public UserGameData()
        {
        }

        public long GetTotalGemAmount() => Gem;

        public HeroData GetSelectedHeroData() => Heroes[SelectedHero];

        public ReadOnlyHeroInventory HeroInventory() => new ReadOnlyHeroInventory(Heroes, SelectedHero);

        public GameLogics.EquipmentInventory EquipmentInventory() => new GameLogics.EquipmentInventory(Equipments, EquipmentSlots);

        public UserDatas.EvolutionData EvolutionData() => new UserDatas.EvolutionData(HighestBasicEvolutionID, HighestSpecialEvolutionID);

        public (IHeroInventory, IEquipmentInventory) CreateUserInventory()
            => (new HeroInventoryView(this), new EquipmentInventoryView(this));

        private sealed class HeroInventoryView : IHeroInventory
        {
            private readonly UserGameData _data;
            public HeroInventoryView(UserGameData data) => _data = data;
            public HeroData MainHero => _data.GetSelectedHeroData();
            public IEnumerable<HeroData> SubHeroes => _data.Heroes.Values.Where(hero => hero.InstanceId != _data.SelectedHero);
            public IEnumerator<HeroData> GetEnumerator() => _data.Heroes.Values.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class EquipmentInventoryView : IEquipmentInventory
        {
            private readonly UserGameData _data;
            public EquipmentInventoryView(UserGameData data) => _data = data;
            public IEnumerator<EquipmentData> GetEnumerator() => _data.Equipments.Values.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
