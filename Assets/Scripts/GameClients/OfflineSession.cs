#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Shared.CSProtocols.ReqResData;
using Shared.CSProtocols.ReqResData.Chapters;
using Shared.DataTables;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using Shared.UserDatas;
using UnityEngine;
using Z.Loggers;

namespace Z.GameClients
{
    /// <summary>
    /// 서버 없이 동작하는 세션. 원래 네트워크 클라이언트가 담당하던 유저 데이터 보관과
    /// 챕터 입장/종료/포기/부활 프로토콜을 로컬에서 즉시 처리한다.
    /// 게임 로직의 호출 형태(요청 → onCompleted 콜백)는 그대로 유지한다.
    /// </summary>
    public sealed class OfflineSession
    {
        public delegate void ServerErrorHandler(string errorReason, string url, string appVersion, long accountId, ExceptionDispatchInfo? exception);

        private const string SAVE_FILE_NAME = "offline-save.json";
        private const long LOCAL_ACCOUNT_ID = 1;

        private readonly StaticDataRepository _staticData;

        public UserGameData UserGameData { get; private set; }
        public IReadOnlyList<SkillId> UserSkillDeck => GameConstants.SKILLDECK_DEFAULT_SEASON_SKILLDECK;
        public long CachedAccountId => LOCAL_ACCOUNT_ID;
        public string AccountName => "LocalPlayer";
        public string ApplicationVersion => Application.version;
        public int ServiceFinalChapterNumber { get; }

        private static string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

        public OfflineSession(StaticDataRepository staticData)
        {
            _staticData = staticData;
            ServiceFinalChapterNumber = CountChapters(staticData);
            UserGameData = LoadOrCreate();
        }

        // ---- 저장 ----

        public void Save()
        {
            try
            {
                File.WriteAllText(SavePath, JsonConvert.SerializeObject(UserGameData, Formatting.Indented));
            }
            catch (Exception e)
            {
                Log.I.Error("오프라인 세이브 저장 실패", e);
            }
        }

        public void ResetSave()
        {
            UserGameData = CreateNewUser();
            Save();
        }

        private UserGameData LoadOrCreate()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    var loaded = JsonConvert.DeserializeObject<UserGameData>(File.ReadAllText(SavePath));
                    if (loaded != null && loaded.Heroes.Count > 0)
                    {
                        return loaded;
                    }
                }
            }
            catch (Exception e)
            {
                Log.I.Error("오프라인 세이브 읽기 실패. 새 데이터로 시작합니다.", e);
            }

            var created = CreateNewUser();
            UserGameData = created;
            Save();
            return created;
        }

        private static UserGameData CreateNewUser()
        {
            var hero = new HeroData(HeroInstanceId.CreateNew(), HeroType.Tenti, Grade.D, 0, 1, DateTime.UtcNow);
            var user = new UserGameData
            {
                Id = LOCAL_ACCOUNT_ID,
                Nickname = "Player",
                AccountLevel = 1,
                Gold = 0,
                ResurrectionCoin = 3,
                ClearedHighestChapter = 0,
                SelectedHero = hero.InstanceId,
            };
            user.Heroes.Add(hero.InstanceId, hero);
            return user;
        }

        private static int CountChapters(StaticDataRepository staticData)
        {
            int count = 0;
            while (staticData.Chapters.FindChapter(count + 1) != null)
            {
                ++count;
            }
            return count;
        }

        // ---- 챕터 프로토콜 (즉시 처리) ----

        public void EnterChapter(EnterChapterRequest request, Action<EnterChapterResponse> onCompleted, ServerErrorHandler onError, Func<Task> onNetworkError)
        {
            var chapter = _staticData.Chapters.FindChapter(request.ChapterNumber);
            if (chapter == null)
            {
                onCompleted(EnterChapterResponse.FromError(EnterChapterResultCode.InvalidChapter));
                return;
            }

            UserGameData.StageResurrectCount = 0;
            Save();

            onCompleted(new EnterChapterResponse(EnterChapterResultCode.Success));
        }

        public void FinishChapter(FinishChapterRequest request, Action<FinishChapterResponse> onCompleted, ServerErrorHandler onError, Func<Task> onNetworkError)
        {
            var chapter = _staticData.Chapters.FindChapter(request.ChapterNumber);
            if (chapter == null)
            {
                onCompleted(FinishChapterResponse.CreateWithErrorResult(FinishChapterResultCode.InvalidChapter));
                return;
            }

            long rewardGold = 0;
            if (request.PlayResult == StagePlayResult.Cleared)
            {
                rewardGold = chapter.RewardGold;
                UserGameData.ClearedHighestChapter = Math.Max(UserGameData.ClearedHighestChapter, request.ChapterNumber);
                UserGameData.AccountExp += chapter.RewardAccountExp;
            }

            long incrementGold = request.GainedGolds + rewardGold;
            UserGameData.Gold += incrementGold;
            UserGameData.Gem += request.GainedGems;
            UserGameData.HighestStageTimeInSeconds = Math.Max(UserGameData.HighestStageTimeInSeconds, (long)request.PlayedStageTime);
            UserGameData.StageResurrectCount = 0;
            Save();

            onCompleted(new FinishChapterResponse(FinishChapterResultCode.Success)
            {
                ClearedHighestChapter = UserGameData.ClearedHighestChapter,
                IncrementAccountExp = request.PlayResult == StagePlayResult.Cleared ? chapter.RewardAccountExp : 0,
                ResultGold = UserGameData.Gold,
                RewardGold = rewardGold,
                IncrementGold = incrementGold,
                GainedGem = request.GainedGems,
                HighestStageTimeInSeconds = UserGameData.HighestStageTimeInSeconds,
            });
        }

        public void GiveUpChapter(GiveUpChapterRequest request, Action<GiveUpChapterResponse> onCompleted, ServerErrorHandler onError, Func<Task> onNetworkError)
        {
            UserGameData.StageResurrectCount = 0;
            Save();
            onCompleted(new GiveUpChapterResponse(GiveUpChapterResultCode.Success));
        }

        public void Resurrect(ResurrectRequest request, Action<ResurrectResponse> onCompleted, ServerErrorHandler onError, Func<Task> onNetworkError)
        {
            long gemCost = GameConstants.RESURRECTION_GEM_COST;
            if (UserGameData.ResurrectionCoin > 0)
            {
                UserGameData.ResurrectionCoin -= 1;
            }
            else if (UserGameData.GetTotalGemAmount() >= gemCost)
            {
                UserGameData.Gem -= gemCost;
            }
            else
            {
                onCompleted(ResurrectResponse.FromError(ResurrectResultCode.InsufficientGemOrCoin, request.StageNumber));
                return;
            }

            UserGameData.StageResurrectCount += 1;
            Save();

            onCompleted(new ResurrectResponse(ResurrectResultCode.Success, request.StageNumber)
            {
                StageResurrectCount = UserGameData.StageResurrectCount,
                ResultGems = UserGameData.Gem,
                ResultResurrectionCoins = UserGameData.ResurrectionCoin,
            });
        }
    }
}
