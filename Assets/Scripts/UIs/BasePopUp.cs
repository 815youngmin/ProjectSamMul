#nullable enable
using System;
using UnityEngine;

namespace SamMul.UIs
{
    /// <summary>
    /// 모든 팝업의 베이스. 닫기 요청은 팝업을 만든 UIRoot 가 넘겨준 closeRequester 로 전달합니다.
    /// </summary>
    public class BasePopup : MonoBehaviour
    {
        // bool : skipAnimation
        private Action<bool>? _closeRequester;

        public bool IsCloseOnOutsideClick { get; private set; }

        /// <param name="closeRequester">이 팝업을 닫고 제거하는 요청. UIRoot 가 넘겨줍니다.</param>
        protected void InitializeBase(Action<bool> closeRequester)
        {
            this.InitializeBase(closeRequester, isCloseOnOutsideClick: false);
        }

        /// <param name="isCloseOnOutsideClick">팝업 바깥을 클릭하면 닫을지 여부.</param>
        protected void InitializeBase(Action<bool> closeRequester, bool isCloseOnOutsideClick)
        {
            _closeRequester = closeRequester;
            IsCloseOnOutsideClick = isCloseOnOutsideClick;
        }

        protected virtual void Awake()
        {
            _closeRequester = null;
        }

        public virtual void Close(bool skipAnimation)
        {
            _closeRequester?.Invoke(skipAnimation);
        }

        /// <summary>
        /// 팝업 등장 애니메이션이 끝난 뒤 UIRoot 가 호출합니다.
        /// </summary>
        public virtual void OnPopupOpenAnimationFinished()
        {
        }
    }
}
