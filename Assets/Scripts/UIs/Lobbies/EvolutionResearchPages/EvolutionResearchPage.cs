using DG.Tweening;
using Shared.StaticDatas;
using Shared.UserDatas;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Z.GameClients;
using Z.ResourcePools;
using Z.Scenes;
using Z.UnityHelpers;

namespace Z.UIs.Lobbies.EvolutionResearchPages
{
    /// <summary>
    /// 기본 기능만 제작되어 있는 진화 연구 페이지
    /// 현재는 기본 진화 연구만 구현되어 있다.
    /// </summary>
    public class EvolutionResearchPage : MonoBehaviour, IPointerClickHandler
    {

        private readonly string _basicEvolutionButtonPath = "Lobbys/UIs/EvolutionPages/BasicEvolutionButton.prefab";
        private readonly string _specialEvolutionButtonPath = "Lobbys/UIs/EvolutionPages/SpecialEvolutionButton.prefab";
        private readonly string _levelLabelPath = "Lobbys/UIs/EvolutionPages/LevelLabel.prefab";
        private readonly string _basicEvolutionLineSliderPath = "Lobbys/UIs/EvolutionPages/EvolutionSlider.prefab";
        private readonly string _specialEvolutionLinePath = "Lobbys/UIs/EvolutionPages/SpecialEvolutionLine.prefab";
        private readonly string _specialEvlutionResearchLineSpritePath = "Lobbys/UIs/EvolutionPages/Special_Evolution_ON02.png";
        private readonly string _specialEvlutionUnResearchLineSpritePath = "Lobbys/UIs/EvolutionPages/Special_Evolution_OFF02.png";

        private readonly string _evolutionWavePath = "Lobbys/UIs/EvolutionPages/EvolutionWave.prefab";
        private readonly string _evolutionWaveBubblePath = "Lobbys/UIs/EvolutionPages/EvolutionBubble.prefab";
        private readonly string _canResearchLinePath = "Lobbys/UIs/EvolutionPages/CanResearchLine.prefab";
        private readonly string _evolutionResearchPopupPath = "Lobbys/UIs/EvolutionPages/ResearchPopup.prefab";

        [SerializeField] private RectTransform _basicButtonTransform;
        [SerializeField] private RectTransform _specialButtonTransform;
        [SerializeField] private RectTransform _levelLabelTransform;
        [SerializeField] private RectTransform _lineTransform;
        [SerializeField] private RectTransform _contentTransform;
        [SerializeField] private RectTransform _backgroundTransform;
        [SerializeField] private ScrollRect _scrollRect;

        private Vector3 _basicEvolutionButtonOffset;
        private Vector3 _specialEvolutionButtonOffset;
        private Vector3 _researchLevelLabelOffset;
        private Vector3 _basicEvolutionResearchLineOffset;

        private int _highestBasicEvolutionID;
        private int _highestSpecialEvolutionID;
        private int _accountLevel;
        private long _goldAmount;
        private long _specialDNAAmount;

        private List<BasicEvolutionResearchButton> _basicEvolutionButtons;          //일반 진화 버튼 (레벨 라벨의 위치 지정에도 사용된다)
        private Dictionary<int, ResearchLevelLabel> _researchLevelLabelsByLevel;    //연구 가능 레벨 라벨(스페셜 진화 버튼 위치 지정에도 사용된다)
        private List<SpecialEvolutionResearchButton> _specialEvolutionButtons;      //스페셜 진화 버튼
        private List<GameObject> _specialEvolutionResearchLines;                    //스페셜 진화 버튼 중간 연결(라인) 오브젝트들


        private Slider _basicEvolutionLineSlider;
        private GameObject _evolutionWave;
        private GameObject _evolutionWaveBubble;
        private GameObject _canResearchLine;
        private EvolutionResearchPopup _evolutionResearchPopup;

        

        public EvolutionResearchPopup EvolutionResearchPopup => _evolutionResearchPopup;
        public bool IsEvolutionResearchPopupOpen => _evolutionResearchPopup.gameObject.activeSelf;

