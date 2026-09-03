using Shared.CSProtocols.ReqResData;
using Shared.DataTables;
using Shared.Localizers;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.GameClients;
using SamMul.UnityHelpers;

namespace SamMul.UIs.Stages.Popups
{
    /// <summary>
    /// 부활 팝업창
    /// 부활코인 또는 보석을 사용해 부활한다. (광고 부활은 오프라인 데모에서 제외)
    /// 부활 취소는 카운트다운 시간 동안 아무것도 안하거나 exit 버튼을 누르면 취소된다.
    /// 부활, 부활 취소 후 부활 팝업창은 종료된다.
    /// </summary>
    public class ResurrectionPopup : BasePopup
    {
        [SerializeField] private ZButton _payButton;
        [SerializeField] private Image _payButtonGemIcon;
        [SerializeField] private Image _payButtonResurrectionCoinIcon;
        [SerializeField] private TextMeshProUGUI _payButtonPriceText;
        [SerializeField] private TextMeshProUGUI _payButtonOKText;

        [SerializeField] private ZButton _exitButton;
        [SerializeField] private Image _countdownOutline;

        [SerializeField] private TMP_Text _countdownText;

        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _messageText;

        private float _showCountdownTime;
        private float _realCountdownTime;

        // 진짜 스테이지 번호다. 챕터 번호 아님
        private int _stageNumber;

        public void Initialize(int stageNumber, Action addResultPopup, Action<bool> closeRequester)
        {
            base.InitializeBase(closeRequester);
            _stageNumber = stageNumber;

            InitializePayButton();

            _exitButton.gameObject.SetActive(true);
            _exitButton.onClick.RemoveAllListeners();
            _exitButton.onClick.AddListener(() =>
            {
                if (!_exitButton.gameObject.activeSelf)
                {
                    return;
                }
                _exitButton.gameObject.SetActive(false);

                // 실패 결과창 팝업으로 이동, 부활 창 닫음
                // 닫으면서 스테이지 정상진행되도록 하고,
                this.Close(skipAnimation: false);

                // 결과창 팝업 띄워준다.
                addResultPopup.Invoke();
            });

            //내부 카운트는 20초를 카운트 하지만 노출은 10초처럼 처리한다.
            _realCountdownTime = 20;
            _showCountdownTime = 10;
            _countdownText.text = _showCountdownTime.ToString("F0");

            _titleText.text = Localizer.Instance.GetText("UI_RESURRECTION_POPUP_TITLE");
            _messageText.text = Localizer.Instance.GetText("UI_RESURRECTION_POPUP_MESSAGE");
        }

        private void Update()
        {
            if (_showCountdownTime > 0)
            {
                _showCountdownTime -= Time.unscaledDeltaTime;
                _countdownText.text = MathF.Truncate(_showCountdownTime).ToString("F0");
                _countdownOutline.fillAmount = _showCountdownTime / 10f;

                if (_showCountdownTime < 4)
                {
                    //숫자가 3으로 보일때부터
                    _countdownText.color = new Color(0.7137255f, 0.1254902f, 0.1254902f);
                }
            }
            else
            {
                //카운트 다운 종료 실패 결과창으로 이동
                _countdownText.text = "0";
                _countdownOutline.fillAmount = 0;
            }

            if(_realCountdownTime > 0)
            {
                _realCountdownTime -= Time.unscaledDeltaTime;
            }
            else
            {
                _exitButton.onClick.Invoke();
            }

        }

        private void InitializePayButton()
        {
            _payButton.gameObject.SetActive(true);
            _payButtonOKText.text = Localizer.Instance.GetText("UI_RESURRECTIONPOPUP_OK");
            long userGems = GameClient.CS.UserGameData.GetTotalGemAmount();
            long userResurrectionCoin = GameClient.CS.UserGameData.ResurrectionCoin;

            _payButton.onClick.RemoveAllListeners();
            _payButton.onClick.AddListener(() =>
            {
                if (!_payButton.gameObject.activeSelf)
                {
                    return;
                }
                _payButton?.gameObject.SetActive(false);

                GameClient.CS.Resurrect(
                    new ResurrectRequest(_stageNumber),
                    onCompleted: (response) =>
                    {
                        if (response.ResultCode == ResurrectResultCode.Success)
                        {
                            var stage = GameClient.Stage;
                            stage.OnResurrected(stage.PC, stage.PC.MaxHP);
                            this?.Close(skipAnimation: false);
                        }
                    }, UnityGlobal.HandleInternalServerError_LogAndRestart, UnityGlobal.HandleNetworkErrorAndContinueRetry);
            });

            if (userResurrectionCoin > 0)
            {
                // 부활코인 있으면 부활코인 먼저
                _payButtonPriceText.color = Color.white;
                _payButtonPriceText.text = $"{userResurrectionCoin}/1";
                _payButtonResurrectionCoinIcon.gameObject.SetActive(true);
                _payButtonGemIcon.gameObject.SetActive(false);

                _payButton.SetInteractable(true);
            }
            else if (userGems >= GameConstants.RESURRECTION_GEM_COST)
            {
                // 보석이라도 있으면 보석을 쓰고
                _payButtonPriceText.color = Color.white;
                _payButtonPriceText.text = GameConstants.RESURRECTION_GEM_COST.ToString();
                _payButtonResurrectionCoinIcon.gameObject.SetActive(false);
                _payButtonGemIcon.gameObject.SetActive(true);

                _payButton.SetInteractable(true);
            }
            else
            {
                // 없으면 보석비용 보여주고 비활성화
                _payButtonPriceText.color = Color.red;
                _payButtonPriceText.text = GameConstants.RESURRECTION_GEM_COST.ToString();
                _payButtonResurrectionCoinIcon.gameObject.SetActive(false);
                _payButtonGemIcon.gameObject.SetActive(true);

                _payButton.onClick.RemoveAllListeners();
                _payButton.SetInteractable(false);
            }

        }
    }
}
