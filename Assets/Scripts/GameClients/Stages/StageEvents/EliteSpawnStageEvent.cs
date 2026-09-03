using Shared.StaticDatas;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.UIs.Stages.HUDs;

namespace SamMul.GameClients.Stages.StageEvents
{
    public class EliteSpawnStageEvent : MonsterSpawnStageEvent
    {
        public EliteSpawnStageEvent(StageEventStaticData stageEventStaticData) : base(stageEventStaticData)
        {

        }

        // MonsterSpawnStageEventBase.SpawnMonster() 함수와 구조가 매우 비슷하니 수정할 때 주의하세요.
        // EliteSpawnStageEvent.SpawnMonster() 함수는 Stage.CreateMonster() 함수를 호출할 때 isElite 값을 true로 전달합니다.
        protected override Monster SpawnMonster(Stage stage, Vector2 spawnPoint)
        {
            if (_remainingSpawnAmountForThisEvent <= 0)
            {
                Debug.Assert(_remainingSpawnAmountForThisEvent < 0);
                return null;
            }

            var monster = stage.CreateMonster(AllianceType.Monsters, _spawnMonsterType,
                MonsterInstanceInitialData.CreateForStageMonster(
                    spawnPoint,
                    _hpWeight,
                    _attackPowerWeight,
                    this.TakeExpToDrop(),
                    0,
                    this.TakeItemsToDrop()),
                isBoss: false, isElite: true); // isBoss, isElite 값에 주의하세요.
            monster.AddEmphasisCircle(EmphasisCircle.Color.Red);
            monster.AddMonsterNameDisplayer(MonsterNameDisplayer.Color.Red);

            _remainingSpawnAmountForThisEvent--;

            return monster;
        }
    }
}
