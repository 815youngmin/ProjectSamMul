using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class IceGolemShockWaveAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => _distance > _movingDistance;

        private Monster _owner;

        private readonly Vector2 targetAreaSize = new Vector2(8 , 2.5f);
        private float _damage;
        private float _moveSpeed;
        private float _distance;

        private float _movingDistance;

        private Vector2 _nextTargetAreaPosition;
        private HashSet<Character> _hittedCharacters;

        private SkeletonAnimation _leftSkeletonAnimation;
        private SkeletonAnimation _rightSkeletonAnimation;
        private Animation _attackAnimation;
        
        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.IceGolemShockWave);
            string prefabPath = "Stages/AreaEffects/IceAge_BossGiantIceGolem_eff1.prefab";
            GameObject leftBodyImage = ResourcePool.Instance.InstantiateFromResource(prefabPath);
            leftBodyImage.transform.SetParent(this.gameObject.transform);
            leftBodyImage.transform.localPosition = new Vector3(-2f, 0f);
            leftBodyImage.transform.localScale = Vector3.one * 2f;
            _leftSkeletonAnimation= leftBodyImage.GetComponent<SkeletonAnimation>();

            GameObject rightBodyImage = ResourcePool.Instance.InstantiateFromResource(prefabPath);
            rightBodyImage.transform.SetParent(this.gameObject.transform);
            rightBodyImage.transform.localPosition = new Vector3(2f, 0f);
            rightBodyImage.transform.localScale = Vector3.one * 2f;
            _rightSkeletonAnimation = rightBodyImage.GetComponent<SkeletonAnimation>();

            _attackAnimation = _leftSkeletonAnimation.skeleton.Data.FindAnimation("attackA_eff2");
            
            _leftSkeletonAnimation.AnimationState.Data.SetMix(_attackAnimation, _attackAnimation, 0);
            _rightSkeletonAnimation.AnimationState.Data.SetMix(_attackAnimation, _attackAnimation, 0);

            _hittedCharacters = new HashSet<Character>();
        }

        public void Initialize(
            Monster owner,
            Vector2 position,
            float damage,
            float moveSpeed,
            float distance)
        {
            _owner = owner;
            this.transform.position = position;
            _damage = damage;
            _moveSpeed = moveSpeed;
            _distance = distance;

            _nextTargetAreaPosition = position;

            _leftSkeletonAnimation.AnimationState.SetAnimation(0, _attackAnimation, false);
            _rightSkeletonAnimation.AnimationState.SetAnimation(0, _attackAnimation, false);
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float moveDistance = _moveSpeed * deltaTime;
            _movingDistance += moveDistance;
            _nextTargetAreaPosition -= new Vector2(0, moveDistance);
            SquareTargetArea targetArea = new SquareTargetArea(_nextTargetAreaPosition, targetAreaSize, 0);
            CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, Vector2.zero, 0, _hittedCharacters, _hittedCharacters, hitSoundPrefabPath: string.Empty);
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _movingDistance = 0;
            _moveSpeed = 0;
            _distance = 0;
            _hittedCharacters.Clear();
        }

    }
}
