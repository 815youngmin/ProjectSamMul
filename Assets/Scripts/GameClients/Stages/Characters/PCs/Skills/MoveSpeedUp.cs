using Shared.StaticDatas;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Stats;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class MoveSpeedUp : SkillBase
    {
        private readonly StatModifier _multiplyMoveSpeed;

        public MoveSpeedUp(SkillStaticData staticData) : base(staticData)
        {
            _multiplyMoveSpeed = new StatModifier(staticData.Parameter1, StatModType.PercentAdd);
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            owner.Stats.MoveSpeed.AddModifier(_multiplyMoveSpeed);
            owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            owner.Stats.MoveSpeed.RemoveModifier(_multiplyMoveSpeed);
            owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            // DO NOTHING
        }
    }

}

