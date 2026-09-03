using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using SamMul.ObjectPools;

namespace SamMul.GameClients.Stages.IndicatorObjects
{
    public class IndicatorPool
    {
        private readonly ObjectPool<IndicatorType, IndicatorObjectBase> _pool;

        public IndicatorPool()
        {
            _pool = new ObjectPool<IndicatorType, IndicatorObjectBase>(objectFactory: AllocateObject);
        }

        // 씬이 정리될 때 호출된다.
        // 씬을 넘어갈 때 유효하지 않은 Pooling Object들을 모두 Destroy해준다. (prefab은 남김)
        public void ClearBeforeChangingScene(Scene relatedScene)
        {
            _pool.DestroyAll(relatedScene, destroyFunction: (IndicatorObjectBase element) =>
            {
                GameObject.Destroy(element.gameObject);
            });
        }
        public TIndicatorObject TakeOneFromPool<TIndicatorObject>(IndicatorType key) where TIndicatorObject : IndicatorObjectBase
        {
            var projectile = this._pool.TakeOne<TIndicatorObject>(key);
            return projectile;
        }

        public void PutBack(IndicatorObjectBase indicatorObject)
        {
            _pool.PutBack(indicatorObject);
        }

        private IndicatorObjectBase AllocateObject(IndicatorType indicatorType)
        {
            var indicatorObject = new GameObject(indicatorType.ToString());

            switch (indicatorType)
            {
                case IndicatorType.SquareAttackRange:
                    {
                        var indicator = indicatorObject.AddComponent<SquareAttackRangeIndicator>();
                        indicator.AllocateSharedResources();
                        return indicator;
                    }
                case IndicatorType.DashAttackRange:
                    {
                        var indicator = indicatorObject.AddComponent<DashAttackRangeIndicator>();
                        indicator.AllocateSharedResources();
                        return indicator;
                    }
                case IndicatorType.DirectionalSquareRange:
                    {
                        var indicator = indicatorObject.AddComponent<DirectionalSquareRangeIndicator>();
                        indicator.AllocateSharedResources();
                        return indicator;
                    }
                case IndicatorType.CircularAttackRange:
                    {
                        var indicator = indicatorObject.AddComponent<CircularAttackRangeIndicator>();
                        indicator.AllocateSharedResources();
                        return indicator;
                    }
                case IndicatorType.BoneToTarget:
                    {
                        var indicator = indicatorObject.AddComponent<BoneToTargetIndicator>();
                        indicator.AllocateSharedResources();
                        return indicator;
                    }
                case IndicatorType.BlinkCircularAttackRange:
                    {
                        var indicator = indicatorObject.AddComponent<BlinkCircularAttackRangeIndicator>();
                        indicator.AllocateSharedResources();
                        return indicator;
                    }
                default:
                    throw new NotImplementedException($"{indicatorType} 구현 안됨. 구현해주세요.");
            }
        }
    }
}

