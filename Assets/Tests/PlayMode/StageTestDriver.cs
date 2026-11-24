#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SamMul.GameClients;
using SamMul.GameClients.Stages;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies;
using SamMul.GameClients.Stages.StageEvents;
using SamMul.Scenes;
using SamMul.UIs.Stages.Popups;
using SamMul.UnityHelpers;
using Shared.StaticDatas;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SamMul.Tests.PlayMode
{
    /// <summary>
    /// 플레이 테스트가 게임을 조작하는 코드를 모두 여기에 모은다.
    /// 게임 코드에는 테스트용 분기를 넣지 않고, 공개되지 않은 값은 이 파일에서만 리플렉션으로 읽는다.
    /// 게임 쪽 이름이 바뀌면 이 파일의 이름 상수만 고치면 된다.
    /// </summary>
    public static class StageTestDriver
    {
        public const float TIMEOUT_SEC = 30f;
        // 스테이지 테스트의 진행 배속. 대기 제한 시간(TIMEOUT_SEC)은 실제 시간 기준이라 배속의 영향을 받지 않는다.
        public const float STAGE_TIME_SCALE = 4f;
        // 스테이지 테스트에서 플레이어의 기본 최대 체력. 테스트 도중 죽지 않도록 무적에 가깝게 둔다.
        public const float PLAYER_MAX_HP = 99999f;

        // 리플렉션으로 접근하는 비공개 멤버 이름.
        private const string STAGE_EVENT_CONTROLLER_FIELD = "_stageEventController"; // Stage
        private const string AI_STRATEGY_FIELD = "aiStrategy";            // MonsterAIController
        private const string BOSS_ACTIONS_PROPERTY = "Actions";           // BossCombatAIStrategy
        private const string SKILL_SELECTOR_BUTTON_FIELD = "_button";     // SkillSelectorButton

        private const BindingFlags NON_PUBLIC_INSTANCE = BindingFlags.Instance | BindingFlags.NonPublic;

        // 앱의 첫 씬만 SceneManager 로 직접 띄우고, 그 뒤로는 게임과 같이 SceneChanger 로 전환해야 초기화된다.
        private static bool s_isFirstSceneLoaded;

        public static Stage Stage => GameClient.Stage ?? throw new InvalidOperationException("스테이지가 없습니다.");

        // ---- 대기 ----

        public static IEnumerator WaitUntil(Func<bool> condition, string what, float timeoutSec = TIMEOUT_SEC)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSec;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail($"{timeoutSec}초 안에 끝나지 않았습니다: {what}");
                }
                yield return null;
            }
        }

        /// <summary>스테이지 안에서 기다린다. 기다리는 동안 배속을 유지하고 레벨업 팝업을 자동으로 넘긴다.</summary>
        public static IEnumerator WaitInStage(Func<bool> condition, string what, float timeoutSec = TIMEOUT_SEC)
        {
            return WaitUntil(() =>
            {
                KeepStageTimeScale();
                SelectFirstLevelUpSkillIfShown();
                return condition();
            }, what, timeoutSec);
        }

        // ---- 씬 이동 ----

        private static bool IsCurrentScene<TScene>() where TScene : BaseScene
        {
            var scene = UnityGlobal.Scenes.GetCurrentScene<TScene>();
            return scene != null && scene.IsSceneInitialized && UnityGlobal.Scenes.CanChangeScene;
        }

        public static IEnumerator EnterLobby()
        {
            if (!s_isFirstSceneLoaded)
            {
                s_isFirstSceneLoaded = true;
                SceneManager.LoadScene(SceneType.Lobby.ToString());
                yield return null;
            }
            else if (!IsCurrentScene<LobbyScene>())
            {
                yield return WaitUntil(() => UnityGlobal.Scenes.CanChangeScene, "이전 씬 전환");
                UnityGlobal.Scenes.ChangeTo(SceneType.Lobby, new LobbySceneInitialData(), "플레이 테스트: 로비로");
            }

            yield return WaitUntil(IsCurrentScene<LobbyScene>, "로비 씬 초기화");
        }

        /// <summary>로비의 시작 버튼과 같은 데이터로 스테이지에 들어간 뒤, 테스트 배속과 플레이어 체력을 맞춘다.</summary>
        public static IEnumerator EnterStage(int chapterNumber)
        {
            yield return EnterLobby();

            var chapter = StaticDataRepository.Instance.Chapters.FindChapter(chapterNumber);
            Assert.IsNotNull(chapter, $"챕터 {chapterNumber} 가 없습니다.");
            var user = GameClient.CS.UserGameData;
            var stageInitialData = StageSceneInitialData.CreateForMainChapter(chapter!, GameClient.CS.UserSkillDeck, user.CreateSelectedHeroData(), user.CreateSelectedEquipments());
            UnityGlobal.Scenes.ChangeTo(SceneType.Stage, stageInitialData, $"플레이 테스트: 챕터 {chapterNumber}");

            yield return WaitUntil(() => IsCurrentScene<StageScene>() && GameClient.Stage != null, "스테이지 씬 초기화");

            KeepStageTimeScale();
            MakePlayerNearlyInvincible();
        }

        // ---- 스테이지 조작 ----

        /// <summary>
        /// 테스트 배속으로 맞춘다. 게임의 배속 버튼과 같은 경로라서 일시정지 후 재개해도 이 배속으로 돌아온다.
        /// 보스전에 들어가면 게임이 배속을 1배로 되돌리므로 매 프레임 다시 맞춘다. 일시정지(0배) 중에는 건드리지 않는다.
        /// </summary>
        public static void KeepStageTimeScale()
        {
            if (Time.timeScale != 0f && Time.timeScale != STAGE_TIME_SCALE)
            {
                UnityGlobal.Scenes.GetCurrentScene<StageScene>().TimeScaleController.ChangeTimeScale(STAGE_TIME_SCALE);
            }
        }

        /// <summary>
        /// 최대 체력의 기본값을 크게 올리고 가득 채운다. 공격력 등 다른 스탯은 그대로다.
        /// 피격 면역은 쓰지 않는다. 면역인 캐릭터는 보스 AI 의 타겟에서 빠지기 때문이다.
        /// </summary>
        public static void MakePlayerNearlyInvincible()
        {
            var pc = Stage.PC;
            pc.Stats.MaxHP.SetBaseValue(PLAYER_MAX_HP);
            pc.RecoverHP(Stage, pc.MaxHP);
        }

        /// <summary>레벨업 팝업이 떠 있으면(스테이지 시간이 멈춘다) 첫 번째 후보를 고른다.</summary>
        public static void SelectFirstLevelUpSkillIfShown()
        {
            var selectorButton = UnityEngine.Object.FindObjectOfType<SkillSelectorButton>();
            if (selectorButton != null)
            {
                GetField<ZButton>(selectorButton, SKILL_SELECTOR_BUTTON_FIELD).onClick.Invoke();
            }
        }

        /// <summary>
        /// 스테이지 시각을 옮기고 그 시점부터 이벤트를 다시 실행시킨다. 그 앞의 이벤트는 건너뛴다.
        /// 스테이지 타이머가 멈춰 있으면(보스전 진행 중 등) 옮기지 못하고 false 를 돌려준다.
        /// </summary>
        public static bool TryJumpToStageTime(float stageTimeSec)
        {
            return GetField<StageEventController>(Stage, STAGE_EVENT_CONTROLLER_FIELD).TryJumpToRunningTime(stageTimeSec);
        }

        public static IEnumerable<Monster> ActiveMonsters => UnityEngine.Object.FindObjectsOfType<Monster>();

        public static Monster? FindAliveBoss()
        {
            return ActiveMonsters.FirstOrDefault(monster => monster.IsBoss && !monster.Action.IsDead);
        }

        /// <summary>
        /// 살아 있는 보스가 있으면 남은 체력만큼의 피해를 줘서 바로 죽인다. 등장 연출 중에는 기다린다.
        /// 보스전 중에는 스테이지 타이머가 멈추므로, 타이머를 계속 돌려야 하는 테스트가 쓴다.
        /// </summary>
        public static void KillBossIfAppeared()
        {
            var boss = FindAliveBoss();
            if (boss != null && !boss.Action.IsAppearing)
            {
                boss.HittedChainType(Stage, boss.CurrentHP, Vector2.right);
            }
        }

        /// <summary>
        /// 보스가 전투 AI 에 들어가 있으면 그 전략이 가진 액션 목록을 돌려준다. 아직 대기 AI 이면 null.
        /// 각 항목은 (Stage, Monster 보스, Character 타겟) 을 받는 델리게이트이고, 제네릭 인자가 액션 타입이다.
        /// </summary>
        public static IReadOnlyList<Delegate>? FindBossActions(Monster boss)
        {
            if (!(GetField<object>(boss.MonsterAIEvent, AI_STRATEGY_FIELD) is BossCombatAIStrategy combatStrategy))
            {
                return null;
            }

            var property = typeof(BossCombatAIStrategy).GetProperty(BOSS_ACTIONS_PROPERTY, NON_PUBLIC_INSTANCE);
            Assert.IsNotNull(property, $"BossCombatAIStrategy.{BOSS_ACTIONS_PROPERTY} 를 찾지 못했습니다. 이름이 바뀌었으면 StageTestDriver 의 상수를 고쳐 주세요.");
            return ((IEnumerable)property!.GetValue(combatStrategy)).Cast<Delegate>().ToList();
        }

        public static Type ActionTypeOf(Delegate bossAction) => bossAction.Method.GetGenericArguments()[0];

        private static T GetField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(fieldName, NON_PUBLIC_INSTANCE);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{fieldName} 를 찾지 못했습니다. 이름이 바뀌었으면 StageTestDriver 의 상수를 고쳐 주세요.");
            return (T)field!.GetValue(target);
        }
    }
}
