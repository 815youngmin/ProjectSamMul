using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.AreaIndicators;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    /// <summary>
    /// 독장판. 전용 이펙트 리소스 없이 판정 범위를 AttackAreaFlashManager 로 붉게 계속 표시한다.
    /// 등장 중에는 서서히 진해지고, 사라질 때는 서서히 연해진다.
    /// </summary>
    public class PoisonousAreaEffectObject : AreaEffectObjectBase
    {
        // 원본 등장 애니메이션 길이. 이 시간 동안은 타격하지 않고 표시만 진해진다.
        private static readonly float APPEAR_DURATION = 0.5f;
        private static readonly float FADE_OUT_DURATION = 0.25f;

        public override bool IsAlive => _isAlive;
        public Vector2 Center => _attackArea.Center;
        public float Radius => _attackArea.Radius;

        private HashSet<Character> _hittedCharacters;

        private Character _owner;
        private float _damage;
        private float _damagePeriod;
        private float _nextTickAt;
        private float _appearedAt;
        private float _disappearsAt;
        private float _disappearStartedAt;

        private bool _isAlive;
        private bool _isDisappearing;
        private bool IsAppearing => Time.time < _appearedAt;

        private CircularTargetArea _attackArea;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.PoisonousArea);

            _hittedCharacters = new HashSet<Character>();
        }

        public void Initialize(Character owner, Vector2 position, float lifeTime, float period, float damage, float radius)
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _damage = damage;
            _damagePeriod = period;
            _nextTickAt = 0.0f;
            _appearedAt = Time.time + APPEAR_DURATION;
            _disappearsAt = Time.time + lifeTime - FADE_OUT_DURATION;

            _isAlive = true;
            _isDisappearing = false;

            this.transform.position = position;
            _attackArea = new CircularTargetArea(position, radius);
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
            if (IsAppearing || !_isAlive || _isDisappearing)
            {
                return;
            }

            _isDisappearing = true;
            _disappearStartedAt = Time.time;
        }

        public override void PuttingBackToPool()
        {
            _hittedCharacters.Clear();
            _owner = null;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if (_disappearsAt < now)
            {
                this.Disappear();
            }

            // 표시: 등장 중엔 진해지고, 사라질 땐 연해진다.
            float alpha = 1f;
            if (IsAppearing)
            {
                alpha = 1f - (_appearedAt - now) / APPEAR_DURATION;
            }
            else if (_isDisappearing)
            {
                alpha = 1f - (now - _disappearStartedAt) / FADE_OUT_DURATION;
                if (alpha <= 0f)
                {
                    _isAlive = false;
                    return;
                }
            }
            var color = AttackAreaFlashManager.ENEMY_COLOR;
            color.a = Mathf.Clamp01(alpha);
            stage.AttackAreaFlashes.Show(_attackArea, color);

            if (IsAppearing)
            {
                return;
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
