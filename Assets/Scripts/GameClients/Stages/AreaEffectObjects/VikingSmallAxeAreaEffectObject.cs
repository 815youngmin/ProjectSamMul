using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class VikingSmallAxeAreaEffectObject : AreaEffectObjectBase
    {
        private const string _axePrefabPath = "Stages/AreaEffects/VikingAxeLeader/VikingSmallAxe.prefab";

        private bool _isAlive;
        public override bool IsAlive => _isAlive;

        private Character _owner;
        private GameObject _bodyImage;
        private float _damage;
        private float _moveSpeed;
        private Vector2 _moveDirection;

        private const float _attackRadius = 2.0f * 0.5f;

        private HashSet<Character> _hittedCharactersInAttackPeriod = new HashSet<Character>();

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.VikingSmallAxe);
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(_axePrefabPath);
            _bodyImage.transform.SetParent(this.gameObject.transform);
            _bodyImage.transform.localPosition = Vector3.zero;
            _bodyImage.transform.localScale = Vector3.one;
            _bodyImage.transform.localRotation = Quaternion.identity;
        }

        public void Initialize(
                        Character owenr,
                        Vector2 direction,
                        float moveSpeed,
                        float damage
                        )
        {
            _owner = owenr;
            _damage = damage;
            _moveSpeed = moveSpeed;
            _moveDirection = direction;
            this.transform.position = owenr.transform.position;
            _isAlive = true;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            _bodyImage.transform.Rotate(0.0f, 0.0f, 360.0f * 5.0f * deltaTime);
            this.transform.Translate(_moveDirection * _moveSpeed * deltaTime);

            CircularTargetArea attackArea = new CircularTargetArea(this.transform.position, _attackRadius);
            _hittedCharactersInAttackPeriod.Clear();
            CombatSystem.HitOnTargetArea(stage, attackArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, attackArea.Center, 0.0f, _hittedCharactersInAttackPeriod, null, string.Empty);
            if (_hittedCharactersInAttackPeriod.Count != 0)
            {
                _isAlive = false;
                return;
            }
            
            if (stage.FenceRect == null)
            {
                return;
            }
            Rect fenceRect = stage.FenceRect.Value;

            if (!fenceRect.Contains(this.transform.position))
            {
                _isAlive = false;
                return;
            }
        }
    }
}
