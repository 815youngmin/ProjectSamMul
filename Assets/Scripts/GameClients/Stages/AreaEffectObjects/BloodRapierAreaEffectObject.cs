using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class BloodRapierAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private Character _owner;
        private Stage _stage;
        private float _weekDamage;
        private float _mainDamage;
        private float _radius;

        private float _weekKnockbackPower;
        private float _mainKnockbackPower;

        private float _createdAt;
        private float _lifeTime;
        private bool _isHit;

        private GameObject _body;
        private SpriteAnimationHandler _spriteAnimationHandler;

        private Vector2 _attackDirection;
        private Vector2 _attackSize;

        private HashSet<Character> _weekHittedCharacters;
        private HashSet<Character> _umbrellaHittedCharacters;

        private float _hpDrainPercent;
        private float _hpDrainAmount;
        
        private string _hitSoundPrefabPath;

        // 우산이 캐챠피 손에 붙어있는지 여부. 우산 펼쳐지고 나면 손에서 놓는다.
        private bool _isHeldByOwner;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.BloodRapier);
            _body = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/Ketchapi_attack.prefab");
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector3.one;
            _spriteAnimationHandler = _body.GetComponent<SpriteAnimationHandler>();
            _spriteAnimationHandler.InitializeAndPlay(SecondHit);
            _spriteAnimationHandler.gameObject.SetActive(false);

            _weekHittedCharacters = new HashSet<Character>();
            _umbrellaHittedCharacters = new HashSet<Character>();

            _isHeldByOwner = true;

        }

        public void Initialize(
            Character owner,
            Stage stage,
            Vector2 direction,
            float radius,
            float weekKnockbackPower,
            float mainKnockbackPower,
            float weekDamage,
            float mainDamage,
            float areaRatio,
            float hpDrainPercent,
            float hpDrainAmount,
            string hitSoundPrefabPath
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _createdAt = now;
            _weekKnockbackPower = weekKnockbackPower;
            _mainKnockbackPower = mainKnockbackPower;
            _owner = owner;
            _stage = stage;
            _radius = radius;
            _weekDamage = weekDamage;
            _mainDamage = mainDamage;
            _attackDirection = direction;
            _attackSize = new Vector2(4.5f * areaRatio, 0.5f * areaRatio);
            _hitSoundPrefabPath = hitSoundPrefabPath;
            
            // 시작할 땐 케챠피 손에 잡고 있다가, 우산이 펼쳐지면 그 때 바닥에 놓는다.
            this.transform.SetParent(_owner.transform);
            this.transform.localPosition = _attackDirection * _attackSize.x * 0.6f + new Vector2(0, 0.5f);
            this.transform.localScale = Vector3.one * areaRatio;
            this.transform.right = direction;

            _body.gameObject.SetActive(true);
            _lifeTime = _spriteAnimationHandler.AnimationDuration;
            _spriteAnimationHandler.Play();

            _hpDrainPercent = hpDrainPercent;
            _hpDrainAmount = hpDrainAmount;

            _isHeldByOwner = true;
        }


        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            if (!_isHeldByOwner)
            {
                return;
            }

            var center = _owner.CenterPos + _attackDirection * (0.5f * _attackSize.x);
            float angle = Mathf.Atan2(_attackDirection.y, _attackDirection.x) * Mathf.Rad2Deg;
            SquareTargetArea targetArea = new SquareTargetArea(center, _attackSize, angle);
            CombatSystem.HitOnTargetArea(
                stage,
                targetArea,
                _owner,
                _weekDamage,
                CombatSystem.KnockBackType.Direction,
                _attackDirection,
                _weekKnockbackPower,
                _weekHittedCharacters,
                _weekHittedCharacters,
                _hitSoundPrefabPath);

            _spriteAnimationHandler.SpriteRenderer.sortingOrder = (int)(this.transform.position.y * -100.0f - 10f);
        }

        public void SecondHit()
        {
            CircularSectorTargetArea circularSectorTargetArea = new CircularSectorTargetArea(_owner.CenterPos, _attackDirection, _radius, 180f);
            CombatSystem.HitOnTargetArea(_stage, circularSectorTargetArea, _owner, _mainDamage, CombatSystem.KnockBackType.Pivot, _owner.Pos, _mainKnockbackPower, _umbrellaHittedCharacters, null, _hitSoundPrefabPath);

            if(Random.value <= _hpDrainPercent)
            {
                _owner.DrainHP(_stage, _owner.MaxHP * _hpDrainAmount * _umbrellaHittedCharacters.Count);
            }
            
            this.transform.SetParent(_owner.transform.parent, worldPositionStays: true);
            _isHeldByOwner = false;
        }


        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _weekHittedCharacters.Clear();
            _umbrellaHittedCharacters.Clear();
        }
    }
}
