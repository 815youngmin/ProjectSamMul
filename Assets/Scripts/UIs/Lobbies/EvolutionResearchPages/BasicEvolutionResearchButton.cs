using Shared.StaticDatas;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Z.ResourcePools;

namespace Z.UIs.Lobbies.EvolutionResearchPages
{
    public class BasicEvolutionResearchButton : MonoBehaviour
    {
        [SerializeField] private Image _buttonBackground;
        [SerializeField] private Image _buttonIcon;
        [SerializeField] private ZButton _button;

        private readonly string _researchedEvolutionImagePath = "Lobbys/UIs/EvolutionPages/Basic_Evolution_ON01.png";
        private readonly string _unResearchedEvolutionImagePath = "Lobbys/UIs/EvolutionPages/Basic_Evolution_OFF01.png";

        public BasicEvolutionStaticData ButtonBasicEvolutionStaticData { get { return _basicEvolutionData; } }
        private BasicEvolutionStaticData _basicEvolutionData;
        private UnityAction<BasicEvolutionResearchButton> _basicEvolutionButtonClickAction;
        private int _highestBasicEvolutionID;
        private int _accountLevel;
        private long _goldAmount;


        public void Initialize(BasicEvolutionStaticData basicEvolutionData, int highestBasicEvolutionID, int accountLevel, long goldAmount, UnityAction<BasicEvolutionResearchButton> onBasicEvolutionButtonClickAction)
        {
            _basicEvolutionData = basicEvolutionData;
            _highestBasicEvolutionID = highestBasicEvolutionID;
            _accountLevel = accountLevel;
            _goldAmount = goldAmount;
            _basicEvolutionButtonClickAction = onBasicEvolutionButtonClickAction;

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClickBasicEvolutionButton);

            _buttonBackground = this.GetComponent<Image>();

            this.UpdateButtonImage();
        }

        private void OnClickBasicEvolutionButton()
        {
            _basicEvolutionButtonClickAction?.Invoke(this);
        }

      

        //버튼에 필요한 정보가 변경된 경우 해당 인터페이스를 통해 업데이트 해준다.
        public void UpdateBasicEvolutionResearchButton(int highestBasicEvolutionID, int accountLevel, long goldAmount)
        {
            _highestBasicEvolutionID = highestBasicEvolutionID;
            _accountLevel = accountLevel;
            _goldAmount = goldAmount;
            this.UpdateButtonImage();
        }

        private void UpdateButtonImage()
        {
            if(_basicEvolutionData.basicEvolutionID <= _highestBasicEvolutionID)
            {
                _buttonBackground.sprite = ResourcePool.Instance.LoadResource<Sprite>(_researchedEvolutionImagePath);
                _buttonIcon.sprite = ResourcePool.Instance.LoadResource<Sprite>(_basicEvolutionData.EnableIconResourcePath);
            }
            else
            {
                _buttonBackground.sprite = ResourcePool.Instance.LoadResource<Sprite>(_unResearchedEvolutionImagePath);
                _buttonIcon.sprite = ResourcePool.Instance.LoadResource<Sprite>(_basicEvolutionData.DisableIconResourcePath);
            }
        }

    }

}
