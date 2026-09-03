using Shared.GameDataTypes;
using Shared.StaticDatas;
using System.Collections.Generic;
using UnityEngine;

namespace Z.GameClients.Stages.Characters.Monsters
{
    public class MonsterInstanceInitialData
    {
        public readonly Vector2 spawnPoint;
        public readonly float hpWeight;
        public readonly float attackPowerWeight;
        public readonly float attackRange;
        public readonly long dropExp;
        // 스테이지에서 드롭할 골드수량임. 스테이지에서 dropGoldSupplier를 통해 꺼내온 값을 넣어야 한다.
        // 임의의 값을 넣으면 안 된다.
        public readonly long dropGolds;
        public readonly IReadOnlyList<DropItemType> dropItems;
        public readonly int projectileCount;
        public readonly float projectileSpeed;
        public readonly bool isHitOnCollision;


        /// <param name="dropGolds">
        /// 스테이지에서 드롭할 골드수량임. 스테이지에서 dropGoldSupplier를 통해 꺼내온 값을 넣어야 한다.
        /// 임의의 값을 넣으면 안 된다.
        /// </param>
        private MonsterInstanceInitialData(
            Vector2 spawnPoint,
            float hpWeight,
            float attackPowerWeight,
            long dropExp,
            long dropGolds,
            IReadOnlyList<DropItemType> dropItems)
        {
            this.spawnPoint = spawnPoint;
            this.hpWeight = hpWeight;
            this.attackPowerWeight = attackPowerWeight;
            this.dropExp = dropExp;
            this.dropGolds = dropGolds;
            this.dropItems = dropItems;
            this.attackRange = 0.0f;
            this.projectileCount = 0;
            this.projectileSpeed = 0.0f;
            this.isHitOnCollision = true;
        }

        private MonsterInstanceInitialData(
            Vector2 spawnPoint,
            float hpWeight,
            float attackPowerWeight,
            float attackRange,
            int projectileCount,
            float projectileSpeed,
            bool isHitOnCollision
            )
        {
            this.spawnPoint = spawnPoint;
            this.hpWeight = hpWeight;
            this.attackPowerWeight = attackPowerWeight;
            this.attackRange = attackRange;
            this.projectileCount = projectileCount;
            this.projectileSpeed = projectileSpeed;
            this.dropExp = 0;
            this.dropItems = new List<DropItemType>();
            this.isHitOnCollision = isHitOnCollision;
        }

        /// <param name="dropGolds">
        /// 스테이지에서 드롭할 골드수량임. 스테이지에서 dropGoldSupplier를 통해 꺼내온 값을 넣어야 한다.
        /// 임의의 값을 넣으면 안 된다.
        /// </param>
        public static MonsterInstanceInitialData CreateForStageMonster
            (Vector2 spawnPoint, float hpWeight, float attackPowerWeight, long dropExp, long dropGolds, IReadOnlyList<DropItemType> dropItems)
        {
            return new MonsterInstanceInitialData(spawnPoint, hpWeight, attackPowerWeight, dropExp, dropGolds, dropItems);
        }

        /// <summary>
        /// 소환 몬스터의 초기 데이터 생성 인터페이스 입니다.
        /// 소환 몬스터의 기본 스탯은 소환자의 스탯을 따라갑니다. (hpWeight, attackPowerWeight)
        /// 소환 몬스터는 경험치, 아이템을 드랍하지 않습니다
        /// </summary>
        public static MonsterInstanceInitialData CreateForSummonMonster 
            (Vector2 spawnPoint, Monster summoner)
        {
            var summonerBaseStats = StaticDataRepository.Instance.Monsters.Get(summoner.CharacterType);
            float summonerHPWeight = summoner.MaxHP / summonerBaseStats.MaxHP;
            float summonerAttackPowerWeight = summoner.CollisionAttackPower / summonerBaseStats.CollisionAttackPower;

            return new MonsterInstanceInitialData(spawnPoint, summonerHPWeight, summonerAttackPowerWeight, 0L, 0L, new List<DropItemType>());
        }

        /// <summary>
        /// 소환사 몬스터가 없는 소환 몬스터의 초기 데이터 생성 인터페이스입니다. 
        /// 소환 몬스터는 경험치, 아이템을 드랍하지 않습니다
        /// </summary>
        public static MonsterInstanceInitialData CreateForSummonedMonsterWithoutSummoner
            (Vector2 spawnPoint, float hpWeight, float attackPowerWeight)
        {
            return new MonsterInstanceInitialData(spawnPoint, hpWeight, attackPowerWeight, 0L, 0L, new List<DropItemType>());
        }
    }
}
