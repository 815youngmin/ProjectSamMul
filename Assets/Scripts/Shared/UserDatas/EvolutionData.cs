#nullable enable
using Newtonsoft.Json;

namespace Shared.UserDatas
{
    /// <summary>Highest researched evolution step of each tree (0 = nothing researched).</summary>
    public readonly struct EvolutionData
    {
        public readonly int HighestBasicEvolutionID;
        public readonly int HighestSpecialEvolutionID;

        [JsonConstructor]
        public EvolutionData(int highestBasicEvolutionID, int highestSpecialEvolutionID)
        {
            HighestBasicEvolutionID = highestBasicEvolutionID;
            HighestSpecialEvolutionID = highestSpecialEvolutionID;
        }
    }
}
