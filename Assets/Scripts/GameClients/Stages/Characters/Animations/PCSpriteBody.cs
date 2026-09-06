#nullable enable
using UnityEngine;
using SamMul.Animations.Placeholder;

namespace SamMul.GameClients.Stages.Characters.Animations
{
    /// <summary>
    /// 스프라이트 한 장 + 애니메이터 컨트롤러(idle/run/attack/die)로 그리는 플레이어 몸체.
    /// 스킬·액션 코드는 그대로 플레이스홀더 스켈레톤의 트랙을 조작하고, 이 클래스는 트랙에서 시작·종료되는
    /// 애니메이션 이름을 받아 같은 이름의 애니메이터 상태를 재생한다. 몬스터의 SpriteMonsterAnimationController 와 같은 자산 구성이다.
    /// </summary>
    public class PCSpriteBody : MonoBehaviour
    {
        // 화면에 보일 캐릭터 키(월드 단위). 원본 스프라이트 크기와 무관하게 이 높이에 맞춘다.
        private const float TARGET_HEIGHT = 1.8f;

        private SpriteRenderer _renderer = null!;
        private Animator _animator = null!;
        private Transform _fitRoot = null!;
        private bool _isFitted;

        // 이동 트랙(idle/run)의 상태. 공격이 끝나면 이 상태로 돌아온다.
        private string _baseState = string.Empty;
        // 공격 트랙에서 재생 중인 상태. 끝나면 _baseState 로 돌아온다.
        private string? _overrideState;
        private bool _isDead;

        public SpriteRenderer Renderer => _renderer;

        /// <summary>몸체 오브젝트 아래에 스프라이트 몸체를 만든다.</summary>
        public static PCSpriteBody Create(GameObject bodyObject, RuntimeAnimatorController animatorController)
        {
            var fitRoot = new GameObject("SpriteRoot");
            fitRoot.transform.SetParent(bodyObject.transform, false);

            var spriteObject = new GameObject("Sprite");
            spriteObject.transform.SetParent(fitRoot.transform, false);

            var body = spriteObject.AddComponent<PCSpriteBody>();
            body._fitRoot = fitRoot.transform;
            body._renderer = spriteObject.AddComponent<SpriteRenderer>();
            body._animator = spriteObject.AddComponent<Animator>();
            body._animator.runtimeAnimatorController = animatorController;
            body._animator.fireEvents = false;
            return body;
        }

        public void Bind(SkeletonAnimation skeleton)
        {
            skeleton.AnimationState.Start += this.OnTrackStart;
            skeleton.AnimationState.Complete += this.OnTrackComplete;
        }

        public void ResetState()
        {
            _isDead = false;
            _overrideState = null;
            _baseState = string.Empty;
            this.Play("idle");
        }

        public void SetFlipped(bool flipped) => _renderer.flipX = flipped;

        public void SetAlpha(float alpha)
        {
            var color = _renderer.color;
            color.a = alpha;
            _renderer.color = color;
        }

        private void OnTrackStart(TrackEntry entry)
        {
            string name = entry.Animation.Name;
            if (!this.HasState(name))
            {
                return;
            }

            switch ((BodyAnimationTrack)entry.TrackIndex)
            {
                case BodyAnimationTrack.Movement:
                case BodyAnimationTrack.WholeBody:
                    _baseState = name;
                    if (name == "die")
                    {
                        _isDead = true;
                        _overrideState = null;
                    }
                    if (_overrideState == null)
                    {
                        this.Play(name);
                    }
                    break;
                case BodyAnimationTrack.AttackAction:
                    if (_isDead || entry.Loop)
                    {
                        return;
                    }
                    _overrideState = name;
                    this.Play(name);
                    break;
            }
        }

        private void OnTrackComplete(TrackEntry entry)
        {
            if (_overrideState != null && entry.TrackIndex == (int)BodyAnimationTrack.AttackAction && entry.Animation.Name == _overrideState)
            {
                _overrideState = null;
                if (!string.IsNullOrEmpty(_baseState))
                {
                    this.Play(_baseState);
                }
            }
        }

        private bool HasState(string name) => _animator.HasState(0, Animator.StringToHash(name));

        private void Play(string name)
        {
            if (this.HasState(name))
            {
                _animator.Play(name, 0, 0f);
            }
        }

        private void LateUpdate()
        {
            // 첫 프레임이 그려진 뒤에야 스프라이트 크기를 알 수 있다. 발이 몸체 원점에 오도록 맞춘다.
            if (_isFitted || _renderer.sprite == null)
            {
                return;
            }
            float parentScale = _fitRoot.parent != null ? _fitRoot.parent.lossyScale.y : 1f;
            float spriteHeight = _renderer.sprite.bounds.size.y;
            float scale = TARGET_HEIGHT / (spriteHeight * parentScale);
            _fitRoot.localScale = new Vector3(scale, scale, 1f);
            _fitRoot.localPosition = new Vector3(0f, spriteHeight * scale * 0.5f, 0f);
            _isFitted = true;
        }
    }
}
