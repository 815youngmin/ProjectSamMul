using Shared.GameDataTypes;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Actions;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Monsters
{
    public partial class Monster : Character
    {
        public bool TryRangeAttack(Stage stage, Character target)
        {
            var now = Time.time;
            if (target.IsImmuneToHit)
            {
                return false;
            }

            if (!this.IsAbleToRangeAttackNow(now))
            {
                return false;
            }

            this.DoRangeAttack(stage, target);
            _lastRangeAttackedAt = Time.time;

            return true;
        }

        public void DoRangeAttack(Stage stage, Character target)
        {
            var bodyPrefabPath = StaticData.SpecialAttack1ResourcePath;
            if (string.IsNullOrEmpty(bodyPrefabPath))
            {
                bodyPrefabPath = "Stages/Projectiles/DummyProjectileBody.prefab";
            }

            var attackAction = new MonsterRangeAttackAction(target, this, bodyPrefabPath, AnimationController);
            _action.ChangeTo(stage, attackAction);
        }

        public bool TryRangeAttack(Stage stage, Vector2 attackDirection)
        {
            var now = Time.time;

            if (!this.IsAbleToRangeAttackNow(now))
            {
                return false;
            }

            this.DoRangeAttack(stage, attackDirection);
            _lastRangeAttackedAt = Time.time;

            return true;
        }

        public void DoRangeAttack(Stage stage, Vector2 attackDirection)
        {
            var bodyPrefabPath = StaticData.SpecialAttack1ResourcePath;
            if (string.IsNullOrEmpty(bodyPrefabPath))
            {
                bodyPrefabPath = "Stages/Projectiles/DummyProjectileBody.prefab";
            }

            var attackAction = new MonsterRangeAttackAction(this, attackDirection, bodyPrefabPath, AnimationController);
            _action.ChangeTo(stage, attackAction);
        }

        public bool TryPoisonousRangeAttack(Stage stage, Character target, float poisonousAreaEffectLifeTime, float poisonousAreaEffectRadius, float projectileLifeTime)
        {
            var now = Time.time;
            if (target.IsImmuneToHit)
            {
                return false;
            }

            if (!this.IsAbleToRangeAttackNow(now))
            {
                return false;
            }

            this.DoPoisonousRangeAttack(stage, target, poisonousAreaEffectLifeTime, poisonousAreaEffectRadius, projectileLifeTime);
            _lastRangeAttackedAt = Time.time;

            return true;
        }

        public void DoPoisonousRangeAttack(Stage stage, Character target, float poisonousAreaEffectLifeTime, float poisonousAreaEffectRadius, float projectileLifeTime)
        {
            var bodyPrefabPath = StaticData.SpecialAttack1ResourcePath;
            if (string.IsNullOrEmpty(bodyPrefabPath))
            {
                bodyPrefabPath = "Stages/Projectiles/DummyProjectileBody.prefab";
            }

            var attackAction = new MonsterPoisonousRangeAttackAction(target, this, bodyPrefabPath, poisonousAreaEffectLifeTime, poisonousAreaEffectRadius, projectileLifeTime, AnimationController);
            _action.ChangeTo(stage, attackAction);
        }

        public bool TryMultipleHorizontalRangeAttack(
            Stage stage,
            Character target,
            int projectileAmount,
            float fireAngle,
            bool isRemovableBySpinBladeObject,
            bool withIndicator)
        {
            var now = Time.time;
            if (target.IsImmuneToHit)
            {
                return false;
            }

            if (!this.IsAbleToRangeAttackNow(now))
            {
                return false;
            }

            this.DoMultipleHorizontalRangeAttack(stage, target, projectileAmount, fireAngle, isRemovableBySpinBladeObject, withIndicator);
            _lastRangeAttackedAt = Time.time;

            return true;
        }

        public bool TryMultipleVerticalRangeAttack(
            Stage stage,
            Character target,
            int projectileAmount,
            float firePeriod,
            bool isRemovableBySpinBladeObject,
            bool withIndicator)
        {
            var now = Time.time;
            if (target.IsImmuneToHit)
            {
                return false;
            }

            if (!this.IsAbleToRangeAttackNow(now))
            {
                return false;
            }

            this.DoMultipleVerticalRangeAttack(stage, target, projectileAmount, firePeriod, isRemovableBySpinBladeObject, withIndicator);
            _lastRangeAttackedAt = Time.time;

            return true;
        }

        public void DoMultipleHorizontalRangeAttack(
            Stage stage,
            Character target,
            int projectileAmount,
            float fireAngle,
            bool isRemovableBySpinBladeObject,
            bool withIndicator)
        {
            var bodyPrefabPath = StaticData.SpecialAttack1ResourcePath;
            if (string.IsNullOrEmpty(bodyPrefabPath))
            {
                bodyPrefabPath = "Stages/Projectiles/DummyProjectileBody.prefab";
            }

            var attackAction = new MultipleHorizontalRangeAttackAction(target, this, projectileAmount, fireAngle, isRemovableBySpinBladeObject, withIndicator, bodyPrefabPath, AnimationController);
            _action.ChangeTo(stage, attackAction);
        }

        public void DoMultipleVerticalRangeAttack(
            Stage stage,
            Character target,
            int projectileAmount,
            float firePeriod,
            bool isRemovableBySpinBladeObject,
            bool withIndicator)
        {
            var bodyPrefabPath = StaticData.SpecialAttack1ResourcePath;
            if (string.IsNullOrEmpty(bodyPrefabPath))
            {
                bodyPrefabPath = "Stages/Projectiles/DummyProjectileBody.prefab";
            }

            var attackAction = new MultipleVerticalRangeAttackAction(target, this, projectileAmount, firePeriod, isRemovableBySpinBladeObject, withIndicator, bodyPrefabPath, AnimationController);
            _action.ChangeTo(stage, attackAction);
        }


        public bool TryMultipleReflectionRangeAttack(
            Stage stage,
            Character target,
            int projectileAmount,
            float projectileSpeed,
            float projectileLifeTime,
            float projectileRadius,
            float projectileRotateSpeed,
            string projectileBodyPrefabPath)
        {
            var now = Time.time;
            if (target.IsImmuneToHit)
            {
                return false;
            }

            if (!this.IsAbleToRangeAttackNow(now))
            {
                return false;
            }

            this.DoMultipleReflectionRangeAttack(stage, target, projectileAmount, projectileSpeed, projectileLifeTime, projectileRadius, projectileRotateSpeed, projectileBodyPrefabPath);
            _lastRangeAttackedAt = Time.time;

            return true;
        }

        public void DoMultipleReflectionRangeAttack(
            Stage stage,
            Character target,
            int projectileAmount,
            float projectileSpeed,
            float projectileLifeTime,
            float projectileRadius,
            float projectileRotateSpeed,
            string projectileBodyPrefabPath)
        {
            var attackAction = new MultipleReflectionRangeAttackAction(
                target,
                this,
                projectileAmount,
                projectileSpeed,
                projectileLifeTime,
                projectileRadius,
                projectileRotateSpeed,
                projectileBodyPrefabPath,
                AnimationController);
            _action.ChangeTo(stage, attackAction);
        }


        public void DoSummon(Stage stage, Character target, int summonAmount, float attackPowerWeight, float hpWeight,  CharacterType summonCharacterType)
        {
            _action.ChangeTo(stage, new MonsterSummonAction(this, target, summonAmount,attackPowerWeight, hpWeight, summonCharacterType, AnimationController));
        }

        public void DoMultipleReflectionRangeAttackAndMonsterSummon(
            Stage stage,
            Character target,
            int projectileAmount,
            float projectileSpeed,
            float projectileLifeTime,
            float projectileRadius,
            float projectileRotateSpeed,
            string projectileBodyPrefabPath,
            int summonAmount,
            float attackPowerWeight,
            float hpWeight,
            CharacterType summonCharacterType)
        {
            _action.ChangeTo(stage, new MultipleReflectionRangeAttackAndMonsterSummonAction(
                this, target, 
                projectileAmount, projectileSpeed, projectileLifeTime, projectileRadius, projectileRotateSpeed, projectileBodyPrefabPath,
                summonAmount, attackPowerWeight, hpWeight, summonCharacterType, AnimationController));
        }

        public void DoTurretAttack(Stage stage, Vector2 attackDirection, float startAngle)
        {
            var bodyPrefabPath = StaticData.SpecialAttack1ResourcePath;
            if (string.IsNullOrEmpty(bodyPrefabPath))
            {
                bodyPrefabPath = "Stages/Projectiles/DummyProjectileBody.prefab";
            }
            _action.ChangeTo(stage, new TurretAttackAction(this, attackDirection, startAngle, bodyPrefabPath, AnimationController));
        }

        public void DoTurretReflectionattack(Stage stage, Character target, Vector2 attackDirection, float startAngle)
        {
            var bodyPrefabPath = StaticData.SpecialAttack1ResourcePath;
            if (string.IsNullOrEmpty(bodyPrefabPath))
            {
                bodyPrefabPath = "Stages/Projectiles/DummyProjectileBody.prefab";
            }
            _action.ChangeTo(stage, new TurretReflectionRangeAttackAction(this, target, attackDirection, startAngle, bodyPrefabPath, AnimationController));

        }

        //이전 몬스터 대쉬 액션 종료되는 시점
        private float _prevMonsterDashActionEndAt = 0;
        /// <summary>
        /// 몬스터 대쉬 액션 
        /// 준비 동작 이후 타겟방향으로 돌진한다. 
        /// 돌진시 타겟 방향은 변경되지 않는다.
        /// </summary>
        /// <param name="stage">현재 플레이중인 스테이지</param>
        /// <param name="target">목표물</param>
        /// <param name="dashAttackAreaSize">충돌 범위</param>
        /// <param name="prepareDuration">준비 시간</param>
        /// <param name="prepareAnimationName">준비 시간 애니메이션 이름</param>
        /// <param name="dashDuration">대쉬 시간</param>
        /// <param name="dashDistance">대쉬 거리</param>
        /// <param name="dashAnimationName">대쉬 애니메이션 이름</param>
        /// <param name="stopDuration">멈추기 시간(생략 가능)</param>
        /// <param name="stopAnimationName">멈추기 애니메이션 이름(생략가능)</param>
        /// <returns>이전 대쉬가 아직 동작중이면 false, 종료되었고 새로 시도했으면 true</returns>
        public bool TryMonsterDashAction(Stage stage, Character target, Vector2 dashAttackAreaSize, float prepareDuration, string prepareAnimationName, float dashDuration, float dashDistance, string dashAnimationName, float stopDuration, string stopAnimationName, bool isRotateToTargetDirection, string[] rotateBoneNames)
        {
            float now = Time.time;
            if (_prevMonsterDashActionEndAt < now)
            {
                _prevMonsterDashActionEndAt = now + prepareDuration + dashDuration + stopDuration;
                this.DoMonsterDashAction(stage, target, dashAttackAreaSize, prepareDuration, prepareAnimationName, dashDuration, dashDistance, dashAnimationName, stopDuration, stopAnimationName, isRotateToTargetDirection, rotateBoneNames);
                return true;
            }
            else
            {
                return false;
            }
        }
        public bool TryMonsterDashAction(Stage stage, Character target, Vector2 dashAttackAreaSize, float prepareDuration, string prepareAnimationName, float dashDuration, float dashDistance, string dashAnimationName, float stopDuration, string stopAnimationName)
        {
            return TryMonsterDashAction(stage, target, dashAttackAreaSize, prepareDuration, prepareAnimationName, dashDuration, dashDistance, dashAnimationName, stopDuration, stopAnimationName, isRotateToTargetDirection: false, rotateBoneNames: null);
        }

        private void DoMonsterDashAction(Stage stage, Character target, Vector2 dashAttackAreaSize, float prepareDuration, string prepareAnimationName, float dashDuration, float dashDistance, string dashAnimationName, float stopDuration, string stopAnimationName, bool isRotateToTargetDirection, string[] rotateBoneNames)
        {
            var monsterDashAction = new MonsterDashAction(
                owner: this,
                target: target,
                (SpineMonsterAnimationController)base._animationController,
                dashAttackAreaSize,
                prepareDuration,
                prepareAnimationName,
                dashDuration,
                dashDistance,
                dashAnimationName,
                stopDuration,
                stopAnimationName,
                isRotateToTargetDirection,
                rotateBoneNames
                );

            _action.ChangeTo(stage, monsterDashAction);
        }

        public void DoDashAction(Stage stage, Character target, float dashSpeed, float dashPreDaly, float dashDuration, float dashPostDelay)
        {
            Action.ChangeTo(stage, new DashAction(this, target, (SpriteMonsterAnimationController)_animationController, dashSpeed, dashPreDaly, dashDuration, dashPostDelay));
        }

        public void DoHealAction(Stage stage, Character target, float healRange, float healPercent, float healDuration, float healPeriod)
        {
            _action.ChangeTo(stage, new HealAction(this, target, healRange, healPercent, healDuration, healPeriod, (SpriteMonsterAnimationController)AnimationController));
        }
        public void DoDropshipSummonAction(Stage stage, Monster owner, Character target, float summonDuration)
        {
            var action = new DropShipSummonAction();
            action.Initialize(owner, target, summonDuration, (SpriteMonsterAnimationController)_animationController);
            Action.ChangeTo(stage, action);
        }


        public void DoMultipleHorizontalRangeAttackAndMonsterSummonAction(
            Stage stage,
            Character target,
            int projectileAmount,
            float fireAngle,
            bool isRemovableBySpinBladeObject,
            bool withIndicator,
            int summonAmount,
            float attackPowerWeight,
            float hpWeight,
            CharacterType summonCharacterType)
        {
            var bodyPrefabPath = StaticData.SpecialAttack1ResourcePath;
            if (string.IsNullOrEmpty(bodyPrefabPath))
            {
                bodyPrefabPath = "Stages/Projectiles/DummyProjectileBody.prefab";
            }

            var attackAction = new MultipleHorizontalRangeAttackAndMonsterSummonAction(target, this, projectileAmount, fireAngle, isRemovableBySpinBladeObject, withIndicator, bodyPrefabPath,
                summonAmount, attackPowerWeight, hpWeight, summonCharacterType, AnimationController);
            _action.ChangeTo(stage, attackAction);
        }

        public void DoMultipleVerticalRangeAttackAndMonsterSummonAction(
            Stage stage,
            Character target,
            int projectileAmount,
            float firePeriod,
            bool isRemovableBySpinBladeObject,
            bool withIndicator,
            int summonAmount,
            float attackPowerWeight,
            float hpWeight,
            CharacterType summonCharacterType
            )
        {
            var bodyPrefabPath = StaticData.SpecialAttack1ResourcePath;
            if (string.IsNullOrEmpty(bodyPrefabPath))
            {
                bodyPrefabPath = "Stages/Projectiles/DummyProjectileBody.prefab";
            }

            var attackAction = new MultipleVerticalRangeAttackAndMonsterSummonAction(target, this, projectileAmount, firePeriod, isRemovableBySpinBladeObject, withIndicator, bodyPrefabPath,
                                summonAmount, attackPowerWeight, hpWeight, summonCharacterType, AnimationController);
            _action.ChangeTo(stage, attackAction);
        }

        public void DoDashAndMonsterSummonAction(
            Stage stage,
            Character target, 
            float dashSpeed, 
            float dashPreDaly, 
            float dashDuration, 
            float dashPostDelay, 
            int summonAmount,
            float attackPowerWeight,
            float hpWeight,
            CharacterType summonCharacterType)
        {
            Action.ChangeTo(stage, new DashAndMonsterSummonAction(this, target, (SpriteMonsterAnimationController)_animationController, dashSpeed, dashPreDaly, dashDuration, dashPostDelay, summonAmount, attackPowerWeight, hpWeight, summonCharacterType));
        }
        public static void Do<T>(Stage stage, Monster owner, Character target) where T : SmartAction<SpineMonsterAnimationController>, new()
        {
            var action = new T();
            action.Initialize(owner, target, (SpineMonsterAnimationController)owner._animationController);
            owner._action.ChangeTo(stage, action);
        }

        public void Do<T>(Stage stage, Character target) where T : SmartAction<SpriteMonsterAnimationController>, new()
        {
            var action = new T();
            action.Initialize(this, target, (SpriteMonsterAnimationController)_animationController);
            _action.ChangeTo(stage, action);
        }
    }
}
