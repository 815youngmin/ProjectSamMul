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

    /// <summary>Row of table "AccountLevels": experience needed to go from Level to Level + 1.</summary>
    public class AccountLevelStaticData
    {
        public int Level { get; set; }
        public long ExpForLevelUp { get; set; }
    }

    public class AccountLevelStaticDataRepository
    {
        private readonly Dictionary<int, AccountLevelStaticData> _levels = new Dictionary<int, AccountLevelStaticData>();

        public int MaxLevel { get; }

        public AccountLevelStaticDataRepository(IReadOnlyList<AccountLevelStaticData> rows)
        {
            int lastLevel = 0;
            foreach (var row in rows)
            {
                if (row.Level != lastLevel + 1)
                {
                    throw new StaticDataValidationError($"Account level {lastLevel + 1} is missing (found {row.Level}).");
                }
                _levels.Add(row.Level, row);
                lastLevel = row.Level;
            }
            MaxLevel = lastLevel;
        }

        public AccountLevelStaticData Get(int level)
            => _levels.TryGetValue(level, out var data) ? data : throw new StaticDataValidationError($"Account level {level} is not defined.");

        /// <summary>Experience needed to leave the given level; long.MaxValue at or above MaxLevel.</summary>
        public long GetExpForLevelUp(int currentLevel)
            => currentLevel >= MaxLevel ? long.MaxValue : _levels[currentLevel].ExpForLevelUp;
    }
}
