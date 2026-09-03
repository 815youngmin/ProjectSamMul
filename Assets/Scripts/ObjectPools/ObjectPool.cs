#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Z.ObjectPools
{
    /// <summary>
    /// 풀 오브젝트 생성 시 넘겨줄 초기화 데이터의 마커 인터페이스입니다.
    /// </summary>
    public interface IPoolObjectInitialDataBase { }

    /// <summary>
    /// 커스텀 타입 오브젝트를 키별로 풀링합니다.
    /// </summary>
    public class ObjectPool<TPoolKey, TValue>
        where TPoolKey : notnull
        where TValue : class, IPoolible<TPoolKey>
    {
        private Dictionary<TPoolKey, Stack<TValue>> _pools = new Dictionary<TPoolKey, Stack<TValue>>();
        private readonly Func<TPoolKey, IPoolObjectInitialDataBase?, TValue> _objectFactory;

        public ObjectPool(Func<TPoolKey, TValue> objectFactory)
        {
            _objectFactory = (key, _) => objectFactory(key);
        }

        public ObjectPool(Func<TPoolKey, IPoolObjectInitialDataBase?, TValue> objectFactory)
        {
            _objectFactory = objectFactory;
        }

        /// <summary>
        /// 풀에 여분이 있으면 꺼내고, 없으면 팩토리로 새로 만들어 리턴합니다.
        /// </summary>
        public TConcreteType TakeOne<TConcreteType>(TPoolKey key) where TConcreteType : class, TValue
        {
            return this.TakeOne<TConcreteType>(key, null);
        }

        public TConcreteType TakeOne<TConcreteType>(TPoolKey key, IPoolObjectInitialDataBase? initialData) where TConcreteType : class, TValue
        {
            var pool = this.GetPool(key);
            var element = pool.Count > 0 ? pool.Pop() : _objectFactory(key, initialData);

            var concrete = element as TConcreteType;
            Debug.Assert(concrete != null, $"풀 오브젝트 타입 불일치. Key[{key}] Expected[{typeof(TConcreteType).Name}]");
            return concrete!;
        }

        /// <summary>
        /// 풀에 <paramref name="count"/>개가 준비되도록 미리 생성해둡니다.
        /// </summary>
        public void Reserve(TPoolKey key, long count)
        {
            this.Reserve(key, null, count);
        }

        public void Reserve(TPoolKey key, IPoolObjectInitialDataBase? initialData, long count)
        {
            var pool = this.GetPool(key);
            while (pool.Count < count)
            {
                var newObject = _objectFactory(key, initialData);
                newObject.PuttingBackToPool();
                pool.Push(newObject);
            }
        }

        public void PutBack(TValue element)
        {
            element.PuttingBackToPool();

            var pool = this.GetPool(element.PoolKey);
            if (pool.Contains(element))
            {
                Debug.LogError($"{element}가 이미 풀에 있습니다. 중복 반환 요청을 무시합니다.");
                return;
            }
            pool.Push(element);
        }

        /// <summary>
        /// <paramref name="relatedScene"/>에서 생성된 오브젝트를 풀에서 빼내어 파괴합니다.
        /// </summary>
        public void DestroyAll(Scene relatedScene, Action<TValue> destroyFunction)
        {
            var survivors = new Dictionary<TPoolKey, Stack<TValue>>();
            foreach (var pair in _pools)
            {
                var survivorPool = new Stack<TValue>();
                while (pair.Value.Count > 0)
                {
                    var element = pair.Value.Pop();
                    if (element.RelatedScene == relatedScene)
                    {
                        destroyFunction(element);
                    }
                    else
                    {
                        survivorPool.Push(element);
                    }
                }

                if (survivorPool.Count > 0)
                {
                    survivors.Add(pair.Key, survivorPool);
                }
            }
            _pools = survivors;
        }

        private Stack<TValue> GetPool(TPoolKey key)
        {
            if (!_pools.TryGetValue(key, out var pool))
            {
                pool = new Stack<TValue>();
                _pools.Add(key, pool);
            }
            return pool;
        }
    }
}
