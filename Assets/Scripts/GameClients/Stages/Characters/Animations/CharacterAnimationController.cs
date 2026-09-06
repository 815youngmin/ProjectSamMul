#nullable enable
using System.Collections.Generic;
using UnityEngine;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using Event = SamMul.Animations.Placeholder.Event;

namespace SamMul.GameClients.Stages.Characters.Animations
{
    public enum BodyAnimationTrack : int
    {
        Face = 0,           // facial expressions
        Movement,           // lower body / locomotion
        AttackAction,       // upper body / attack actions
        WholeBody,          // whole-body clips (monsters)
        OverBodyEffect,     // effects layered over the body, independent of the body clips
    }

    /// <summary>
    /// Base of the per-character animation drivers. Body effects (hit flash, burn, death fade) are written
    /// as _FillPhase/_FillColor into the renderer's property block; the placeholder body blends them in and
    /// sprite bodies draw with the SamMul/SpriteFillEffect material.
    /// </summary>
    public abstract class CharacterAnimationController
    {
        protected readonly List<CharacterBodyEffect> _activeBodyEffects;
        protected readonly List<CharacterBodyEffect> _expiredBodyEffects;

        #region FillShader
        protected readonly Renderer _renderer;
        protected readonly Shader? _fillShader;
        protected readonly int _fillPhaseId;
        protected readonly int _fillColorId;
        protected readonly MaterialPropertyBlock _materialProperty;
        private static Material? s_spriteFillMaterial;
        #endregion

        /// <summary>When set, PlayHitted requests are ignored so the current clip keeps playing.</summary>
        public bool IsSkipHitAnimation => _isSkipHitAnimation;
        private bool _isSkipHitAnimation;

        public abstract bool IsPlayingMovement { get; }
        public abstract float BodyLocalScale { get; }
        public abstract bool IsFlippedX { get; }
        public int SortingOrder => _renderer != null ? _renderer.sortingOrder : 0;

        public bool IsPlayerOrBoss => _isPlayerOrBoss;
        private readonly bool _isPlayerOrBoss;

        protected CharacterAnimationController(bool isPlayerOrBoss, Renderer renderer, string shaderPath)
        {
            _isPlayerOrBoss = isPlayerOrBoss;
            _renderer = renderer;
            _activeBodyEffects = new List<CharacterBodyEffect>();
            _expiredBodyEffects = new List<CharacterBodyEffect>();
            _isSkipHitAnimation = false;

            _renderer.sortingLayerID = SortingLayer.NameToID("Object");

            _fillPhaseId = Shader.PropertyToID("_FillPhase");
            _fillColorId = Shader.PropertyToID("_FillColor");
            _fillShader = Shader.Find(shaderPath); // null for the Spine placeholder; that body reads the property block instead

            // 스프라이트 몸체는 채움 셰이더 머티리얼로 그려야 _FillPhase/_FillColor 가 보인다.
            if (_fillShader != null && renderer is SpriteRenderer spriteRenderer)
            {
                s_spriteFillMaterial ??= new Material(_fillShader) { name = "SpriteFillEffect", hideFlags = HideFlags.HideAndDontSave };
                spriteRenderer.sharedMaterial = s_spriteFillMaterial;
            }

            _materialProperty = new MaterialPropertyBlock();
        }

        public abstract void SetToInitialState();

        public virtual void Update()
        {
            UpdateBodyEffect();
        }

        public abstract void StopMovement();
        public abstract void PlayIdleAttackActionInfinitely();
        public abstract void PlayMoveInfinitely(Vector2 moveVector);
        public abstract void PlayAttackForce(float attackDuration);
        public abstract void StopAttack();
        public abstract void PlayHitted(bool continuePreviousAnimation);
        public abstract void StopAllAndPlayDead();
        public abstract float DeadAnimationDuration { get; }
        public abstract void PlayAppear(float appearDuration);
        public abstract float AppearAnimationDuration { get; }
        public abstract void PlayDisappear();
        public abstract float DisappearAnimationDuration { get; }
        public abstract void PlayHeal(float healDuration);
        public abstract void UpdateBodyDirectionByMoveDirection(Vector2 moveDir);
        public abstract void BeginHittedBodyEffect(bool isBigCharacter);
        /// <param name="alpha">0.0f ~ 1.0f</param>
        public abstract void SetBodyAlpha(float alpha);

