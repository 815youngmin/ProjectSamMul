using DG.Tweening;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using SamMul.Animations.Placeholder;
using System;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using SamMul.Scenes;
using SamMul.UnityHelpers;
using Random = UnityEngine.Random;

namespace SamMul.GameClients.Stages.StageEvents
{
    public class StageEnterInitEvent : StageEventBase
    {
        public StageEnterInitEvent(StageEventStaticData stageEventStaticData) : base(stageEventStaticData)
        {
        }

        public void StageEnterInitialize(Stage stage)
        {
            stage.PC.gameObject.SetActive(true);
            stage.PC.SetImmuneToHit();
        }

        // 포트폴리오 데모 버전이라 연출 관련 리소스가 제거 되었다.
        // 챕터 입장 연출을 제거하고 바로 플레이로 들어간다.
        public override void Begin(Stage stage)
        {
            base.Begin(stage);
        }

        public override void End(Stage stage)
        {
            var stageUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
            var chapter = stage.ChapterStaticData!;
            stageUI.ShowChapterInfo(chapter.ChapterName, chapter.ChapterNumber, chapter.ElementType);
            GameClient.CameraController.ShakeController.StageEnterEffectShake();
            stage.KillAllMonsterAllianceCharacters();

            PlayerCharacter pc = stage.PC;
            pc.UnsetImmuneToHit();
            pc.gameObject.SetActive(true);
            pc.OnEnterredIntoStage(stage);
        }

        public override void Update(Stage stage, int stageEventTickNumber)
        {
        }
    }
}
