using Shared.StaticDatas;
using System.Collections;
using UnityEngine;
using Z.Scenes;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.StageEvents
{
    public class StageEffectWarningStageEvent : StageEventBase
    {
        private WarningPopup.WarningType _warningType;
        public StageEffectWarningStageEvent(StageEventStaticData stageEventStaticData, WarningPopup.WarningType warningType) : base(stageEventStaticData)
        {
            _warningType = warningType;
        }

        public override void Begin(Stage stage)
        {
            base.Begin(stage);
            Debug.Log("Call StageEffectWarningStageEvent.Begin " + BeginAt);

            var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
            Debug.Assert(stageSceneUI != null);
            stageSceneUI.OnBeginWarningEvent(_warningType);

            stageSceneUI.StartCoroutine(PlayWarningSound(stage.PC?.Pos ?? Vector2.zero));
        }

        public IEnumerator PlayWarningSound(Vector2 position)
        {
            for (int i = 0; i < 3; ++i)
            {
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/11-Warning_SFX.prefab", position);

                yield return new WaitForSecondsRealtime(1.2f);
            }
        }

        public override void End(Stage stage)
        {
            Debug.Log("Call RushWarningStageEvent.End " + EndAt);
            var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
            Debug.Assert(stageSceneUI != null);
            stageSceneUI.OnEndWarningEvent();
        }

        public override void Update(Stage stage, int stageEventTickNumber)
        {
        }
    }
}

