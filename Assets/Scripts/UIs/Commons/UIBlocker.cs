#nullable enable
using UnityEngine;
using UnityEngine.UI;
using Z.UIs;

/// <summary>
/// 팝업 뒤를 덮어 클릭을 막는 스크린. 바깥 클릭으로 닫히는 팝업이면 클릭 시 팝업을 닫습니다.
/// </summary>
public class UIBlocker : MonoBehaviour
{
    [SerializeField] private Button _blockClickButton = null!;

    private BasePopup? _targetPopup;

    public void SetTargetPopup(BasePopup targetPopup)
    {
        _targetPopup = targetPopup;

        _blockClickButton.onClick.RemoveAllListeners();
        _blockClickButton.onClick.AddListener(() =>
        {
            if (_targetPopup != null && _targetPopup.IsCloseOnOutsideClick)
            {
                _targetPopup.Close(false);
            }
        });
    }
}
