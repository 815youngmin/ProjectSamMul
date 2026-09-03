using Shared.StaticDatas;
using UnityEngine;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    public class BossDamageUpSkill : SkillBase
    {
        private float _additionalDamageRatio;
        public BossDamageUpSkill(SkillStaticData staticData) : base(staticData)
        {
            _additionalDamageRatio = staticData.Parameter1;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {

        }
        public override void AttackedEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            if(!enemy.IsBoss)
            {
                return;
            }
            enemy.Hitted(stage, null, _additionalDamageRatio * damage, Vector2.zero, enemy.Pos, null);
        }
    }
}

