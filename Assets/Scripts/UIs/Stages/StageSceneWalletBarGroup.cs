#nullable enable
using TMPro;
using UnityEngine;

namespace SamMul.UIs
{
    /// <summary>스테이지 상단의 골드/보석 표시.</summary>
    public class StageSceneWalletBarGroup : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _goldText = null!;

        public void UpdateGoldAmount(long totalGoldAmount)
        {
            _goldText.text = totalGoldAmount.ToString("N0");
        }
    }
}
