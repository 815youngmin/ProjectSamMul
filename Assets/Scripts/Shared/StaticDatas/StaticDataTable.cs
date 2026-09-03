#nullable enable
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Shared.StaticDatas
{
    /// <summary>Returns the JSON text of one static-data table, or null when the table does not exist.</summary>
    public delegate string? StaticDataTableReader(string tableName);

    internal static class StaticDataTable
    {
        /// <summary>Deserializes a table stored as a JSON array of rows. A missing or empty table yields no rows.</summary>
        public static List<T> Load<T>(StaticDataTableReader reader, string tableName)
        {
            var json = reader(tableName);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<T>();
            }

            return JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>();
        }
    }
}
