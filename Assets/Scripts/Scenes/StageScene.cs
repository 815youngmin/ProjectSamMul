#nullable enable
using Shared.DataTables;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using Shared.UserDatas;
using SamMul.Animations.Placeholder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using SamMul.GameClients;
using SamMul.GameClients.Stages;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;
using SamMul.UnityHelpers.Sounds;

namespace SamMul.Scenes
{
    public class StageSceneInitialData : ISceneInitialData
    {
        public SceneType SceneType => SceneType.Stage;

        public readonly StageType StageType;
        public readonly ChapterStaticData? Chapter;

        public StageStaticData StageStaticData => this.StageType switch
        {
            StageType.Chapter => Chapter!.StageStaticData,
            _ => throw new NotImplementedException($"{this.StageType} 구현 안 됨.")
        };

        public readonly IReadOnlyList<SkillId> UserSkillDeck;

        public readonly HeroData UserHeroData;
        public readonly IEnumerable<EquipmentData> UserHeroEquippedEquipments;

        private StageSceneInitialData(
            StageType stageType,
            ChapterStaticData? chapter,
            IReadOnlyList<SkillId> userSkillDeck,
            HeroData userHeroData,
            IEnumerable<EquipmentData> userHeroEquippedEquipments)
        {
            StageType = stageType;
            Chapter = chapter;
            Debug.Assert(chapter != null);

            UserSkillDeck = userSkillDeck;

            UserHeroData = userHeroData;
            UserHeroEquippedEquipments = userHeroEquippedEquipments;
        }

        public static StageSceneInitialData CreateForMainChapter(
            ChapterStaticData chapter,
            IReadOnlyList<SkillId> userSkillDeck,
            HeroData heroData,
            IEnumerable<EquipmentData> equippedEquipments)
        {
            return new StageSceneInitialData(
                StageType.Chapter,
                chapter,
                userSkillDeck,
                heroData,
                equippedEquipments);
        }
    }

    public class StageScene : BaseScene
    {
        public override SceneType SceneType => SceneType.Stage;

        public new StageSceneUIRoot UI => (StageSceneUIRoot)base.UI;


        // 개발용으로만 지원하는 값입니다.
        // 라이브에서는 이 값을 쓰면 안 됩니다. 라이브에서는 -1을 사용해주세요.
        [SerializeField] private int TEST_ChapterNumberForTest = -1;
        [SerializeField] private SpriteRenderer _floor;
        // 사각형 맵 전용 Renderer, 4등분으로 약속되어있다.
        [SerializeField] private SpriteRenderer[] _rectangleFloorRender;

        // 챕터 리소스를 미리 다운로드 받아두는 작업을 중단시킨다.
        private CancellationTokenSource? _preDownloadStopper;
        // 챕터에서 사용할 몬스터를 미리 생성하는 작업을 중단시킨다.
        private CancellationTokenSource? _preCreateMonstersStopper;

        protected override void Awake()
        {
            base.Awake();
        }

        private void OnDestroy()
        {
            if (_preDownloadStopper != null)
            {
                _preDownloadStopper.Cancel();
            }

            if (_preCreateMonstersStopper != null)
            {
                _preCreateMonstersStopper.Cancel();
            }
        }

