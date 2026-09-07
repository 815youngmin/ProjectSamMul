using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    /// <summary>
    /// 메테오. 전용 리소스 없이 떨어지는 운석은 공용 공격 비주얼로, 낙하 후 불타는 바닥은 AttackAreaFlashManager 로 표시한다.
    /// </summary>
    public class MeteorAreaEffectObject : AreaEffectObjectBase
    {
        private static readonly Quaternion METEOR_DROP_ROTATION = Quaternion.Euler(0.0f, 0.0f, 45.0f);
        private static readonly Vector2 METEOR_DROP_DIRECTION = METEOR_DROP_ROTATION * Vector2.down;
        private static readonly float METEOR_DROP_SPEED = 30.0f;
        private static readonly float METEOR_DROP_TIME = 1.0f;
        private static readonly float AREA_EFFECT_DAMAGE_PER_TICK_COEFFICIENT = 0.25f;
        private static readonly float AREA_EFFECT_TICK_PERIOD = 0.25f;
        // 부모 스케일(공격 반지름) 기준 운석 지름
        private const float METEOR_DIAMETER = 0.8f;

        public override bool IsAlive => _isAlive;

        private GameObject _visual;

        private Character _owner;
        private float _attackDamage;
        private float _attackRadius;
        private float _knockbackPower;
        private float _areaEffectDamagePerTick;
        private float _tickPeriod;
        private bool _isTranscendent;

        private CircularTargetArea _targetArea;
        private bool _isAlive;
        private bool _hasBoomed;
        private float _boomAt;
        private float _disappearsAt;
        private float _tickAt;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.Meteor);

            _visual = PlayerAttackVisual.Attach(transform, METEOR_DIAMETER);
        }

        public void Initialize(
            Character owner,
            Vector2 dropPosition,
            float attackDamage,
            float attackRadius,
            float knockbackPower,
            float areaEffectLifetime,
            bool isTranscendent)
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _attackDamage = attackDamage;
            _attackRadius = attackRadius;
            _knockbackPower = knockbackPower;
            _areaEffectDamagePerTick = AREA_EFFECT_DAMAGE_PER_TICK_COEFFICIENT * _attackDamage;
            _tickPeriod = AREA_EFFECT_TICK_PERIOD / _owner.Stats.SkillAttackSpeed.Value;
            _isTranscendent = isTranscendent;

            _targetArea = new CircularTargetArea(dropPosition, attackRadius);
            _isAlive = true;
            _hasBoomed = false;
            _boomAt = Time.time + METEOR_DROP_TIME;
            _disappearsAt = _boomAt + areaEffectLifetime;
            _tickAt = _boomAt + _tickPeriod;

            transform.SetPositionAndRotation(dropPosition - METEOR_DROP_SPEED * METEOR_DROP_DIRECTION, METEOR_DROP_ROTATION);
            transform.localScale = attackRadius * Vector3.one;

            _visual.SetActive(true);
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if (now < _boomAt)
            {
                transform.Translate(deltaTime * METEOR_DROP_SPEED * METEOR_DROP_DIRECTION, Space.World);
                return;
            }

            if (!_hasBoomed)
            {
                this.Boom(stage);
                _hasBoomed = true;
            }

            // 불타는 바닥 범위를 사라질 때까지 계속 표시한다.
            stage.AttackAreaFlashes.Show(_targetArea);

            if (_disappearsAt < now)
            {
                _isAlive = false;
                return;
            }

            if (_tickAt < now)
            {
                this.Tick(stage);
                _tickAt += _tickPeriod;
            }
        }

        private void Boom(Stage stage)
        {
            transform.rotation = Quaternion.identity;
            _visual.SetActive(false);

            CombatSystem.HitOnTargetArea(stage, _targetArea, _owner, _attackDamage, CombatSystem.KnockBackType.Pivot, _targetArea.Center, _knockbackPower, null, null, null);
        }

        private void Tick(Stage stage)
        {
            CombatSystem.HitOnTargetArea(stage, _targetArea, _owner, _areaEffectDamagePerTick, CombatSystem.KnockBackType.Pivot, Vector2.zero, 0.0f, null, null, null);
            stage.ForAllAliveAreaEffects((areaEffect) =>
            {
                if (areaEffect.AreaEffectObjectType != AreaEffectType.PoisonousArea)
                {
                    return true;
                }

                var poisonousAreaEffect = areaEffect as PoisonousAreaEffectObject;
                if (poisonousAreaEffect == null)
                {
                    return true;
                }

                if (!_targetArea.Contains(poisonousAreaEffect.Center, poisonousAreaEffect.Radius))
                {
                    return true;
                }

                poisonousAreaEffect.TryDisappear();
                return true;
            });
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _visual.SetActive(false);
        }
    }
}
