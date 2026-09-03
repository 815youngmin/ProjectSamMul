using Shared.GameDataTypes;
using Shared.GameLogics;
using Shared.StaticDatas;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Z.GameClients.Stages
{
    /// <summary>
    /// 스테이지에서 드롭할 아이템을 관리합니다.
    /// 골드, 보석 등의 드롭아이템 총량을 제어하면서
    /// 지급량을 적당히 랜덤하게 펼치기 위해 사용합니다.
    /// </summary>
    public class StageDropItemSupplier
    {
        private readonly RandomValueWithMeanOne _randomValueWithMeanOne;

        private long _remainingItems;
        private int _remainingMonsters;

        public bool HasItemsToDrop => _remainingItems > 0;
        public long RemainingItmes => _remainingItems;
        /// <summary>
        /// 아이템 공급자를 생성합니다. 
        /// </summary>
        /// <param name="totalItemsToDrop">
        /// 몬스터가 드롭할 총 골드의 양입니다.
        /// </param>
        /// <param name="stageEventStaticDatas">
        /// 스테이지 이벤트들의 리스트입니다. 객체의 종류에 따라 몬스터 수의 증가량을 조절할 수 있습니다. 
        /// </param>
        public static StageDropItemSupplier Create(long totalItemsToDrop, IReadOnlyList<StageEventStaticData> stageEventStaticDatas)
        {
            return new StageDropItemSupplier(totalItemsToDrop, stageEventStaticDatas);
        }

        private StageDropItemSupplier(long totalItemsToDrop, IReadOnlyList<StageEventStaticData> stageEventStaticDatas)
        {
            System.Diagnostics.Debug.Assert(totalItemsToDrop > 0);
            System.Diagnostics.Debug.Assert(stageEventStaticDatas != null);

            _remainingItems = totalItemsToDrop;
            _remainingMonsters = 0;

            foreach (var stageEventStaticData in stageEventStaticDatas)
            {
                if (stageEventStaticData.EventType == StageEventType.StageEnterInit || stageEventStaticData.EventType == StageEventType.BossSpawn)
                {
                    continue;
                }
                _remainingMonsters += stageEventStaticData.Amount;
            }

            _randomValueWithMeanOne = new RandomValueWithMeanOne(1.0f / 3.0f, 5.0f);
        }

        private StageDropItemSupplier()
        {
            _remainingItems = 0;
            _remainingMonsters = 0;
            _randomValueWithMeanOne = new RandomValueWithMeanOne(1.0f / 3.0f, 5.0f);
        }

        /// <summary>
        /// 요청된 갯수만큼을 꺼냅니다.
        /// 드롭 가능한 수량이 <paramref name="requestedAmount"/>보다 작다면, 가능한 남은 수량만큼을 리턴해줍니다.
        /// </summary>
        public long TakeItemsToDrop(long requestedAmount)
        {
            long amount = math.min(requestedAmount, _remainingItems);
            _remainingItems -= amount;

            return amount;
        }
    }

}
