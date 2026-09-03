using Shared.GameDataTypes;
using Shared.StaticDatas;
using System;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.Characters.PCs;
using Z.GameClients.Stages.CombatSystems;
using Random = UnityEngine.Random;

namespace Z.GameClients.Stages.StageEvents
{
    public abstract class MonsterSpawnStageEventBase : StageEventBase
    {
        // 몬스터를 스폰할 바운더리 관련 비율.
        protected CharacterType _spawnMonsterType;
        protected float _hpWeight;
        protected float _attackPowerWeight;
        protected int _totalAmount;
        protected IReadOnlyList<DropItemType> _dropItems;
        public IReadOnlyList<DropItemType> DropItems => _dropItems;
        // 이 스테이지 이벤트가 스폰해야할 몬스터중 남은 마리 수
        protected int _remainingSpawnAmountForThisEvent;
        // 이 스테이지 이벤트가 발급해야할 EXP 총량
        private long _totalExpAmount;
        public long TotalExpAmount => _totalExpAmount;
        // 이 스테이지 이벤트가 발급해야할 EXP중 남은 양
        private long _remainingExpToDrop;
        private List<DropItemType> _remainingItemsToDrop;
        // 한마리당 드롭할 EXP 평균
        private long _averageDropExp;

        public MonsterSpawnStageEventBase(StageEventStaticData stageEventStaticData) : base(stageEventStaticData)
        {
        }

        public override void Begin(Stage stage)
        {
            base.Begin(stage);

            //----------- 튜토리얼용 코드 ---------------
            // 0챕터(0스테이지) 첫번째 도전의 90초~100초의 탱커들은 체력도 높이고
            // 스폰량을 크게 늘린다.
            bool isFirstTutorialStageRushTime =
                _stageEventStaticData.EventType != StageEventType.BossSpawn &&
                _stageEventStaticData.StageNumber == 0 &&
                (_stageEventStaticData.BeginAt >= 90f) &&
                (_stageEventStaticData.BeginAt <= 95f) &&
                (GameClient.CS.UserGameData.HighestStageTimeInSeconds <= 0);

            int spawnAmount = isFirstTutorialStageRushTime ? (int)(_stageEventStaticData.Amount * 1.3f) : _stageEventStaticData.Amount;
            float hpWeight = isFirstTutorialStageRushTime ? _stageEventStaticData.MonsterHPWeight * 12f : _stageEventStaticData.MonsterHPWeight;
            float attackPowerWeight = isFirstTutorialStageRushTime ? _stageEventStaticData.MonsterAttackPowerWeight * 2.2f : _stageEventStaticData.MonsterAttackPowerWeight;
            //-----------------------------------------

            _spawnMonsterType = _stageEventStaticData.MonsterType;
            _hpWeight = hpWeight;
            _attackPowerWeight = attackPowerWeight;

            _remainingSpawnAmountForThisEvent = _totalAmount = (int)(spawnAmount * (1f + stage.AdditionalSpawnAmountWeight));
            _remainingExpToDrop = _totalExpAmount = (long)(_stageEventStaticData.TotalExp * (1f + stage.AdditionalExpDropWeight));
            _dropItems = _stageEventStaticData.DropItems;
            _remainingItemsToDrop = new List<DropItemType>(_dropItems);
            _averageDropExp = _totalExpAmount / _totalAmount;
        }

        public override void End(Stage stage)
        {
        }

        public override void Update(Stage stage, int stageEventTickNumber)
        {
        }

        protected abstract void SpawnMonsters(Stage stage, int amount);

        protected virtual Monster SpawnMonster(Stage stage, Vector2 spawnPoint)
        {
            if (_remainingSpawnAmountForThisEvent <= 0)
            {
                Debug.Assert(_remainingSpawnAmountForThisEvent < 0);
                return null;
            }

            var monster = stage.CreateMonster(AllianceType.Monsters, _spawnMonsterType,
                MonsterInstanceInitialData.CreateForStageMonster(
                    spawnPoint,
                    _hpWeight,              //스폰할 몬스터의 HP 가중치. 몬스터 테이블의 기본값에 이만큼을 곱해서 밸런싱한다.
                    _attackPowerWeight,     //스폰할 몬스터의 공격력 가중치. 몬스터 테이블의 기본값에 이만큼을 곱해서 밸런싱한다.
                    this.TakeExpToDrop(),
                    0,
                    this.TakeItemsToDrop()),
                isBoss: false,
                isElite: false);

            _remainingSpawnAmountForThisEvent--;

            return monster;
        }

        // 드롭할 EXP 양에서, 이번에 드롭할 EXP를 꺼내온다.
        // 남은 스폰량에 따라 조정하는 로직이 포함된다.
        protected long TakeExpToDrop()
        {
            long expToDrop = 0;
            if (_remainingSpawnAmountForThisEvent <= 1)
            {
                // 마지막 스폰하는 몬스터에게는 남아있는 모든 경험치를 준다.
                expToDrop = _remainingExpToDrop;
                _remainingExpToDrop = 0;
                return expToDrop;
            }

            float randomProbability = Random.Range(0f, 1.0f);
            if (randomProbability < 0.40f)
            {
                // 40프로 확률로 아무것도 안줌
                return 0;
            }
            else if (randomProbability < 0.42f)
            {
                // 2프로 확률로 많이 줌
                // (많이 드롭하는 경우, 한마리에게 드롭할 경험치 = 안주는 경험치 총량 / 많이 줄 마리수)
                // 위와 같이 드롭할 경험치를 결정해서 일단 준다.
                // 하지만, 스테이지 이벤트에서 발급할 경험치 총량을 벗어나지는 않도록 아래에서 별도로 제어한다.
                expToDrop = (long)(_totalExpAmount * 0.40f / (_totalAmount * 0.02f));
            }
            else
            {
                // 나머지 경우는 평균치 근사하게 줌
                expToDrop = (long)Random.Range(_averageDropExp * 0.8f, _averageDropExp * 1.2f);
            }

            if (expToDrop > _remainingExpToDrop)
            {
                expToDrop = _remainingExpToDrop;
            }

            _remainingExpToDrop -= expToDrop;
            return expToDrop;
        }

        /// <summary>
        /// 드롭할 아이템 리스트에서, 이번에 드롭할 아이템을 꺼내온다.
        /// </summary>
        /// <remarks>반환되는 리스트는 해당 스택에서만 사용해야 한다. 레퍼런스를 다른 수행흐름으로 넘기거나, 클래스의 생명주기 내로 가져가서는 안 된다 (멤버로 참조해서는 안 된다) </remarks>
        protected IReadOnlyList<DropItemType> TakeItemsToDrop()
        {
            var itemsToDrop = new List<DropItemType>();
            if (_remainingItemsToDrop.Count <= 0)
            {
                return itemsToDrop;
            }

            if (_remainingSpawnAmountForThisEvent <= 1)
            {
                // 마지막 몬스터라면 모두 줘야 한다.
                foreach (var item in _remainingItemsToDrop)
                {
                    itemsToDrop.Add(item);
                }

                _remainingItemsToDrop.Clear();
                return itemsToDrop;
            }

            for (int i = 0; i < _remainingItemsToDrop.Count; i++)
            {
                bool isDropItem = Random.Range(0, _remainingSpawnAmountForThisEvent) == 0;
                if (isDropItem)
                {
                    itemsToDrop.Add(_remainingItemsToDrop[i]);
                }
            }

            for (int i = 0; i < itemsToDrop.Count; i++)
            {
                _remainingItemsToDrop.Remove(itemsToDrop[i]);
            }

            return itemsToDrop;
        }
    }
}
