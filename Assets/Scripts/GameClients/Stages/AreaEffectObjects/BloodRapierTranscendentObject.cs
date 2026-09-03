using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class BloodRapierTranscendentObject : AreaEffectObjectBase
    {

        private static readonly float TrailFollowDuration = 0.5f;

        public override bool IsAlive => _isAlive;

        private Character _owner;
        private float _damage;
        private float _hpDrainRatio;

        private float _createdAt;
        private float _lifeTime;
        private float _hitAt;
        private float _hitTimeOnAnimation;
        private float _knockBackPower;

        private GameObject _body;
        private SpriteAnimationHandler _spriteAnimationHandler;
        private HashSet<Character> _hittedCharacters;
        private List<BloodTrail> _bloodTrails;

        private Vector2 _size = new Vector2(9.0f, 3.6f);
        private Vector2 _position;
        private Vector2 _direction;

        private bool _isAlive;

        private string _hitSoundPrefabPath;
        private string _bloodTrailPrefabPath;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.BloodRapierTranscendent);

            _body = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/Ketchapi_attack_S.prefab");
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector3.one;
            _spriteAnimationHandler = _body.GetComponent<SpriteAnimationHandler>();
            _spriteAnimationHandler.InitializeOnly();
            _body.gameObject.SetActive(false);

            _bloodTrailPrefabPath = BloodTrail.PREFAB_PATH;

        }

        public void Initialize(
            Character owner,
            Vector2 position,
            Vector2 direction,
            float damage,
            float hpDrainRatio,
            float knockBackPower,
            string hitSoundPrefabPath
            )
        {
            base.InitializeAreaObject(owner.Alliance);

            this.transform.localScale = Vector3.one * 1.8f;

            float now = Time.time;
            _owner = owner;
            _createdAt = now;
            _lifeTime = _spriteAnimationHandler.AnimationDuration;

            _hittedCharacters = new HashSet<Character>();
            _hitSoundPrefabPath = hitSoundPrefabPath;

            this.transform.position = position + direction;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _body.transform.rotation = Quaternion.Euler(0, 0, angle);
            _body.gameObject.SetActive(true);

            _hitTimeOnAnimation = _spriteAnimationHandler.HitTimeOnAnimation;
            _hitAt = now + _hitTimeOnAnimation;

            _bloodTrails = new List<BloodTrail>();

            _spriteAnimationHandler.Play();

            _damage = damage;
            _hpDrainRatio = hpDrainRatio;
            _position = position + direction;
            _direction = direction;
            _knockBackPower = knockBackPower;
            _isAlive = true;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if (_hitAt <= now)
            {
                Vector2 center = _position + _direction * _size.x * 0.5f;
                float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
                SquareTargetArea squareTargetArea = new SquareTargetArea(center, _size, angle);
                CombatSystem.HitOnTargetArea(stage, squareTargetArea, _owner, damage: _damage, CombatSystem.KnockBackType.Pivot, _position, _knockBackPower, _hittedCharacters, _hittedCharacters, _hitSoundPrefabPath);

                _hitAt = float.MaxValue;
                if (_hittedCharacters.Count > 0)
                {
                    foreach (Character character in _hittedCharacters)
                    {
                        var bloodTrail = ResourcePool.Instance.InstantiateFromResource<BloodTrail>(_bloodTrailPrefabPath);
                        _bloodTrails.Add(bloodTrail);
                        Vector2 trailCenter = (_owner.Pos + character.Pos) * 0.5f;
                        trailCenter += Random.insideUnitCircle * 5f;
                        bloodTrail.Initialize(_owner, character.Pos, trailCenter, _lifeTime - _hitTimeOnAnimation);
                    }
                }
            }

            if (now >= _createdAt + _lifeTime + TrailFollowDuration)
            {
                _isAlive = false;
                _owner.DrainHP(stage, _owner.MaxHP * _hpDrainRatio * _hittedCharacters.Count);
            }

            foreach (var bloodTrail in _bloodTrails)
            {
                bloodTrail.UpdateLogic(deltaTime);
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _hittedCharacters.Clear();

            foreach (var bloodTrail in _bloodTrails)
            {
                ResourcePool.Instance.PutBackInstance(_bloodTrailPrefabPath, bloodTrail.gameObject);
            }
            _bloodTrails.Clear();
        }
    }
}
