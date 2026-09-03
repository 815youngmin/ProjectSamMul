using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class PoisonousAreaEffectObject : AreaEffectObjectBase
    {
        private const string APPEARING_RESOURCE_PATH = "Stages/AreaEffects/PoisonEffects/PoisonAreaEffectAppearing.prefab";
        private const string REPEATING_RESOURCE_PATH = "Stages/AreaEffects/PoisonEffects/PoisonAreaEffectRepeating.prefab";

        private static readonly float FADE_OUT_DURATION = 0.25f;

        public override bool IsAlive => _isAlive;
        public Vector2 Center => _attackArea.Center;
        public float Radius => _attackArea.Radius;

        private SpriteAnimationHandler _appearingAnimation;
        private SpriteAnimationHandler _repeatingAnimation;
        private HashSet<Character> _hittedCharacters;

        private Character _owner;
        private float _damage;
        private float _damagePeriod;
        private float _nextTickAt;
        private float _disappearsAt;

        private bool _isAlive;
        private bool _isApeearing;
        private bool _isDisappearing;

        private CircularTargetArea _attackArea;
        private Sequence _fadeOutSequence;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.PoisonousArea);

            _appearingAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(APPEARING_RESOURCE_PATH);
            _appearingAnimation.InitializeOnly();
            _appearingAnimation.gameObject.SetActive(true);
            _appearingAnimation.gameObject.transform.SetParent(this.transform);
            _appearingAnimation.transform.localPosition = Vector3.zero;

            _repeatingAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(REPEATING_RESOURCE_PATH);
            _repeatingAnimation.InitializeOnly();
            _repeatingAnimation.gameObject.SetActive(false);
            _repeatingAnimation.gameObject.transform.SetParent(this.transform);
            _repeatingAnimation.transform.localPosition = Vector3.zero;

            _hittedCharacters = new HashSet<Character>();
        }

        public void Initialize(Character owner, Vector2 position, float lifeTime, float period, float damage, float radius)
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _damage = damage;
            _damagePeriod = period;
            _nextTickAt = 0.0f;
            _disappearsAt = Time.time + lifeTime - FADE_OUT_DURATION;

            _isAlive = true;
            _isApeearing = true;
            _isDisappearing = false;

            _appearingAnimation.gameObject.SetActive(true);
            _appearingAnimation.InitializeAndPlay(hitEventHandler: null, endEventHandler: () =>
            {
                _isApeearing = false;
                _appearingAnimation.gameObject.SetActive(false);
                _repeatingAnimation.gameObject.SetActive(true);
                _repeatingAnimation.Play();
            });

            _repeatingAnimation.SpriteRenderer.color = Color.white;

            // 유닛 사이즈 기준으로, 스프라이트가 (radius * 2.0f)유닛 크기보다 살짝 크게(+0.3f) 그려지도록 맞춘다.
            Vector2 bounds = _appearingAnimation.SpriteRenderer.sprite.bounds.size;
            float resultScale = (radius * 2.0f + 0.3f) * (1.0f / bounds.x);
            _repeatingAnimation.transform.localScale = _appearingAnimation.transform.localScale = Vector3.one * resultScale;

            this.transform.position = position;
            _attackArea = new CircularTargetArea(position, radius);

            this.UpdateSortingOrder();
        }

        /// <summary>
        /// 독장판 제거를 시도합니다. 만약 독장판이 보스가 생성한 것이면 제거하지 못합니다.
        /// </summary>
        public void TryDisappear()
        {
            if (_owner.IsBoss)
            {
                return;
            }

            this.Disappear();
        }

        private void Disappear()
        {
            if (_isApeearing || !_isAlive || _isDisappearing)
            {
                return;
            }

            _fadeOutSequence = DOTween.Sequence()
                .Append(_repeatingAnimation.SpriteRenderer.DOFade(0f, FADE_OUT_DURATION).SetEase(Ease.InSine))
                .OnComplete(() => _isAlive = false);
            _isDisappearing = true;
        }

        public override void PuttingBackToPool()
        {
            _appearingAnimation.gameObject.SetActive(false);
            _repeatingAnimation.gameObject.SetActive(false);

            _appearingAnimation.transform.localPosition = _repeatingAnimation.transform.localPosition = Vector3.zero;
            _appearingAnimation.transform.localScale = _repeatingAnimation.transform.localScale = Vector3.one;

            _fadeOutSequence.Kill();
            _fadeOutSequence = null;

            _owner = null;
        }


        private void UpdateSortingOrder()
        {
            _appearingAnimation.SpriteRenderer.sortingOrder
                = _repeatingAnimation.SpriteRenderer.sortingOrder
                = (int)(transform.position.y * -100.0f) - ((int)transform.position.x % 80);
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if (_disappearsAt < now)
            {
                this.Disappear();
            }

            if (_nextTickAt < now)
            {
                _nextTickAt = now + _damagePeriod;
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
                _hittedCharacters.Add(character);
            }
        }
    }
}
