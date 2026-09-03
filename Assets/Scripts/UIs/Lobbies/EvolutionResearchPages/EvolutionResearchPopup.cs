#nullable enable
using Shared.Localizers;
using Shared.StaticDatas;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.GameClients;
using SamMul.ResourcePools;
using SamMul.Scenes;
using SamMul.UnityHelpers;
using UnityEngine.Events;

namespace SamMul.UIs.Lobbies.EvolutionResearchPages
{
    public class EvolutionResearchPopup : MonoBehaviour
    {
        public ZButton ResearchButton => _researchButton;
        public BasicEvolutionStaticData? BasicEvolutionStaticData { get; private set; }
        public SpecialEvolutionStaticData? SpecialEvolutionStaticData { get; private set; }

        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _messageText;

        [SerializeField] private Image _backgroundImage;
        [SerializeField] private GameObject _researchButtonParent;
        [SerializeField] private ZButton _researchButton;

        [SerializeField] private GameObject _researchIngredientGroup;
        [SerializeField] private GameObject _researchLevelInfo;
        [SerializeField] private GameObject _researchSpecialDNAInfo;
        [SerializeField] private GameObject _researchGoldInfo;

        [SerializeField] private TextMeshProUGUI _researchLevelInfoText;
        [SerializeField] private TextMeshProUGUI _researchSpecialDNAInfoText;
        [SerializeField] private TextMeshProUGUI _researchGoldInfoText;

        [SerializeField] private GameObject _basicEvolutionPopupTailObject;
        [SerializeField] private GameObject _specialEvolutionPopupTailObject;

        [SerializeField] private ContentSizeFitter _popupContentSizeFitter;
        [SerializeField] private ContentSizeFitter _researchMaterialsInfoContentSizeFitter;
        [SerializeField] private LayoutElement _spacingLayoutElement;

        [SerializeField] private GameObject _specialEvolutionNavigationGroup;
        [SerializeField] private TextMeshProUGUI _specialEvolutionNavigationText;
        [SerializeField] private ZButton _specialEvolutionNavigationButton;

        private readonly string RESEARCH_BACKGROUND_PATH = "Lobbys/UIs/EvolutionPages/ResearchPopup.png";
        private readonly string RESEARCHED_BACKGROUND_PATH = "Lobbys/UIs/EvolutionPages/ResearchedPopup.png";

        public void InitializeFromBasicEvolution(BasicEvolutionStaticData basicEvolutionData, int highestBasicEvolutionID, int accountLevel, long goldAmount, UnityAction closeEvolutionResearchPopup)
        {
            BasicEvolutionStaticData = basicEvolutionData;
            SpecialEvolutionStaticData = null;

            _basicEvolutionPopupTailObject.SetActive(true);
            _specialEvolutionPopupTailObject.SetActive(false);

            _titleText.text = basicEvolutionData.BasicEvolutionName;
            _messageText.text = basicEvolutionData.BasicEvolutionDescription;

            _researchLevelInfoText.text = basicEvolutionData.learnLevel.ToString();
            _researchGoldInfoText.text = basicEvolutionData.requiredGold.ToString();

            if (basicEvolutionData.requiredGold <= goldAmount)
            {
                _researchGoldInfoText.color = Color.white;
            }
            else
            {
                _researchGoldInfoText.color = Color.red;
            }

            if(basicEvolutionData.learnLevel <= accountLevel)
            {
                _researchLevelInfoText.color = Color.white;
            }
            else
            {
                _researchLevelInfoText.color = Color.red;
            }


            if (highestBasicEvolutionID + 1 == basicEvolutionData.basicEvolutionID && basicEvolutionData.learnLevel <= accountLevel)
            {
                _backgroundImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(RESEARCH_BACKGROUND_PATH);
                _researchButtonParent.SetActive(true);
                _researchButton.gameObject.SetActive(true);
                _researchButton.SetInteractable(true);
                _researchButton.GetComponentInChildren<TextMeshProUGUI>().text = Localizer.Instance.GetText("UI_EVOLUTION_POPUP_OK");
                _researchButton.onClick.RemoveAllListeners();
                _researchButton.onClick.AddListener(() =>
                {
                    this.ResearchBasicEvolution(basicEvolutionData, accountLevel, closeEvolutionResearchPopup);
                });
                _specialEvolutionNavigationGroup.gameObject.SetActive(false);
                _researchIngredientGroup.SetActive(true);
                _researchLevelInfo.SetActive(false);
                _researchGoldInfo.SetActive(true);
                _researchSpecialDNAInfo.SetActive(false);
                _spacingLayoutElement.gameObject.SetActive(true);
                _spacingLayoutElement.minWidth = 630;
            }
            else
            {
                if(basicEvolutionData.basicEvolutionID > highestBasicEvolutionID)
                {
                    _backgroundImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(RESEARCH_BACKGROUND_PATH);
                    _researchIngredientGroup.SetActive(true);
                    _researchLevelInfo.SetActive(true);
                    _researchGoldInfo.SetActive(true);
                    _spacingLayoutElement.gameObject.SetActive(true);
                    _spacingLayoutElement.minWidth = 630;
                }
                else
                {
                    _backgroundImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(RESEARCHED_BACKGROUND_PATH);
                    _researchIngredientGroup.SetActive(false);
                    _spacingLayoutElement.gameObject.SetActive(false);
                }
                _researchButtonParent.SetActive(false);
                _researchSpecialDNAInfo.SetActive(false);
                _specialEvolutionNavigationGroup.gameObject.SetActive(false);
            }
            _spacingLayoutElement.minWidth = _researchIngredientGroup.GetComponent<RectTransform>().rect.width;
            var popupRectTransform = this.GetComponent<RectTransform>();
            this.RefefreshContentFitter(popupRectTransform);
        }

