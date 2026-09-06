using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;

namespace SamMul.GameClients.Stages.Characters.Animations
{
    public class SpriteMonsterAnimationController : MonsterAnimationController
    {
        private readonly Animator _animator;
        private readonly AnimationClip _walk;
        private readonly AnimationClip _idle;
        private readonly AnimationClip _dead;
        private readonly AnimationClip _hitted; // 영문법과는 맞지 않지만, 피격됨을 명시적으로 나타내기 위해 -ed를 붙인다.

        private readonly AnimationClip _attack;
        private readonly AnimationClip _appear;
        private readonly AnimationClip _disappear;
        private readonly AnimationClip _heal;

        public override float AttackAnimationDuration => _attack?.length ?? 0.0f;
        public override float AppearAnimationDuration => _appear?.length ?? 0.0f;
        public override float DeadAnimationDuration => _dead?.length ?? 0.0f;
        public override float DisappearAnimationDuration => _disappear?.length ?? 0.0f;

        protected new SpriteRenderer _renderer => (SpriteRenderer)base._renderer;
        public override float BodyLocalScale => 1.0f;

        public override bool IsFlippedX => _renderer.flipX; 

        public override bool IsPlayingMovement
        {
            get
            {
                return this.GetCurrentAnimation() == _walk;
            }
        }

        private Color _fillColor;
        private float _fillingRate;
        private bool _isUpdateShaderProperty;

        private Queue<AnimationClip> _animationQueue;
        private AnimationClip? _currentAnimation;

        public SpriteMonsterAnimationController(
            Animator animator,
            Renderer renderer,
            bool isPlayerOrBoss,
            string idleAnimationName,
            string walkAnimationName,
            string hittedAnimationName,
            string attackAnimationName,
            string deadAnimationName,
            string appearAnimationName,
            string disappearAnimationName,
            string healAnimationName
            ) : base(isPlayerOrBoss, renderer, "SamMul/SpriteFillEffect")
        {
            _animator = animator;
            _animator.transform.localScale = new Vector3(BodyLocalScale, BodyLocalScale, 1.0f);
            _animator.fireEvents = false;

            _hitted = this.FindAnimation(hittedAnimationName);
            _attack = this.FindAnimation(attackAnimationName);
            _dead = this.FindAnimation(deadAnimationName);
            _appear = this.FindAnimation(appearAnimationName);
            _disappear = this.FindAnimation(disappearAnimationName) ?? _dead;
            _heal = this.FindAnimation(healAnimationName);

            _idle = this.FindAnimation(idleAnimationName);
            Debug.Assert(_idle != null, $"[{_animator.transform.parent.name}] idle 애니메이션이 존재하지 않습니다.");
            _walk = this.FindAnimation(walkAnimationName);
            Debug.Assert(_walk != null, $"[{_animator.transform.parent.name}] walk 애니메이션이 존재하지 않습니다.");

            _animationQueue = new Queue<AnimationClip>();
            _currentAnimation = null;
            this.SetAnimation(_idle, 1.0f);
        }

        public override void Update()
        {
            base.Update();

            if(0 != _animationQueue.Count) 
            {
                if (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
                {
                    AnimationClip nextClip = _animationQueue.Dequeue();
                    if (nextClip != null)
                    {
                        _animator.Play(nextClip.name, 0);
                        _animator.speed = 1f;
                    }
                    _currentAnimation = nextClip;
                }
            }
        }

        public void SetAnimation(AnimationClip? clip, float timeScale)
        {
            // NOTE: SetAnimation 할 때는 기존 대기열 애니메이션을 날리고 강제로 재생하는것으로 한다.
            _animationQueue.Clear();

            if (clip == null)
            {
                clip = _idle;
                timeScale = 1f;
            }

            if (clip != null)
            {
                _animator.Play(clip.name, 0);
                _animator.speed = timeScale;
            }

            _currentAnimation = clip;
        }

        private AnimationClip? GetCurrentAnimation()
        {
            if (_currentAnimation != null)
            {
                return _currentAnimation;
            }

            AnimatorClipInfo[] clipInfo = _animator.GetCurrentAnimatorClipInfo(0);
            if ((0 == clipInfo.Length))
            {
                _currentAnimation = null;
                return null;
            }

            _currentAnimation = clipInfo[0].clip;
            return _currentAnimation;
        }

        private void ContinueAnimation(AnimationClip? clip)
        {
            _animationQueue.Enqueue(clip);
        }

        private AnimationClip FindAnimation(string animationName)
        {
            AnimationClip[] clips = _animator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; ++i)
            {
                if (clips[i].name == animationName)
                {
                    return clips[i];
                }
            }
            return null;
        }

        public override void PlayAttackForceSlowwing(float attackDuration)
        {
            if (_attack == null)
            {
                return;
            }

            float timeScale = _attack.length / attackDuration;
            this.SetAnimation(_attack, timeScale);
        }

        public override float FindHitTimeOnAttackAnimation()
        {
            if (null == _attack)
            {
                return 0.0f;
            }

            AnimationEvent[] events = _attack.events;
            if (events.Length > 0 && events[0].functionName == "HitFrame")
            {
                return events[0].time;
            }

            return 0;
        }

        public override void SetToInitialState()
        {
            _activeBodyEffects.Clear();
            this.ClearBodyEffectShader();
        }

        public override void PlayIdleAttackActionInfinitely()
        {
            // 몬스터는 여기서 Idle애니메이션 재생
            if (_idle == null)
            {
                this.SetAnimation(null, 1.0f);
                return;
            }

            if (this.GetCurrentAnimation() == _idle)
            {
                return;
            }

            this.SetAnimation(_idle, 1.0f);
        }

