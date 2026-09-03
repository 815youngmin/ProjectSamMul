using UnityEngine;

namespace Z.GameClients.Stages.Particles
{
    public class ParticleObject : MonoBehaviour
    {
        // resource full path
        public string PoolingKey { get; private set; }
        ParticleSystem[] particleSystems;
        ParticleSystem _rootParticleSystem;

        [SerializeField]
        private bool infinity = false;

        private float destroyAt;

        private void Awake()
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>();
            
            _rootParticleSystem = GetComponent<ParticleSystem>();
            Debug.Assert(_rootParticleSystem != null);
        }

        public void Initialize(string resourceFullPath, float lifeTime)
        {
            this.PoolingKey = resourceFullPath;
            this.destroyAt = Time.time + lifeTime;
        }

        // 파티클 lifeTime만큼 살아있었고, 정리할 때가 되었는지를 판단. 
        public bool IsAlive(float now)
        {
            if (this.infinity)
            {
                return true;
            }

            return now < this.destroyAt;
        }

        public void Reset()
        {
            if (particleSystems != null)
            {
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    particleSystems[i].Clear();
                }
            }

            this.transform.SetParent(null);
            gameObject.SetActive(false);
        }

        public float CalculateParticleLifeTime()
        {
            float playTime = 0;
            float waitTime = 0;
            GameObject particleObject = this.gameObject;
            ParticleSystem[] particleSystems = particleObject.GetComponentsInChildren<ParticleSystem>();
            ParticleSystemRenderer[] renderes = particleObject.GetComponentsInChildren<ParticleSystemRenderer>();

            for (int i = 0; i < particleSystems.Length; i++)
            {
                if (!renderes[i].enabled)
                {
                    continue;
                }
                var ps = particleSystems[i].main;
                if (ps.loop)
                {
                    waitTime = -1;
                    break;
                }
                else
                {
                    playTime = ps.duration / ps.simulationSpeed;
                    if (waitTime < playTime)
                    {
                        waitTime = playTime;
                    }
                }
            }
            return waitTime;
        }

    }

}
