#nullable enable
using System;

namespace Shared.StaticDatas
{
    /// <summary>
    /// Root of all static-data tables. Each table is a JSON array of the row class it exposes
    /// (table names are the strings passed below), read through the <see cref="StaticDataTableReader"/> given to <see cref="Initialize"/>.
    /// </summary>
    public class StaticDataRepository
    {
        private static StaticDataRepository? s_instance;

        public static StaticDataRepository Instance => s_instance ?? throw new InvalidOperationException("StaticDataRepository is not initialized.");
        public static bool IsInitialized => s_instance != null;

        public StageStaticDataRepository Stages { get; }
        public ChapterStaticDataRepository Chapters { get; }
        public MonsterStaticDataRepository Monsters { get; }
        public MonsterImagePathStaticDataRepository MonsterImagePaths { get; }
        public HeroStaticDataRepository Heroes { get; }
        public CharacterImagePathStaticDataRepository CharacterImagePaths { get; }
        public SkillStaticDataRepository Skills { get; }
        public EquipmentStaticDataRepository Equipments { get; }
        public BasicEvolutionStaticDataRepository BasicEvolutions { get; }
        public SpecialEvolutionStaticDataRepository SpecialEvolutions { get; }
        public ExpTable ExpTable { get; }
        public AccountLevelStaticDataRepository AccountLevels { get; }

        public static void Initialize(StaticDataTableReader reader)
        {
            if (s_instance != null)
            {
                return;
            }

            s_instance = new StaticDataRepository(reader);
        }

        public static void ForceReInitialize(StaticDataTableReader reader)
        {
            s_instance = new StaticDataRepository(reader);
        }

        private StaticDataRepository(StaticDataTableReader reader)
        {
            Stages = new StageStaticDataRepository(
                StaticDataTable.Load<StageStaticData>(reader, "Stages"),
                StaticDataTable.Load<StageEventStaticData>(reader, "StageEvents"));
            Chapters = new ChapterStaticDataRepository(StaticDataTable.Load<ChapterStaticData>(reader, "Chapters"), Stages);
            Monsters = new MonsterStaticDataRepository(StaticDataTable.Load<MonsterStaticData>(reader, "Monsters"));
            MonsterImagePaths = new MonsterImagePathStaticDataRepository(StaticDataTable.Load<MonsterImagePathStaticData>(reader, "MonsterImagePaths"));
            Heroes = new HeroStaticDataRepository(
                StaticDataTable.Load<HeroStaticData>(reader, "Heroes"),
                StaticDataTable.Load<HeroStatStaticData>(reader, "HeroStats"),
                StaticDataTable.Load<HeroGradeEffectStaticData>(reader, "HeroGradeEffects"));
            CharacterImagePaths = new CharacterImagePathStaticDataRepository(StaticDataTable.Load<CharacterImagePathStaticData>(reader, "CharacterImagePaths"));
            Skills = new SkillStaticDataRepository(
                StaticDataTable.Load<SkillStaticData>(reader, "Skills"),
                StaticDataTable.Load<SkillLockStaticData>(reader, "SkillLocks"),
                Heroes);
            Equipments = new EquipmentStaticDataRepository(
                StaticDataTable.Load<EquipmentStaticData>(reader, "Equipments"),
                StaticDataTable.Load<EquipmentStatStaticData>(reader, "EquipmentStats"),
                StaticDataTable.Load<EquipmentGradeEffectStaticData>(reader, "EquipmentGradeEffects"));
            BasicEvolutions = new BasicEvolutionStaticDataRepository(StaticDataTable.Load<BasicEvolutionStaticData>(reader, "BasicEvolutions"));
            SpecialEvolutions = new SpecialEvolutionStaticDataRepository(StaticDataTable.Load<SpecialEvolutionStaticData>(reader, "SpecialEvolutions"));
            ExpTable = new ExpTable(StaticDataTable.Load<ExpTableRow>(reader, "Exps"));
            AccountLevels = new AccountLevelStaticDataRepository(StaticDataTable.Load<AccountLevelStaticData>(reader, "AccountLevels"));
        }
    }
}