        public override void PlayMoveInfinitely(Vector2 moveVector)
        {
            if (_walk == null)
            {
                this.SetAnimation(null, 1.0f);
                return;
            }

            var currentAnimation = this.GetCurrentAnimation();
            if (currentAnimation == _walk)
            {
                return;
            }

            if (null != _hitted && currentAnimation == _hitted)
            {
                // 히트 재생중엔 히트끝난 뒤에 이동애니메이션 재생한다.
                this.ContinueAnimation(_walk);
                return;
            }

            this.SetAnimation(_walk, 1.0f);
        }

        public override void PlayAttackForce(float attackDuration)
        {
            if (_attack == null)
            {
                this.SetAnimation(null, 1f);
                return;
            }

            float timeScale = _attack.length / attackDuration;
            this.SetAnimation(_attack, timeScale);
        }

        public override void StopAttack()
        {
            if (_attack == null)
            {
                this.SetAnimation(_idle, 1.0f);
                return;
            }

            if (this.GetCurrentAnimation() != _attack)
            {
                return;
            }

            this.SetAnimation(_idle, 1.0f);
        }

        public override void StopMovement()
        {
            base.StopMovement();

            this.SetAnimation(_idle, 1.0f);
        }

        public override void PlayHitted(bool continuePreviousAnimation)
        {
            if (this.IsSkipHitAnimation)
            {
                return;
            }

            if (_hitted == null)
            {
                return;
            }

            AnimationClip previousAnimation = null;
            bool wasLoopped = false;
            if (continuePreviousAnimation)
            {
                previousAnimation = this.GetCurrentAnimation();
                if (previousAnimation == _hitted)
                {
                    // 앞에 Hit가 재생되고 있었다면, 구태여 이어서 다시 재생하지 않는다.
                    previousAnimation = null;
                }
                if (previousAnimation != null)
                {
                    wasLoopped = previousAnimation.isLooping;
                }
            }

            this.SetAnimation(_hitted, 1.0f);
            if (previousAnimation != null &&
                continuePreviousAnimation)
            {
                this.ContinueAnimation(previousAnimation);
            }
        }

        public override void StopAllAndPlayDead()
        {
            if (_dead == null)
            {
                return;
            }

            this.SetAnimation(_dead, 1.0f);
            this.BeginDeadBodyEffect();
        }

        public override void PlayAppear(float appearDuration)
        {
            if (_appear == null)
            {
                return;
            }

            float timeScale = _appear.length / appearDuration;
            this.SetAnimation(_appear, timeScale);
        }

        public override void PlayDisappear()
        {
            if (_disappear == null)
            {
                return;
            }

            this.SetAnimation(_disappear, 1.0f);
        }

        public override void PlayHeal(float healDuration)
        {
            if (_heal == null)
            {
                return;
            }

            float timeScale = _heal.length / healDuration;
            this.SetAnimation(_heal, timeScale);
        }

        public override void UpdateBodyDirectionByMoveDirection(Vector2 moveDir)
        {
            bool isMovingToLeft = moveDir.x <= 0;
            _renderer.flipX = !isMovingToLeft;
        }

        public override void ClearBodyEffectShader()
        {
            _fillColor = Color.white;
            _fillingRate = 0.0f;
            _isUpdateShaderProperty = false;
            int count = _renderer.materials.Length;
            for (int i = 0; i < count; ++i)
            {
                _renderer.SetPropertyBlock(null, i);
            }
        }

        public override void FillBody(float fillingRate, Color color)
        {
            if (_fillShader == null)
            {
                return;
            }

            if (fillingRate <= 0f)
            {
                this.ClearBodyEffectShader();
                return;
            }

            _fillColor = color;
            _fillingRate = fillingRate;
            _isUpdateShaderProperty = true;
        }

        /// <summary>
        /// SpriteRenderer의 알파값을 조절해주는 함수
        /// </summary>
        /// <param name="alpha">0.0f ~ 1.0f</param>
        public override  void SetBodyAlpha(float alpha)
        {
            if(_renderer == null)
            {
                Debug.LogError("_renderer 가 null값이면 안됩니다. 로직 확인이 필요합니다.");
                return;
            }
            Color prevColor = _renderer.color;
            _renderer.color = new Color(prevColor.r, prevColor.g, prevColor.b, alpha);
        }

        private void SetShaderProperty()
        {
            _renderer.GetPropertyBlock(_materialProperty);
            _materialProperty.SetFloat(_fillPhaseId, _fillingRate);
            _materialProperty.SetColor(_fillColorId, _fillColor);

            int count = _renderer.materials.Length;
            for (int i = 0; i < count; ++i)
            {
                _renderer.SetPropertyBlock(_materialProperty, i);
            }
        }

        public void LateUpdate()
        {
            if (_isUpdateShaderProperty)
            {
                SetShaderProperty();
            }
        }

        public override void GetAnimationNames(out List<string> animationNames)
        {
            AnimationClip[] animationClips = _animator.runtimeAnimatorController.animationClips;
            animationNames = new List<string>(animationClips.Length);
            foreach(AnimationClip clip in animationClips)
            {
                animationNames.Add(clip.name);
            }
        }

        public override void PlayAnimationDevToolMode(string animationName)
        {
            var animation = this.FindAnimation(animationName);
            if (animation == null)
            {
                return;
            }
            this.SetAnimation(animation, 1.0f);
        }
    }
}