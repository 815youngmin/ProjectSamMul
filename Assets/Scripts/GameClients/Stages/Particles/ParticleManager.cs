using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.Particles
{
    public class ParticleManager
    {

        private readonly HashSet<ParticleObject> aliveParticles;
        private readonly List<ParticleObject> particlesToDelete;

        public ParticleManager()
        {
            this.aliveParticles = new HashSet<ParticleObject>();
            this.particlesToDelete = new List<ParticleObject>();
        }

        public void ClearBeforeChangingScene(Scene relatedScene)
        {
            foreach (var particle in this.aliveParticles)
            {
                if (particle.gameObject.scene == relatedScene)
                {
                    this.particlesToDelete.Add(particle);
                }
            }

            foreach (var element in this.particlesToDelete)
            {
                this.aliveParticles.Remove(element);
                GameObject.Destroy(element.gameObject);
            }
            this.particlesToDelete.Clear();
        }

        public void Update()
        {
            float now = Time.time;
            foreach (var particle in this.aliveParticles)
            {
                if (!particle.IsAlive(now))
                {
                    this.particlesToDelete.Add(particle);
                }
            }

            foreach (var particle in this.particlesToDelete)
            {
                this.aliveParticles.Remove(particle);
                this.PutBack(particle);
            }
            this.particlesToDelete.Clear();

        }

        public ParticleObject CreateParticle(string particlePath, Vector3 pos, Vector3 localScale)
        {
            var particle = ResourcePool.Instance.InstantiateFromResource<ParticleObject>(particlePath);
            float lifeTime = particle.CalculateParticleLifeTime();
            particle.Initialize(particlePath, lifeTime);
            this.aliveParticles.Add(particle);

            particle.gameObject.transform.position = pos;
            particle.gameObject.transform.rotation = Quaternion.identity;
            particle.gameObject.transform.localScale = localScale;
            particle.gameObject.SetActive(true);

            return particle;
        }

        public ParticleObject CreateParticle(string particlePath, Transform trans)
        {
            return CreateParticle(particlePath, trans.position, trans.rotation, trans.localScale);
        }

        public ParticleObject CreateParticle(string particlePath, Vector3 pos, Quaternion rot, Vector3 scale)
        {
            ParticleObject particle = ResourcePool.Instance.InstantiateFromResource<ParticleObject>(particlePath);

            if (particle != null)
            {
                var lifeTime = particle.CalculateParticleLifeTime();// GetParticleDestroyTime(particle.gameObject);
                particle.Initialize(particlePath, lifeTime);
                this.aliveParticles.Add(particle);
                particle.gameObject.transform.position = pos;
                particle.gameObject.transform.rotation = rot;
                particle.gameObject.transform.localScale = scale;
                particle.gameObject.SetActive(true);
                return particle;
            }
            else
            {
                return null;
            }
        }

        public ParticleObject CreateParticle(string particlePath, Transform trans, float lifeTime)
        {
            return CreateParticle(particlePath, trans.position, trans.rotation, lifeTime);
        }

        public ParticleObject CreateParticle(string particlePath, Vector3 pos, Quaternion rot, float lifeTime)
        {
            ParticleObject particle = ResourcePool.Instance.InstantiateFromResource<ParticleObject>(particlePath);
            if (particle != null)
            {
                particle.Initialize(particlePath, lifeTime);
                this.aliveParticles.Add(particle);
                particle.gameObject.transform.position = pos;
                particle.gameObject.transform.rotation = rot;
                particle.gameObject.SetActive(true);
                return particle;
            }
            else
            {
                return null;
            }
        }

        // 루핑이 들어갔다던지 해서, lifetime에 의해 자동으로 삭제되지 않는 파티클을 강제로 제거
        public void RemoveParticle(ParticleObject member)
        {
            var _ = aliveParticles.Remove(member);
            this.PutBack(member);
        }

        private void PutBack(ParticleObject member)
        {
            member.transform.SetParent(null);
            member.gameObject.SetActive(false);
            ResourcePool.Instance.PutBackInstance(member.PoolingKey, member.gameObject);
        }
    }
}