        public override void Initialize(ISceneInitialData initialData)
        {
            base.Initialize(initialData);

            _preDownloadStopper = null;
            _preCreateMonstersStopper = null;

            StageSceneInitialData stageSceneInitialData;
            if (initialData != null)
            {
                Debug.Assert(initialData.SceneType == this.SceneType);
                stageSceneInitialData = (StageSceneInitialData)initialData;
            }
            else
            {
                // 개발용 빌드에서, Starter씬을 거치지 않고 Stage씬을 바로 실행한 경우임
                // 개발용 값으로 메인씬을 실행한다.
                // (해당 씬의 Awake() 콜스택에서 실행되고 있음에 유의)
                int testChapterNumber = 1;
                if (this.TEST_ChapterNumberForTest != -1)
                {
                    testChapterNumber = this.TEST_ChapterNumberForTest;
                }

                var chapter = StaticDataRepository.Instance.Chapters.FindChapter(testChapterNumber) ?? throw new LogicErrorException($"{testChapterNumber} 챕터 정보가 없음");
                var tempHero = new HeroData(HeroInstanceId.CreateNew(), HeroType.Ignatia, Grade.SS, promotionPoint: 0, level: 1, DateTime.UtcNow);
                var equippedEquipments = Enumerable.Empty<EquipmentData>();


                stageSceneInitialData = StageSceneInitialData.CreateForMainChapter(
                    chapter,
                    StaticDataRepository.Instance.Skills.GetUserSkillDeck(GameConstants.SKILLDECK_DEFAULT_SEASON_SKILLDECK, testChapterNumber),
                    tempHero,
                    equippedEquipments);
            }

            int? chapterNumber = stageSceneInitialData.StageType switch
            {
                StageType.Chapter => stageSceneInitialData.Chapter!.ChapterNumber,
                _ => null,
            };
            this.UI.Initialize(this.SceneType, stageSceneInitialData.UserHeroData.HeroType, chapterNumber);

            this.InitializeFloor(stageSceneInitialData.StageStaticData);

            if (stageSceneInitialData.StageType == StageType.Chapter)
            {
                GameClient.Instance.CreateMainChapterStage(
                    stageSceneInitialData.Chapter!,
                    stageSceneInitialData.UserSkillDeck,
                    stageSceneInitialData.UserHeroData,
                    stageSceneInitialData.UserHeroEquippedEquipments);
            }
            else
            {
                throw new NotImplementedException($"{stageSceneInitialData.StageType} 구현 안 됨 구현해주세요");
            }

            Debug.Assert(GameClient.Stage != null, "스테이지 초기화 위에서 완료했어야 함");

            var stage = GameClient.Stage!;
            var pc = stage.PC;

            // 자동 전투 활성화 조건: 이미 클리어한 메인 챕터 플레이.
            bool activateAutoPlay =
                (GameClient.CS.UserGameData != null &&
                stage.StageType == StageType.Chapter &&
                stageSceneInitialData.Chapter!.ChapterNumber <= GameClient.CS.UserGameData!.ClearedHighestChapter);

            GameClient.Instance.CreatePlayerController(this.UI.Joystick, activateAutoPlay);
            // NOTE: 스테이지 입장처리 구현하고 나면 그쪽으로 옮겨야 한다.
            this.UI.OnPlayerAppearingEnd(pc, activateAutoPlay);

            var walkableArea = stageSceneInitialData.StageStaticData.GetWalkableArea();
            GameClient.CameraController.SetupMainCamera(this.gameObject.scene, walkableArea, pc.gameObject);

            int stageNumber = stageSceneInitialData.StageStaticData.StageNumber;
            int? nextStageNumber = GetNextStageNumber(stageSceneInitialData);
            _preDownloadStopper = BeginPreDownloadAssets(stageNumber, nextStageNumber);
            _preCreateMonstersStopper = BeginPreCreateMonsters(stage);
        }

        #region PredownloadAssets
        private static int? GetNextStageNumber(StageSceneInitialData stageSceneInitialData)
        {
            switch (stageSceneInitialData.StageType)
            {
                case StageType.Chapter:
                    {
                        int nextChapterNumber = stageSceneInitialData.Chapter!.ChapterNumber + 1;
                        return StaticDataRepository.Instance.Chapters.FindChapter(nextChapterNumber)?.StageStaticData.StageNumber;
                    }
                default:
                    {
                        throw new NotImplementedException($"{stageSceneInitialData.StageType} 구현 안 됨 구현해주세요");
                    }
            }
        }

        private static CancellationTokenSource BeginPreDownloadAssets(int stageNumber, int? nextStageNumber)
        {
            var predownloadStopper = new CancellationTokenSource();
            GameClient.AsyncDispatcher.RunOrReserveAsyncJob(async () =>
            {
                // 이번스테이지의 리소스들을 쭉 훑으면서 미리 다운로드 받고 로드해둔다.
                try
                {
                    if (predownloadStopper.IsCancellationRequested)
                    {
                        return;
                    }

                    await DownloadAndReserveResourcesAsync(stageNumber, predownloadStopper.Token);

                    if (predownloadStopper.IsCancellationRequested)
                    {
                        return;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(6), predownloadStopper.Token);
                    if (predownloadStopper.IsCancellationRequested)
                    {
                        return;
                    }

                    // 다음 스테이지 리소스는 다운로드 받아둔다.
                    if (nextStageNumber != null)
                    {
                        await DownloadAssetsForStageAsync(nextStageNumber.Value, predownloadStopper.Token);
                    }
                    else
                    {
                        Debug.Log($"Stage [{stageNumber}] is the last stage for this stage type. Don't download next stage resources.");
                    }
                }
                catch (TaskCanceledException e)
                {
                    Debug.LogWarning($"Predownload asset process has canceled by request. [{e.Message}]");
                }
            });

            return predownloadStopper;
        }

