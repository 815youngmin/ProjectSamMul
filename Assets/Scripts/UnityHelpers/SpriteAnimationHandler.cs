using UnityEngine;
using System.Linq;
using JetBrains.Annotations;

namespace SamMul.UnityHelpers
{
    /// <summary>
    /// 스프라이트 프레임 목록을 지정한 fps 로 넘겨 보여주는 플립북 애니메이션입니다.
    /// 마지막 프레임을 지나면 <see cref="AnimationEnd"/>, 히트 프레임에 도달하면 <see cref="HitFrame"/>이 호출됩니다.
    /// </summary>
    public class SpriteAnimationHandler : MonoBehaviour
    {
       
        public delegate void EventHandler();
        public bool IsAlive => !_isAnimationEnded;
        public float AnimationDuration => _clipDuration;

        public float HitTimeOnAnimation => _hitTimeOnAnimation;

        private SpriteRenderer _spriteRenderer;
        private Animator _spriteAnimator;
        private AnimationClip _animationClip;

        public SpriteRenderer SpriteRenderer => _spriteRenderer;
        [CanBeNull] public Animator SpriteAnimator => _spriteAnimator;

        private EventHandler _hitEventHandler;
        private EventHandler _endEventHandler;

        // 애니메이션 클립의 시간길이, 초단위
        private float _clipDuration;

        //애니메이션 클립의 히트프레임 시간, 초단위 (히트 프레임 없으면 0)
        private float _hitTimeOnAnimation;

        // 애니메이션이 종료되었는지 여부. 애니메이션 클립의 마지막 프레임이 실행되었거나, 사용자 요청에 의해 강제로 종료되었으면 true
        private bool _isAnimationEnded;

        private bool _isSharedResourcesAllocated = false;


        // 애니메이션 프리팹 파일의 리소스 경로를 PoolingKey로 사용하고 있다.
        // SpriteAnimationManager를 통해 풀링하는 경우에만 값이 유효하고,
        // SpriteAnimationManager를 통하지 않고 사용하는 경우 null 임.
        [CanBeNull] public string PoolingKey { get; private set; }

        public void AllocateSharedResources([CanBeNull] string resourcePath)
        {
            if (_isSharedResourcesAllocated)
            {
                return;
            }
            _isSharedResourcesAllocated = true;

            PoolingKey = resourcePath;
            _spriteRenderer = this.gameObject.GetComponent<SpriteRenderer>();
            _spriteAnimator = this.gameObject.GetComponent<Animator>();

            var animationController = _spriteAnimator.runtimeAnimatorController;
            if (animationController.animationClips.Length != 1)
            {
                Debug.LogWarning($"{nameof(SpriteAnimationHandler)}은 현재 애니메이션 클립 한개만 지원합니다. {this.name}에 {animationController.animationClips.Length}개의 클립이 있어 오동작 예상됩니다.");
                _clipDuration = 0f;
            }
            else
            {
                _animationClip = animationController.animationClips.First();
                _clipDuration = _animationClip.length;

                bool isExistEventData = false;
                bool isHitEventData = false;

                foreach (var eventData in _animationClip.events)
                {
                    if (eventData.functionName == nameof(AnimationEnd))
                    {
                        isExistEventData = true;
                    }
                    else if(eventData.functionName == nameof(HitFrame))
                    {
                        _hitTimeOnAnimation = eventData.time;
                        isHitEventData = true;
                    }
                }

                if (!isExistEventData)
                {
                    var endEvent = new AnimationEvent();
                    endEvent.time = _clipDuration;
                    endEvent.functionName = nameof(AnimationEnd);
                    endEvent.stringParameter = _animationClip.name;

                    _animationClip.AddEvent(endEvent);
                }

                if (!isHitEventData) //히트프레임 없음
                {
                    _hitTimeOnAnimation = 0;
                }

            }

            _isAnimationEnded = false;
        }

        /// <summary>
        /// <see cref="Play"/>는 하지 않고 초기화만 해둔다.
        /// </summary>
        public void InitializeOnly()
        {
            if (!_isSharedResourcesAllocated)
            {
                this.AllocateSharedResources(null);
            }

            _hitEventHandler = null;
            _endEventHandler = null;
        }

        public void InitializeAndPlay([CanBeNull] EventHandler hitEventHandler, [CanBeNull] EventHandler endEventHandler)
        {
            if (!_isSharedResourcesAllocated)
            {
                this.AllocateSharedResources(null);
            }

            _hitEventHandler = hitEventHandler;
            _endEventHandler = endEventHandler;

            this.Play();
        }
        
        public void InitializeAndPlay([CanBeNull] EventHandler hitEventHandler)
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

            _spriteAnimator.Play(_animationClip.name);

            _isAnimationEnded = false;
        }

        public void StopAndReserveToDestroy()
        {
            _isAnimationEnded = true;
        }

        // 리소스(애니메이션 컨트롤러)에서 지정할 히트프레임 핸들러
        // 리소스의 히트프레임에 "HitFrame"이 호출되도록 연결해주어야 합니다.
        public void HitFrame()
        {
            _hitEventHandler?.Invoke();
        }

        // 애니메이션 클립이 종료되는 시점에 호출되는 핸들러
        // <see cref="SpriteAnimationHandler"/> 스크립트에서, 애니메이션 클립의 마지막 프레임에 자동으로 연결해줍니다.
        public void AnimationEnd()
        {
            _isAnimationEnded = true;

            _endEventHandler?.Invoke();
        }
    }
}