        public void InitializeFromSpecialEvolution(SpecialEvolutionStaticData specialEvolutionData, int highestSpecialEvolutionID, int accountLevel, long goldAmount, long specialDNAAmount, UnityAction onClickNavigationButton, UnityAction closeEvolutionResearchPopup)
        {
            BasicEvolutionStaticData = null;
            SpecialEvolutionStaticData = specialEvolutionData;

            _basicEvolutionPopupTailObject.SetActive(false);
            _specialEvolutionPopupTailObject.SetActive(true);

            _titleText.text = specialEvolutionData.SpecialEvolutionName;
            _messageText.text = string.Format( specialEvolutionData.SpecialEvolutionDescription, specialEvolutionData.param1, specialEvolutionData.param2);
            _specialEvolutionNavigationText.text = Localizer.Instance.GetText("UI_EVOLUTION_POPUP_SPECIALEVOLUTION_NAVIGATION");

            _researchLevelInfoText.text = specialEvolutionData.learnLevel.ToString();
            _researchGoldInfoText.text = specialEvolutionData.requiredGold.ToString();
            _researchSpecialDNAInfoText.text = specialEvolutionData.requiredSpecialDNA.ToString();


            if (specialEvolutionData.requiredGold <= goldAmount)
            {
                _researchGoldInfoText.color = Color.white;
            }
            else
            {
                _researchGoldInfoText.color = Color.red;
            }

            if (specialEvolutionData.requiredSpecialDNA <= specialDNAAmount)
            {
                _researchSpecialDNAInfoText.color = Color.white;
            }
            else
            {
                _researchSpecialDNAInfoText.color = Color.red;
            }

            if (specialEvolutionData.learnLevel <= accountLevel)
            {
                _researchLevelInfoText.color = Color.white;
            }
            else
            {
                _researchLevelInfoText.color = Color.red;
            }


            if (highestSpecialEvolutionID + 1 == specialEvolutionData.specialEvolutionID&& specialEvolutionData.learnLevel <= accountLevel)
            {
                _backgroundImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(RESEARCH_BACKGROUND_PATH);
                _specialEvolutionNavigationGroup.gameObject.SetActive(false);
                _researchButtonParent.SetActive(true);
                _researchButton.gameObject.SetActive(true);
                _researchButton.SetInteractable(true);
                _researchButton.GetComponentInChildren<TextMeshProUGUI>().text = Localizer.Instance.GetText("UI_EVOLUTION_POPUP_OK");
                _researchButton.onClick.RemoveAllListeners();
                _researchButton.onClick.AddListener(() =>
                {
                    this.ResearchSpecialEvolution(specialEvolutionData, accountLevel, closeEvolutionResearchPopup);
                });
                _researchIngredientGroup.SetActive(true);
                _researchLevelInfo.SetActive(false);
                _researchGoldInfo.SetActive(true);
                _researchSpecialDNAInfo.SetActive(true);
                _spacingLayoutElement.gameObject.SetActive(true);
                _spacingLayoutElement.minWidth = 630;
            }
            else
            {
                if (specialEvolutionData.specialEvolutionID > highestSpecialEvolutionID)
                {
                    _backgroundImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(RESEARCH_BACKGROUND_PATH);

                    if(specialEvolutionData.learnLevel <= accountLevel)
                    {
                        _specialEvolutionNavigationGroup.gameObject.SetActive(true);
                        _specialEvolutionNavigationButton.onClick.RemoveAllListeners();
                        _specialEvolutionNavigationButton.onClick.AddListener(onClickNavigationButton);
                        _specialEvolutionNavigationButton.onClick.AddListener(closeEvolutionResearchPopup);
                    }
                    else
                    {
                        _specialEvolutionNavigationGroup.gameObject.SetActive(false);
                    }
                    _researchIngredientGroup.SetActive(true);
                    _researchLevelInfo.SetActive(true);
                    _researchGoldInfo.SetActive(true);
                    _researchSpecialDNAInfo.SetActive(true);
                    _spacingLayoutElement.gameObject.SetActive(true);
                    _spacingLayoutElement.minWidth = 930;
                }
                else
                {
                    _backgroundImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(RESEARCHED_BACKGROUND_PATH);
                    _specialEvolutionNavigationGroup.gameObject.SetActive(false);
                    _researchIngredientGroup.SetActive(false);
                    _spacingLayoutElement.gameObject.SetActive(false);
                }
                _researchButtonParent.SetActive(false);
            }

            var popupRectTransform = this.GetComponent<RectTransform>();
            this.RefefreshContentFitter(popupRectTransform);
        }

