#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Shared;
using Shared.GameDataTypes;
using Shared.Localizers;
using Shared.StaticDatas;
using UnityEngine;

namespace SamMul.Tests.EditMode
{
    /// <summary>
    /// Resources/StaticData 의 JSON 테이블을 실제 로더로 읽어, 테이블 사이의 참조와 스테이지 구성을 검증한다.
    /// 밸런스 수치를 고친 뒤 데이터가 깨지지 않았는지 확인하는 용도다.
    /// </summary>
    public class StaticDataTests
    {
        private const int BOSS_COUNT_PER_STAGE = 2;

        // 몬스터를 실제로 만드는 이벤트. 경고 이벤트(BossWarning, RushWarning)는 몬스터 타입과 가중치를 쓰지 않는다.
        private static readonly StageEventType[] SPAWN_EVENT_TYPES =
        {
            StageEventType.StageEnterInit,
            StageEventType.MonsterSpawn,
            StageEventType.EliteSpawn,
            StageEventType.MonsterMassSpawn,
            StageEventType.AutoRespawnMonster,
            StageEventType.BossSpawn,
        };

        [OneTimeSetUp]
        public void LoadStaticData()
        {
            SharedInitializer.ForceReInitialize(LanguageType.Korean);
        }

        private static StaticDataRepository Repository => StaticDataRepository.Instance;

        private static IReadOnlyList<StageEventStaticData> EventsOf(ChapterStaticData chapter)
        {
            var events = Repository.Stages.FindStageEvents(chapter.StageNumber);
            Assert.IsNotNull(events, $"챕터 {chapter.ChapterNumber} 의 스테이지 {chapter.StageNumber} 에 이벤트가 없습니다.");
            return events!;
        }

        private static IEnumerable<StageEventStaticData> SpawnEventsOf(ChapterStaticData chapter)
        {
            return EventsOf(chapter).Where(stageEvent => SPAWN_EVENT_TYPES.Contains(stageEvent.EventType));
        }

        [Test]
        public void Chapters_AreThree_AndEachHasStageAndEvents()
        {
            Assert.AreEqual(3, Repository.Chapters.All.Count);

            foreach (var chapter in Repository.Chapters.All)
            {
                Assert.IsNotNull(Repository.Stages.Find(chapter.StageNumber), $"챕터 {chapter.ChapterNumber} 의 스테이지 {chapter.StageNumber} 가 Stages 에 없습니다.");
                Assert.IsNotEmpty(EventsOf(chapter));
            }
        }

        [Test]
        public void SpawnEvents_ReferenceMonstersDefinedInMonsterTable()
        {
            foreach (var chapter in Repository.Chapters.All)
            {
                foreach (var stageEvent in SpawnEventsOf(chapter))
                {
                    Assert.IsNotNull(Repository.Monsters.Find(stageEvent.MonsterType),
                        $"스테이지 {stageEvent.StageNumber} {stageEvent.EventType}({stageEvent.BeginAt}초) 의 몬스터 {stageEvent.MonsterType} 가 Monsters 에 없습니다.");
                }
            }
        }

        [Test]
        public void SpawnEvents_HavePositiveStatWeights()
        {
            foreach (var chapter in Repository.Chapters.All)
            {
                foreach (var stageEvent in SpawnEventsOf(chapter))
                {
                    string where = $"스테이지 {stageEvent.StageNumber} {stageEvent.EventType}({stageEvent.BeginAt}초) {stageEvent.MonsterType}";
                    Assert.Greater(stageEvent.MonsterHPWeight, 0f, $"{where} 의 HP 가중치");
                    Assert.Greater(stageEvent.MonsterAttackPowerWeight, 0f, $"{where} 의 공격력 가중치");
                }
            }
        }

