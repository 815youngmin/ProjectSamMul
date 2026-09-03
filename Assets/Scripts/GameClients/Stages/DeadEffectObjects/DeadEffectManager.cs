using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Z.GameClients.Stages.Characters;
using Z.ObjectPools;

using Random = UnityEngine.Random;

namespace Z.GameClients.Stages.DeadEffectObjects
{
    public class DeadEffectManager
    {
        //몬스터 죽을때 날아가는 이펙트 풀(뼈, 해골 등)
        private readonly ObjectPool<DeadEffectType, DeadEffectObjectBase> _pool;
        private Dictionary<Sequence, List<DeadEffectObjectBase>> _aliveSequenceAndDeadEffectObjects;
        private List<Sequence> _completedSequence;
        // 에디터 Hierarchy에서 스테이지 씬 현황을 정리하기 위함.
        private GameObject _deadEffectsRoot;

        public DeadEffectManager()
        {
            _pool = new ObjectPool<DeadEffectType, DeadEffectObjectBase>(objectFactory: AllocateObject);
            _aliveSequenceAndDeadEffectObjects = new Dictionary<Sequence, List<DeadEffectObjectBase>>();
            _completedSequence = new List<Sequence>();
        }
        
        public void InitializeForStageScene()
        {
            _deadEffectsRoot = new GameObject("@DeadEffectsRoot");
            _pool.Reserve(key: DeadEffectType.Bone, count: 20);
            _pool.Reserve(key: DeadEffectType.Skull, count: 20);
        }

        private DeadEffectObjectBase AllocateObject(DeadEffectType deadEffectType)
        {
            var deadEffectObject = new GameObject(deadEffectType.ToString());
            deadEffectObject.transform.SetParent(_deadEffectsRoot.gameObject.transform);

            switch (deadEffectType)
            {
                case DeadEffectType.Bone:
                    {
                        var deadEffectObjectBase = deadEffectObject.AddComponent<DeadEffectObjectBase>();
                        deadEffectObjectBase.AllocateSharedResources(deadEffectType, "Stages/DeadEffects/bone.png");
                        return deadEffectObjectBase;
                    }
                case DeadEffectType.Skull:
                    {
                        var deadEffectObjectBase = deadEffectObject.AddComponent<DeadEffectObjectBase>();
                        deadEffectObjectBase.AllocateSharedResources(deadEffectType, "Stages/DeadEffects/skull.png");
                        return deadEffectObjectBase;
                    }
                default:
                    {
                        throw new NotImplementedException($"{deadEffectType} 구현 안됨. 구현해주세요.");
                    }
            }
        }

        private TDeadEffectObject TakeOneFromPool<TDeadEffectObject>(DeadEffectType key) where TDeadEffectObject : DeadEffectObjectBase
        {
            var deadEffect = this._pool.TakeOne<TDeadEffectObject>(key);
            deadEffect.gameObject.transform.SetParent(_deadEffectsRoot.gameObject.transform);
            deadEffect.gameObject.SetActive(true);
            return deadEffect;
        }

        public void PutBack(DeadEffectObjectBase areaEffect)
        {
            _pool.PutBack(areaEffect);
        }

        public void ClearBeforeChangingScene(Scene relatedScene)
        {

            foreach (var kvp in _aliveSequenceAndDeadEffectObjects)
            {
                kvp.Key.Kill(false);
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    GameObject.Destroy(kvp.Value[i]);
                }
            }
            _aliveSequenceAndDeadEffectObjects.Clear();

            this._pool.DestroyAll(relatedScene, destroyFunction: (DeadEffectObjectBase element) =>
            {
                GameObject.Destroy(element.gameObject);
            });
        }

        //기본 몬스터 죽을때 효과 처리
        //여러개의 뼈를 하나의 시퀀스로 처리하기 때문에 이펙트를 제작 할때마다 생각보다 많은 코드가 필요해졌다.
        //이를 줄일 수 있는 방법이 있다면 추후 수정 할 것
        public void CreateNormalDeadEffect(Character owner, Vector2 hitVector)
        {
            Sequence bonesSequence = DOTween.Sequence();
            List<DeadEffectObjectBase> bones = new List<DeadEffectObjectBase>();

            float power = Random.Range(1f,3f);
            int boneCount = Random.Range(2,5);
            int skullCount = Random.Range(0,2);

            for (int i = 0; i < boneCount; i++)
            {
                var boneObject = this.TakeOneFromPool<DeadEffectObjectBase>(DeadEffectType.Bone);
                boneObject.Initialize(owner.Pos);
                float duration = Random.Range(0.4f, 0.6f);
                boneObject.transform.position = owner.UIPos;
                boneObject.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
                bonesSequence.Join(boneObject.transform.DOLocalJump(owner.Pos + (hitVector * power) + Random.insideUnitCircle, power, 1, duration));
                bonesSequence.Join(boneObject.transform.DOShakeRotation(duration, new Vector3(0, 0, 360 * Random.Range(1f, 2f)), 0));

                bones.Add(boneObject);
            }

            for (int i = 0; i < skullCount; i++)
            {
                var skullObject = this.TakeOneFromPool<DeadEffectObjectBase>(DeadEffectType.Skull);
                skullObject.Initialize(owner.Pos);
                float duration = Random.Range(0.4f, 0.6f);
                skullObject.transform.position = owner.UIPos;
                skullObject.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
                bonesSequence.Join(skullObject.transform.DOLocalJump(owner.Pos + (hitVector * power) + Random.insideUnitCircle, power, 1, duration));
                bonesSequence.Join(skullObject.transform.DOShakeRotation(duration, new Vector3(0, 0, 360 * Random.Range(1f, 2f)), 0));
                bones.Add(skullObject);
            }

            foreach (var bone in bones)
            {
                bonesSequence.Insert(0.6f, bone.GetComponent<SpriteRenderer>().DOFade(0, 0.3f));
            }

            bonesSequence.SetAutoKill(false);

            _aliveSequenceAndDeadEffectObjects.Add(bonesSequence, bones);
        }

        public void Update()
        {
            foreach (var kvp in _aliveSequenceAndDeadEffectObjects)
            {
                if(!kvp.Key.IsComplete())
                {
                    continue;
                }

                for(int i = 0; i < kvp.Value.Count; i++)
                {
                    this.PutBack(kvp.Value[i]);
                }
                _completedSequence.Add(kvp.Key);
            }

            foreach (var sequence in _completedSequence)
            {
                _aliveSequenceAndDeadEffectObjects.Remove(sequence);
                sequence.Kill();
            }
            _completedSequence.Clear();
        }

    }
}

