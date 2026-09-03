using Shared.StaticDatas;
using UnityEngine;
using Z.GameClients.Stages.Characters.Stats;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    // 이건 쿨타임이 데이터에 설정되어있다. 쿨타임 동작 테스트용 
    public class TestAttackSpeedCooltimeSkill : SkillBase
    {
        // 공격속도 상승 (초당 발사횟수 이만큼 증가 시킴. 덧셈)
        private StatModifier _attackSpeedIncreaser;

        public TestAttackSpeedCooltimeSkill(SkillStaticData staticData) : base(staticData)
        {
            _attackSpeedIncreaser = new StatModifier(staticData.Parameter1, StatModType.Flat);
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);

            owner.Stats.CharacterAttackSpeed.AddModifier(_attackSpeedIncreaser);

            Debug.Log("TestPassiveSkill Activated");
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);

            owner.Stats.CharacterAttackSpeed.RemoveModifier(_attackSpeedIncreaser);

            Debug.Log("TestPassiveSkill Deactivated");
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            // DO NOTHING
        }
    }
}