        [Test]
        public void EachStage_HasTwoBosses_EachWithWarning_WithinStageTime()
        {
            foreach (var chapter in Repository.Chapters.All)
            {
                var stage = Repository.Stages.Get(chapter.StageNumber);
                var events = EventsOf(chapter);
                var bossSpawns = events.Where(stageEvent => stageEvent.EventType == StageEventType.BossSpawn).ToList();
                var bossWarnings = events.Where(stageEvent => stageEvent.EventType == StageEventType.BossWarning).ToList();

                Assert.AreEqual(BOSS_COUNT_PER_STAGE, bossSpawns.Count, $"스테이지 {stage.StageNumber} 의 보스 수");
                Assert.AreEqual(bossSpawns.Count, bossWarnings.Count, $"스테이지 {stage.StageNumber} 의 보스 경고 수");

                foreach (var bossSpawn in bossSpawns)
                {
                    Assert.LessOrEqual(bossSpawn.BeginAt, stage.TotalStageTimeSec, $"스테이지 {stage.StageNumber} 보스 {bossSpawn.MonsterType} 의 등장 시각");
                    Assert.IsTrue(bossWarnings.Any(warning => warning.BeginAt < bossSpawn.BeginAt && warning.EndAt <= bossSpawn.BeginAt),
                        $"스테이지 {stage.StageNumber} 보스 {bossSpawn.MonsterType}({bossSpawn.BeginAt}초) 앞에 끝나는 경고가 없습니다.");
                }
            }
        }

        [Test]
        public void SpawnedMonsters_HaveLocalizedNames()
        {
            foreach (var chapter in Repository.Chapters.All)
            {
                foreach (var monsterType in SpawnEventsOf(chapter).Select(stageEvent => stageEvent.MonsterType).Distinct())
                {
                    string nameKey = Repository.Monsters.Get(monsterType).MonsterNameKey;
                    Assert.IsTrue(Localizer.Instance.CurrentLanguage.Texts.ContainsKey(nameKey), $"{monsterType} 의 이름 키 {nameKey} 가 Localization 에 없습니다.");
                }
            }
        }

        /// <summary>
        /// 몬스터 데이터에 적힌 리소스 경로(Assets/Resources 기준)에 실제 파일이 있는지만 확인한다.
        /// 파일이 없으면 게임은 플레이스홀더로 대체하고 경고 로그만 남기므로 플레이 테스트로는 잡히지 않는다.
        /// </summary>
        [Test]
        public void MonsterResourcePaths_PointToExistingFiles()
        {
            var problems = new List<string>();

            void Check(CharacterType monsterType, string column, string resourcePath, bool isRequired)
            {
                if (string.IsNullOrEmpty(resourcePath))
                {
                    if (isRequired)
                    {
                        problems.Add($"{monsterType}.{column}: 경로가 비어 있습니다.");
                    }
                }
                else if (!File.Exists(Path.Combine(Application.dataPath, "Resources", resourcePath)))
                {
                    problems.Add($"{monsterType}.{column}: {resourcePath}");
                }
            }

            foreach (var monster in Repository.Monsters.Monsters.Values)
            {
                Check(monster.MonsterType, nameof(monster.SkeletonDataPath), monster.SkeletonDataPath, isRequired: true);
                // 특수 공격이 없는 몬스터는 비워 둔다.
                Check(monster.MonsterType, nameof(monster.SpecialAttack1ResourcePath), monster.SpecialAttack1ResourcePath, isRequired: false);
            }
            foreach (var imagePath in Repository.MonsterImagePaths.MonsterImagePathStaticDatas.Values)
            {
                Check(imagePath.CharacterType, nameof(imagePath.IllustImagePath), imagePath.IllustImagePath, isRequired: true);
            }

            CollectionAssert.IsEmpty(problems, "파일이 없는 몬스터 리소스 경로");
        }

        [Test]
        public void Localization_KoreanAndEnglish_HaveSameKeys_AndNoEmptyText()
        {
            Localizer.Instance.SetLanguage(LanguageType.Korean);
            var korean = new Dictionary<string, string>(Localizer.Instance.CurrentLanguage.Texts);
            Localizer.Instance.SetLanguage(LanguageType.English);
            var english = new Dictionary<string, string>(Localizer.Instance.CurrentLanguage.Texts);
            Localizer.Instance.SetLanguage(LanguageType.Korean);

            CollectionAssert.IsEmpty(korean.Keys.Except(english.Keys), "English 에 없는 키");
            CollectionAssert.IsEmpty(english.Keys.Except(korean.Keys), "Korean 에 없는 키");
            CollectionAssert.IsEmpty(korean.Where(pair => string.IsNullOrEmpty(pair.Value)).Select(pair => pair.Key), "Korean 의 빈 텍스트");
            CollectionAssert.IsEmpty(english.Where(pair => string.IsNullOrEmpty(pair.Value)).Select(pair => pair.Key), "English 의 빈 텍스트");
        }
    }
}
