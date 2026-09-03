using Shared.DataTables;
using TMPro;
using UnityEngine;
using SamMul.GameClients;
using SamMul.UnityHelpers;

namespace SamMul.UIs.Lobbies
{
    public class LobbyWalletBarGroup : MonoBehaviour
    {
        // 데모에서 사용하지 않는 재화 그룹. 프리팹에 남아 있으므로 초기화 시 숨겨준다.
        [Header("Unused")]
        [SerializeField] private GameObject _ticketGroupGameObject;
        [SerializeField] private GameObject _staminaGroupGameObject;
        [SerializeField] private GameObject _rocketFuelsGameObject;
        [SerializeField] private GameObject _temporarySpaceCoinsGameObject;
        [SerializeField] private GameObject _permanentSpaceCoinsGameObject;

        [Header("Special DNA")]
        [SerializeField] private GameObject _specialDNAGroupGameObject;
        [SerializeField] private TextMeshProUGUI _specialDNAAmountText;

        [Header("Gems")]
        [SerializeField] private GameObject _gemGroupGameObject;
        [SerializeField] private TextMeshProUGUI _gemAmountText;

        [Header("Golds")]
        [SerializeField] private GameObject _goldGroupGameObject;
        [SerializeField] private TextMeshProUGUI _goldAmountText;

        public void Initialize()
        {
            Debug.Assert(_goldAmountText);
            Debug.Assert(_gemAmountText);
            Debug.Assert(_specialDNAAmountText);

            _ticketGroupGameObject.SetActive(false);
            _staminaGroupGameObject.SetActive(false);
            _rocketFuelsGameObject.SetActive(false);
            _temporarySpaceCoinsGameObject.SetActive(false);
            _permanentSpaceCoinsGameObject.SetActive(false);

            this.UpdateLogic();
            this.SetDefaultMode();
        }

        public void UpdateLogic()
        {
            var userGameData = GameClient.CS.UserGameData;

            this.UpdateGold(userGameData.Gold);
            this.UpdateGem(userGameData.GetTotalGemAmount());
            this.UpdateSpecialDNA(userGameData.SpecialDNA);
        }

        private void UpdateGold(long goldAmount)
        {
            _goldAmountText.text = goldAmount.ToShortNumericText();
        }

        private void UpdateGem(long gemAmount)
        {
            _gemAmountText.text = gemAmount.ToShortNumericText(GameConstants.GEM_THRESHOLD_TO_SHORTEN);
        }

        private void UpdateSpecialDNA(long amount)
        {
            _specialDNAAmountText.text = amount.ToShortNumericText();
        }

        public void SetDefaultMode()
        {
            var rectTransform = (RectTransform)this.transform;
            var walletAnchorPosition = rectTransform.anchoredPosition;
            walletAnchorPosition.y = -80.0f;
            rectTransform.anchoredPosition = walletAnchorPosition;

            _gemGroupGameObject.SetActive(true);
            _goldGroupGameObject.SetActive(true);
            _specialDNAGroupGameObject.SetActive(false);
        }

        public void SetEvolutionPageMode()
        {
            var rectTransform = (RectTransform)this.transform;
            var walletAnchorPosition = rectTransform.anchoredPosition;
            walletAnchorPosition.y = -80.0f;
            rectTransform.anchoredPosition = walletAnchorPosition;

            _gemGroupGameObject.SetActive(true);
            _goldGroupGameObject.SetActive(true);
            _specialDNAGroupGameObject.SetActive(true);
        }
    }
}
