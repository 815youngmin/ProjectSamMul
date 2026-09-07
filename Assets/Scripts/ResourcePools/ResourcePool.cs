using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SamMul.ResourcePools
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
                resource = CreatePlaceholderResource<TObject>(fullPath);
                if (resource == null)
                {
                    Debug.LogError($"리소스 로드 실패. Type[{typeof(TObject).Name}] Path[{fullPath}]");
                    return null;
                }
            }

            _loadedResources[key] = resource;
            return resource;
        }

        /// <summary>
        /// 플레이스홀더를 만들지 않고, 해당 경로의 리소스가 실제로 존재하는지만 확인합니다.
        /// </summary>
        public bool HasResource<TObject>(string fullPath) where TObject : UnityEngine.Object
            => Resources.Load<TObject>(ToResourcesPath(fullPath)) != null;

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

            var resource = Resources.Load<GameObject>(ToResourcesPath(fullPath));
            if (resource == null)
            {
                PlaceholderFactory.ReportOnce("프리팹", fullPath);
                return PlaceholderFactory.CreateObject(fullPath, null);
            }
            return GameObject.Instantiate(resource);
        }

        public TRootComponent InstantiateFromResource<TRootComponent>(string fullPath) where TRootComponent : Component
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
                return pooled.GetComponent<TRootComponent>();
            }

            var resource = Resources.Load<GameObject>(ToResourcesPath(fullPath));
            if (resource == null)
            {
                PlaceholderFactory.ReportOnce("프리팹", fullPath);
                return PlaceholderFactory.CreateObject(fullPath, typeof(TRootComponent)).GetComponent<TRootComponent>();
            }
            return GameObject.Instantiate(resource).GetComponent<TRootComponent>();
        }

        /// <summary>
        /// Resources 에 없는 리소스를 대신할 플레이스홀더를 만든다. 스프라이트는 흰 사각형, 프리팹은 빈 오브젝트 템플릿,
        /// 플레이스홀더 애니메이터의 스켈레톤 데이터는 기본 클립 세트다. 만들 수 없는 타입이면 null.
        /// </summary>
        private TObject CreatePlaceholderResource<TObject>(string fullPath) where TObject : UnityEngine.Object
        {
            var type = typeof(TObject);
            if (type == typeof(Sprite))
            {
                PlaceholderFactory.ReportOnce("스프라이트", fullPath);
                return PlaceholderFactory.WhiteSprite as TObject;
            }
            if (type == typeof(GameObject))
            {
                PlaceholderFactory.ReportOnce("프리팹", fullPath);
                var template = PlaceholderFactory.CreateObject(fullPath, null);
                template.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(template);
                return template as TObject;
            }
            if (type == typeof(SamMul.Animations.Placeholder.SkeletonDataAsset))
            {
                PlaceholderFactory.ReportOnce("스켈레톤 데이터", fullPath);
                var asset = ScriptableObject.CreateInstance<SamMul.Animations.Placeholder.SkeletonDataAsset>();
                asset.name = fullPath;
                return asset as TObject;
            }
            return null;
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