        private readonly float _basicEvolutionButtonHeight = 400f;

        private GameObject _specialEvolutionNavigationPopup;
        private SpecialEvolutionResearchButton _canResearchSpecialEvolutionButton;
        private RectTransform _canResearchSpecialEvolutionButtonRectTransform;
        private RectTransform _bottomNotResearchSpecialEvolutionButtonRectTransform;
        private bool _isOpenSpecialEvolutionNavigationPopup;

        /// <summary>
        /// 다음 순서의 특수 진화이고 레벨/골드/특수 DNA 조건을 만족하면 연구할 수 있다.
        /// </summary>
        private static bool IsResearchable(SpecialEvolutionStaticData specialEvolution, int highestSpecialEvolutionID, int accountLevel, long goldAmount, long specialDNAAmount)
        {
            return specialEvolution.specialEvolutionID == highestSpecialEvolutionID + 1 &&
                specialEvolution.learnLevel <= accountLevel &&
                specialEvolution.requiredGold <= goldAmount &&
                specialEvolution.requiredSpecialDNA <= specialDNAAmount;
        }

        public void Initialize(EvolutionData evolutionData, int accountLevel, long goldAmount, long specialDNAAmount)
        {
            _highestBasicEvolutionID = evolutionData.HighestBasicEvolutionID;
            _highestSpecialEvolutionID = evolutionData.HighestSpecialEvolutionID;
            _accountLevel = accountLevel;
            _goldAmount = goldAmount;
            _specialDNAAmount = specialDNAAmount;

            float sectionWidth = UnityGlobal.Scenes.GetCurrentSceneUI<LobbySceneUIRoot>().GetComponent<RectTransform>().rect.width / 4f; //GameClient.CameraController.CameraResolution.x / 4;
            _basicEvolutionButtonOffset = new Vector3(-sectionWidth, _basicEvolutionButtonHeight * 1.5f, 0f);
            _researchLevelLabelOffset = new Vector3(0, _basicEvolutionButtonHeight * 1.5f, 0f);
            _specialEvolutionButtonOffset = new Vector3(sectionWidth, 0f, 0f);  //연구 가능 레벨 위치값을 가져와 사용하기 때문에 높이에 대한 조절은 필요 없다

            _basicEvolutionResearchLineOffset = _basicEvolutionButtonOffset + new Vector3(0, -37f, 0);   //라인이미지 공백으로 인한 수정

            //스크롤을 위한 스크롤컨텐츠 사이즈 조절 하는 코드 
            var basicEvolutionStaticDatas = StaticDataRepository.Instance.BasicEvolutions.BasicEvolutionStaticDatas;
            _contentTransform.sizeDelta = new Vector2(_contentTransform.sizeDelta.x, _basicEvolutionButtonHeight * basicEvolutionStaticDatas.Count - _basicEvolutionButtonHeight * 3);

            //파도 연출은 연구 가능 라벨보다 먼저 생성되어야 한다.
            //UI 레이어 순서때문에
            this.CreateEvolutionWave(evolutionData);
            this.CreateResearchLevelLabels(accountLevel);
            this.CreateEvolutionResearchPopup();
            this.CreateBasicEvolutionButtons(evolutionData, accountLevel, goldAmount);
            this.CreateSpecialEvolutionButtons(evolutionData, accountLevel, goldAmount, specialDNAAmount);
            this.CreateSpecialEvolutionNavigationPopup();
        }

        private void CreateEvolutionResearchPopup()
        {
            _evolutionResearchPopup = ResourcePool.Instance.InstantiateFromResource<EvolutionResearchPopup>(_evolutionResearchPopupPath);
            _evolutionResearchPopup.transform.SetParent(_contentTransform);
            _evolutionResearchPopup.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            _evolutionResearchPopup.gameObject.SetActive(false);
        }