        #region Shader-Based Body Effect
        public virtual void ClearBodyEffectShader()
        {
            // 블록을 통째로 지워 렌더러 기본값(스프라이트 텍스처 포함)으로 되돌린다.
            _materialProperty.Clear();
            _renderer.SetPropertyBlock(null);
        }

        /// <param name="fillingRate">0.0 ~ 1.0</param>
        public virtual void FillBody(float fillingRate, Color color)
        {
            if (fillingRate <= 0f)
            {
                ClearBodyEffectShader();
                return;
            }

            // SpriteRenderer 는 _MainTex 등을 자체 블록으로 넘기므로 기존 값을 읽어 온 뒤에 덧써야 한다.
            _renderer.GetPropertyBlock(_materialProperty);
            _materialProperty.SetFloat(_fillPhaseId, fillingRate);
            _materialProperty.SetColor(_fillColorId, color);
            _renderer.SetPropertyBlock(_materialProperty);
        }
        #endregion

        public void SkipHitAnimation(bool skipIfTrue)
        {
            _isSkipHitAnimation = skipIfTrue;
        }

        private void UpdateBodyEffect()
        {
            float now = Time.time;
            foreach (var bodyEffect in _activeBodyEffects)
            {
                bodyEffect.Progress(now);
                if (bodyEffect.EndAt <= now)
                {
                    _expiredBodyEffects.Add(bodyEffect);
                }
            }

            if (_expiredBodyEffects.Count == 0)
            {
                return;
            }

            foreach (var bodyEffect in _expiredBodyEffects)
            {
                _activeBodyEffects.Remove(bodyEffect);
            }
            _expiredBodyEffects.Clear();

            // Fall back to the newest remaining effect, or to the plain body.
            if (_activeBodyEffects.Count > 0)
            {
                _activeBodyEffects[_activeBodyEffects.Count - 1].Apply();
            }
            else
            {
                ClearBodyEffectShader();
            }
        }

        public void BeginDeadBodyEffect()
        {
            // Players and bosses keep their normal look while dying.
            if (_isPlayerOrBoss)
            {
                return;
            }

            int previousIndex = _activeBodyEffects.FindIndex(x => x.Type == CharacterBodyEffectType.Dead);
            if (previousIndex >= 0)
            {
                _activeBodyEffects.RemoveAt(previousIndex);
            }

            bool darkened = false;
            var deadEffect = CharacterBodyEffect.Dead(
                bodyEffectApplier: () => FillBody(1.0f, Color.white),
                progressiveBodyEffect: (float leftTime, float progressedTime) =>
                {
                    // Short white flash, then stay darkened.
                    if (!darkened && progressedTime > 0.05f)
                    {
                        darkened = true;
                        FillBody(0.6f, Color.black);
                    }
                });
            deadEffect.Apply();
            _activeBodyEffects.Add(deadEffect);
        }

        public void BeginBurnBodyEffect(float duration)
        {
            int index = _activeBodyEffects.FindIndex(x => x.Type == CharacterBodyEffectType.Burn);
            if (index >= 0)
            {
                _activeBodyEffects.RemoveAt(index);
            }

            var burnEffect = CharacterBodyEffect.Burn(duration, () => FillBody(0.55f, Color.red));
            burnEffect.Apply();
            _activeBodyEffects.Add(burnEffect);
        }

        /// <summary>
        /// Finds the first frame event of <paramref name="eventData"/> in the clip. Placeholder clips carry no
        /// authored events, so a synthetic event at the middle of the clip is returned instead; callers rely
        /// on a non-null result to read the hit time.
        /// </summary>
        public static Event FindEventInAnimationTimeline(Animation animation, EventData? eventData)
        {
            if (eventData != null)
            {
                foreach (var frameEvent in animation.Events)
                {
                    if (frameEvent.Data == eventData)
                    {
                        return frameEvent;
                    }
                }
            }

            return new Event(animation.Duration * 0.5f, eventData ?? new EventData("hit"));
        }

    }
}
