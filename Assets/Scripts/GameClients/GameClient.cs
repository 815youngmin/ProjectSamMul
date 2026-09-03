#nullable enable
using System.Collections.Generic;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using Shared.UserDatas;
using UnityEngine;
using UnityEngine.SceneManagement;
using SamMul.GameClients.Cameras;
using SamMul.GameClients.Stages;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.Scenes;

namespace SamMul.GameClients
{
    /// <summary>
    /// 게임을 구동하는 전역 클라이언트입니다. 서버 없이 동작하는 오프라인 버전입니다.
    /// </summary>
    public sealed partial class GameClient
    {
        private static GameClient? s_instance;
        public static GameClient Instance => s_instance ??= new GameClient();

        private bool _hasInitialized;

        private AsyncJobDispatcher _asyncDispatcher = null!;
        private CameraController _cameraController = null!;
        private StaticDataRepository _staticDataRepository = null!;
        private OfflineSession _session = null!;
        private Stage? _stage;
        private PlayerCharacterController? _pcController;

        public static AsyncJobDispatcher AsyncDispatcher => Instance._asyncDispatcher;
        /// <summary>옛 네트워크 클라이언트 자리. 오프라인 세션이 같은 호출 형태로 대신한다.</summary>
        public static OfflineSession CS => Instance._session;
        public static CameraController CameraController => Instance._cameraController;
        public static Stage? Stage => Instance._stage;
        public static PlayerCharacterController? PCController => Instance._pcController;
        public bool IsAutoPlayActivated => _pcController?.IsAutoPlayActivated ?? false;

        public void Initialize(StaticDataRepository staticData)
        {
            if (_hasInitialized)
            {
                return;
            }
            _hasInitialized = true;

            _staticDataRepository = staticData;
            _asyncDispatcher = new AsyncJobDispatcher();
            _session = new OfflineSession(staticData);
            _cameraController = new CameraController();
            _stage = null;
            _pcController = null;
        }

        public void Update(SceneType sceneType)
        {
            if (sceneType == SceneType.Stage)
            {
                if (_stage != null && !_stage.HasStagePlayResult)
                {
                    _pcController?.Update(_stage);
                    _stage.Update();
                }

                _cameraController.Update();
            }

            _asyncDispatcher.Dispatch();
        }

        public void ClearBeforeChangingScene(Scene clearingScene, SceneType clearingSceneType)
        {
            if (clearingSceneType == SceneType.Stage)
            {
                _stage?.ClearBeforeChangingScene(clearingScene);
                _stage = null;
                _pcController = null;
            }
        }

        public void CreateMainChapterStage(
            ChapterStaticData chapterStaticData,
            IReadOnlyList<SkillId> userSkillDeck,
            HeroData userHeroData,
            IEnumerable<EquipmentData> userHeroEquippedEquipments)
        {
            _stage = Stage.CreateForMainChapter(chapterStaticData, _staticDataRepository);
            _stage.Initialize(userSkillDeck, userHeroData, userHeroEquippedEquipments);
        }

        public void CreatePlayerController(VariableJoystick joystick, bool activateAutoPlay)
        {
            Debug.Assert(_stage != null, "스테이지를 먼저 생성해야 합니다.");
            _pcController = new PlayerCharacterController(_stage!.PC, joystick);
            _pcController.SwitchAutoPlay(activateAutoPlay);
        }
    }
}
