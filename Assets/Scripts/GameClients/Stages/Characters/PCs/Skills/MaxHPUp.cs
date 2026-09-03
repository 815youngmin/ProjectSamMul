using Shared.StaticDatas;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters.ConditionalEffects;
using Z.GameClients.Stages.Characters.Stats;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    public class MaxHPUp : SkillBase
    {
        private StatModifier _multiplyMaxHp;
        public MaxHPUp(SkillStaticData staticData) : base(staticData)
        {
            _multiplyMaxHp = new StatModifier(staticData.Parameter1, StatModType.PercentAdd);
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);

            float prevHP = owner.Stats.MaxHP.Value;
            owner.Stats.MaxHP.AddModifier(_multiplyMaxHp);
            owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
            float currentHP = owner.Stats.MaxHP.Value;

            //증가한만큼 HP를 회복시켜준다.
            owner.RecoverHP(stage, currentHP - prevHP);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            owner.Stats.MaxHP.RemoveModifier(_multiplyMaxHp);
            owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            // DO NOTHING
        }
    }

}