        private static async Task DownloadAndReserveResourcesAsync(int stageNumber, CancellationToken canceller)
        {
            Debug.Log($"Begin download and reserve resources for Stage[{stageNumber}]");
            var stageEvents = StaticDataRepository.Instance.Stages.FindStageEvents(stageNumber);
            if (stageEvents == null)
            {
                return;
            }

            var monsterSet = new HashSet<CharacterType>(capacity: 16);
            foreach (var stageEvent in stageEvents)
            {
                if (stageEvent.MonsterType != CharacterType.None)
                {
                    monsterSet.Add(stageEvent.MonsterType);
                }
            }

            foreach (var monsterType in monsterSet)
            {
                if (canceller.IsCancellationRequested)
                {
                    return;
                }

                var monsterStaticData = StaticDataRepository.Instance.Monsters.Find(monsterType);
                if (monsterStaticData == null)
                {
                    continue;
                }

                string resourcePath = monsterStaticData.SkeletonDataPath;
                if (Path.GetExtension(resourcePath) == ".controller")
                {
                    await ResourcePool.Instance.ReserveResourceAsync<RuntimeAnimatorController>(resourcePath);
                }
                else if (Path.GetExtension(resourcePath) == ".asset")
                {
                    await ResourcePool.Instance.ReserveResourceAsync<SkeletonDataAsset>(resourcePath);
                }
                else
                {
                    Debug.LogError($"Unknown Extension of MonsterStaticData.SkeletonDataPath. [{resourcePath}]");
                }

                await Task.Delay(TimeSpan.FromSeconds(3), canceller);
            }

            // 보스 이미지도 미리 비동기로 다운로드 받는다.
            foreach (var stageEvent in stageEvents)
            {
                if (canceller.IsCancellationRequested)
                {
                    return;
                }

                if (stageEvent.EventType != StageEventType.BossSpawn)
                {
                    continue;
                }

                if (!StaticDataRepository.Instance.MonsterImagePaths.MonsterImagePathStaticDatas.TryGetValue(stageEvent.MonsterType, out var monsterImagePath))
                {
                    continue;
                }

                var resourcePath = monsterImagePath.IllustImagePath;
                await ResourcePool.Instance.ReserveResourceAsync<Sprite>(resourcePath);

                await Task.Delay(TimeSpan.FromSeconds(4), canceller);
            }

            Debug.Log($"End download and reserve resources for Stage[{stageNumber}]");
        }

        private static async Task DownloadAssetsForStageAsync(int stageNumber, CancellationToken canceller)
        {
            Debug.Log($"Begin download resources for Stage[{stageNumber}]");
            var stageEvents = StaticDataRepository.Instance.Stages.FindStageEvents(stageNumber);
            if (stageEvents == null)
            {
                return;
            }

            var monsterSet = new HashSet<CharacterType>(capacity: 16);
            foreach (var stageEvent in stageEvents)
            {
                if (stageEvent.MonsterType != CharacterType.None)
                {
                    monsterSet.Add(stageEvent.MonsterType);
                }
            }

            foreach (var monsterType in monsterSet)
            {
                if (canceller.IsCancellationRequested)
                {
                    return;
                }

                var monsterStaticData = StaticDataRepository.Instance.Monsters.Find(monsterType);
                if (monsterStaticData == null)
                {
                    continue;
                }

                string resourcePath = monsterStaticData.SkeletonDataPath;
                if (Path.GetExtension(resourcePath) == ".controller")
                {
                    await ResourcePool.Instance.ReserveResourceAsync<RuntimeAnimatorController>(resourcePath);
                }
                else if (Path.GetExtension(resourcePath) == ".asset")
                {
                    await ResourcePool.Instance.ReserveResourceAsync<SkeletonDataAsset>(resourcePath);
                }

                await Task.Delay(TimeSpan.FromSeconds(4), canceller);
            }

            {
                // 바닥 리소스 
                var stageStaticData = StaticDataRepository.Instance.Stages.Find(stageNumber);
                if (stageStaticData != null)
                {
                    string tileResourcePath = stageStaticData.FloorTileResourcePath;

                    if (stageStaticData.StageFormType == StageFormType.Rectangle)
                    {
                        int pathIndex = tileResourcePath.LastIndexOf('/');
                        string folderName = tileResourcePath.Substring(pathIndex + 1);

                        // 닫힌맵은 1번 타일리소스만 접근해도 연관된 것 다 다운받아진다.
                        string newFilePath = tileResourcePath + '/' + folderName + "_1.png";
                        tileResourcePath = newFilePath;
                    }

                    await ResourcePool.Instance.ReserveResourceAsync<Sprite>(tileResourcePath);

                    await Task.Delay(TimeSpan.FromSeconds(4), canceller);
                }
            }

            // 보스 이미지도 미리 비동기로 다운로드 받는다.
            foreach (var stageEvent in stageEvents)
            {
                if (canceller.IsCancellationRequested)
                {
                    return;
                }

                if (stageEvent.EventType != StageEventType.BossSpawn)
                {
                    continue;
                }

                if (!StaticDataRepository.Instance.MonsterImagePaths.MonsterImagePathStaticDatas.TryGetValue(stageEvent.MonsterType, out var monsterImagePath))
                {
                    continue;
                }

                var resourcePath = monsterImagePath.IllustImagePath;
                await ResourcePool.Instance.ReserveResourceAsync<Sprite>(resourcePath);

                await Task.Delay(TimeSpan.FromSeconds(4), canceller);
            }

            Debug.Log($"End download resources for Stage[{stageNumber}]");
        }
        #endregion // Pre Download Assets

