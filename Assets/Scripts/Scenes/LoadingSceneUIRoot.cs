using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SamMul.Scenes
{

    public class LoadingSceneUIRoot : BaseSceneUIRoot
    {
        [SerializeField]
        private Image _firstStoryBackground;

        [SerializeField] 
        private Image _secondStoryBackground;

        [SerializeField]
        private GameObject _mainLoadingObject;

        [SerializeField]
        private Image _titleLogo;

        // Prefab에 이름이 연결되어있어 리네이밍하지 못하고 이대로 쓴다. 이녀석의 올바른 이름은 _firstStoryLoadingTextGameObject다.
        [SerializeField]
        private GameObject _firstStoryLoadingText;

        // Prefab에 이름이 연결되어있어 리네이밍하지 못하고 이대로 쓴다. 이녀석의 올바른 이름은 _mainLoadingTextGameObject다.
        [SerializeField]
        private GameObject _mainLoadingText;

        [SerializeField]
        private TextMeshProUGUI _loadingTextText;

        // 아... 이거 왜....
        [SerializeField]
        private TextMeshProUGUI _firstStoryLoadingTextText;

        [SerializeField]
        private LoadingProgressBar _loadingProgressBar;

        // loadingStoryFlag : 0 - main loading, 1 - first story loading, 2 - second story loading
        public void Initialize(SceneType sceneType, int loadingStoryFlag)
        {
            this.InitializeBase(sceneType);

            _loadingTextText.text = "LOADING..."; // 여기 로딩씬이라서 Localizer쓸수가 없음 Localizer.Instance.GetText("UI_LOADING_TEXT"); 로딩씬은 제약이 잇음
            _firstStoryLoadingTextText.text = "LOADING..."; // Localizer.Instance.GetText("UI_LOADING_TEXT");


            if (loadingStoryFlag == 1)
            {
                // 0챕터 입장시 로딩씬 이미지 그려줌
                _firstStoryBackground.gameObject.SetActive(true);
                _secondStoryBackground.gameObject.SetActive(false);
                _mainLoadingObject.SetActive(false);
                _titleLogo.gameObject.SetActive(false);

                _mainLoadingText.SetActive(false);
                _firstStoryLoadingText.SetActive(true);
                _loadingProgressBar.gameObject.SetActive(true);
                _loadingProgressBar.Initialize();
            }
            else if (loadingStoryFlag == 2)
            {
                // 0챕터 퇴장시 로딩씬 이미지 그려줌
                _firstStoryBackground.gameObject.SetActive(false);
                _secondStoryBackground.gameObject.SetActive(true);
                _mainLoadingObject.SetActive(false);
                _titleLogo.gameObject.SetActive(false);

                _mainLoadingText.SetActive(false);
                _firstStoryLoadingText.SetActive(true);
                _loadingProgressBar.gameObject.SetActive(true);
                _loadingProgressBar.Initialize();
            }
            else
            {
                _firstStoryBackground.gameObject.SetActive(false);
                _secondStoryBackground.gameObject.SetActive(false);
                _mainLoadingObject.SetActive(true);
                _titleLogo.gameObject.SetActive(true);

                _mainLoadingText.SetActive(true);
                _firstStoryLoadingText.SetActive(false);
                _loadingProgressBar.gameObject.SetActive(true);
                _loadingProgressBar.Initialize();
            }
        }
        public void SetProgressValue(float value)
        {
            _loadingProgressBar.SetProgressValue(value);
        }
    }
}