        private void CreateSpecialEvolutionNavigationPopup()
        {
            _specialEvolutionNavigationPopup = ResourcePool.Instance.InstantiateFromResource(SpecialEvolutionNavigationPopup.PREFAB_PATH);
            _specialEvolutionNavigationPopup.transform.SetParent(this.transform);
            _specialEvolutionNavigationPopup.transform.localPosition = Vector3.zero;
            _specialEvolutionNavigationPopup.gameObject.SetActive(false);

        }

        //기본 진화 버튼, 버튼들 연결해주는 슬라이더 생성
        private void CreateBasicEvolutionButtons(EvolutionData evolutionData, int accountLevel, long goldAmount)
        {
            int buttonCount = 0;

            _basicEvolutionButtons = new List<BasicEvolutionResearchButton>();
            var basicEvolutionStaticDatas = StaticDataRepository.Instance.BasicEvolutions.BasicEvolutionStaticDatas;

            GameObject buttonPrefab = ResourcePool.Instance.LoadResource<GameObject>(_basicEvolutionButtonPath);
            foreach (var kvp in basicEvolutionStaticDatas)
            {
                GameObject buttonObject = Instantiate(buttonPrefab, _basicButtonTransform);
                buttonObject.transform.localPosition = _basicEvolutionButtonOffset + new Vector3(0, _basicEvolutionButtonHeight * buttonCount++);
                BasicEvolutionResearchButton basicEvolutionButton = buttonObject.GetComponent<BasicEvolutionResearchButton>();
                basicEvolutionButton.Initialize(kvp.Value, evolutionData.HighestBasicEvolutionID, accountLevel, goldAmount, OnBasicEvolutionButtonClick);
                _basicEvolutionButtons.Add(basicEvolutionButton);
            }

            GameObject basicEvolutionLineSliderPrefab = ResourcePool.Instance.LoadResource<GameObject>(_basicEvolutionLineSliderPath);
            _basicEvolutionLineSlider = Instantiate(basicEvolutionLineSliderPrefab, _lineTransform).GetComponent<Slider>();
            _basicEvolutionLineSlider.transform.localPosition = _basicEvolutionResearchLineOffset;
            _basicEvolutionLineSlider.GetComponent<RectTransform>().sizeDelta = new Vector3(91f, _basicEvolutionButtonHeight * (basicEvolutionStaticDatas.Count - 1));
            _basicEvolutionLineSlider.minValue = 1;
            _basicEvolutionLineSlider.maxValue = basicEvolutionStaticDatas.Count;
            _basicEvolutionLineSlider.value = evolutionData.HighestBasicEvolutionID;
        }



