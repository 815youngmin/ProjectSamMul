using DG.Tweening;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;
using Sequence = DG.Tweening.Sequence;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class MeteorAreaEffectObject : AreaEffectObjectBase
    {
        private const string NORMAL_BOOM_EFFECT_PATH = "Stages/SkillEffects/fx_meteoExplosion.prefab";
        private const string NORMAL_SKELETON_ANIMATION_PATH = "Stages/AreaEffects/Meteor/prefab_MeteorAreaEffectObject.prefab";
        private const string TRANSCENDENT_DROP_ANIMATION_PATH = "Stages/AreaEffects/Meteor/prefab_MeteorBall_S_AreaEffectObject.prefab";
        private const string TRANSCENDENT_BOOM_ANIMATION_PATH = "Stages/AreaEffects/Meteor/prefab_MeteorBoom_S_AreaEffect.prefab";
        private const string NORMAL_AREA_EFFECT_ANIMATION_PATH = "Stages/AreaEffects/Meteor/prefab_MeterorAreaEffectGroundObject.prefab";
        private const string TRANSCENDENT_AREA_EFFECT_ANIMATION_PATH = "Stages/AreaEffects/Meteor/prefab_MeteorGround_S_AreaEffectObject.prefab";

        private static readonly Quaternion METEOR_DROP_ROTATION = Quaternion.Euler(0.0f, 0.0f, 45.0f);
        private static readonly Vector2 METEOR_DROP_DIRECTION = METEOR_DROP_ROTATION * Vector2.down;
        private static readonly float METEOR_DROP_SPEED = 30.0f;
        private static readonly float METEOR_DROP_TIME = 1.0f;
        private static readonly float AREA_EFFECT_DAMAGE_PER_TICK_COEFFICIENT = 0.25f;
        private static readonly float AREA_EFFECT_TICK_PERIOD = 0.25f;
        private static readonly float FADE_IN_DURATION = 0.5f;
        private static readonly float FADE_OUT_DURATION = 0.5f;

        public override bool IsAlive => _isAlive;

        private SkeletonAnimation _normalSkeletonAnimation;
        private SpriteAnimationHandler _normalAreaEffectAnimation;
        private SpriteAnimationHandler _transcendentDropAnimation;
        private SpriteAnimationHandler _transcendentBoomAnimation;
        private SpriteAnimationHandler _transcendentAreaEffectAnimation;

        private Animation _normalDropAnimation;
        private Animation _normalBoomAnimation;
        private Sequence _normalAreaEffectFadeIn;
        private Sequence _normalAreaEffectFadeOut;
        private Sequence _transcendentAreaEffectFadeOut;
        private Color _transcendentAreaEffectColor;


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

            _normalSkeletonAnimation = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(NORMAL_SKELETON_ANIMATION_PATH);
            _normalSkeletonAnimation.transform.SetParent(transform);
            _normalSkeletonAnimation.transform.localPosition = Vector3.zero;
            _normalSkeletonAnimation.transform.localScale = 0.8f * Vector3.one;
            _normalSkeletonAnimation.skeleton.FindSlot("size").Attachment = null; // 배경 제거.

            _normalAreaEffectAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(NORMAL_AREA_EFFECT_ANIMATION_PATH);
            _normalAreaEffectAnimation.InitializeOnly();
            _normalAreaEffectAnimation.transform.SetParent(transform);
            _normalAreaEffectAnimation.transform.localPosition = Vector3.zero;
            _normalAreaEffectAnimation.transform.localScale = 0.8f * Vector3.one;

            _transcendentDropAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(TRANSCENDENT_DROP_ANIMATION_PATH);
            _transcendentDropAnimation.InitializeOnly();
            _transcendentDropAnimation.transform.SetParent(transform);
            _transcendentDropAnimation.transform.localPosition = Vector3.zero;
            _transcendentDropAnimation.transform.localScale = 0.8f * Vector3.one;

            _transcendentBoomAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(TRANSCENDENT_BOOM_ANIMATION_PATH);
            _transcendentBoomAnimation.InitializeOnly();
            _transcendentBoomAnimation.transform.SetParent(transform);
            _transcendentBoomAnimation.transform.localPosition = Vector3.zero;
            _transcendentBoomAnimation.transform.localScale = 0.8f * Vector3.one;

            _transcendentAreaEffectAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(TRANSCENDENT_AREA_EFFECT_ANIMATION_PATH);
            _transcendentAreaEffectAnimation.InitializeOnly();
            _transcendentAreaEffectAnimation.transform.SetParent(transform);
            _transcendentAreaEffectAnimation.transform.localPosition = Vector3.zero;
            _transcendentAreaEffectAnimation.transform.localScale = 0.8f * Vector3.one;

            _normalDropAnimation = _normalSkeletonAnimation.skeleton.Data.FindAnimation("ing");
            _normalBoomAnimation = _normalSkeletonAnimation.skeleton.Data.FindAnimation("hit");


            float maxFadeValue = 1f;
            _transcendentAreaEffectColor = Color.white;

            _normalAreaEffectFadeIn = DOTween.Sequence()
                .Append(_normalAreaEffectAnimation.SpriteRenderer.DOFade(maxFadeValue, FADE_IN_DURATION).From(0.0f))
                .SetRecyclable(true)
                .SetAutoKill(false)
                .Pause();

            _normalAreaEffectFadeOut = DOTween.Sequence()
                .Append(_normalAreaEffectAnimation.SpriteRenderer.DOFade(0.0f, FADE_OUT_DURATION).From(maxFadeValue))
                .SetRecyclable(true)
                .SetAutoKill(false)
                .OnComplete(() => _isAlive = false)
                .Pause();

            _transcendentAreaEffectFadeOut = DOTween.Sequence()
                .Append(_transcendentAreaEffectAnimation.SpriteRenderer.DOFade(0.0f, FADE_OUT_DURATION).From(maxFadeValue))
                .SetRecyclable(true)
                .SetAutoKill(false)
                .OnComplete(() => _isAlive = false)
                .Pause();

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
            _disappearsAt = _boomAt + areaEffectLifetime - FADE_OUT_DURATION;
            _tickAt = _boomAt + _tickPeriod;

            transform.SetPositionAndRotation(dropPosition - METEOR_DROP_SPEED * METEOR_DROP_DIRECTION, METEOR_DROP_ROTATION);
            transform.localScale = attackRadius * Vector3.one;

            if (_isTranscendent)
            {
                _normalSkeletonAnimation.gameObject.SetActive(false);
                _transcendentDropAnimation.gameObject.SetActive(true);
                _transcendentBoomAnimation.gameObject.SetActive(false);
                _normalAreaEffectAnimation.gameObject.SetActive(false);
                _transcendentAreaEffectAnimation.gameObject.SetActive(false);
                _transcendentDropAnimation.InitializeAndPlay();
            }
            else
            {
                _normalSkeletonAnimation.gameObject.SetActive(true);
                _transcendentDropAnimation.gameObject.SetActive(false);
                _transcendentBoomAnimation.gameObject.SetActive(false);
                _normalAreaEffectAnimation.gameObject.SetActive(false);
                _transcendentAreaEffectAnimation.gameObject.SetActive(false);
                _normalSkeletonAnimation.AnimationState.SetAnimation(0, _normalDropAnimation, loop: true);
            }
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

            if (_disappearsAt < now)
            {
                (_isTranscendent ? _transcendentAreaEffectFadeOut : _normalAreaEffectFadeOut).Restart();
                _disappearsAt = float.MaxValue;
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
            if (_isTranscendent)
            {
                _transcendentDropAnimation.gameObject.SetActive(false);
                _transcendentBoomAnimation.gameObject.SetActive(true);
                _transcendentBoomAnimation.InitializeAndPlay(null, () =>
                {
                    _transcendentBoomAnimation.gameObject.SetActive(false);
                    _transcendentAreaEffectAnimation.gameObject.SetActive(true);

                    _transcendentAreaEffectAnimation.SpriteRenderer.color = _transcendentAreaEffectColor;

                    _transcendentAreaEffectAnimation.Play();
                });
            }
            else
            {
                _normalSkeletonAnimation.AnimationState.SetAnimation(0, _normalBoomAnimation, loop: false);
                stage.Particles.CreateParticle(NORMAL_BOOM_EFFECT_PATH, transform.position, 0.8f * _attackRadius * Vector3.one);
                _normalAreaEffectAnimation.gameObject.SetActive(true);
                _normalAreaEffectAnimation.Play();
                _normalAreaEffectFadeIn.Restart();
            }

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

            _normalSkeletonAnimation.gameObject.SetActive(false);
            _transcendentDropAnimation.gameObject.SetActive(false);
            _transcendentBoomAnimation.gameObject.SetActive(false);
            _normalAreaEffectAnimation.gameObject.SetActive(false);
            _transcendentAreaEffectAnimation.gameObject.SetActive(false);

            _normalAreaEffectFadeIn.Pause();
            _normalAreaEffectFadeOut.Pause();
            _transcendentAreaEffectFadeOut.Pause();
        }
    }
}