        #region PreCreaeMonsters
        private static CancellationTokenSource BeginPreCreateMonsters(Stage stage)
        {
            var stopper = new CancellationTokenSource();

            GameClient.AsyncDispatcher.RunOrReserveAsyncJob(async () =>
            {
                // 이번스테이지의 몬스터들을 최대량만큼 미리 생성해서 풀링해둔다.
                // 몬스터가 많이 스폰될 때의 성능최적화를 위함이기 때문에, 디테일하게 완벽히 풀링할 필요는 없다.
                // 대충 잡몹 많이 스폰되는 것들 풀링하는 것으로 OK
                try
                {
                    Debug.Log($"Begin PreCreate monsters for Stage[{stage.StaticData.StageNumber}]");
                    var stageEvents = StaticDataRepository.Instance.Stages.FindStageEvents(stage.StaticData.StageNumber);
                    if (stageEvents == null)
                    {
                        return;
                    }

                    var monsterCount = new Dictionary<CharacterType, int>();
                    var autoRespawnMonster = new Dictionary<CharacterType, (float EndAt, int Amount)>();
                    foreach (var stageEvent in stageEvents)
                    {
                        if (stageEvent.MonsterType == CharacterType.None ||
                            stageEvent.EventType == StageEventType.BossSpawn ||
                            stageEvent.Amount <= 0)
                        {
                            continue;
                        }

                        if (stageEvent.EventType == StageEventType.AutoRespawnMonster)
                        {
                            var (endAt, amount) = autoRespawnMonster.GetValueOrDefault(stageEvent.MonsterType, (float.MinValue, 0));

                            if (endAt < stageEvent.BeginAt)
                            {
                                amount = 0;
                            }
                            amount += stageEvent.Amount;

                            if (endAt < stageEvent.EndAt)
                            {
                                endAt = stageEvent.EndAt;
                            }

                            autoRespawnMonster[stageEvent.MonsterType] = (endAt, amount);

                            if (!monsterCount.TryGetValue(stageEvent.MonsterType, out int count))
                            {
                                monsterCount.Add(stageEvent.MonsterType, amount);
                                continue;
                            }

                            if (count < amount)
                            {
                                monsterCount[stageEvent.MonsterType] = amount;
                            }
                        }
                        else
                        {
                            if (!monsterCount.TryGetValue(stageEvent.MonsterType, out int count))
                            {
                                monsterCount.Add(stageEvent.MonsterType, stageEvent.Amount);
                                continue;
                            }

                            if (count < stageEvent.Amount)
                            {
                                monsterCount[stageEvent.MonsterType] = stageEvent.Amount;
                            }
                        }
                    }

                    var emptyItemList = new List<DropItemType>();
                    foreach ((var monsterType, int count) in monsterCount)
                    {
                        if (stopper.IsCancellationRequested)
                        {
                            return;
                        }

                        await Task.Delay(TimeSpan.FromSeconds(3.3f), stopper.Token);

                        int spawnCount = count;
                        if ((stage.StaticData.StageNumber == 0) &&
                            ((GameClient.CS.UserGameData?.HighestStageTimeInSeconds ?? 0) <= 0))
                        {
                            spawnCount = (int)(spawnCount * 1.5f);
                        }
                        else
                        {
                            spawnCount = (int)(spawnCount * 1.2f);
                        }

                        Debug.Log($"Begin PreCreate monster[{monsterType}] amount[{spawnCount}] for Stage[{stage.StaticData.StageNumber}]");

                        var monsterStaticData = StaticDataRepository.Instance.Monsters.Find(monsterType);
                        if (monsterStaticData == null)
                        {
                            continue;
                        }

                        // 6마리씩 미리 생성.
                        for (int i = 0; i < spawnCount + 6; i += 6)
                        {
                            if (stopper.IsCancellationRequested)
                            {
                                return;
                            }

                            stage.PreCreateMonster(monsterType, i);
                            await Task.Delay(TimeSpan.FromSeconds(0.33f), stopper.Token);
                        }
                    }
                }
                catch (TaskCanceledException e)
                {
                    Debug.LogWarning($"PreCreateMonster process has canceled by request. [{e.Message}]");
                }

                Debug.Log($"Done PreCreate monsters for Stage[{stage.StaticData.StageNumber}]");
            });

            return stopper;
        }
        #endregion // PreCreateMonsters

