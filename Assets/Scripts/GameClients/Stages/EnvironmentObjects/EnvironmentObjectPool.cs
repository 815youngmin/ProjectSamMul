#nullable enable
using UnityEngine;
using UnityEngine.SceneManagement;
using SamMul.ObjectPools;

namespace SamMul.GameClients.Stages.EnvironmentObjects
{
    public class EnvironmentObjectPool
    {
        private readonly ObjectPool<EnvironmentObjectType, EnvironmentObject> _pool;

        public EnvironmentObjectPool()
        {
            _pool = new ObjectPool<EnvironmentObjectType, EnvironmentObject>(objectFactory: EnvironmentObject.Create);
        }

        // Destroys every pooled instance that belongs to the scene being unloaded.
        public void ClearBeforeChangingScene(Scene relatedScene)
        {
            _pool.DestroyAll(relatedScene, destroyFunction: element => Object.Destroy(element.gameObject));
        }

        public TObjectType TakeOneFromPool<TObjectType>(EnvironmentObjectType key) where TObjectType : EnvironmentObject
        {
            return _pool.TakeOne<TObjectType>(key);
        }

        public void PutBack(EnvironmentObject environmentObject)
        {
            _pool.PutBack(environmentObject);
        }
    }
}
