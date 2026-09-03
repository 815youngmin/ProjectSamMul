#nullable enable
using System;
using TMPro;
using UnityEngine;

namespace SamMul.UIs.Commons.Popups
{
    /// <summary>제목/메시지와 버튼 1~2개로 이루어진 범용 확인 팝업.</summary>
    public class CommonPopup : BasePopup
    {
        [SerializeField] private TextMeshProUGUI _titleText = null!;
        [SerializeField] private TextMeshProUGUI _messageText = null!;
        [SerializeField] private ZButton _leftButton = null!;
        [SerializeField] private TextMeshProUGUI _leftButtonText = null!;
        [SerializeField] private ZButton _rightButton = null!;
        [SerializeField] private TextMeshProUGUI _rightButtonText = null!;

        /// <summary>버튼 하나짜리 팝업.</summary>
        public void InitializeCommonPopup(string title, string message, string buttonText, Action? buttonAction, Action<bool> closeRequester)
        {
            base.InitializeBase(closeRequester);
            _titleText.text = title;
            _messageText.text = message;

            _leftButton.gameObject.SetActive(false);
            SetButton(_rightButton, _rightButtonText, buttonText, buttonAction);
        }

        /// <summary>왼쪽(확인)/오른쪽(취소) 버튼 두 개짜리 팝업.</summary>
        public void Initialize(string title, string message, string leftButtonText, Action? leftButtonAction, string rightButtonText, Action? rightButtonAction, Action<bool> closeRequester)
        {
            base.InitializeBase(closeRequester);
            _titleText.text = title;
            _messageText.text = message;

            _leftButton.gameObject.SetActive(true);
            SetButton(_leftButton, _leftButtonText, leftButtonText, leftButtonAction);
            SetButton(_rightButton, _rightButtonText, rightButtonText, rightButtonAction);
        }

        private void SetButton(ZButton button, TextMeshProUGUI label, string text, Action? action)
        {
            button.gameObject.SetActive(true);
            label.text = text;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                this.Close(skipAnimation: false);
                action?.Invoke();
            });
        }
    }
}
