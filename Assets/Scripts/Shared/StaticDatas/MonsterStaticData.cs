#nullable enable
using System.Collections.Generic;
using Newtonsoft.Json;
using Shared.GameDataTypes;
using Shared.Localizers;

namespace Shared.StaticDatas
{
    /// <summary>Monster body and base stats. Table "Monsters".</summary>
    public class MonsterStaticData
    {
        public CharacterType MonsterType { get; set; }
        public MonsterAIType AIType { get; set; }
        public string MonsterNameKey { get; set; } = "";
        [JsonIgnore] public string MonsterName => Localizer.Instance.GetText(MonsterNameKey);

        public string SkeletonDataPath { get; set; } = "";
        public float ColliderRadius { get; set; }
        public float[] HitBoxSize { get; set; } = new float[2];
        public float[] HitBoxOffset { get; set; } = new float[2];
        public float BodyPixelYOffset { get; set; }
        public float ShadowSize { get; set; }

        public float MaxHP { get; set; }
        public float CollisionAttackPower { get; set; }
        public float CollisionAttackSpeed { get; set; }
        public float MoveSpeed { get; set; }
        public float Mass { get; set; }
        public float Drag { get; set; }
        public float KnockBackResistance { get; set; }

        public float SpecialAttack1DamageRate { get; set; }
        public float SpecialAttack1AttackSpeed { get; set; }
        public float SpecialAttack1EffectiveRange { get; set; }
        public string SpecialAttack1ResourcePath { get; set; } = "";

        /// <summary>Monster spawned by summoner-type AIs.</summary>
        public CharacterType SpawnMonsterType { get; set; }

        // AI-specific tuning values; meaning depends on AIType.
        public float Param1 { get; set; }
        public float Param2 { get; set; }
        public float Param3 { get; set; }
    }

    public class MonsterStaticDataRepository
    {
        private readonly Dictionary<CharacterType, MonsterStaticData> _monsters = new Dictionary<CharacterType, MonsterStaticData>();

        public IReadOnlyDictionary<CharacterType, MonsterStaticData> Monsters => _monsters;

        public MonsterStaticDataRepository(IReadOnlyList<MonsterStaticData> rows)
        {
            foreach (var row in rows)
            {
                if (_monsters.ContainsKey(row.MonsterType))
                {
                    throw new StaticDataValidationError($"Duplicate monster {row.MonsterType}.");
                }
                _monsters.Add(row.MonsterType, row);
            }
        }

        public MonsterStaticData Get(CharacterType monsterType)
            => Find(monsterType) ?? throw new StaticDataValidationError($"Monster {monsterType} is not defined.");

        public MonsterStaticData? Find(CharacterType monsterType)
            => _monsters.TryGetValue(monsterType, out var monster) ? monster : null;
    }

    /// <summary>Illustration shown in boss intro popups. Table "MonsterImagePaths".</summary>
    public class MonsterImagePathStaticData
    {
        public CharacterType CharacterType { get; set; }
        public string IllustImagePath { get; set; } = "";
    }

    public class MonsterImagePathStaticDataRepository
    {
        private readonly Dictionary<CharacterType, MonsterImagePathStaticData> _paths = new Dictionary<CharacterType, MonsterImagePathStaticData>();

        public IReadOnlyDictionary<CharacterType, MonsterImagePathStaticData> MonsterImagePathStaticDatas => _paths;

        public MonsterImagePathStaticDataRepository(IReadOnlyList<MonsterImagePathStaticData> rows)
        {
            foreach (var row in rows)
            {
                _paths[row.CharacterType] = row;
            }
        }

        public MonsterImagePathStaticData Get(CharacterType characterType)
            => _paths.TryGetValue(characterType, out var path) ? path : throw new StaticDataValidationError($"Monster image path for {characterType} is not defined.");
    }
}