        /// <summary>
        /// 서버 없이 동작하는 데모라서 연구 결과를 로컬 유저 데이터에 바로 반영하고 저장한다.
        /// </summary>
        private void ResearchBasicEvolution(BasicEvolutionStaticData basicEvolutionData, int accountLevel, UnityAction closeEvolutionResearchPopup)
        {
            var userGameData = GameClient.CS.UserGameData;
            _researchButton.SetInteractable(false);
            closeEvolutionResearchPopup?.Invoke();

            var lobbySceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<LobbySceneUIRoot>();
            if (basicEvolutionData.learnLevel > userGameData.AccountLevel)
            {
                lobbySceneUI.AddCommonMessagePopup(Localizer.Instance.GetText("UI_FAILED"), Localizer.Instance.GetText("UI_LACKING_IN_COUQUERING_LEVEL"), Localizer.Instance.GetText("UI_OK"), null);
                return;
            }
            if (basicEvolutionData.requiredGold > userGameData.Gold)
            {
                lobbySceneUI.AddCommonMessagePopup(Localizer.Instance.GetText("UI_FAILED"), Localizer.Instance.GetText("UI_INSUFFICIENT_GOLD"), Localizer.Instance.GetText("UI_OK"), null);
                return;
            }

            userGameData.Gold -= basicEvolutionData.requiredGold;
            userGameData.HighestBasicEvolutionID = basicEvolutionData.basicEvolutionID;
            GameClient.CS.Save();

            //해당 로직에 들어온 코드는 AccountLevel에 변동이 없다. 기존 값을 넣어주면 됨
            lobbySceneUI.EvolutionResearchPage.UpdateBasicEvolutionResearchPage(userGameData.HighestBasicEvolutionID, accountLevel, userGameData.Gold);

            var evolutionData =  StaticDataRepository.Instance.BasicEvolutions.FindBasicEvolutionStaticData(userGameData.HighestBasicEvolutionID)!;
            int upStatValue;

            //소수점 으로 올라가는 스탯은 *100을 해준다음 표현해준다.
            if(evolutionData.basicEvolutionType == Shared.GameDataTypes.EvolutionType.Tenacity ||
                evolutionData.basicEvolutionType == Shared.GameDataTypes.EvolutionType.Restoration)
            {
                upStatValue = (int)(evolutionData.param1 * 100.0f);
            }
            else
            {
                upStatValue = (int)evolutionData.param1;
            }

            //현재 진화시 스탯 연출 증가 아이콘은 진화 아이콘을 사용 하고 있다.
            //추후 스탯별 아이콘이 전부 나오면 진화 타입에 따라 아이콘 경로를 나눠 정상적으로 나올 수 있도록 수정해야한다.
            var statAnimation = ResourcePool.Instance.InstantiateFromResource("Lobbys/UIs/StatChangeAnimation.prefab");
            string statIconPath;
            if(evolutionData.basicEvolutionType == Shared.GameDataTypes.EvolutionType.Strength)
            {
                statIconPath = "Commons/Icon/AttackPower_Icon.png";
            }
            else
            {
                statIconPath = "Commons/Icon/HP_Icon.png";
            }
            statAnimation.GetComponent<StatChangeAnimationPopup>().InitializeAndPlay(statIconPath, 0, upStatValue,
                lobbySceneUI.UserInfoGroup.statUpStartRectTransform, Vector3.zero, 1.5f);
        }

