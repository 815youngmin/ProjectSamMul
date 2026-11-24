#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using UnityEngine;
using UnityEngine.TestTools;

namespace SamMul.Tests.PlayMode
{
    /// <summary>
    /// 챕터의 스폰 이벤트에 나오는 몬스터 종류마다, 처음 등장하는 시각으로 스테이지 타이머를 옮겨
    /// 실제로 화면에 나오는지 확인하고 다음 종류로 넘어간다.
    /// 보스전 중에는 타이머가 멈춰 옮길 수 없으므로, 보스가 등장하면 바로 죽인다. 보스 검증은 <see cref="BossPatternTests"/> 가 담당한다.
    /// 실행 중 예외나 에러 로그가 나오면 Unity Test Framework 가 실패 처리한다.
    /// </summary>
    public class MonsterSpawnTests
    {
        // 등장을 확인한 뒤 AI 와 공격이 돌도록 더 지켜보는 게임 시간.
        private const float OBSERVE_SEC = 2f;

        private static readonly StageEventType[] MONSTER_SPAWN_EVENT_TYPES =
        {
            StageEventType.StageEnterInit,
            StageEventType.MonsterSpawn,
            StageEventType.EliteSpawn,
            StageEventType.MonsterMassSpawn,
            StageEventType.AutoRespawnMonster,
        };

        [UnityTest]
        public IEnumerator Chapter_SpawnsEveryMonsterType_AtItsFirstSpawnTime([Values(1, 2, 3)] int chapterNumber)
        {
            yield return StageTestDriver.EnterStage(chapterNumber);

            // 몬스터 종류별 첫 등장 시각. 이른 순서로 확인한다.
            var firstSpawns = StaticDataRepository.Instance.Stages.FindStageEvents(StageTestDriver.Stage.StaticData.StageNumber)!
                .Where(stageEvent => MONSTER_SPAWN_EVENT_TYPES.Contains(stageEvent.EventType))
                .GroupBy(stageEvent => stageEvent.MonsterType)
                .Select(group => (MonsterType: group.Key, SpawnAt: group.Min(stageEvent => stageEvent.BeginAt)))
                .OrderBy(spawn => spawn.SpawnAt)
                .ToList();
            Assert.IsNotEmpty(firstSpawns, $"챕터 {chapterNumber} 에 몬스터 스폰 이벤트가 없습니다.");

            // 기다리는 동안 한 번이라도 화면에 나온 종류를 모은다. 나오자마자 죽는 경우도 놓치지 않기 위해서다.
            var seenMonsterTypes = new HashSet<CharacterType>();
            bool HasSeen(CharacterType monsterType)
            {
                StageTestDriver.KillBossIfAppeared();
                seenMonsterTypes.UnionWith(StageTestDriver.ActiveMonsters.Select(monster => monster.StaticData.MonsterType));
                return seenMonsterTypes.Contains(monsterType);
            }

            foreach (var spawn in firstSpawns)
            {
                if (spawn.SpawnAt > StageTestDriver.Stage.StageRunningTime)
                {
                    // 보스가 살아 있으면 타이머가 멈춰 있어 옮기지 못한다. 보스를 죽이고 타이머가 다시 돌 때까지 시도한다.
                    yield return StageTestDriver.WaitInStage(() =>
                    {
                        StageTestDriver.KillBossIfAppeared();
                        return StageTestDriver.TryJumpToStageTime(spawn.SpawnAt);
                    }, $"챕터 {chapterNumber}: 스테이지 시각을 {spawn.SpawnAt}초로 이동");
                }

                yield return StageTestDriver.WaitInStage(() => HasSeen(spawn.MonsterType), $"챕터 {chapterNumber}: {spawn.MonsterType} 등장 ({spawn.SpawnAt}초)");

                // 스테이지 타이머는 보스전 중에 잠시 멈출 수 있으므로 게임 시간(Time.time)으로 잰다.
                float observeUntil = Time.time + OBSERVE_SEC;
                yield return StageTestDriver.WaitInStage(() => HasSeen(spawn.MonsterType) && Time.time >= observeUntil, $"{spawn.MonsterType} 관찰");
            }

            Assert.Greater(StageTestDriver.Stage.PC.CurrentHP, 0f, "플레이어가 죽었습니다.");

            yield return StageTestDriver.EnterLobby();
        }
    }
}
