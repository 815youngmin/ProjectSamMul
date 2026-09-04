#nullable enable
using System.Collections.Generic;
using Newtonsoft.Json;
using Shared.GameDataTypes;
using Shared.Localizers;

namespace Shared.StaticDatas
{
    /// <summary>Playable hero. Table "Heroes".</summary>
    public class HeroStaticData
    {
        public HeroType HeroType { get; set; }
        public string HeroNameKey { get; set; } = "";
        [JsonIgnore] public string HeroName => Localizer.Instance.GetText(HeroNameKey);
        public Rarity Rarity { get; set; }
        public ElementType ElementType { get; set; }
        public SkillId BasicSkill { get; set; } = SkillId.Invalid;
        public string SkeletonDataPath { get; set; } = "";

        public float ColliderRadius { get; set; }
        public float[] HitBoxSize { get; set; } = new float[2];
        public float[] HitBoxOffset { get; set; } = new float[2];
        public float MaxHP { get; set; }
        public float AttackPower { get; set; }
        public float MoveSpeed { get; set; }
    }

    /// <summary>Base stat per rarity and grade; the value grows by IncrementalValue per level above 1. Table "HeroStats".</summary>
    public class HeroStatStaticData
    {
        public Rarity Rarity { get; set; }
        public Grade Grade { get; set; }
        public float AttackPowerDefaultValue { get; set; }
        public float AttackPowerIncrementalValue { get; set; }
        public float MaxHpDefaultValue { get; set; }
        public float MaxHpIncrementalValue { get; set; }
    }

    /// <summary>Effect a hero gains at a grade. Table "HeroGradeEffects".</summary>
    public class HeroGradeEffectStaticData
    {
        public HeroType HeroType { get; set; }
        public Grade Grade { get; set; }
        public GradeEffectType GradeEffectType { get; set; }
        public float Parameter1 { get; set; }
        public float Parameter2 { get; set; }
    }

    public class HeroStaticDataRepository
    {
        private static readonly IReadOnlyDictionary<Grade, HeroGradeEffectStaticData> s_noGradeEffects = new Dictionary<Grade, HeroGradeEffectStaticData>();

        private readonly Dictionary<HeroType, HeroStaticData> _heroes = new Dictionary<HeroType, HeroStaticData>();
        private readonly Dictionary<(Rarity, Grade), HeroStatStaticData> _stats = new Dictionary<(Rarity, Grade), HeroStatStaticData>();
        private readonly Dictionary<HeroType, Dictionary<Grade, HeroGradeEffectStaticData>> _gradeEffects = new Dictionary<HeroType, Dictionary<Grade, HeroGradeEffectStaticData>>();

        public IReadOnlyDictionary<HeroType, HeroStaticData> HeroStaticDatas => _heroes;
        /// <summary>테이블 순서.</summary>
        public IReadOnlyList<HeroStaticData> All { get; }

        public HeroStaticDataRepository(
            IReadOnlyList<HeroStaticData> heroes,
            IReadOnlyList<HeroStatStaticData> stats,
            IReadOnlyList<HeroGradeEffectStaticData> gradeEffects)
        {
            All = heroes;
            foreach (var hero in heroes)
            {
                if (_heroes.ContainsKey(hero.HeroType))
                {
                    throw new StaticDataValidationError($"Duplicate hero {hero.HeroType}.");
                }
                _heroes.Add(hero.HeroType, hero);
            }
            foreach (var stat in stats)
            {
                _stats[(stat.Rarity, stat.Grade)] = stat;
            }
            foreach (var effect in gradeEffects)
            {
                if (!_gradeEffects.TryGetValue(effect.HeroType, out var byGrade))
                {
                    byGrade = new Dictionary<Grade, HeroGradeEffectStaticData>();
                    _gradeEffects.Add(effect.HeroType, byGrade);
                }
                byGrade[effect.Grade] = effect;
            }
        }

        public HeroStaticData Get(HeroType heroType)
            => _heroes.TryGetValue(heroType, out var hero) ? hero : throw new StaticDataValidationError($"Hero {heroType} is not defined.");

        public HeroStatStaticData GetHeroStat(Rarity rarity, Grade grade)
            => _stats.TryGetValue((rarity, grade), out var stat) ? stat : throw new StaticDataValidationError($"Hero stat for {rarity}/{grade} is not defined.");

        public IReadOnlyDictionary<Grade, HeroGradeEffectStaticData> GetHeroGradeEffects(HeroType heroType)
            => _gradeEffects.TryGetValue(heroType, out var effects) ? effects : s_noGradeEffects;
    }

    /// <summary>UI image paths per hero. Table "CharacterImagePaths".</summary>
    public class CharacterImagePathStaticData
    {
        public HeroType HeroType { get; set; }
        public string DatachipIconPath { get; set; } = "";
        public string SkillSelectorTitleStickerGroupPath { get; set; } = "";
        public string stageExpHudPath { get; set; } = "";
        public List<string> SkillSelectorBackParticleImagePaths { get; set; } = new List<string>();
    }

    public class CharacterImagePathStaticDataRepository
    {
        private readonly Dictionary<HeroType, CharacterImagePathStaticData> _paths = new Dictionary<HeroType, CharacterImagePathStaticData>();

        public IReadOnlyDictionary<HeroType, CharacterImagePathStaticData> CharacterImagePathStaticDatas => _paths;

        public CharacterImagePathStaticDataRepository(IReadOnlyList<CharacterImagePathStaticData> rows)
        {
            foreach (var row in rows)
            {
                _paths[row.HeroType] = row;
            }
        }

        public CharacterImagePathStaticData Get(HeroType heroType)
            => _paths.TryGetValue(heroType, out var path) ? path : throw new StaticDataValidationError($"Character image path for {heroType} is not defined.");
    }
}
