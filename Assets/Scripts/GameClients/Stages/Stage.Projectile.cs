using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.GameClients.Stages.ProjectileObjects;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages
{
    public partial class Stage
    {
        // Partial 클래스의 멤버는 생성자가 정의된 기본 코드파일에 정의해주세요. 
        // 이 클래스의 경우, Stage.cs가 멤버를 정의할 기본 코드 파일입니다.

        // NOTE: 프로젝타일 데이터를 테이블로 빼고, 이 함수의 인자는 projectileType만 받을까?
        public ProjectileObject CreateProjectile(
            string bodyResourcePath,
            AllianceType alliance,
            Character owner,
            float baseDamage,
            float knockBackPower,
            Vector2 spawnPosition,
            Vector2 direction,
            float speed,
            float acceleration,
            float collidingRadius,
            float aliveDistance,
            int hitChances,
            int splitCount,
            string hitSoundPrefabPath)
        {
            var projectile = _projectilePool.TakeOneFromPool<ProjectileObject>(bodyResourcePath);
            projectile.InitializeProjectile(
                alliance,
                owner,
                baseDamage,
                knockBackPower,
                direction,
                speed,
                acceleration,
                collidingRadius,
                aliveDistance,
                hitChances,
                splitCount,
                hitSoundPrefabPath);

            projectile.transform.position = spawnPosition;
            _projectilesCreatedOnThisFrame.Add(projectile);

            projectile.gameObject.SetActive(true);
            return projectile;
        }

        public ProjectileObject CreateProjectile(
            string bodyResourcePath, 
            AllianceType alliance, 
            Character owner, 
            float baseDamage, 
            float knockBackPower, 
            Vector2 spawnPosition,
            Vector2 direction,
            float speed, 
            float acceleration,
            float collidingRadius,
            float aliveDistance, 
            int hitChances, 
            int splitCount, 
            bool isRemovableBySpinBladeObject,
            string hitSoundPrefabPath)
        {
            var projectile = _projectilePool.TakeOneFromPool<ProjectileObject>(bodyResourcePath);
            projectile.InitializeProjectile(
                alliance,
                owner,
                baseDamage,
                knockBackPower,
                direction,
                speed,
                acceleration,
                collidingRadius,
                aliveDistance,
                hitChances,
                splitCount,
                isRemovableBySpinBladeObject,
                hitSoundPrefabPath
                );

            projectile.transform.position = spawnPosition;
            _projectilesCreatedOnThisFrame.Add(projectile);

            projectile.gameObject.SetActive(true);
            return projectile;
        }

        public ProjectileObject CreateProjectile(
            string bodyResourcePath,
            AllianceType alliance,
            Character owner,
            float baseDamage,
            float knockBackPower,
            Vector2 spawnPosition,
            Vector2 direction,
            float speed,
            float acceleration,
            float collidingRadius,
            float aliveDistance,
            int hitChances,
            int splitCount,
            bool isRemovableBySpinBladeObject,
            OnHitCharacterHandler hitCharacterHandler,
            OnHitItemObjectHandler hitItemHandler,
            OnFinishedHandler onFinishedHandler,
            string hitSoundPrefabPath
            )
        {
            var projectile = _projectilePool.TakeOneFromPool<ProjectileObject>(bodyResourcePath);
            projectile.InitializeProjectile(
                alliance, owner, baseDamage, knockBackPower, 
                direction, speed, acceleration, collidingRadius, aliveDistance,
                hitChances, splitCount, isRemovableBySpinBladeObject,
                hitCharacterHandler, hitItemHandler, onFinishedHandler, hitSoundPrefabPath);

            projectile.transform.position = spawnPosition;
            _projectilesCreatedOnThisFrame.Add(projectile);

            projectile.gameObject.SetActive(true);

            return projectile;
        
        }

        private void UpdateProjectiles(float now, float deltaTime)
        {
#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("Stage.UpdateProjectiles"))
#endif
            {
                foreach (var projectile in _projectilesCreatedOnThisFrame)
                {
                    _aliveProjectiles.Add(projectile);
                }
                _projectilesCreatedOnThisFrame.Clear();

                this.DoRemoveReservedProjectiles();

                foreach (var projectile in _aliveProjectiles)
                {
                    projectile.UpdateLogic(this, deltaTime);
                    if (!projectile.IsAlive)
                    {
                        projectile.CallFinishedHander(this);
                        this.ReserveToRemoveProjectile(projectile);
                    }
                }

                this.DoRemoveReservedProjectiles();
            }
        }

        //해당 함수에서는 제거 예약 역할만 진행한다. 
        //프로젝타일이 제거되었을때 진행되어야하는 이벤트등은 이 함수에서 실행되지 않는다.
        public void ReserveToRemoveProjectile(ProjectileObject projectile)
        {
            if(!projectile.IsReserveToRemove)
            {
                _projectilesToRemove.Add(projectile);
                projectile.ReserveToRemove();
            }
        }

        private void DoRemoveReservedProjectiles()
        {
            foreach (var projectile in _projectilesToRemove)
            {
                if (!_aliveProjectiles.Remove(projectile))
                {
                    if (!_projectilesCreatedOnThisFrame.Remove(projectile))
                    {
                        Debug.LogWarning($"프로젝타일이 이미 제거된 것 같은데요? {projectile.name}");
                        return;
                    }
                }

                projectile.gameObject.SetActive(false);
                _projectilePool.PutBack(projectile);
            }
            _projectilesToRemove.Clear();
        }

        public void FindAliveProjectilesInArea(AllianceType alliance, CircularTargetArea area, in List<ProjectileObject> result)
        {
            IReadOnlyList<ProjectileObject> candidates = null;
            candidates = _aliveProjectiles;

            foreach (var projectile in candidates)
            {
                if (IsInArea(projectile, area) &&
                    projectile.Alliance == alliance) 
                {
                    result.Add(projectile);
                }
            }
        }

        public static bool IsInArea(ProjectileObject projectileObject, CircularTargetArea area)
        {
            float squaredSearchingRadius = (area.Radius + projectileObject.CollidingRadius);
            squaredSearchingRadius *= squaredSearchingRadius;

            Vector2 pos = projectileObject.transform.position;

            if ((pos - area.Center).sqrMagnitude <= squaredSearchingRadius)
            {
                return true;
            }
            return false;
        }
    }
}
