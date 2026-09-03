namespace SamMul.GameClients.Stages.Characters.Stats
{
    public enum StatModType
    {
        Flat = 100,
        PercentAdd = 200,
        PercentMult = 300,
    }

    public class StatModifier
    {
        public float Value => _value;
        public readonly StatModType Type;
        public readonly int Order;
        public readonly object Source;

        private float _value;

        public StatModifier(float value, StatModType type, int order, object source)
        {
            _value = value;
            Type = type;
            Order = order;
            Source = source;
        }

        public StatModifier(float value, StatModType type) : this(value, type, (int)type, null) { }

        public StatModifier(float value, StatModType type, int order) : this(value, type, order, null) { }

        public StatModifier(float value, StatModType type, object source) : this(value, type, (int)type, source) { }

        /// <summary>
        /// StatModifier 인스턴스를 재활용할 때 초기화하기 위해 제공합니다.
        /// 이미 사용중인 StatModifier에서 호출해서는 안 됩니다. 이미 사용중인 곳에서 value를 변경할 경우, 변경사항이 올바로 전파되지 않습니다.
        /// </summary>
        public void ReInitializeValueForReusingStatModifier(float value)
        {
            _value = value;
        }

        public override string ToString()
        {
            return $"Type({Type.ToString()}), Value({Value.ToString("N2")}), Source({Source?.ToString()})";
        }
    }
}
