using Shared.GameLogics;
using Shared.StaticDatas;
using Shared.UserDatas;
using UnityEngine;
using SamMul.Loggers;

namespace SamMul.GameClients.Stages.Characters.Stats
{
    public readonly struct BaseStats
    {
        public readonly float MaxHP;
        public readonly float AttackPower;
        public readonly float BasicAttackSpeed;    //공격속도
        public readonly float SkillAttackSpeed;    //스킬속도
        public readonly float MoveSpeed;            //이동속도
        public readonly float KnockBackResistance;  // 넉백 저항. 0~1 사이의 값. 피격시 KnockBack하는 힘을 차감할 비율

        public BaseStats(float maxHP, float attackPower, float basicAttackSpeed, float skillAttackSpeed,  float moveSpeed, float knockBackResistance)
        {
            this.MaxHP = maxHP;
            this.AttackPower = attackPower;
            this.BasicAttackSpeed = basicAttackSpeed;
            this.SkillAttackSpeed = skillAttackSpeed;
            this.MoveSpeed = moveSpeed;
            this.KnockBackResistance = knockBackResistance;
        }

        /// <param name="weight">스탯 변경 가중치</param>
        public static BaseStats FromMonsterStaticData(MonsterStaticData staticData, float hpWeight, float attackPowerWeight, float moveSpeedWeight)
        {
            if (hpWeight <= 0f)
            {
                Log.I.Warn($"몬스터[{staticData.MonsterType} : {staticData.AIType}] 스폰의 HPWeight 가 [{hpWeight}]임. 잘못된 데이터임.");
            }
            if (attackPowerWeight <= 0f)
            {
                Log.I.Warn($"몬스터[{staticData.MonsterType} : {staticData.AIType}] 스폰의 AttackPowerWeight 가 [{attackPowerWeight}]임. 잘못된 데이터임.");
            }

            return new BaseStats(
                staticData.MaxHP * hpWeight,
                attackPower: staticData.CollisionAttackPower * attackPowerWeight,
                basicAttackSpeed: staticData.SpecialAttack1AttackSpeed,
                skillAttackSpeed: 0f,
                staticData.MoveSpeed * moveSpeedWeight,
                staticData.KnockBackResistance);
        }

        public static BaseStats FromHeroData(HeroStaticData heroStaticData, HeroData heroData)
        {
            // 장비에의햔 효과는 장비효과 적용시점에 처리된다 (`InitializeEquipmentEffects`)

            var attackPower = AvatarLogic.CalculateHeroBasicAttackPower(heroData);
            var maxHp = AvatarLogic.CalculateHeroBasicMaxHp(heroData);

            return new BaseStats(
                maxHp,
                attackPower,
                basicAttackSpeed: 0f, // NOTE: PC의 기본 공격속도는 0의 값을 가져야 한다. 기본공격 스킬이 바로 공격속도를 올려준다.
                skillAttackSpeed: 1f,
                heroStaticData.MoveSpeed,
                knockBackResistance: 0f // 플레이어는 넉백 없음. 넉백저항도 그냥 0으로 친다.
                );
        }
    }

}
