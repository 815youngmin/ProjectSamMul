using Shared.StaticDatas;
using System.Collections;
using UnityEngine;
using Z.Scenes;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.StageEvents
{
    public class BossWarningStageEvent : StageEventBase
    {

        public BossWarningStageEvent(StageEventStaticData stageEventStaticData) : base(stageEventStaticData)
        {
        }

        public override void Begin(Stage stage)
        {
            base.Begin(stage);
            Debug.Log("Call BossWarningStageEvent.Begin " + BeginAt);

            var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
            Debug.Assert(stageSceneUI != null);
            stageSceneUI.OnBeginWarningEvent(WarningPopup.WarningType.Boss);

            stageSceneUI.StartCoroutine(PlayWarningSound(stage.PC?.Pos ?? Vector2.zero));
        }
        private IEnumerator PlayWarningSound(Vector2 position)
        {
            for (int i = 0; i < 3; ++i)
            {
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/11-Warning_SFX.prefab", position);

                yield return new WaitForSecondsRealtime(1.2f);
            }
        }


        public override void End(Stage stage)
        {
            Debug.Log("Call BossWarningStageEvent.End " + EndAt);

            var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
            Debug.Assert(stageSceneUI != null);
            stageSceneUI.OnEndWarningEvent();
        }

        public override void Update(Stage stage, int stageEventTickNumber)
        {           
        }
    }

}
