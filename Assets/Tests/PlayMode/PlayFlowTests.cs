#nullable enable
using System.Collections;
using NUnit.Framework;
using SamMul.GameClients;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.Scenes;
using SamMul.UnityHelpers;
using Shared.StaticDatas;
using UnityEngine.TestTools;

namespace SamMul.Tests.PlayMode
{
    /// <summary>
    /// 실제 씬을 띄워 로비 → 스테이지 → 로비 흐름을 돌려 보는 스모크 테스트.
    /// 도중에 예외나 에러 로그가 나오면 Unity Test Framework 가 테스트를 실패 처리한다.
    /// 로비 초기화가 캐릭터·장비 선택을 저장하므로 로컬 세이브 파일의 선택 값은 첫 번째 항목으로 바뀐다.
    /// </summary>
    public class PlayFlowTests
    {
        // 스테이지를 실제로 진행시키는 시간. 시작 직후 스폰(0초, 2초)이 지나도록 잡았다.
        private const float STAGE_PLAY_SEC = 3f;

        [UnityTest]
        public IEnumerator Lobby_LoadsAndInitializes()
        {
            yield return StageTestDriver.EnterLobby();

            Assert.IsNotNull(UnityGlobal.Scenes.GetCurrentScene<LobbyScene>().UI);
            Assert.AreEqual(3, StaticDataRepository.Instance.Chapters.All.Count);
        }

        [UnityTest]
        public IEnumerator Chapter_EntersStage_SpawnsMonsters_AndReturnsToLobby([Values(1, 2, 3)] int chapterNumber)
        {
            yield return StageTestDriver.EnterStage(chapterNumber);
            yield return StageTestDriver.WaitUntil(() => GameClient.Stage!.StageRunningTime >= STAGE_PLAY_SEC, $"스테이지 시간 {STAGE_PLAY_SEC}초 진행");

            Assert.Greater(GameClient.Stage!.PC.CurrentHP, 0f, "플레이어가 시작하자마자 죽었습니다.");
            Assert.Greater(UnityEngine.Object.FindObjectsOfType<Monster>().Length, 0, "몬스터가 스폰되지 않았습니다.");

            // 스테이지 진행 중에 로비로 돌아간다. 씬 전환 중 이전 씬이 계속 업데이트되어 생기는 오류를 잡는다.
            yield return StageTestDriver.EnterLobby();
            yield return null;
        }
    }
}
