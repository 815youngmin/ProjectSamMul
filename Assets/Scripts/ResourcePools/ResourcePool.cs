using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Z.ResourcePools
{
    /// <summary>
    /// 리소스 로드/인스턴스 풀링을 담당하는 전역 객체입니다.
    ///
    /// 경로 규칙 :
    /// 호출 측은 "Stages/Projectiles/DummyProjectileBody.prefab" 처럼 확장자가 붙은 경로를 넘깁니다.
    /// 이 구현은 확장자를 떼어낸 뒤 Assets/Resources/ 아래의 같은 상대 경로를 Resources.Load 합니다.
    /// 예) "Stages/Projectiles/DummyProjectileBody.prefab" -> Resources/Stages/Projectiles/DummyProjectileBody
    /// </summary>
    public class ResourcePool
    {
        private static ResourcePool s_instance = null;
        public static ResourcePool Instance
        {
            get
            {
                if (s_instance == null)
                {
                    throw new InvalidOperationException($"{nameof(ResourcePool)}이 초기화되기 전에 접근했습니다.");
                }
                return s_instance;
            }
        }

        // Key : (리소스 타입, 원본 경로). Resources.Load 결과를 캐싱한다.
        private readonly Dictionary<(Type, string), UnityEngine.Object> _loadedResources = new Dictionary<(Type, string), UnityEngine.Object>();
        // Key : 원본 경로. 씬에 Instantiate 한 뒤 반환된 인스턴스를 풀링한다.
        private readonly Dictionary<string, Stack<GameObject>> _instancePools = new Dictionary<string, Stack<GameObject>>();

        public static void Initialize()
        {
            if (s_instance != null)
            {
                Debug.LogWarning($"{nameof(ResourcePool)} 두 번 초기화 시도됨. 무시합니다.");
                return;
            }
            s_instance = new ResourcePool();
        }

        private ResourcePool()
        {
        }

        /// <summary>
        /// 확장자가 붙은 원본 경로를 Resources.Load 용 경로로 바꿉니다.
        /// </summary>
        public static string ToResourcesPath(string fullPath)
        {
            int dot = fullPath.LastIndexOf('.');
            int slash = fullPath.LastIndexOf('/');
            return dot > slash ? fullPath.Substring(0, dot) : fullPath;
        }

        /// <summary>
        /// 씬 전환 직전, 사라지는 씬에 속한 풀링 인스턴스를 파괴합니다.
        /// </summary>
        public void ClearBeforeChangingScene(Scene scene)
        {
            var survivors = new Stack<GameObject>();
            foreach (var pool in _instancePools.Values)
            {
                while (pool.Count > 0)
                {
                    var element = pool.Pop();
                    if (element == null)
                    {
                        continue;
                    }

                    if (element.scene == scene)
                    {
                        GameObject.Destroy(element);
                    }
                    else
                    {
                        survivors.Push(element);
                    }
                }

                while (survivors.Count > 0)
                {
                    pool.Push(survivors.Pop());
                }
            }
        }

        /// <summary>
        /// 해당 경로의 리소스를 로드하여 리턴합니다. (Instantiate 하지 않음)
        /// 실패하면 에러 로그를 남기고 null 을 리턴합니다.
        /// </summary>
        public TObject LoadResource<TObject>(string fullPath) where TObject : UnityEngine.Object
        {
            var key = (typeof(TObject), fullPath);
            if (_loadedResources.TryGetValue(key, out var cached) && cached != null)
            {
                return (TObject)cached;
            }

            var resource = Resources.Load<TObject>(ToResourcesPath(fullPath));
            if (resource == null)
            {
                Debug.LogError($"리소스 로드 실패. Type[{typeof(TObject).Name}] Path[{fullPath}]");
                return null;
            }

            _loadedResources[key] = resource;
            return resource;
        }

        /// <summary>
        /// 리소스를 미리 캐시에 올려둡니다.
        /// </summary>
        public Task ReserveResourceAsync<TObject>(string fullPath) where TObject : UnityEngine.Object
        {
            this.LoadResource<TObject>(fullPath);
            return Task.CompletedTask;
        }

        public GameObject InstantiateFromResource(string fullPath)
        {
            var pool = this.GetInstancePool(fullPath);
            while (pool.Count > 0)
            {
                var pooled = pool.Pop();
                if (pooled == null)
                {
                    continue;
                }
                pooled.transform.SetParent(null, worldPositionStays: false);
                pooled.SetActive(true);
                return pooled;
            }

            var resource = this.LoadResource<GameObject>(fullPath);
            if (resource == null)
            {
                throw new InvalidOperationException($"{fullPath} 리소스 없음.");
            }
            return GameObject.Instantiate(resource);
        }

        public TRootComponent InstantiateFromResource<TRootComponent>(string fullPath) where TRootComponent : Component
        {
            return this.InstantiateFromResource(fullPath).GetComponent<TRootComponent>();
        }

        public void PutBackInstance(string fullPath, GameObject instance)
        {
            if (instance == null)
            {
                Debug.LogWarning($"null 을 인스턴스 풀에 돌려놓으려고 함. 무시합니다. Path[{fullPath}]");
                return;
            }

            instance.transform.SetParent(null, worldPositionStays: false);
            instance.SetActive(false);
            this.GetInstancePool(fullPath).Push(instance);
        }

        private Stack<GameObject> GetInstancePool(string fullPath)
        {
            if (!_instancePools.TryGetValue(fullPath, out var pool))
            {
                pool = new Stack<GameObject>();
                _instancePools.Add(fullPath, pool);
            }
            return pool;
        }
    }
}