        //스페셜 진화 버튼들, 중간 연결해주는 라인 오브젝트들 생성
        private void CreateSpecialEvolutionButtons(EvolutionData evolutionData, int accountLevel, long goldAmount, long specialDNAAmount)
        {
            Debug.Assert(_researchLevelLabelsByLevel != null, "레벨 라벨이 생성되어 있어야 됩니다.");

            _specialEvolutionButtons = new List<SpecialEvolutionResearchButton>();
            var specialEvolutionStaticDatas = StaticDataRepository.Instance.SpecialEvolutions.SpecialEvolutionStaticDatas;

            _canResearchSpecialEvolutionButton = null;
            _canResearchSpecialEvolutionButtonRectTransform = null;

            var userGameData = GameClient.CS.UserGameData;
            GameObject buttonPrefab = ResourcePool.Instance.LoadResource<GameObject>(_specialEvolutionButtonPath);
            foreach (var kvp in specialEvolutionStaticDatas)
            {
                GameObject buttonObject = Instantiate(buttonPrefab, _specialButtonTransform);
                buttonObject.transform.localPosition = _specialEvolutionButtonOffset + new Vector3(0f, _researchLevelLabelsByLevel[kvp.Value.learnLevel].transform.localPosition.y, 0f);
                SpecialEvolutionResearchButton specialEvolutionButton = buttonObject.GetComponent<SpecialEvolutionResearchButton>();
                specialEvolutionButton.Initialize(kvp.Value, evolutionData.HighestSpecialEvolutionID, accountLevel, goldAmount, specialDNAAmount, OpenSpecialEvolutionResearchPopup);
                _specialEvolutionButtons.Add(specialEvolutionButton);
                
                //습득 가능한 특수 진화 버튼은 네비게이션 팝업을 위해 기억해둔다.
               if(IsResearchable(kvp.Value, userGameData.HighestSpecialEvolutionID, userGameData.AccountLevel, userGameData.Gold, userGameData.SpecialDNA))
                {
                    _canResearchSpecialEvolutionButton = specialEvolutionButton;
                    _canResearchSpecialEvolutionButtonRectTransform = specialEvolutionButton.GetComponent<RectTransform>();
                    _bottomNotResearchSpecialEvolutionButtonRectTransform = _canResearchSpecialEvolutionButtonRectTransform;
                }
            }

            //습득 가능한 특수 진화 버튼이 없으면 가장 아래에 있는(재화가 있으면 습득 가능한) 버튼 정보를 기억한다.
            if (_canResearchSpecialEvolutionButtonRectTransform == null)
            {
                foreach (var button in _specialEvolutionButtons)
                {
                    if (IsResearchable(button.ButtonSpecialEvolutionStaticData, userGameData.HighestSpecialEvolutionID, userGameData.AccountLevel, long.MaxValue, long.MaxValue))
                    {
                        _bottomNotResearchSpecialEvolutionButtonRectTransform = button.GetComponent<RectTransform>();
                        break;
                    }
                }
            }

            _specialEvolutionResearchLines = new List<GameObject>();
            GameObject linePrefab = ResourcePool.Instance.LoadResource<GameObject>(_specialEvolutionLinePath);
            for (int i = 0; i < _specialEvolutionButtons.Count - 1; i++)
            {
                GameObject lineObject;
                lineObject = Instantiate(linePrefab, _lineTransform);
                if (evolutionData.HighestSpecialEvolutionID >= i + 2)
                {
                    lineObject.GetComponent<Image>().sprite = ResourcePool.Instance.LoadResource<Sprite>(_specialEvlutionResearchLineSpritePath);
                }
                else
                {
                    lineObject.GetComponent<Image>().sprite = ResourcePool.Instance.LoadResource<Sprite>(_specialEvlutionUnResearchLineSpritePath);
                }

                lineObject.GetComponent<RectTransform>().anchoredPosition = _specialEvolutionButtons[i].transform.localPosition;
                lineObject.GetComponent<RectTransform>().sizeDelta = new Vector2(
                                                                    lineObject.GetComponent<RectTransform>().sizeDelta.x,
                                                                    _specialEvolutionButtons[i + 1].transform.localPosition.y - _specialEvolutionButtons[i].transform.localPosition.y - 150f);
                _specialEvolutionResearchLines.Add(lineObject);
            }
        }

        //중간 레벨 라벨 오브젝트 생성, 현재 습득 가능한 진화 라인 생성
        private void CreateResearchLevelLabels(int accountLevel)
        {
            _researchLevelLabelsByLevel = new Dictionary<int, ResearchLevelLabel>();
            var basicEvolutionStaticDatas = StaticDataRepository.Instance.BasicEvolutions.BasicEvolutionStaticDatas;

            int prevLevel = basicEvolutionStaticDatas.First().Value.learnLevel;
            int dataCount = 0;
            GameObject levelPrefab = ResourcePool.Instance.LoadResource<GameObject>(_levelLabelPath);
            foreach (var kvp in basicEvolutionStaticDatas)
            {
                if (prevLevel != kvp.Value.learnLevel)
                {
                    GameObject labelObject = Instantiate(levelPrefab, _levelLabelTransform);
                    labelObject.transform.localPosition = _researchLevelLabelOffset + new Vector3(0, (_basicEvolutionButtonHeight * (dataCount - 1)), 0);

                    ResearchLevelLabel researchLevelLabel = labelObject.GetComponent<ResearchLevelLabel>();
                    researchLevelLabel.Initialize(prevLevel, accountLevel);
                    _researchLevelLabelsByLevel.Add(prevLevel, researchLevelLabel);

                    prevLevel = kvp.Value.learnLevel;
                    dataCount++;
                }
                else
                {
                    dataCount++;
                }
            }

            if (accountLevel >= basicEvolutionStaticDatas.First().Value.learnLevel)
            {
                GameObject canResearchLinePrefab = ResourcePool.Instance.LoadResource<GameObject>(_canResearchLinePath);
                _canResearchLine = Instantiate(canResearchLinePrefab, _backgroundTransform);  //해당 라인은 배경으로 처리한다.
                _canResearchLine.GetComponent<RectTransform>().anchoredPosition = _researchLevelLabelsByLevel[accountLevel].transform.localPosition;
            }
        }

