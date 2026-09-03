using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class AmbushMonsterObject : AreaEffectObjectBase
    {
        private bool _isAlive;
        public override bool IsAlive => _isAlive;

        private GameObject _gameObject;
        private SpriteAnimationHandler _normalSpriteHandler;
        private SpriteAnimationHandler _transcendSkillSpriteHandler;
        private SpriteAnimationHandler _bodyAnimation;

        private Character _owner;
        private float _areaEffectRadius;
        private float _damage;
        private bool _isHitFired;

        public static readonly string BASIC_BODY_PARTICLE_PATH = "Stages/AreaEffects/AmbushMonsters/AmbushMonster.prefab";
        public static readonly string TRANSCENDENT_BODY_PARTICLE_PATH = "Stages/AreaEffects/AmbushMonsters/AmbushMonster_S.prefab";

        private string _hitSoundPrefabPath;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.AmbushMonster);

            _gameObject = new GameObject("AmbushMonsterObject");
            _gameObject.SetActive(false);
            _gameObject.transform.SetParent(this.transform);

            _normalSpriteHandler = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(BASIC_BODY_PARTICLE_PATH);
            _normalSpriteHandler.InitializeOnly();
            _normalSpriteHandler.transform.SetParent(_gameObject.transform);

            _transcendSkillSpriteHandler = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(TRANSCENDENT_BODY_PARTICLE_PATH);
            _transcendSkillSpriteHandler.InitializeOnly();
            _transcendSkillSpriteHandler.transform.SetParent(_gameObject.transform);
            _transcendSkillSpriteHandler.transform.localScale = new Vector3(1.3f, 1.3f);
            _transcendSkillSpriteHandler.gameObject.SetActive(false);
            
            _bodyAnimation = _normalSpriteHandler;
            _isAlive = false;

        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _isAlive = false;
        }

        public void Initialize(
          Stage stage,
          AllianceType alliance,
          Character owner,
          float areaEffectRadius,
          Vector2 hitPoint,
          float damage,
          bool isTranscendent,
          string hitSoundPrefabPath)
        {
            base.InitializeAreaObject(alliance);

            _owner = owner;
            _areaEffectRadius = areaEffectRadius;
            _damage = damage;
            this.transform.position = hitPoint;

            _gameObject.SetActive(true);
            _transcendSkillSpriteHandler.gameObject.SetActive(false);
            _normalSpriteHandler.gameObject.SetActive(false);
            _bodyAnimation = (isTranscendent ? _transcendSkillSpriteHandler : _normalSpriteHandler);
            _bodyAnimation.gameObject.SetActive(true);
            _bodyAnimation.InitializeAndPlay(() =>
            {
                this.AttackToTargetArea(stage);
            });

            _isAlive = true;
            _hitSoundPrefabPath = hitSoundPrefabPath;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            if (_isHitFired)
            {
                if (1 <= _bodyAnimation.SpriteAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime)
                {
                    _isAlive = false;
                }
                return;
            }

        }

        private void AttackToTargetArea(Stage stage)
        {
            _isHitFired = true;
            var targetArea = new CircularTargetArea(this.transform.position, _areaEffectRadius);
            CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, transform.position, knockBackPower: 0f, null, null, _hitSoundPrefabPath);
        }
    }

}
