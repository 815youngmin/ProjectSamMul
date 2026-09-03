using Shared.StaticDatas;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Z.GameClients.Stages.Characters;
using Z.ObjectPools;

namespace Z.GameClients.Stages.ProjectileObjects
{
    public class ProjectilePool
    {
        private readonly ObjectPool<string, ProjectileObject> _projectilePool;

        private readonly StaticDataRepository _staticDataRepository;

        public ProjectilePool(StaticDataRepository staticDataRepository)
        {
            _projectilePool = new ObjectPool<string, ProjectileObject>(objectFactory: AllocateProjectile);
            _staticDataRepository = staticDataRepository;
        }

        public void Init()
        {
        }

        // 씬이 정리될 때 호출된다.
        // 씬을 넘어갈 때 유효하지 않은 Pooling Object들을 모두 Destroy해준다. (prefab은 남김)
        public void ClearBeforeChangingScene(Scene relatedScene)
        {
            _projectilePool.DestroyAll(relatedScene, destroyFunction: (ProjectileObject element) =>
            {
                GameObject.Destroy(element.gameObject);
            });
        }

        /// <remarks>반환된 <see cref="ProjectileObject"/>는 반드시 <see cref="ProjectileObject.InitializeProjectile(CombatSystems.AllianceType, Character, float, Vector2, float, float, float, int)"/>를 호출하여 초기화해야 한다.</remarks>
        public TProjectile TakeOneFromPool<TProjectile>(string key) where TProjectile : ProjectileObject
        {
            var projectile = this._projectilePool.TakeOne<TProjectile>(key);
            return projectile;
        }

        public void PutBack(ProjectileObject projectile)
        {
            _projectilePool.PutBack(projectile);           
        }

        private ProjectileObject AllocateProjectile(string projectileBodyResourcePath)
        {
            var lastName = projectileBodyResourcePath.Split('/').Last();

            var projectileObject = new GameObject(lastName);
            var projectile = projectileObject.AddComponent<ProjectileObject>();
            projectile.AllocateSharedResources(projectileBodyResourcePath);
            return projectile;
        }


    }

}
