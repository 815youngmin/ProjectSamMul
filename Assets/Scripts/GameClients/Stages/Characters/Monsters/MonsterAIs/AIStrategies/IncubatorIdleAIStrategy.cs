using Shared.StaticDatas;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Actions;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies
{
    public class IncubatorIdleAIStrategy : MonsterAIStrategyBase
    {
        private readonly float _waitingDuration = 10.0f;    // 부화 대기시간 
        private float _disappearAt;     // 사라질 시각
        private float _spawnAt;         // 사라진 후 몬스터를 스폰할 시각

        public override void Begin(Stage stage, Monster owner)
        {
            owner.StopMovement();
            _disappearAt = Time.time + _waitingDuration;
            _spawnAt = _disappearAt + owner.AnimationController.DisappearAnimationDuration;
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            float now = Time.time;
            var pc = stage.PC;

            if (_disappearAt <= now && pc && !pc.Action.IsDead)
            {
                _disappearAt = float.MaxValue;
                owner.AnimationController.PlayDisappear();
            }

            if (_spawnAt <= now)
            {
                _spawnAt = float.MaxValue;
                this.SpawnMonster(stage, owner);
                owner.Action.ChangeTo(stage, new DeadAction(Vector2.zero, owner, owner.AnimationController));
            }

            return null;
        }

        private void SpawnMonster(Stage stage, Monster owner)
        {
            var ownerBaseStats = StaticDataRepository.Instance.Monsters.Get(owner.CharacterType);
            float ownerHPWeight = owner.MaxHP / ownerBaseStats.MaxHP;
            float ownerAttackPowerWeight = owner.CollisionAttackPower / ownerBaseStats.CollisionAttackPower;

            stage.CreateMonster(owner.Alliance, owner.StaticData.SpawnMonsterType,
                MonsterInstanceInitialData.CreateForStageMonster(owner.Pos,
                hpWeight: ownerHPWeight,
                attackPowerWeight: ownerAttackPowerWeight,
                dropExp: owner.DropExp,
                dropGolds: owner.DropGolds,
                dropItems: owner.DropItems),
                isBoss: false,
                isElite: false);
        }

        public override void End(Stage stage, Monster owner)
        {

        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            return null;
        }
    }
}
