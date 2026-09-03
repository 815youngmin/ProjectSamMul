using DG.Tweening;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class ShockBombAreaEffectObject : AreaEffectObjectBase
    {
        private static readonly Vector2 SHADOW_OFFSET = new Vector2(-0.0361f, -0.5f);
        private static readonly Vector3 SHADOW_MAX_SCALE = 1.2f * Vector3.one;
        private static readonly Vector3 SHADOW_MIN_SCALE = 0.8f * Vector3.one;

        public override bool IsAlive => !_isHit && Time.time <= _createdAt + _lifeTime;

        private Character _owner;
        private float _damage;

        private float _attackRadius;
        private float _knockbackPower;
        private float _duration;
        private float _hitPeriod; // Param 타격하는 주기
        private bool _isTranscend;
        private string _hitSoundPrefabPath;

        private float _createdAt;
        private float _lifeTime;
        private bool _isHit;

        private GameObject _body;
        private GameObject _shadow;
        private GameObject _effect;

        private MeshRenderer _bodyMeshRenderer;
        private SkeletonAnimation _bodySkeletonAnimation;
        private SkeletonAnimation _effectSkeletonAnimation;

        private Animation _normalThrowAnimation;
        private Animation _normalIdleAnimation;
        private Animation _normalArriveAnimation;
        private Animation _normalEndAnimation;

        private Animation _normalExplosionEffectAnimation;
        private Animation _normalIdleEffectAnimation;

        private Animation _transcendThrowAnimation;
        private Animation _transcendIdleAnimation;
        private Animation _transcendArriveAnimation;
        private Animation _transcendEndAnimation;
        private Animation _transcendExplosionEffectAnimation;
        private Animation _transcendIdleEffectAnimation;


        private Animation _throwAnimation => _isTranscend ? _transcendThrowAnimation : _normalThrowAnimation;
        private Animation _idleAnimation => _isTranscend ? _transcendIdleAnimation : _normalIdleAnimation;
        private Animation _arriveAnimation => _isTranscend ? _transcendArriveAnimation : _normalArriveAnimation;
        private Animation _endAnimation => _isTranscend ? _transcendEndAnimation : _normalEndAnimation;
        private Animation _explosionEffectAnimation => _isTranscend ? _transcendExplosionEffectAnimation : _normalExplosionEffectAnimation;
        private Animation _idleEffectAnimation => _isTranscend ? _transcendIdleEffectAnimation : _normalIdleEffectAnimation;

        private float _arriveAt;
        private float _hitAt;
        private float _hitEndAt;

        private float _tickDamageNextAt;
        private HashSet<Character> _hittedCharacters;

        private Color _effectColor;
        private Sequence _fadeOut;


        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.ShockBomb);

            _body = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/ShockBomb/ShockBomb.prefab");
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;

            _shadow = new GameObject("Shadow");
            _shadow.transform.localPosition = Vector2.zero;
            _shadow.transform.localScale = SHADOW_MAX_SCALE;
            var shadow = _shadow.AddComponent<SpriteRenderer>();
            shadow.sprite = ResourcePool.Instance.LoadResource<Sprite>("Stages/Characters/characterShadow.png");
            shadow.color = new Color(shadow.color.r, shadow.color.g, shadow.color.b, 1.00f);
            shadow.sortingLayerID = SortingLayer.NameToID("LowShadow");
            shadow.drawMode = SpriteDrawMode.Simple;

            _effect = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/ShockBomb/ShockBomb_eff.prefab");
            _effect.transform.SetParent(this.transform);
            _effect.transform.localPosition = Vector2.zero;
            _effect.transform.localScale = Vector2.one;

            _bodyMeshRenderer = _body.GetComponent<MeshRenderer>();
            _bodySkeletonAnimation = _body.GetComponent<SkeletonAnimation>();
            _bodySkeletonAnimation.AnimationState.Data.DefaultMix = 0.0f;
            _effectSkeletonAnimation = _effect.GetComponent<SkeletonAnimation>();
            _effectSkeletonAnimation.AnimationState.Data.DefaultMix = 0.0f;

            _normalThrowAnimation = _bodySkeletonAnimation.skeleton.Data.FindAnimation("boom_Nthrow");
            _normalIdleAnimation = _bodySkeletonAnimation.skeleton.Data.FindAnimation("boom_Nidle");
            _normalArriveAnimation = _bodySkeletonAnimation.skeleton.Data.FindAnimation("boom_Ntrance");
            _normalEndAnimation = _bodySkeletonAnimation.skeleton.Data.FindAnimation("boom_Nend");
            
            _normalExplosionEffectAnimation = _effectSkeletonAnimation.skeleton.Data.FindAnimation("boom_N");
            _normalIdleEffectAnimation = _effectSkeletonAnimation.skeleton.Data.FindAnimation("idle_N");

            _transcendThrowAnimation = _bodySkeletonAnimation.skeleton.Data.FindAnimation("boom_Sthrow");
            _transcendIdleAnimation = _bodySkeletonAnimation.skeleton.Data.FindAnimation("boom_Sidle");
            _transcendArriveAnimation = _bodySkeletonAnimation.skeleton.Data.FindAnimation("boom_Strance");
            _transcendEndAnimation = _bodySkeletonAnimation.skeleton.Data.FindAnimation("boom_Send");

            _transcendExplosionEffectAnimation = _effectSkeletonAnimation.skeleton.Data.FindAnimation("boom_S");
            _transcendIdleEffectAnimation = _effectSkeletonAnimation.Skeleton.Data.FindAnimation("idle_S");

            _hittedCharacters = new HashSet<Character>();

            _effectColor = _effectSkeletonAnimation.Skeleton.GetColor();

            //공격 이펙트 사라질때 자연스럽게 사라지기 위해서 페이드 아웃 제작
            _fadeOut = DOTween.Sequence();
            _fadeOut.Append(DOTween.To(() => _effectColor.a, x => _effectSkeletonAnimation.skeleton.A = x, 0f, 0.2f));
            _fadeOut.OnComplete(() =>
            {
                _effectSkeletonAnimation.skeleton.SetColor(_effectColor);
                _effectSkeletonAnimation.AnimationState.SetEmptyAnimation(0, 0);
            });
            _fadeOut.SetRecyclable(true);
            _fadeOut.SetAutoKill(false);
            _fadeOut.Pause();
        }


        public void Initialize(
            Character owner,
            Vector2 arrivalPosition,
            float arrivalTime,
            float damage,
            float radius,
            float knockbackPower,
            float duration,
            float hitPeriod,
            bool isTranscend,
            string hitSoundPrefabPath
            )
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _damage = damage;
            _attackRadius = radius;
            _knockbackPower = knockbackPower;
            _duration = duration;
            _hitPeriod = hitPeriod;
            _isTranscend = isTranscend;
            _hitSoundPrefabPath = hitSoundPrefabPath;

            float now = Time.time;
            _createdAt = now;
            _lifeTime = arrivalTime + _arriveAnimation.Duration + duration + (_endAnimation.Duration / _bodySkeletonAnimation.timeScale);

            this.transform.position = _owner.CenterPos;
            this.transform.DOJump(arrivalPosition, jumpPower: 5f, numJumps: 1, duration: arrivalTime).SetEase(Ease.Linear);

            _shadow.SetActive(true);
            _shadow.transform.position = _owner.CenterPos + SHADOW_OFFSET;
            _shadow.transform.DOMove(arrivalPosition + SHADOW_OFFSET, duration: arrivalTime).SetEase(Ease.Linear);
            DOTween.Sequence(_shadow)
                .Append(_shadow.transform.DOScale(SHADOW_MIN_SCALE, arrivalTime / 2.0f))
                .Append(_shadow.transform.DOScale(SHADOW_MAX_SCALE, arrivalTime / 2.0f));

            _bodyMeshRenderer.sortingLayerID = SortingLayer.NameToID("HighParticle");
            _bodyMeshRenderer.sortingOrder = 0;
            _bodySkeletonAnimation.AnimationState.SetAnimation(0, _throwAnimation, true);
            _effectSkeletonAnimation.AnimationState.SetEmptyAnimation(0, mixDuration: 0f);

            _arriveAt = now + arrivalTime;
            _hitAt = now + arrivalTime + _arriveAnimation.Duration;
            _hitEndAt = now + arrivalTime + _arriveAnimation.Duration + _duration;
            _effect.transform.localScale = Vector3.one * radius;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            //ShockBomb이 목적지에 도착한 시점
            //변신 애니메이션을 재생 후 공격 이펙트를 활성화 해준다.
            if (_arriveAt <= now)
            {
                _arriveAt = float.MaxValue;
                _tickDamageNextAt = now;
                _bodyMeshRenderer.sortingLayerID = SortingLayer.NameToID("Object");
                _bodyMeshRenderer.sortingOrder = (int)(this.transform.position.y * -100.0f);
                _bodySkeletonAnimation.AnimationState.SetAnimation(0, _arriveAnimation, false);
                _bodySkeletonAnimation.AnimationState.AddAnimation(0, _idleAnimation, false, 0f);

                _effectSkeletonAnimation.AnimationState.SetEmptyAnimation(0, 0f);
                _effectSkeletonAnimation.AnimationState.AddAnimation(0, _explosionEffectAnimation, loop: false, _arriveAnimation.Duration);
                _effectSkeletonAnimation.AnimationState.AddAnimation(0, _idleEffectAnimation, loop: true, delay: 0f);

                _shadow.SetActive(false);
            }

            if (_hitAt <= now)
            {
                this.UpdateAttacked(stage, now);
            }


            if (_hitEndAt <= now)
            {
                // 종료 시점에 _endAniamtion 애니메이션을 재생해준다.
                // _endAnimation 이후 다시 _throwAnimation을 넣어주는 이유는 
                // 해당 오브젝트가 재사용될때 자꾸 _endAnimation 마지막 프레임이 보여서...
                // 실질적으로 _throwAnimation은 보이지 않고 종료된다. 혹시몰라 넣어두는것
                // 애니메이션 재생 속력이 1이 아니므로 _endAnimation 애니메이션의 길이를 제대로 참조하려면
                // (_endAnimation.Duration / _bodySkeletonAnimation.timeScale)를 참조해야 한다.
                _bodySkeletonAnimation.AnimationState.SetAnimation(0, _endAnimation, false);
                _bodySkeletonAnimation.AnimationState.AddAnimation(0, _throwAnimation, true, 0f);
                _fadeOut.Restart();

                _hitAt = float.MaxValue;
                _hitEndAt = float.MaxValue;
            }
        }

        private void UpdateAttacked(Stage stage, float now)
        {
            if (_tickDamageNextAt <= now)
            {
                _hittedCharacters.Clear();
                _tickDamageNextAt = Time.time + _hitPeriod;
            }

            CircularTargetArea circularTargetArea = new CircularTargetArea(this.transform.position, _attackRadius);
            CombatSystem.HitOnTargetArea(stage, circularTargetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, this.transform.position, _knockbackPower, _hittedCharacters, _hittedCharacters, _hitSoundPrefabPath);
        }

        public override void PuttingBackToPool()
        {
            _bodySkeletonAnimation.skeleton.SetBonesToSetupPose();

            _effectSkeletonAnimation.skeleton.SetBonesToSetupPose();
            _effectSkeletonAnimation.Skeleton.SetColor(_effectColor);

            _fadeOut.Pause();

            _hittedCharacters.Clear();
            base.PuttingBackToPool();
        }
    }

}
