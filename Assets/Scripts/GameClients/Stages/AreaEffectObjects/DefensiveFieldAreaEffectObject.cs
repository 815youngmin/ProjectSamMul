using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class DefensiveFieldAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => _isAlive;
        private bool _isAlive;

        // 이건 스파인
        private const string _normalSkillPath = "Stages/AreaEffects/DefensiveField/fx_DefensiveField.prefab";
        // 이건 스프라이트 시퀀스
        private const string _transcendSkillPath = "Stages/AreaEffects/DefensiveField/fx_DefensiveField_S.prefab";

        private PlayerCharacter _owner;
        private float _damage;
        private float _radius;
        private float _attackInterval;
        private float _attackClearTickAt;
        private float _knockBackPower;

        private SkeletonAnimation _normalSkillSkeletonAnimation;
        private SpriteAnimationHandler _transcendSkillAnimation;

        private HashSet<Character> _hittedCharacters;

        private bool _isTranscend;

        private string _soundPrefabPath;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.DefensiveField);

            _normalSkillSkeletonAnimation = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(_normalSkillPath);
            _normalSkillSkeletonAnimation.gameObject.transform.parent = this.transform;
            _normalSkillSkeletonAnimation.Initialize(true);                
             
            _transcendSkillAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(_transcendSkillPath);
            _transcendSkillAnimation.gameObject.transform.parent = this.transform;
            _transcendSkillAnimation.InitializeOnly();

            _hittedCharacters = new HashSet<Character>();
            _isAlive = false;
            _isTranscend = false;

        }

        public void Initialize(PlayerCharacter owner, float damage, float radius, float attackInterval, float knockBackPower, bool isTranscend, string soundPrefabPath)
        {
            _isAlive = true;
            _owner= owner;
            _damage= damage;
            _radius= radius;
            _attackInterval= attackInterval;
            _knockBackPower = knockBackPower;

            _soundPrefabPath = soundPrefabPath;
            this.transform.position = _owner.transform.position;

            _isTranscend = isTranscend;
            if (isTranscend)
            {
                _transcendSkillAnimation.gameObject.SetActive(true);
                _normalSkillSkeletonAnimation.gameObject.SetActive(false);

                _transcendSkillAnimation.Play();
            }
            else
            {
                _transcendSkillAnimation.gameObject.SetActive(false);
                _normalSkillSkeletonAnimation.gameObject.SetActive(true);

                var animation = _normalSkillSkeletonAnimation.skeleton.Data.FindAnimation("animation");
                _normalSkillSkeletonAnimation.AnimationState.SetAnimation(0, animation, true);
            }

            this.SetResorceScaling(isTranscend);
        }

        public void ReApplyStat(float damage, float radius, float attackInterval, float knockbackPower)
        {
            _damage = damage;
            _radius = radius;
            _attackInterval = attackInterval;
            _knockBackPower = knockbackPower;
            this.SetResorceScaling(_isTranscend);
        }

        public void Dead()
        {
            _isAlive = false;
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _normalSkillSkeletonAnimation.AnimationState.ClearTracks();
            _normalSkillSkeletonAnimation.gameObject.SetActive(false);
            
            _transcendSkillAnimation.gameObject.SetActive(false);
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            this.transform.position = _owner.transform.position;

            float now = Time.time;
            if(_attackClearTickAt <= now)
            {
                _hittedCharacters.Clear();
                _attackClearTickAt = now + _attackInterval;
            }

            CircularTargetArea attackArea = new CircularTargetArea(this.transform.position, _radius);
            CombatSystem.HitOnTargetArea(stage, attackArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, _owner.Pos, _knockBackPower, _hittedCharacters, _hittedCharacters, _soundPrefabPath);
        }

        private void SetResorceScaling(bool isTranscend)
        {
            if (isTranscend)
            {
                _transcendSkillAnimation.gameObject.transform.localScale = Vector3.one * (_radius * 0.57f);//(_radius * 0.23f);
            }
            else
            {
                _normalSkillSkeletonAnimation.gameObject.transform.localScale = Vector3.one * (_radius * 0.75f);
            }
        }
    }
}
