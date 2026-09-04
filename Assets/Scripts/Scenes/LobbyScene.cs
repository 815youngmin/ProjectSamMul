using Shared.UserDatas;
using System;
using UnityEngine;
using SamMul.GameClients;
using SamMul.UnityHelpers;
using SamMul.UnityHelpers.Sounds;

namespace SamMul.Scenes
{
    public class LobbySceneInitialData : ISceneInitialData
    {
        public SceneType SceneType => SceneType.Lobby;

        public readonly int ClearedHighestChapter;

        public LobbySceneInitialData(UserGameData userGameData)
        {
            this.ClearedHighestChapter = userGameData.ClearedHighestChapter;
        }
    }

    public class LobbyScene : BaseScene
    {
        public override SceneType SceneType => SceneType.Lobby;

        public new LobbySceneUIRoot UI => (LobbySceneUIRoot)base.UI;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void Initialize(ISceneInitialData initialData)
        {
            base.Initialize(initialData);

            LobbySceneInitialData lobbySceneInitialData = null;
            if (initialData != null)
            {
                Debug.Assert(initialData.SceneType == this.SceneType);
                lobbySceneInitialData = (LobbySceneInitialData)initialData;
            }
            else
            {
                // 앱의 첫 씬으로 바로 시작한 경우: 로컬 세션의 유저 데이터로 초기화한다.
                lobbySceneInitialData = new LobbySceneInitialData(GameClient.CS.UserGameData);
            }

            this.UI.Initialize(this.SceneType, lobbySceneInitialData.ClearedHighestChapter);
        }

        public override void OnLoadingSceneRemoved()
        {
            base.OnLoadingSceneRemoved();

            UnityGlobal.Sounds.PlayBGM("Sounds/BGMs/BGM_MainLobby_SFX.prefab", BGMPlayRule.ForcePlay);
        }

        public override void Clear()
        {
            this.UI.Clear();

            base.Clear();
        }

        protected override void Update()
        {
            base.Update();

            if (!this.IsSceneInitialized || this.IsSceneCleared)
            {
                return;
            }

            this.UI.UpdateLogic();
        }
    }

}