        /// <summary>
        /// 서버 없이 동작하는 데모라서 연구 결과를 로컬 유저 데이터에 바로 반영하고 저장한다.
        /// </summary>
        private void ResearchSpecialEvolution(SpecialEvolutionStaticData specialEvolutionData, int accountLevel, UnityAction closeEvolutionResearchPopup)
        {
            var userGameData = GameClient.CS.UserGameData;
            _researchButton.SetInteractable(false);
            closeEvolutionResearchPopup?.Invoke();

            var lobbySceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<LobbySceneUIRoot>();
            if (specialEvolutionData.learnLevel > userGameData.AccountLevel)
            {
                lobbySceneUI.AddCommonMessagePopup(Localizer.Instance.GetText("UI_FAILED"), Localizer.Instance.GetText("UI_LACKING_IN_COUQUERING_LEVEL"), Localizer.Instance.GetText("UI_OK"), null);
                return;
            }
            if (specialEvolutionData.requiredGold > userGameData.Gold)
            {
                lobbySceneUI.AddCommonMessagePopup(Localizer.Instance.GetText("UI_FAILED"), Localizer.Instance.GetText("UI_INSUFFICIENT_GOLD"), Localizer.Instance.GetText("UI_OK"), null);
                return;
            }
            if (specialEvolutionData.requiredSpecialDNA > userGameData.SpecialDNA)
            {
                lobbySceneUI.AddCommonMessagePopup(Localizer.Instance.GetText("UI_FAILED"), Localizer.Instance.GetText("UI_INSUFFICIENT_SPECIAL_DNA"), Localizer.Instance.GetText("UI_OK"), null);
                return;
            }

            userGameData.Gold -= specialEvolutionData.requiredGold;
            userGameData.SpecialDNA -= specialEvolutionData.requiredSpecialDNA;
            userGameData.HighestSpecialEvolutionID = specialEvolutionData.specialEvolutionID;
            GameClient.CS.Save();

            //해당 로직에 들어온 코드는 AccountLevel에 변동이 없다. 기존 값을 넣어주면 됨
            lobbySceneUI.EvolutionResearchPage.UpdateSpecialEvolutionResearchPage(userGameData.HighestSpecialEvolutionID, accountLevel, userGameData.Gold, userGameData.SpecialDNA);
        }

        //LayoutGroup, ContentSizeFitter들을 리프레시 해줘서 UI사이즈를 제대로 나오게 처리하는 코드
        private void RefefreshContentFitter(RectTransform transform)
        {
            if (transform == null || !transform.gameObject.activeSelf)
            {
                return;
            }

            //자식이 있다면 자식 먼저 하고 이후 처리를 해줘야 정상적으로 사이즈 적용이 된다.
            foreach (RectTransform child in transform)
            {
                RefefreshContentFitter(child);
            }

            var layoutGroup = transform.GetComponent<LayoutGroup>();
            var contentSizeFitter = transform.GetComponent<ContentSizeFitter>();
            if (layoutGroup != null)
            {
                layoutGroup.SetLayoutHorizontal();
                layoutGroup.SetLayoutVertical();
            }

            if (contentSizeFitter != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(transform);
            }
        }
    }
}