        public override void OnLoadingSceneRemoved()
        {
            base.OnLoadingSceneRemoved();

            var stage = GameClient.Stage;
            if (stage != null)
            {
                UnityGlobal.Sounds.PlayBGM(stage.StaticData.BGMPath, BGMPlayRule.ForcePlay);
            }
            else
            {
                UnityGlobal.Sounds.PlayBGM("Sounds/BGMs/BGM_Ingame_Common_SFX.prefab", BGMPlayRule.ForcePlay);
            }


            var bgm = UnityGlobal.Sounds.CurrentBGM;
            if (bgm != null)
            {
                var pc = GameClient.Stage?.PC;
                if (pc != null)
                {
                    bgm.gameObject.transform.SetParent(pc.transform, worldPositionStays: false);
                }
                else
                {
                    Debug.LogWarning($"Stage?.PC가 null이라서 배경음악을 PC에 못 붙였음. 상황 확인하고 오류 없나 봐주세요. 정상 스테이지에서는 일어나면 안됨.");
                }
            }
        }

        public override void Clear()
        {

            this.UI.Clear();

            base.Clear();
        }


        protected override void Update()
        {
            base.Update();

            if (!this.IsSceneInitialized || this.IsSceneCleared)
            {
                return;
            }

            if (GameClient.Stage == null)
            {
                return;
            }

            float stageRunningTime = GameClient.Stage.StageRunningTime;
            float maxStageTime = GameClient.Stage.MaxStageTime;
            UI.StageTimer.UpdateTimer(stageRunningTime, maxStageTime);

            this.UI.UpdateLogic();
        }

        protected void InitializeFloor(StageStaticData stageStaticData)
        {
            var walkableArea = stageStaticData.GetWalkableArea();

            if (stageStaticData.StageFormType == StageFormType.Rectangle)
            {
                _floor.transform.position = Vector3.zero;
                _floor.sprite = null;
                this.SetActiveOfRectangleFloor(true);
                this.LoadSpriteOfRectangleFloor(stageStaticData.FloorTileResourcePath);
            }
            else if (stageStaticData.StageFormType == StageFormType.Vertical)
            {
                _floor.sprite = ResourcePool.Instance.LoadResource<Sprite>(stageStaticData.FloorTileResourcePath);
                float spriteWidth = _floor.sprite.textureRect.width / _floor.sprite.pixelsPerUnit;

                if (spriteWidth < walkableArea.width)
                {
                    //아마 타일형식에 사이즈를 강제로 늘리면 배경화면이 반복되어 보여 이상하게 보일것
                    //이 형식은 들어오지 않는게 맞지만 혹시 모르니 작업해둔다.
                    Debug.LogWarning("배경 이미지가 이상하게 나올것으로 예상됩니다. 잘 나오는지 확인해주세요");
                    _floor.size = new Vector2(walkableArea.width, walkableArea.height);
                }
                else
                {
                    _floor.size = new Vector2(spriteWidth, walkableArea.height);
                }
                _floor.transform.position = Vector3.zero;
                _floor.drawMode = SpriteDrawMode.Tiled;
                _floor.spriteSortPoint = SpriteSortPoint.Center;
                this.SetActiveOfRectangleFloor(false);
            }
            else
            {
                _floor.sprite = ResourcePool.Instance.LoadResource<Sprite>(stageStaticData.FloorTileResourcePath);
                _floor.size = walkableArea.size;
                _floor.transform.position = Vector3.zero;
                _floor.drawMode = SpriteDrawMode.Tiled;
                _floor.spriteSortPoint = SpriteSortPoint.Center;
                this.SetActiveOfRectangleFloor(false);
            }

            this.CreateStageEffect(stageStaticData);
            this.CreateBlockingColliders(walkableArea);
        }

