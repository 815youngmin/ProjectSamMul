
using Shared.Localizers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpecialEvolutionNavigationPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Image _arrowImage;
    [SerializeField] private ZButton _navigationButton;
    public static readonly string PREFAB_PATH = "Lobbys/UIs/EvolutionPages/SpecialEvolutionNavigationPopup.prefab";


    public void Initialize(bool isArrowUp, UnityEngine.Events.UnityAction onClickNavigationButton)
    {
        _navigationButton.onClick.RemoveAllListeners();
        _navigationButton.onClick.AddListener(onClickNavigationButton);

        _text.text = Localizer.Instance.GetText("UI_SPECIALEVOLUTION_NAVIGATION_MESSAGE");

        if(isArrowUp)
        {
            _arrowImage.transform.localRotation = Quaternion.Euler(0, 0, 0f);
        }
        else
        {
            _arrowImage.transform.localRotation = Quaternion.Euler(0, 0, 180f);
        }
    }
}
