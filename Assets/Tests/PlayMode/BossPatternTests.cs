#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace SamMul.Tests.PlayMode
{
    /// <summary>
    /// 보스 6종(챕터 3개 x 중간·최종 보스)이 가진 전용 액션을 하나씩 직접 실행시켜,
    /// 각 액션이 시작되고 끝난 뒤 대기 상태로 돌아오는지 확인한다.
    /// 실행 중 예외나 에러 로그가 나오면 Unity Test Framework 가 실패 처리한다.
    /// </summary>
    public class BossPatternTests
    {
        private const int MIN_ACTION_COUNT = 2;

        [UnityTest]
        public IEnumerator Boss_RunsEveryAction_AndReturnsToIdle([Values(1, 2, 3)] int chapterNumber, [Values(0, 1)] int bossIndex)
        {
            yield return StageTestDriver.EnterStage(chapterNumber);
            var stage = StageTestDriver.Stage;

            // 에디터의 숫자 키 1~2 와 같은 기능: n번째 보스 경고 직전으로 스테이지 시각을 옮긴다.
            stage.TEST_JumpToBossBattle(bossIndex);
            yield return StageTestDriver.WaitInStage(() => StageTestDriver.FindAliveBoss() != null, "보스 등장");
            var boss = StageTestDriver.FindAliveBoss()!;
            string bossName = boss.StaticData.MonsterType.ToString();

            yield return StageTestDriver.WaitInStage(() => StageTestDriver.FindBossActions(boss) != null, $"{bossName} 전투 AI 시작");
            var actions = StageTestDriver.FindBossActions(boss)!;
            Assert.GreaterOrEqual(actions.Count, MIN_ACTION_COUNT, $"{bossName} 의 액션 수");

            foreach (var action in actions)
            {
                string actionName = StageTestDriver.ActionTypeOf(action).Name;

                // 보스 AI 가 스스로 고른 액션이 진행 중일 수 있으므로 대기 상태가 될 때까지 기다린 뒤 실행한다.
                yield return StageTestDriver.WaitInStage(() => boss.Action.IsDead || boss.Action.IsIdle, $"{bossName}: {actionName} 실행 전 대기 상태");
                Assert.IsFalse(boss.Action.IsDead, $"{bossName} 가 {actionName} 를 실행하기 전에 죽었습니다.");

                action.DynamicInvoke(stage, boss, stage.PC);
                Assert.IsInstanceOf(StageTestDriver.ActionTypeOf(action), boss.Action.CurrentAction, $"{bossName}: {actionName} 가 시작되지 않았습니다.");

                yield return StageTestDriver.WaitInStage(() => boss.Action.IsDead || boss.Action.IsIdle, $"{bossName}: {actionName} 종료");
            }

            Assert.Greater(stage.PC.CurrentHP, 0f, "플레이어가 죽었습니다.");

            yield return StageTestDriver.EnterLobby();
        }
    }
}
