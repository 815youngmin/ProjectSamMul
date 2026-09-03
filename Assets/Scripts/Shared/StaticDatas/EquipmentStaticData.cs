#nullable enable
using System.Collections.Generic;
using Newtonsoft.Json;
using Shared.GameDataTypes;
using Shared.Localizers;

namespace Shared.StaticDatas
{
    /// <summary>Equipment definition. Table "Equipments".</summary>
    public class EquipmentStaticData
    {
        public EquipmentId Id { get; set; } = EquipmentId.Invalid;
        public EquipmentSetType SetType { get; set; }
        public EquipmentSlot Slot { get; set; }
        public Rarity Rarity { get; set; }
        public string NameKey { get; set; } = "";
        public string DescriptionKey { get; set; } = "";
        [JsonIgnore] public string Name => Localizer.Instance.GetText(NameKey);
        [JsonIgnore] public string Description => Localizer.Instance.GetText(DescriptionKey);
        public string IconPath { get; set; } = "";
    }

    /// <summary>Stat an equipment gives per slot, rarity and grade; grows by IncrementalValue per level above 1. Table "EquipmentStats".</summary>
    public class EquipmentStatStaticData
    {
        public EquipmentSlot EquipmentSlot { get; set; }
        public Rarity Rarity { get; set; }
        public Grade Grade { get; set; }
        public StatType StatType { get; set; }
        public float DefaultValue { get; set; }
        public float IncrementalValue { get; set; }
    }

    /// <summary>Effect an equipment gains at a grade. Table "EquipmentGradeEffects".</summary>
    public class EquipmentGradeEffectStaticData
    {
        public EquipmentId EquipmentId { get; set; } = EquipmentId.Invalid;
        public Grade EquipmentGrade { get; set; }
        public GradeEffectType GradeEffectType { get; set; }
        public float Parameter1 { get; set; }
        public float Parameter2 { get; set; }
    }

    public class EquipmentStaticDataRepository
    {
        private static readonly IReadOnlyDictionary<Grade, EquipmentGradeEffectStaticData> s_noGradeEffects = new Dictionary<Grade, EquipmentGradeEffectStaticData>();

        private readonly Dictionary<EquipmentId, EquipmentStaticData> _equipments = new Dictionary<EquipmentId, EquipmentStaticData>();
        private readonly Dictionary<(EquipmentSlot, Rarity, Grade), EquipmentStatStaticData> _stats = new Dictionary<(EquipmentSlot, Rarity, Grade), EquipmentStatStaticData>();
        private readonly Dictionary<EquipmentId, Dictionary<Grade, EquipmentGradeEffectStaticData>> _gradeEffects = new Dictionary<EquipmentId, Dictionary<Grade, EquipmentGradeEffectStaticData>>();

        public EquipmentStaticDataRepository(
            IReadOnlyList<EquipmentStaticData> equipments,
            IReadOnlyList<EquipmentStatStaticData> stats,
            IReadOnlyList<EquipmentGradeEffectStaticData> gradeEffects)
        {
            foreach (var equipment in equipments)
            {
                if (_equipments.ContainsKey(equipment.Id))
                {
                    throw new StaticDataValidationError($"Duplicate equipment {equipment.Id}.");
                }
                _equipments.Add(equipment.Id, equipment);
            }
            foreach (var stat in stats)
            {
                _stats[(stat.EquipmentSlot, stat.Rarity, stat.Grade)] = stat;
            }
            foreach (var effect in gradeEffects)
            {
                if (!_gradeEffects.TryGetValue(effect.EquipmentId, out var byGrade))
                {
                    byGrade = new Dictionary<Grade, EquipmentGradeEffectStaticData>();
                    _gradeEffects.Add(effect.EquipmentId, byGrade);
                }
                byGrade[effect.EquipmentGrade] = effect;
            }
        }

        public EquipmentStaticData Get(EquipmentId id)
            => _equipments.TryGetValue(id, out var equipment) ? equipment : throw new StaticDataValidationError($"Equipment {id} is not defined.");

        public EquipmentStatStaticData GetEquipmentStat(EquipmentSlot slot, Rarity rarity, Grade grade)
            => _stats.TryGetValue((slot, rarity, grade), out var stat) ? stat : throw new StaticDataValidationError($"Equipment stat for {slot}/{rarity}/{grade} is not defined.");

        public IReadOnlyDictionary<Grade, EquipmentGradeEffectStaticData> GetEquipmentGradeEffects(EquipmentId equipmentId)
            => _gradeEffects.TryGetValue(equipmentId, out var effects) ? effects : s_noGradeEffects;
    }
}
