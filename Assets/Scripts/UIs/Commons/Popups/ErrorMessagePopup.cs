#nullable enable
using System;
using TMPro;
using UnityEngine;

namespace SamMul.UIs.Commons.Popups
{
    /// <summary>오류 안내 팝업. 문의용으로 계정/버전/요청 이름을 함께 보여준다.</summary>
    public class ErrorMessagePopup : CommonPopup
    {
        public static readonly string PREFAB_PATH = "Commons/Popups/ErrorMessagePopup.prefab";

        [SerializeField] private TextMeshProUGUI _accountIdText = null!;
        [SerializeField] private TextMeshProUGUI _clientVersionText = null!;
        [SerializeField] private TextMeshProUGUI _protocolText = null!;

        public void InitializeErrorMessagePopup(
            long accountId, string applicationVersion, string protocolName,
            string title, string message, string buttonText, Action? buttonAction, Action<bool> closeRequester)
        {
            base.InitializeCommonPopup(title, message, buttonText, buttonAction, closeRequester);
            _accountIdText.text = accountId.ToString();
            _clientVersionText.text = applicationVersion;
            _protocolText.text = protocolName;
        }
    }
}
