#nullable enable
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Shared.DataTables;
using Shared.GameDataTypes;
using Shared.Localizers;

namespace Shared.StaticDatas
{
    /// <summary>One level of one skill. Table "Skills" (one row per skill id and level, levels starting at 1).</summary>
    public class SkillStaticData
    {
        public SkillId Id { get; set; } = SkillId.Invalid;
        public SkillType skillType { get; set; }
        public int Level { get; set; }

        public string NameKey { get; set; } = "";
        public string DescriptionKey { get; set; } = "";
        [JsonIgnore] public string Name => Localizer.Instance.GetText(NameKey);
        [JsonIgnore] public string Description => Localizer.Instance.GetText(DescriptionKey);

        /// <summary>Hero this skill is exclusive to; null when every hero can use it.</summary>
        public HeroType? DesignatedHero { get; set; }
        /// <summary>Hero whose basic skill this is (derived from the Heroes table); null otherwise.</summary>
        [JsonIgnore] public HeroType? HeroBasicSkillOwner { get; internal set; }
        [JsonIgnore] public bool IsHeroBasicSkill => HeroBasicSkillOwner != null;

        public float Duration { get; set; }
        public float Cooltime { get; set; }

        /// <summary>Skill that must be owned to transcend this one at max level.</summary>
        public SkillId TranscendCondition { get; set; } = SkillId.Invalid;
        [JsonIgnore] public bool HasTranscendCondition => TranscendCondition != SkillId.Invalid;
        /// <summary>Skills whose transcendence requires this skill (derived).</summary>
        [JsonIgnore] public IReadOnlyList<SkillId> TranscendTargets { get; internal set; } = new List<SkillId>();

        public float Parameter1 { get; set; }
        public float Parameter2 { get; set; }
        public float Parameter3 { get; set; }
        public float Parameter4 { get; set; }
        public float Parameter5 { get; set; }
        public float Parameter6 { get; set; }
        /// <summary>Monster spawned by summon-type skills.</summary>
        public CharacterType CharacterType { get; set; }

        public string IconResourcePath { get; set; } = "";
        public string SkillSFXPath { get; set; } = "";
        public string SpawnedObjectSFXPath { get; set; } = "";
        public string SkillHitSFXPath { get; set; } = "";
    }

    /// <summary>Chapter a skill becomes available at. Table "SkillLocks". Skills without a row are always available.</summary>
    public class SkillLockStaticData
    {
        public SkillId SkillId { get; set; } = SkillId.Invalid;
        public int RequiredChapter { get; set; }

        public bool IsUnlocked(int clearedHighestChapter) => clearedHighestChapter >= RequiredChapter;
    }

    public class SkillStaticDataRepository
    {
        private readonly Dictionary<SkillKey, SkillStaticData> _skills = new Dictionary<SkillKey, SkillStaticData>();
        private readonly Dictionary<SkillId, IReadOnlyList<SkillStaticData>> _skillsById = new Dictionary<SkillId, IReadOnlyList<SkillStaticData>>();
        private readonly Dictionary<SkillId, SkillLockStaticData> _skillLocks = new Dictionary<SkillId, SkillLockStaticData>();

        public IReadOnlyDictionary<SkillKey, SkillStaticData> Skills => _skills;
        public IReadOnlyDictionary<SkillId, SkillLockStaticData> SkillLockStaticDatas => _skillLocks;

        public SkillStaticDataRepository(IReadOnlyList<SkillStaticData> rows, IReadOnlyList<SkillLockStaticData> locks, HeroStaticDataRepository heroes)
        {
            var byId = new Dictionary<SkillId, List<SkillStaticData>>();
            foreach (var row in rows)
            {
                var key = new SkillKey(row.Id, row.Level);
                if (_skills.ContainsKey(key))
                {
                    throw new StaticDataValidationError($"Duplicate skill {row.Id} level {row.Level}.");
                }
                _skills.Add(key, row);
                if (!byId.TryGetValue(row.Id, out var list))
                {
                    list = new List<SkillStaticData>();
                    byId.Add(row.Id, list);
                }
                list.Add(row);
            }

            foreach (var pair in byId)
            {
                pair.Value.Sort((lhs, rhs) => lhs.Level.CompareTo(rhs.Level));
                for (int i = 0; i < pair.Value.Count; ++i)
                {
                    if (pair.Value[i].Level != i + 1)
                    {
                        throw new StaticDataValidationError($"Skill {pair.Key} is missing level {i + 1}.");
                    }
                }
                _skillsById.Add(pair.Key, pair.Value);
            }

            var basicSkillOwners = heroes.HeroStaticDatas.Values
                .Where(hero => hero.BasicSkill != SkillId.Invalid)
                .ToDictionary(hero => hero.BasicSkill, hero => hero.HeroType);
            var transcendTargets = new Dictionary<SkillId, List<SkillId>>();
            foreach (var skill in rows)
            {
                if (basicSkillOwners.TryGetValue(skill.Id, out var owner))
                {
                    skill.HeroBasicSkillOwner = owner;
                }
                if (skill.Level == GameConstants.SKILL_MAX_LEVEL && skill.HasTranscendCondition)
                {
                    if (!transcendTargets.TryGetValue(skill.TranscendCondition, out var targets))
                    {
                        targets = new List<SkillId>();
                        transcendTargets.Add(skill.TranscendCondition, targets);
                    }
                    targets.Add(skill.Id);
                }
            }
            foreach (var skill in rows)
            {
                if (transcendTargets.TryGetValue(skill.Id, out var targets))
                {
                    skill.TranscendTargets = targets;
                }
            }

            foreach (var skillLock in locks)
            {
                _skillLocks[skillLock.SkillId] = skillLock;
            }
        }

        public SkillStaticData Get(SkillKey key)
            => _skills.TryGetValue(key, out var skill) ? skill : throw new StaticDataValidationError($"Skill {key.Id} level {key.Level} is not defined.");

        /// <summary>Every level of a skill, ordered by level.</summary>
        public IReadOnlyList<SkillStaticData> GetSkills(SkillId id)
            => _skillsById.TryGetValue(id, out var skills) ? skills : throw new StaticDataValidationError($"Skill {id} is not defined.");

        /// <summary>The season deck minus the skills still locked for the given progress.</summary>
        public IReadOnlyList<SkillId> GetUserSkillDeck(IReadOnlyList<SkillId> seasonSkillDeck, int clearedHighestChapter)
        {
            var deck = new List<SkillId>(seasonSkillDeck.Count);
            foreach (var skillId in seasonSkillDeck)
            {
                if (!_skillLocks.TryGetValue(skillId, out var skillLock) || skillLock.IsUnlocked(clearedHighestChapter))
                {
                    deck.Add(skillId);
                }
            }
            return deck;
        }
    }
}