        private void CreateBlockingColliders(Rect walkableArea)
        {
            // 좌우 
            {
                // 좌
                var colliderObject = new GameObject("FloorColliderLeft");
                colliderObject.transform.SetParent(_floor.gameObject.transform);
                colliderObject.layer = LayerMask.NameToLayer("LowWall");

                var collider = colliderObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(100f, walkableArea.height + 100f);
                collider.offset = new Vector2(-(walkableArea.size.x * 0.5f + collider.size.x * 0.5f), 0f);
            }
            {
                // 우
                var colliderObject = new GameObject("FloorColliderRight");
                colliderObject.transform.SetParent(_floor.gameObject.transform);
                colliderObject.layer = LayerMask.NameToLayer("LowWall");

                var collider = colliderObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(100f, walkableArea.height + 100f);
                collider.offset = new Vector2(walkableArea.size.x * 0.5f + collider.size.x * 0.5f, 0f);
            }
            {
                // 상
                var colliderObject = new GameObject("FloorColliderTop");
                colliderObject.transform.SetParent(_floor.gameObject.transform);
                colliderObject.layer = LayerMask.NameToLayer("LowWall");

                var collider = colliderObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(walkableArea.width + 100f, 100f);
                collider.offset = new Vector2(0f, walkableArea.size.y * 0.5f + collider.size.y * 0.5f);
            }
            {
                // 하
                var colliderObject = new GameObject("FloorColliderBottom");
                colliderObject.transform.SetParent(_floor.gameObject.transform);
                colliderObject.layer = LayerMask.NameToLayer("LowWall");

                var collider = colliderObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(walkableArea.width + 100f, 100f);
                collider.offset = new Vector2(0f, -(walkableArea.size.y * 0.5f + collider.size.y * 0.5f));
            }
        }

