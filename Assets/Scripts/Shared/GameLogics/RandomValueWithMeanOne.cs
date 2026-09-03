#nullable enable
using System;

namespace Shared.GameLogics
{
    /// <summary>
    /// Random drop multiplier whose expected value is 1: returns 0 with probability zeroRatio, otherwise a uniform value
    /// in [min, min * minMaxRatio] where min is chosen so that the overall mean is 1.
    /// </summary>
    public class RandomValueWithMeanOne
    {
        private readonly Random _random = new Random();
        private readonly float _zeroRatio;
        private readonly float _min;
        private readonly float _max;

        public RandomValueWithMeanOne(float zeroRatio, float minMaxRatio)
        {
            _zeroRatio = zeroRatio;
            _min = 2f / ((1f - zeroRatio) * (1f + minMaxRatio));
            _max = _min * minMaxRatio;
        }

        public float Generate()
        {
            if (_random.NextDouble() < _zeroRatio)
            {
                return 0f;
            }
            return _min + (float)_random.NextDouble() * (_max - _min);
        }
    }
}
