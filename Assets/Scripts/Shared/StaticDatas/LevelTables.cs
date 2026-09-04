#nullable enable
using System.Collections.Generic;

namespace Shared.StaticDatas
{
    /// <summary>Row of table "Exps": experience needed to go from Level to Level + 1 inside a stage.</summary>
    public class ExpTableRow
    {
        public int Level { get; set; }
        public long ExpForLevelUp { get; set; }
    }

    /// <summary>In-stage character level table. Levels start at 1 and must be contiguous.</summary>
    public class ExpTable
    {
        private readonly Dictionary<int, long> _expForLevelUp = new Dictionary<int, long>();

        public int MaxLevel { get; }

        public ExpTable(IReadOnlyList<ExpTableRow> rows)
        {
            int lastLevel = 0;
            foreach (var row in rows)
            {
                if (row.Level != lastLevel + 1)
                {
                    throw new StaticDataValidationError($"Exp table level {lastLevel + 1} is missing (found {row.Level}).");
                }
                _expForLevelUp.Add(row.Level, row.ExpForLevelUp);
                lastLevel = row.Level;
            }
            MaxLevel = lastLevel;
        }

        /// <summary>Experience needed to leave the given level; long.MaxValue at or above MaxLevel.</summary>
        public long GetExpForLevelUp(int currentLevel)
            => currentLevel >= MaxLevel ? long.MaxValue : _expForLevelUp[currentLevel];
    }
}