        //진화 파도 오브젝트, 신규 진화시 거품 애니메이션 생성
        private void CreateEvolutionWave(EvolutionData evolutionData)
        {
            GameObject evolutionWavePrefab = ResourcePool.Instance.LoadResource<GameObject>(_evolutionWavePath);
            _evolutionWave = Instantiate(evolutionWavePrefab, _backgroundTransform);

            _evolutionWave.GetComponent<RectTransform>().sizeDelta = new Vector2(_evolutionWave.GetComponent<RectTransform>().sizeDelta.x, (_basicEvolutionButtonHeight * evolutionData.HighestBasicEvolutionID) + (_basicEvolutionButtonHeight * 1.0f));
            _evolutionWave.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0, 0);

            GameObject evolutionWaveBubblePrefab = ResourcePool.Instance.LoadResource<GameObject>(_evolutionWaveBubblePath);
            _evolutionWaveBubble = Instantiate(evolutionWaveBubblePrefab, _evolutionWave.transform);
            _evolutionWaveBubble.transform.position = new Vector3(Screen.width * 0.5f, 200f, 0);
            _evolutionWaveBubble.gameObject.SetActive(false);
        }


        /// <summary>
        /// 기본 진화 데이터만 업데이트
        /// </summary>
        /// <param name="highestBasicEvolutionID">플레이어 기본 진화 최대값</param>
        public void UpdateBasicEvolutionResearchPage(int highestBasicEvolutionID, int accountLevel, long goldAmount)
        {
            Debug.Assert(_basicEvolutionButtons != null);
            Debug.Assert(_scrollRect != null);
            Debug.Assert(_evolutionWave != null);
            Debug.Assert(_basicEvolutionLineSlider != null);

            _highestBasicEvolutionID = highestBasicEvolutionID;
            _accountLevel = accountLevel;
            _goldAmount = goldAmount;
            foreach (var button in _basicEvolutionButtons)
            {
                button.UpdateBasicEvolutionResearchButton(highestBasicEvolutionID, accountLevel, goldAmount);
            }

            //웨이브 올라가는 연출
            Vector2 nextEvolutionWaveSizeDelta = new Vector2(_evolutionWave.GetComponent<RectTransform>().sizeDelta.x, (_basicEvolutionButtonHeight * _highestBasicEvolutionID) + (_basicEvolutionButtonHeight * 1.0f));
            _evolutionWave.GetComponent<RectTransform>().DOSizeDelta(nextEvolutionWaveSizeDelta, 1.0f);
            _evolutionWave.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0, 0);

            //베이직 라인 증가 연출
            _basicEvolutionLineSlider.DOValue(highestBasicEvolutionID, 1.0f);

