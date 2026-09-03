using Shared.StaticDatas;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Z.ResourcePools;

namespace Z.UIs.Lobbies.EvolutionResearchPages
{
    public class SpecialEvolutionResearchButton : MonoBehaviour
    {
        [SerializeField] private Image _buttonBackground;
        [SerializeField] private Image _buttonIcon;
        [SerializeField] private ZButton _button;

        private Image _image;

        private readonly string _researchedEvolutionImagePath = "Lobbys/UIs/EvolutionPages/Special_Evolution_ON01.png";
        private readonly string _unResearchedEvolutionImagePath = "Lobbys/UIs/EvolutionPages/Special_Evolution_OFF01.png";

        public SpecialEvolutionStaticData ButtonSpecialEvolutionStaticData => _specialEvolutionData;
        private SpecialEvolutionStaticData _specialEvolutionData;
        private UnityAction<SpecialEvolutionResearchButton> _researchPopupOpenAction;
        private int _highestSpecialEvolutionID;
        private int _accountLevel;
        private long _goldAmount;
        private long _specialDNAAmount;


        public void Initialize(SpecialEvolutionStaticData specialEvolutionData, int highestBasicEvolutionID, int accountLevel, long goldAmount, long specialDNAAmount, UnityAction<SpecialEvolutionResearchButton> researchPopupOpenAction)
        {
            _specialEvolutionData = specialEvolutionData;
            _highestSpecialEvolutionID = highestBasicEvolutionID;
            _accountLevel = accountLevel;
            _goldAmount = goldAmount;
            _specialDNAAmount = specialDNAAmount;
            _researchPopupOpenAction = researchPopupOpenAction;

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClickSpecialEvolutionButton);

            this.UpdateButtonImage();
        }

        private void OnClickSpecialEvolutionButton()
        {
            _researchPopupOpenAction?.Invoke(this);
        }

        public void UpdateSpecialEvolutionResearchButton(int highestSpecialEvolutionID, int accountLevel, long goldAmount, long specialDNAAmount)
        {
            _highestSpecialEvolutionID = highestSpecialEvolutionID;
            _accountLevel = accountLevel;
            _goldAmount = goldAmount;
            _specialDNAAmount = specialDNAAmount;
            this.UpdateButtonImage();
        }

        private void UpdateButtonImage()
        {
            if (_specialEvolutionData.specialEvolutionID <= _highestSpecialEvolutionID)
            {
                _buttonBackground.sprite = ResourcePool.Instance.LoadResource<Sprite>(_researchedEvolutionImagePath);
                _buttonIcon.sprite = ResourcePool.Instance.LoadResource<Sprite>(_specialEvolutionData.EnableIconResourcePath);
            }
            else
            {
                _buttonBackground.sprite = ResourcePool.Instance.LoadResource<Sprite>(_unResearchedEvolutionImagePath);
                _buttonIcon.sprite = ResourcePool.Instance.LoadResource<Sprite>(_specialEvolutionData.DisableIconResourcePath);
            }
        }
    }
}

