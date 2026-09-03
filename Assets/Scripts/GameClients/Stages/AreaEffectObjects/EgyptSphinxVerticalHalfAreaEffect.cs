using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class EgyptSphinxVerticalAreaEffect : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _creatAt + _lifeTime;

        private GameObject _bodyImage;
        private Monster _owner;
        private float _movingSpeed;
        private float _lifeTime;
        private float _damage;
        private float _creatAt;
        private float _radius;

        private HashSet<Character> _hittedCharacters;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.EgyptSphinxVerticalObject);

            _bodyImage = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/SphinxVerticalAttackArea.prefab");
            _bodyImage.transform.SetParent(this.gameObject.transform);
            _bodyImage.transform.localPosition = Vector3.one;
            _bodyImage.transform.localScale = Vector3.one / 1.5f;
            _hittedCharacters = new HashSet<Character>();
        }

        public void Initialize(
            Monster owner,
            Vector2 startPosition,
            float lifeTime,
            float moveSpeed,
            float radius,
            float damage
            )
        {
            _owner = owner;
            _movingSpeed = moveSpeed;
            _damage = damage;
            _lifeTime = lifeTime;
            _creatAt = Time.time;
            _radius = radius;
            this.transform.position = startPosition;
            this.transform.localScale = Vector3.one * radius;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            this.MoveToCurrentPosition(deltaTime);
            this.HitOnTargetCurrentPosition(stage);
        }

        private void MoveToCurrentPosition(float deltaTime)
        {
            float currentSpeed = _movingSpeed * deltaTime;
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + Vector2.down * currentSpeed;         

            this.transform.position = nextPosition;
        }

        private void HitOnTargetCurrentPosition(Stage stage)
        {
            if (stage.FenceRect == null)
            {
                return;
            }

            var targetArea = new CircularTargetArea(this.transform.position, _radius);
            CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, Vector2.zero, 0, _hittedCharacters, _hittedCharacters, hitSoundPrefabPath: string.Empty);
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            
            _hittedCharacters.Clear();
        }
    }

}