        private void CreateStageEffect(StageStaticData stageStaticData)
        {
            switch (stageStaticData.StageNumber)
            {
                case 0:
                    {
                        var stageEffect = ResourcePool.Instance.InstantiateFromResource("Stages/Floors/Effect/Floor0/Floor0Object.prefab");
                        stageEffect.transform.SetParent(_floor.transform);
                        stageEffect.transform.localPosition = Vector3.zero;
                        stageEffect.transform.localScale = Vector3.one;
                        _floor.transform.localPosition = new Vector3(0, 1, 0);  //0챕터 배경 이미지 중점이 안맞아서 살짝 올림
                    }
                    break;
                case 9:
                    {
                        var stageEffect = ResourcePool.Instance.InstantiateFromResource("Stages/Floors/Effect/Stage9Fire.prefab");
                        stageEffect.transform.SetParent(_floor.transform);
                        stageEffect.transform.localPosition = Vector3.zero;
                        stageEffect.transform.localScale = Vector3.one;
                    }
                    break;
                case 11:
                    {
                        var stageEffect = ResourcePool.Instance.InstantiateFromResource("Stages/Floors/Effect/Floor11/Chapter11StageEffect.prefab");
                        stageEffect.transform.SetParent(_floor.transform);
                        stageEffect.transform.localPosition = Vector3.zero;
                        stageEffect.transform.localScale = Vector3.one;
                    }
                    break;
                case 17:
                    {
                        var stageEffect = ResourcePool.Instance.InstantiateFromResource("Stages/Floors/Effect/Floor11/Chapter11StageEffect.prefab");
                        stageEffect.transform.SetParent(_floor.transform);
                        stageEffect.transform.localPosition = Vector3.zero;
                        stageEffect.transform.localScale = Vector3.one;
                    }
                    break;
                case 18:
                    {
                        _floor.transform.localPosition = new Vector3(0, 1.5f, 0);  // 배경 이미지 중점이 안맞아서 살짝 올림
                    }
                    break;
                case 25:
                    {
                        var stageEffect = ResourcePool.Instance.InstantiateFromResource("Stages/Floors/Effect/Floor33/river_0.prefab");
                        stageEffect.transform.SetParent(_floor.transform);
                        stageEffect.transform.localPosition = new Vector3(-0.31f, 0.0f, 0.0f);
                        stageEffect.transform.localScale = Vector3.one;
                    }
                    break;
                case 26:
                    {
                        var stageEffect = ResourcePool.Instance.InstantiateFromResource("Stages/Floors/Effect/Floor34/TrainStageEffect.prefab");
                        stageEffect.transform.SetParent(_floor.transform);
                        stageEffect.transform.localPosition = new Vector3(-9.5f, 0f, 0f);
                        stageEffect.transform.localScale = Vector3.one;

                        var animators = stageEffect.GetComponentsInChildren<Animator>();

                        for (int i = 0; i < animators.Length; i++)
                        {
                            float normalizedTime = (float)i / animators.Length;
                            animators[i].Play("steam_train", 0, normalizedTime);
                        }

                    }
                    break;
                case 20002:
                    {
                        this.GetEarthGuardianStageEffectObject(stageStaticData.StageFormType);
                    }
                    break;
                case 20003:
                    {
                        this.GetEarthGuardianStageEffectObject(stageStaticData.StageFormType);
                    }
                    break;
                case 20004:
                    {
                        this.GetEarthGuardianStageEffectObject(stageStaticData.StageFormType);
                    }
                    break;
                case 20005:
                    {
                        this.GetEarthGuardianStageEffectObject(stageStaticData.StageFormType);
                    }
                    break;
                case 20006:
                    {
                        //지구방위대 오브젝트
                        this.GetEarthGuardianStageEffectObject(stageStaticData.StageFormType);

                        //파도 오브젝트
                        var stageEffect = ResourcePool.Instance.InstantiateFromResource("Stages/Floors/Effect/Floor11/Chapter11StageEffect.prefab");
                        stageEffect.transform.SetParent(_floor.transform);
                        stageEffect.transform.localPosition = Vector3.zero;
                        stageEffect.transform.localScale = Vector3.one;

                        //마스크 오브젝트
                        var maskObject = ResourcePool.Instance.InstantiateFromResource("Stages/Floors/Effect/EarthGuardian/Mask.prefab");
                        maskObject.GetComponent<SpriteMask>().sprite = ResourcePool.Instance.LoadResource<Sprite>("Commons/Square.png");
                        maskObject.transform.SetParent(_floor.transform);
                        maskObject.transform.localPosition = new Vector3(5.1f, 0f, 0f);
                        maskObject.transform.localScale = new Vector3(3.8f, 800f, 1);

                    }
                    break;
                case 20007:
                    {
                        this.GetEarthGuardianStageEffectObject(stageStaticData.StageFormType);
                    }
                    break;
                case 20010:
                    {
                        this.GetEarthGuardianStageEffectObject(stageStaticData.StageFormType);
                    }
                    break;
                case 40:
                    {
                        var stageEffect = ResourcePool.Instance.InstantiateFromResource("Stages/Floors/Effect/Floor0/Floor0Object.prefab");
                        stageEffect.transform.SetParent(_floor.transform);
                        stageEffect.transform.localPosition = Vector3.zero;
                        stageEffect.transform.localScale = Vector3.one;
                        _floor.transform.localPosition = new Vector3(0, 1, 0);  //0챕터 배경 이미지 중점이 안맞아서 살짝 올림
                    }
                    break;
                case 20013:
                    {
                        this.GetEarthGuardianStageEffectObject(stageStaticData.StageFormType);
                    }
                    break;
                case 51:
                    {
                        var stageEffect = ResourcePool.Instance.InstantiateFromResource("Stages/Floors/Effect/Floor11/Chapter11StageEffect.prefab");
                        stageEffect.transform.SetParent(_floor.transform);
                        stageEffect.transform.localPosition = Vector3.zero;
                        stageEffect.transform.localScale = Vector3.one;
                    }
                    break;
                case 20016:
                    {
                        //지구방위대 오브젝트
                        this.GetEarthGuardianStageEffectObject(stageStaticData.StageFormType);

                        //파도 오브젝트
                        var stageEffect = ResourcePool.Instance.InstantiateFromResource("Stages/Floors/Effect/Floor11/Chapter11StageEffect.prefab");
                        stageEffect.transform.SetParent(_floor.transform);
                        stageEffect.transform.localPosition = Vector3.zero;
                        stageEffect.transform.localScale = Vector3.one;

                        //마스크 오브젝트
                        var maskObject = ResourcePool.Instance.InstantiateFromResource("Stages/Floors/Effect/EarthGuardian/Mask.prefab");
                        maskObject.GetComponent<SpriteMask>().sprite = ResourcePool.Instance.LoadResource<Sprite>("Commons/Square.png");
                        maskObject.transform.SetParent(_floor.transform);
                        maskObject.transform.localPosition = new Vector3(5.1f, 0f, 0f);
                        maskObject.transform.localScale = new Vector3(3.8f, 800f, 1);

                    }
                    break;
                case 57:
                    {
                        //파도 오브젝트
                        var stageEffect = ResourcePool.Instance.InstantiateFromResource("Stages/Floors/Effect/Floor11/Chapter11StageEffect.prefab");
                        stageEffect.transform.SetParent(_floor.transform);
                        stageEffect.transform.localPosition = Vector3.zero;
                        stageEffect.transform.localScale = Vector3.one;
                    }
                    break;
                case 58:
                    {
                        _floor.transform.localPosition = new Vector3(0, 1.5f, 0);  // 배경 이미지 중점이 안맞아서 살짝 올림
                    }
                    break;
                default:
                    break;
            }
        }

