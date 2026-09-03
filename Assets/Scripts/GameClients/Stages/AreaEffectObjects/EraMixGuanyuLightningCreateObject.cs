using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.ProjectileObjects;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class EraMixGuanyuLightningCreateObject : AreaEffectObjectBase
    {
        public override bool IsAlive => _projectile.IsAlive;

        private Monster _owner;

        private ProjectileObject _projectile;
        private float _lightningCreateAt;


        private static readonly float PROJECTILE_DAMAGE_COEFFICIENT = 1.0f;

        private static readonly string ProjectileBodyPath = "Stages/Projectiles/guan_misile.prefab";
        private static readonly float ProjectileSpeed = 15f;
        private static readonly float ProjectileAcceleration = 1f;
        private static readonly float ProjectileRadius = 1.0f;
        private static readonly float ProjectileAliveDistance = 100;
        private static readonly float ProjectileKnobackPower = 0.1f;
        private static readonly bool IsSpinBladeCollide = false;

        private static readonly float LIGHTNINGDAMAGE_COEFFICIENT = 1.0f;
        private static readonly float AreaEffectRadius = 2;
        private static readonly float IndicatorDuration = 0f;
        private static readonly float LightningDelay = 0.2f;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.EraMixGuanyuLightningCreateObject);
        }

        public void Initialize(
            Stage stage,
            Monster owner,
            Vector2 firePosition, 
            Vector2 fireDirection
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _owner = owner;

            _projectile = FireProjectile(stage, firePosition, fireDirection);
            _lightningCreateAt = now + LightningDelay;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if(_lightningCreateAt < now)
            {
                this.CreateLightningAreaEffect(stage, _projectile.transform.position);
                _lightningCreateAt = now + LightningDelay;
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

        private ProjectileObject FireProjectile(Stage stage, Vector2 firePosition, Vector2 fireDirection)
        {
            return stage.CreateProjectile(
                    ProjectileBodyPath,
                    _owner.Alliance,
                    _owner,
                    _owner.RangeAttackPower * PROJECTILE_DAMAGE_COEFFICIENT,
                    ProjectileKnobackPower,
                    firePosition,
                    fireDirection,
                    ProjectileSpeed,
                    ProjectileAcceleration,
                    ProjectileRadius,
                    ProjectileAliveDistance,
                    hitChances: 999,
                    splitCount: 0,
                    IsSpinBladeCollide,
                    hitSoundPrefabPath: string.Empty
                    );
        }



        private void CreateLightningAreaEffect(Stage stage, Vector2 createPos)
        {
            stage.CreateZeusLightningAreaEffectObject(_owner, createPos, AreaEffectRadius, _owner.SpecialAttackPower * LIGHTNINGDAMAGE_COEFFICIENT, delay: 1.5f, IndicatorDuration);
        }

    }

}
