#nullable enable
using System;

namespace SamMul.GameClients.Stages.StageEvents
{
    /// <summary>
    /// One scheduled call (Begin / Update tick / End) of a stage event. Sorted by execution time, then by the
    /// event's priority index, then by call type.
    /// </summary>
    public class StageEventExecutionInfo : IComparable<StageEventExecutionInfo>
    {
        public enum StageEventCallType
        {
            Begin,
            Update,
            End,
        }

        private readonly int _priorityIndex;
        public readonly StageEventCallType CallType;
        public readonly float ExecutingAt;          // stage running time at which this call fires
        public readonly StageEventBase StageEvent;
        public readonly int StageEventTickNumber;   // only meaningful for Update calls

        public StageEventExecutionInfo(StageEventBase stageEvent, int priorityIndex, float time, StageEventCallType callType, int stageEventTickNumber)
        {
            _priorityIndex = priorityIndex;
            this.CallType = callType;
            this.ExecutingAt = time;
            this.StageEvent = stageEvent;
            this.StageEventTickNumber = stageEventTickNumber;
        }

        public int CompareTo(StageEventExecutionInfo? other)
        {
            if (other == null)
            {
                return 1;
            }

            int result = this.ExecutingAt.CompareTo(other.ExecutingAt);
            if (result == 0)
            {
                result = _priorityIndex.CompareTo(other._priorityIndex);
            }
            if (result == 0)
            {
                result = this.CallType.CompareTo(other.CallType);
            }
            return result;
        }
    }
}
