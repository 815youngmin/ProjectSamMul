#nullable enable
using UnityEngine;

namespace Z.UnityHelpers
{
    /// <summary>
    /// 스프라이트 프레임 목록을 지정한 fps 로 넘겨 보여주는 플립북 애니메이션입니다.
    /// 마지막 프레임을 지나면 <see cref="AnimationEnd"/>, 히트 프레임에 도달하면 <see cref="HitFrame"/>이 호출됩니다.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnimationHandler : MonoBehaviour
    {
        public delegate void EventHandler();

        [SerializeField] private Sprite[] _frames = System.Array.Empty<Sprite>();
        [SerializeField] private float _framesPerSecond = 12f;
        // 히트 이벤트를 발생시킬 프레임 인덱스. 음수면 히트 프레임 없음.
        [SerializeField] private int _hitFrameIndex = -1;
        // 마지막 프레임 이후 처음부터 반복할지 여부. 반복하더라도 한 바퀴마다 AnimationEnd 는 호출된다.
        [SerializeField] private bool _loop = false;

        private SpriteRenderer? _spriteRenderer;
        private Animator? _spriteAnimator;

        private EventHandler? _hitEventHandler;
        private EventHandler? _endEventHandler;

        private bool _isSharedResourcesAllocated;
        private bool _isPlaying;
        private bool _isAnimationEnded;
        private bool _hasHitFired;
        private float _elapsed;
        private int _currentFrame = -1;

        public bool IsAlive => !_isAnimationEnded;

        /// <summary>애니메이션 길이, 초 단위.</summary>
        public float AnimationDuration => _frames.Length / this.SafeFps;

        /// <summary>히트 프레임 시간, 초 단위. 히트 프레임이 없으면 0.</summary>
        public float HitTimeOnAnimation => _hitFrameIndex >= 0 ? _hitFrameIndex / this.SafeFps : 0f;

        public SpriteRenderer SpriteRenderer => _spriteRenderer != null ? _spriteRenderer : (_spriteRenderer = this.GetComponent<SpriteRenderer>());

        /// <summary>같은 오브젝트에 Animator 가 있으면 그 참조. 플립북 재생과는 무관합니다.</summary>
        public Animator? SpriteAnimator => _spriteAnimator;

        // SpriteAnimationManager 를 통해 풀링될 때 사용하는 리소스 경로. 직접 사용하는 경우 null.
        public string? PoolingKey { get; private set; }

        private float SafeFps => Mathf.Max(_framesPerSecond, 0.0001f);

        public void AllocateSharedResources(string? resourcePath)
        {
            if (_isSharedResourcesAllocated)
            {
                return;
            }
            _isSharedResourcesAllocated = true;

            PoolingKey = resourcePath;
            _spriteRenderer = this.GetComponent<SpriteRenderer>();
            _spriteAnimator = this.GetComponent<Animator>();
            _isAnimationEnded = false;
        }

        /// <summary>
        /// 재생하지 않고 초기화만 합니다.
        /// </summary>
        public void InitializeOnly()
        {
            this.AllocateSharedResources(null);
            _hitEventHandler = null;
            _endEventHandler = null;
            _isPlaying = false;
        }

        public void InitializeAndPlay(EventHandler? hitEventHandler, EventHandler? endEventHandler)
        {
            this.AllocateSharedResources(null);
            _hitEventHandler = hitEventHandler;
            _endEventHandler = endEventHandler;
            this.Play();
        }

        public void InitializeAndPlay(EventHandler? hitEventHandler)
        {
            this.InitializeAndPlay(hitEventHandler, null);
        }

        public void InitializeAndPlay()
        {
            this.InitializeAndPlay(null, null);
        }

        public void Play()
        {
            _isAnimationEnded = false;
            _hasHitFired = false;
            _elapsed = 0f;
            _currentFrame = -1;

            if (_frames.Length == 0)
            {
                Debug.LogWarning($"{this.name}에 프레임이 없습니다. 바로 종료 처리합니다.");
                _isPlaying = false;
                this.AnimationEnd();
                return;
            }

            _isPlaying = true;
            this.ShowFrame(0);
        }

        public void StopAndReserveToDestroy()
        {
            _isPlaying = false;
            _isAnimationEnded = true;
        }

        // 히트 프레임 도달 시 호출됩니다.
        public void HitFrame()
        {
            _hitEventHandler?.Invoke();
        }

        // 마지막 프레임을 지난 시점에 호출됩니다.
        public void AnimationEnd()
        {
            _isAnimationEnded = true;
            _endEventHandler?.Invoke();
        }

        private void Update()
        {
            if (!_isPlaying || _frames.Length == 0)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            int frame = Mathf.FloorToInt(_elapsed * this.SafeFps);
            bool cycleEnded = frame >= _frames.Length;
            this.ShowFrame(cycleEnded ? _frames.Length - 1 : frame);

            if (!cycleEnded)
            {
                return;
            }

            if (_loop)
            {
                _elapsed = 0f;
                _hasHitFired = false;
            }
            else
            {
                _isPlaying = false;
            }

            this.AnimationEnd();
        }

        private void ShowFrame(int frame)
        {
            if (frame != _currentFrame)
            {
                _currentFrame = frame;
                this.SpriteRenderer.sprite = _frames[frame];
            }

            if (!_hasHitFired && _hitFrameIndex >= 0 && frame >= _hitFrameIndex)
            {
                _hasHitFired = true;
                this.HitFrame();
            }
        }
    }
}
