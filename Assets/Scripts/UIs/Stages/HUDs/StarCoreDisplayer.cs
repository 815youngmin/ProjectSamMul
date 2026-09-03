#nullable enable
using TMPro;
using UnityEngine;

namespace Z.UIs.Stages.HUDs
{
    /// <summary>플레이어 머리 위에 획득한 스타코어 수를 보여주는 표시기.</summary>
    public class StarCoreDisplayer : MonoBehaviour
    {
        public static readonly string PREFAB_PATH = "Stages/UIs/HUDs/StarCoreDisplayer/StarCoreDisplayer.prefab";

        [SerializeField] private Canvas _canvas = null!;
        [SerializeField] private RectTransform _rectTransform = null!;
        [SerializeField] private GameObject? _background;
        [SerializeField] private TextMeshProUGUI _starCoreAmountText = null!;

        public void Initialize(int sortingOrder, Transform parent, Vector2 pivot, Vector3 localPosition, Vector3 localScale, bool withBackground)
        {
            transform.SetParent(parent, worldPositionStays: false);
            transform.localPosition = localPosition;
            transform.localScale = localScale;
            _rectTransform.pivot = pivot;
            _canvas.sortingOrder = sortingOrder;
            if (_background != null)
            {
                _background.SetActive(withBackground);
            }
            UpdateStarCoreAmountText(0);
        }

        public void UpdateStarCoreAmountText(int acquiredStarCores)
        {
            _starCoreAmountText.text = acquiredStarCores.ToString();
        }

        public void UpdateStarCoreAmountText(int absorbedStarCores, int targetStarCores)
        {
            _starCoreAmountText.text = $"{absorbedStarCores}/{targetStarCores}";
        }
    }
}
