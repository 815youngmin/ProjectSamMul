using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class TupacAmaruSandMakeObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private Character _owner;

        private float _createdAt;
        private float _lifeTime;

        private GameObject _body;

        private static readonly string ProjectileBodyPath = "Stages/Projectiles/Stone_Radius1.prefab";
        private static readonly float ProjectileSpeed = 15f;
        private static readonly float ProjectileRadius = 0.5f;
        private static readonly float ProjectileAliveDistance = 50;

        private static readonly float AreaEffectMakeDleay = 0.5f;
        private static readonly float AreaEffectRadius = 2;
        private static readonly float IndicatorDuration = 1.5f;
        private static readonly float AreaEffectAttackPeriod = 0.25f;
        private static readonly float AreaEffectLifeTime = 5f;

        private HashSet<Character> _hittedCharacters;
        private Vector2 _direction;
        private float _projectileDamage;
        private float _areaEffectDamage;

        private float _areaEffectCreateAt;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.TupacAmaruSandMakeObject);
            _body = ResourcePool.Instance.InstantiateFromResource(ProjectileBodyPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;
        }

        public void Initialize(
            Character owner,
            Vector2 startPosition,
            Vector2 direction,
            float projectileDamage,
            float areaEffectDamage)
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _owner = owner;
            this.transform.position = startPosition;
            _direction = direction;
            this.transform.right = direction;

            _projectileDamage = projectileDamage;
            _areaEffectDamage = areaEffectDamage;

            _hittedCharacters = new HashSet<Character>();
            
            _createdAt = now;
            _areaEffectCreateAt = now;
            _lifeTime = ProjectileAliveDistance / ProjectileSpeed;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            //이동
            this.transform.Translate(_direction * ProjectileSpeed * deltaTime, Space.World);

            //타겟 처리
            CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, ProjectileRadius);
            CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _projectileDamage, CombatSystem.KnockBackType.Pivot, Vector2.zero, 0,
                _hittedCharacters, _hittedCharacters, null);

            if(_areaEffectCreateAt <= now)
            {
                Vector2 dir1 = Quaternion.Euler(0, 0, -90f) * _direction;
                Vector2 dir2 = Quaternion.Euler(0, 0, 90f) * _direction;
                Vector2 pos = this.transform.position;
                
                this.CreateAreaEffect(stage, pos + dir1 * AreaEffectRadius * 1.5f);
                this.CreateAreaEffect(stage, pos + dir2 * AreaEffectRadius * 1.5f);

                _areaEffectCreateAt = now + AreaEffectMakeDleay;
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

        private void CreateAreaEffect(Stage stage, Vector2 pos)
        {
            stage.CreateSandAreaEffectObject(_owner, delay: 0f, IndicatorDuration, _areaEffectDamage,
                AreaEffectAttackPeriod, AreaEffectLifeTime, AreaEffectRadius, pos);
        }

    }

}