            //
            _evolutionWaveBubble.gameObject.SetActive(true);
            _evolutionWaveBubble.transform.position = new Vector3(Screen.width * 0.5f, 200f, 0);
            DOVirtual.DelayedCall(2f, () => { _evolutionWaveBubble.SetActive(false); });
        }

        public void UpdateSpecialEvolutionResearchPage(int highestSpecialEvolutionID, int accountLevel, long goldAmount, long specialDNAAmount)
        {
            Debug.Assert(_specialEvolutionButtons != null);
            Debug.Assert(_scrollRect != null);
            _highestSpecialEvolutionID = highestSpecialEvolutionID;
            _accountLevel = accountLevel;
            _goldAmount = goldAmount;
            _specialDNAAmount = specialDNAAmount;


            _canResearchSpecialEvolutionButton = null;
            _canResearchSpecialEvolutionButtonRectTransform = null;
            var userGameData = GameClient.CS.UserGameData;
            foreach (var button in _specialEvolutionButtons)
            {
                button.UpdateSpecialEvolutionResearchButton(highestSpecialEvolutionID, accountLevel, goldAmount, specialDNAAmount);
                //습득 가능한 특수 진화 버튼도 업데이트 해준다.
                if (IsResearchable(button.ButtonSpecialEvolutionStaticData, userGameData.HighestSpecialEvolutionID, userGameData.AccountLevel, userGameData.Gold, userGameData.SpecialDNA))
                {
                    _canResearchSpecialEvolutionButton = button;
                    _canResearchSpecialEvolutionButtonRectTransform = button.GetComponent<RectTransform>();
                    _bottomNotResearchSpecialEvolutionButtonRectTransform = _canResearchSpecialEvolutionButtonRectTransform;
                }
            }

            //습득 가능한 특수 진화 버튼이 없으면 가장 아래에 있는(재화가 있으면 습득 가능한) 버튼 정보를 기억한다.
            if(_canResearchSpecialEvolutionButtonRectTransform == null)
            {
                foreach (var button in _specialEvolutionButtons)
                {
                    if (IsResearchable(button.ButtonSpecialEvolutionStaticData, userGameData.HighestSpecialEvolutionID, userGameData.AccountLevel, long.MaxValue, long.MaxValue))
                    {
                        _bottomNotResearchSpecialEvolutionButtonRectTransform = button.GetComponent<RectTransform>();
                        break;
                    }
                }
            }

            if (highestSpecialEvolutionID >= 2)
            {
                int index = highestSpecialEvolutionID - 2;
                _specialEvolutionResearchLines[index].GetComponent<Image>().sprite = ResourcePool.Instance.LoadResource<Sprite>(_specialEvlutionResearchLineSpritePath);
            }

        }

        /// <summary>
        /// 모든 데이터 업데이트(새로고침)
        /// </summary>
        public void RefreshEvolutionResearchPage(EvolutionData evolutionData, int accountLevel, long goldAmount)
        {
            Debug.Assert(_basicEvolutionButtons != null);
            Debug.Assert(_scrollRect != null);
            Debug.Assert(_evolutionWave != null);
            Debug.Assert(_basicEvolutionLineSlider != null);
            Debug.Assert(_researchLevelLabelsByLevel != null);

            _highestBasicEvolutionID = evolutionData.HighestBasicEvolutionID;
            _highestSpecialEvolutionID = evolutionData.HighestSpecialEvolutionID;
            _accountLevel = accountLevel;
            _goldAmount = goldAmount;

            //베이직 버튼 업데이트
            foreach (var button in _basicEvolutionButtons)
            {
                button.UpdateBasicEvolutionResearchButton(_highestBasicEvolutionID, accountLevel, goldAmount);
            }

            //스크롤 위치 조정
            _scrollRect.verticalNormalizedPosition = (float)_highestBasicEvolutionID / _basicEvolutionButtons.Count;

            //웨이브 위치 조정
            Vector2 nextEvolutionWaveSizeDelta = new Vector2(_evolutionWave.GetComponent<RectTransform>().sizeDelta.x, (_basicEvolutionButtonHeight * _highestBasicEvolutionID) + (_basicEvolutionButtonHeight * 1.0f));
            _evolutionWave.GetComponent<RectTransform>().sizeDelta = nextEvolutionWaveSizeDelta;
            _evolutionWave.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 0, 0);

            //베이직 라인 위치 조정
            _basicEvolutionLineSlider.value = evolutionData.HighestBasicEvolutionID;

            //획득 가능한 라인 위치 조정
            if (_canResearchLine != null && accountLevel >= _researchLevelLabelsByLevel.First().Key)
            {
                _canResearchLine.GetComponent<RectTransform>().anchoredPosition = _researchLevelLabelsByLevel[accountLevel].transform.localPosition;
            }
        }


        private void OnBasicEvolutionButtonClick(BasicEvolutionResearchButton basicEvolutionButton)
        {
            Debug.Assert(_evolutionResearchPopup != null);

            this.ScrollMoveResearchEvolutionButton(basicEvolutionButton.GetComponent<RectTransform>());

            _evolutionResearchPopup.gameObject.SetActive(true);
            _evolutionResearchPopup.InitializeFromBasicEvolution(basicEvolutionButton.ButtonBasicEvolutionStaticData, _highestBasicEvolutionID, _accountLevel, _goldAmount, CloseEvolutionResearchPopup);

            RectTransform rectTransform = _evolutionResearchPopup.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = basicEvolutionButton.transform.localPosition + new Vector3(rectTransform.rect.width * 0.5f - 171, 132.5f, 0f);

            DOTween.Sequence(_evolutionResearchPopup)
                 .Append(_evolutionResearchPopup.transform.DOScale(Vector3.one + new Vector3(0.05f, 0.05f, 0.05f), 0.065f).SetEase(Ease.InCubic))
                 .Append(_evolutionResearchPopup.transform.DOScale(Vector3.one, 0.05f).SetEase(Ease.InCubic))
                 .SetUpdate(isIndependentUpdate: true);
        }
        private void OpenSpecialEvolutionResearchPopup(SpecialEvolutionResearchButton specialEvolutionButton)
        {
            Debug.Assert(_evolutionResearchPopup != null);

            this.ScrollMoveResearchEvolutionButton(specialEvolutionButton.GetComponent<RectTransform>());

            _evolutionResearchPopup.gameObject.SetActive(true);
            _evolutionResearchPopup.InitializeFromSpecialEvolution(
                specialEvolutionButton.ButtonSpecialEvolutionStaticData, _highestSpecialEvolutionID, _accountLevel, _goldAmount, _specialDNAAmount,
                () =>
                {
                    this.ScrollMoveResearchEvolutionButton(_bottomNotResearchSpecialEvolutionButtonRectTransform);
                }, CloseEvolutionResearchPopup);

            RectTransform rectTransform = _evolutionResearchPopup.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = specialEvolutionButton.transform.localPosition + new Vector3(-rectTransform.rect.width * 0.5f + 171f, 169f, 0f);

            DOTween.Sequence(_evolutionResearchPopup)
               .Append(_evolutionResearchPopup.transform.DOScale(Vector3.one + new Vector3(0.05f, 0.05f, 0.05f), 0.065f).SetEase(Ease.InCubic))
               .Append(_evolutionResearchPopup.transform.DOScale(Vector3.one, 0.05f).SetEase(Ease.InCubic))
               .SetUpdate(isIndependentUpdate: true);
        }

        private void CloseEvolutionResearchPopup()
        {
            DOTween.Sequence(_evolutionResearchPopup)
                     .Append(_evolutionResearchPopup.transform.DOScale(new Vector3(0f, 0f, 0f), 0.075f).SetEase(Ease.InCubic))
                     .OnComplete(() =>
                     {
                         _evolutionResearchPopup.gameObject.SetActive(false);
                     })
                     .SetUpdate(isIndependentUpdate: true);
        }

        private void OpenSpecialEvolutionNavigationPopup(bool isArrowUp)
        {
            Debug.Assert(_specialEvolutionNavigationPopup != null);
            _specialEvolutionNavigationPopup.gameObject.SetActive(true);
            _specialEvolutionNavigationPopup.GetComponent<SpecialEvolutionNavigationPopup>().Initialize(isArrowUp, ()=>
            {
                this.ScrollMoveResearchEvolutionButton(_bottomNotResearchSpecialEvolutionButtonRectTransform);
            });

            DOTween.Sequence(_specialEvolutionNavigationPopup)
                .Append(_specialEvolutionNavigationPopup.transform.DOScale(1f, 0.1f).SetEase(Ease.InCubic).From(0f))
                .SetUpdate(isIndependentUpdate: true);

            _isOpenSpecialEvolutionNavigationPopup = true;
        }

        private void CloseSpecialEvolutionNavigationPopup()
        {
            DOTween.Sequence(_specialEvolutionNavigationPopup)
                 .Append(_specialEvolutionNavigationPopup.transform.DOScale(0f, 0.1f).SetEase(Ease.InCubic))
                 .OnComplete(() =>
                 {
                     _specialEvolutionNavigationPopup.gameObject.SetActive(false);
                 })
                 .SetUpdate(isIndependentUpdate: true);
            _isOpenSpecialEvolutionNavigationPopup = false;
        }

        //연구 팝업외에 클릭되면 연구팝업을 닫는 역할을 한다.
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!IsEvolutionResearchPopupOpen)
            {
                return;
            }

            GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;
            if (clickedObject != _evolutionResearchPopup)
            {
                this.CloseEvolutionResearchPopup();
            }
        }

        public void UpdateLogic()
        {
            if(!isActiveAndEnabled)
            {
                if(_isOpenSpecialEvolutionNavigationPopup)
                {
                    this.CloseSpecialEvolutionNavigationPopup();
                }
                return;
            }

            if(_canResearchSpecialEvolutionButtonRectTransform != null)
            {
                //타겟의 화면 위치를 계산한다.
                Vector3[] buttonCorners = new Vector3[4];
                _canResearchSpecialEvolutionButtonRectTransform.GetWorldCorners(buttonCorners);
                Vector2 buttonBottomLeft = RectTransformUtility.WorldToScreenPoint(GameClient.CameraController.MainCamera, buttonCorners[0]);
                Vector2 buttonTopRight = RectTransformUtility.WorldToScreenPoint(GameClient.CameraController.MainCamera, buttonCorners[2]);

                //화면 비율 맞춘다고 스크린에 생기는 검은 공백 부분
                float screenY = Screen.height * (1 - GameClient.CameraController.MainCamera.rect.height) * 0.5f;
                LobbySceneUIRoot lobbySceneUIRoot = UnityGlobal.Scenes.GetCurrentSceneUI<LobbySceneUIRoot>();

                if (buttonBottomLeft.y + screenY > Screen.height )
                {
                    //버튼이 위에 있는 경우
                    if(!_isOpenSpecialEvolutionNavigationPopup)
                    {
                        this.OpenSpecialEvolutionNavigationPopup(isArrowUp: true);
                    }
                }
                else if(buttonTopRight.y - screenY <  100f)  //대략적인 네비게이션 바 높이
                {
                    //버튼이 아래에 있는 경우
                    if(!_isOpenSpecialEvolutionNavigationPopup)
                    {
                        this.OpenSpecialEvolutionNavigationPopup(isArrowUp: false);
                    }
                }
                else
                {
                    //버튼이 화면에 나오고 있는 경우
                    if (_isOpenSpecialEvolutionNavigationPopup)
                    {
                        this.CloseSpecialEvolutionNavigationPopup();
                    }
                }
            }
        } 
        

        private void ScrollMoveResearchEvolutionButton(RectTransform buttonRectTransform)
        {
            if(buttonRectTransform != null)
            {
                _scrollRect.DOVerticalNormalizedPos((buttonRectTransform.localPosition.y - Screen.height * 0.3f) / _contentTransform.sizeDelta.y, 0.2f); 
            }
        }
    }
}
