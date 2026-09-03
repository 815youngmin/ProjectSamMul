using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class GumSpecialMineSlowAreaEffect : AreaEffectObjectBase
    {
        private static readonly float ATTACKPERIOD = 0.5f;
        private static readonly float SLOWDURATION = 0.5f;

        public override bool IsAlive => Time.time <= _createdAt + _attackDuration;
        public Vector2 Center => _attackArea.Center;
        public float Radius => _attackArea.Radius;

        private GameObject _body;
        private SkeletonAnimation _bodySkeletonAnimation;

        private Animation _endAnimation;
        private HashSet<Character> _hittedCharacters;

        private PlayerCharacter _owner;
        private float _damage;
        private float _nextTickAt;
        private float _slowRate;
        private float _createdAt;
        private float _attackDuration;

        private CircularTargetArea _attackArea;

        private readonly string _slowStatusEffectKey = "GumSpecialMineSlowAreaEffect";

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.GumSpecialMineSlowAreaEffect);
            
             _body = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/GumMine/GumSpecialMineSlowAreaEffect.prefab");
            _bodySkeletonAnimation = _body.GetComponent<SkeletonAnimation>();
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;
            _endAnimation = _bodySkeletonAnimation.skeleton.Data.FindAnimation("end");

            _hittedCharacters = new HashSet<Character>();

        }

        public void Initialize(
            PlayerCharacter owner, 
            Vector2 position, 
            float duration,
            float damage,
            float radius,
            float slowRate)
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _damage = damage;
            _slowRate = slowRate;
            _nextTickAt = 0.0f;
            _attackDuration = duration;
            _createdAt = Time.time;

            this.transform.position = position;
            _attackArea = new CircularTargetArea(position, radius);

            _bodySkeletonAnimation.AnimationState.SetAnimation(0, "gum", true);
            _bodySkeletonAnimation.AnimationState.AddAnimation(0, "end", false, duration - _endAnimation.Duration);
            _body.GetComponent<MeshRenderer>().sortingOrder = (int)(this.transform.position.y * -100.0f);
            _body.transform.localScale = Vector3.one * radius / 2f;
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _owner = null;
            _bodySkeletonAnimation.AnimationState.SetEmptyAnimation(0, 0f);
            _hittedCharacters.Clear();
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if (_nextTickAt < now)
            {
                _nextTickAt = now + ATTACKPERIOD;
                _hittedCharacters.Clear();
            }

            float radiusSqrMagnitude = _attackArea.Radius * _attackArea.Radius;
            List<Character> characters = new List<Character>();
            stage.FindAliveCharactersInArea(_owner.Alliance.ToEnemyAlliance(), _attackArea, characters);
            foreach (var character in characters)
            {
                if (_hittedCharacters.Contains(character))
                {
                    continue;
                }

                float sqrMagnitude = (character.Pos - _attackArea.Center).sqrMagnitude;
                if (radiusSqrMagnitude < sqrMagnitude)
                {
                    continue;
                }

                character.Hitted(stage, _owner, _damage, Vector2.zero, character.Pos, hitSoundPrefabPath: string.Empty);

                if (!character.IsBoss)
                {
                    character.StatusEffects.AddOrUpdateStatusEffect(stage, character, Characters.StatusEffects.StatusEffectType.SlowMove, _slowStatusEffectKey, SLOWDURATION, now, _slowRate);
                }
                _hittedCharacters.Add(character);
            }
        }
    }
}
