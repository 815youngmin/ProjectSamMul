using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    /// <summary>
    /// 슈팅 스타 폭발. 전용 리소스 없이 폭발 범위는 판정 시 AttackAreaFlashManager 가 표시한다.
    /// </summary>
    public class ShootingStarExplosionAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive =>Time.time <= _createdAt + EXPLOSION_DURATION;

        // 원본 폭발 애니메이션 길이. 이 시간 동안 범위 안의 적을 한 번씩 타격한다.
        private const float EXPLOSION_DURATION = 0.5f;

        private Character _owner;
        private float _damage;

        private float _createdAt;

        private float _knockbackPower;
        private float _radius;
        private Vector2 _direction;
        private bool _isTranscend;

        private HashSet<Character> _hittedCharacters = new HashSet<Character>();

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.ShootingStarExplosion);
        }

        public void Initialize(
            PlayerCharacter owner,
            float damage,
            float knobackPower,
            float radius,
            Vector2 position,
            Vector2 direction,
            bool isTranscend
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _createdAt = now;
            _owner = owner;
            _damage = damage;
            _knockbackPower = knobackPower;
            _radius = radius;
            _isTranscend = isTranscend;
            _direction = direction;

            this.transform.position = position;
            if(isTranscend)
            {
                transform.right = _direction;
            }
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            if(_isTranscend)
            {
                CircularSectorTargetArea targetArea = new CircularSectorTargetArea(this.transform.position, _direction, _radius, angle: 120f);
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, this.transform.position, _knockbackPower, _hittedCharacters, _hittedCharacters, null);
            }
            else
            {
                CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, _radius);
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, this.transform.position, _knockbackPower, _hittedCharacters, _hittedCharacters, null);
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _hittedCharacters.Clear();
        }
    }

}
