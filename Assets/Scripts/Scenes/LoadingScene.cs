using UnityEngine;
using Z.UnityHelpers;
using Z.UnityHelpers.Sounds;

namespace Z.Scenes
{
    public class LoadingScene : BaseScene
    {
        public override SceneType SceneType => SceneType.Loading;
        public new LoadingSceneUIRoot UI => (LoadingSceneUIRoot)base.UI;

        private bool _isInitialized = false;
        private float _initializedAt;

        protected override void Awake()
        {
            base.Awake();
        }

        // 로딩씬은 이게 안들어온다.
        public override void Initialize(ISceneInitialData initialData)
        {
            base.Initialize(initialData);
            _initializedAt = Time.time;
            this.SetLoadingProgress(0.0f);

            UnityGlobal.Sounds.PlayBGM("Sounds/BGMs/BGM_MainLobby_SFX.prefab", BGMPlayRule.SkipRePlayIfSameBGM);
        }

        protected override void Update()
        {
            base.Update();

            if (!_isInitialized)
            {
                _isInitialized = true;

                int loadingStoryFlag = PlayerPrefs.GetInt("TEMP_TEMP_KEY_LOADING_STORY_SCENE", defaultValue: 0);
                PlayerPrefs.SetInt("TEMP_TEMP_KEY_LOADING_STORY_SCENE", 0);

                this.UI.Initialize(this.SceneType, loadingStoryFlag);
            }
            
            // 로딩 프로그레스는 SceneChanger 측에서, 현재 로드하고 있는 씬의 로딩 상태를 참조하여 업데이트 해준다.
            // SetLoadingProgress(...)를 통해 제어함.
        }

        public void SetLoadingProgress(float progress)
        {
            this.UI.SetProgressValue(progress);
        }

        public override void Clear()
        {
            base.Clear();
        }

    }
}