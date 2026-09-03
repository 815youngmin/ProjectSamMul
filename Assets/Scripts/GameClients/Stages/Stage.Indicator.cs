using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.IndicatorObjects;
using Z.Animations.Placeholder;

namespace Z.GameClients.Stages
{
    public partial class Stage
    {

        private void UpdateIndicator(float now, float deltaTime)
        {
#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("Stage.UpdateIndicator"))
#endif
            {
                foreach (var indicator in _indicatorsCreatedOnThisFrame)
                {
                    _aliveIndicators.Add(indicator);
                }
                _indicatorsCreatedOnThisFrame.Clear();

                this.DoRemoveReservedIndicators();

                foreach (var indicator in _aliveIndicators)
                {
                    if (indicator.IsAlive)
                    {
                        indicator.UpdateLogic(this, deltaTime);
                    }
                    else
                    {
                        _indicatorsToRemove.Add(indicator);
                    }
                }
                this.DoRemoveReservedIndicators();
            }
        }

        private void DoRemoveReservedIndicators()
        {
            foreach (var indicator in _indicatorsToRemove)
            {
                if(!_aliveIndicators.Remove(indicator))
                {
                    if (!_indicatorsCreatedOnThisFrame.Remove(indicator))
                    {
                        Debug.LogWarning($"인디케이터 이미 제거된 것 같은데요? {indicator.name}");
                        continue;
                    }
                }

                indicator.gameObject.SetActive(false);
                Debug.Assert(!_aliveIndicators.Contains(indicator));
                _indicatorPool.PutBack(indicator);
            }
            _indicatorsToRemove.Clear();
        }

        public SquareAttackRangeIndicator CreateSquareAttackRangeIndicator(Vector3 center, float width, float height, float angle, float duration)
        {
            var indicator = _indicatorPool.TakeOneFromPool<SquareAttackRangeIndicator>(IndicatorType.SquareAttackRange);
            indicator.Initialize(center, width, height, angle, duration);
            _indicatorsCreatedOnThisFrame.Add(indicator);
            indicator.gameObject.SetActive(true);
            return indicator;
        }

        public DashAttackRangeIndicator CreateDashAttackRangeIndicator(Character owner, Vector2 ownerOffset, Character target, float thickness, float distance, float duration)
        {
            var indicator = _indicatorPool.TakeOneFromPool<DashAttackRangeIndicator>(IndicatorType.DashAttackRange);
            indicator.Initialize(owner, ownerOffset, target, thickness, distance, duration);
            _indicatorsCreatedOnThisFrame.Add(indicator);
            indicator.gameObject.SetActive(true);
            return indicator;
        }

        public DashAttackRangeIndicator CreateDashAttackRangeIndicator(Character owner, Character target, float thickness, float distance, float duration)
        {
            return this.CreateDashAttackRangeIndicator(owner, Vector2.zero, target, thickness, distance, duration);
        }

        public DirectionalSquareRangeIndicator CreateDirectionalSquareRangeIndicator(Vector2 startPosition, Vector2 direction, float thickness, float distance, float duration)
        {
            var indicator = _indicatorPool.TakeOneFromPool<DirectionalSquareRangeIndicator>(IndicatorType.DirectionalSquareRange);
            indicator.Initialize(startPosition, direction, thickness, distance, duration);
            _indicatorsCreatedOnThisFrame.Add(indicator);
            indicator.gameObject.SetActive(true);
            return indicator;
        }

        public CircularAttackRangeIndicator CreateCircularAttackRangeIndicator(Vector2 position, float radius, float duration)
        {
            var indicator = _indicatorPool.TakeOneFromPool<CircularAttackRangeIndicator>(IndicatorType.CircularAttackRange);
            indicator.Initialize(position, radius, duration);
            _indicatorsCreatedOnThisFrame.Add(indicator);
            indicator.gameObject.SetActive(true);
            return indicator;
        }

        public BlinkCircularAttackRangeIndicator CreateBlinkCircularAttackRangeIndicator(Vector2 position, float radius, float duration)
        {
            var indicator = _indicatorPool.TakeOneFromPool<BlinkCircularAttackRangeIndicator>(IndicatorType.BlinkCircularAttackRange);
            indicator.Initialize(position, radius, duration);
            _indicatorsCreatedOnThisFrame.Add(indicator);
            indicator.gameObject.SetActive(true);
            return indicator;
        }

    }
}