        private void GetEarthGuardianStageEffectObject(StageFormType stageFormType)
        {
            switch (stageFormType)
            {
                case StageFormType.Infinite:
                    {
                        int random = UnityEngine.Random.Range(0, 5);
                        var stageEffect = ResourcePool.Instance.InstantiateFromResource($"Stages/Floors/Effect/EarthGuardian/Infinite/mapObjectPrefab{random}.prefab");
                        stageEffect.transform.SetParent(_floor.transform);
                        stageEffect.transform.localPosition = Vector3.zero;
                        stageEffect.transform.localScale = Vector3.one;
                    }
                    break;
                case StageFormType.Rectangle:
                    {
                        int random = UnityEngine.Random.Range(0, 5);
                        var stageEffect = ResourcePool.Instance.InstantiateFromResource($"Stages/Floors/Effect/EarthGuardian/Rectangle/mapObjectPrefab{random}.prefab");
                        stageEffect.transform.SetParent(_floor.transform);
                        stageEffect.transform.localPosition = Vector3.zero;
                        stageEffect.transform.localScale = Vector3.one;
                    }
                    break;
                case StageFormType.Vertical:
                    {
                        int random = UnityEngine.Random.Range(0, 5);
                        var stageEffect = ResourcePool.Instance.InstantiateFromResource($"Stages/Floors/Effect/EarthGuardian/Vertical/mapObjectPrefab{random}.prefab");
                        stageEffect.transform.SetParent(_floor.transform);
                        stageEffect.transform.localPosition = Vector3.zero;
                        stageEffect.transform.localScale = Vector3.one;
                    }
                    break;
                default:
                    {
                        Debug.LogError("지구방위대 맵 타입이 뭔가 이상합니다. 확인이 필요합니다.");
                    }
                    break;
            }
        }


        private void SetActiveOfRectangleFloor(bool isActive)
        {
            foreach (var renderer in _rectangleFloorRender)
            {
                renderer.gameObject.SetActive(isActive);
            }
        }

        private void LoadSpriteOfRectangleFloor(string floorTileResourcePath)
        {
            int count = _rectangleFloorRender.Length;
            int pathIndex = floorTileResourcePath.LastIndexOf('/');
            string folderName = floorTileResourcePath.Substring(pathIndex + 1);
            for (int i = 0; i < count; ++i)
            {
                string newFilePath = floorTileResourcePath + '/' + folderName + $"_{i + 1}.png";
                _rectangleFloorRender[i].sprite = ResourcePool.Instance.LoadResource<Sprite>(newFilePath);
            }

            Vector2 spriteSize;
            Vector2 spritehalfSize;
            Vector2 moveOffset;
            SpriteRenderer targetRenderer;
            // LeftTop number 1
            {
                targetRenderer = _rectangleFloorRender[0];
                spriteSize = new Vector2(targetRenderer.sprite.texture.width, targetRenderer.sprite.texture.height);
                spritehalfSize = spriteSize * 0.5f;
                moveOffset = new Vector2(-spritehalfSize.x, spritehalfSize.y) * 0.01f;
                targetRenderer.transform.localPosition = moveOffset;
            }
            // RightTop number 2
            {
                targetRenderer = _rectangleFloorRender[1];
                spriteSize = new Vector2(targetRenderer.sprite.texture.width, targetRenderer.sprite.texture.height);
                spritehalfSize = spriteSize * 0.5f;
                moveOffset = new Vector2(spritehalfSize.x, spritehalfSize.y) * 0.01f;
                targetRenderer.transform.localPosition = moveOffset;
            }
            // LeftBottom number 3
            {
                targetRenderer = _rectangleFloorRender[2];
                spriteSize = new Vector2(targetRenderer.sprite.texture.width, targetRenderer.sprite.texture.height);
                spritehalfSize = spriteSize * 0.5f;
                moveOffset = new Vector2(-spritehalfSize.x, -spritehalfSize.y) * 0.01f;
                targetRenderer.transform.localPosition = moveOffset;
            }
            // RightBottom number 4
            {
                targetRenderer = _rectangleFloorRender[3];
                spriteSize = new Vector2(targetRenderer.sprite.texture.width, targetRenderer.sprite.texture.height);
                spritehalfSize = spriteSize * 0.5f;
                moveOffset = new Vector2(spritehalfSize.x, -spritehalfSize.y) * 0.01f;
                targetRenderer.transform.localPosition = moveOffset;
            }
        }
    }
}