using Shared.GameDataTypes;
using Shared.Localizers;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.ResourcePools;
using SamMul.Scenes;
using SamMul.UIs.Stages.HUDs;
using SamMul.UIs.Stages.Popups;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.ItemObjects
{
    public class SkillBoxObject : AcquirableItemObject
    {
        private const string effectPath = "Stages/ETCEffects/fx_skillrouletteBox.prefab";
        protected GameObject _effect;

        private const string NAVIGATION_ARROW_PREFAB_PATH = "Stage/UIs/SkillBoxPopup/SkillBoxNavigationArrow.prefab";
        protected NavigationArrow _navigationArrow;

        public static SkillBoxObject Create()
        {
            var gameObject = new GameObject("SkillBoxObject");

            var skillBox = gameObject.AddComponent<SkillBoxObject>();

            skillBox.AllocateSharedResources(DropItemType.SkillBox);
            gameObject.SetActive(true);

            return skillBox;
        }

        public override void AllocateSharedResources(DropItemType dropItemType)
        {
            base.AllocateSharedResources(dropItemType);
            Body.sortingLayerID = SortingLayer.NameToID("Object");
            _effect = ResourcePool.Instance.InstantiateFromResource(effectPath);
            _effect.transform.SetParent(transform);
            this.AllocateShadowComponent();
        }

        private void AllocateShadowComponent()
        {
            var shadowObject = new GameObject("Shadow");
            shadowObject.transform.SetParent(this.transform, worldPositionStays: false);

            var shadow = shadowObject.AddComponent<SpriteRenderer>();

            shadow.sprite = ResourcePool.Instance.LoadResource<Sprite>("Stage/Common/ItemShadow.png");
            shadow.color = new Color(shadow.color.r, shadow.color.g, shadow.color.b, 0.50f);
            shadow.sortingLayerID = SortingLayer.NameToID("LowShadow");
            shadow.drawMode = SpriteDrawMode.Simple;
            shadow.transform.localPosition = new Vector3(0.0f, -0.5f, 0.0f);
            shadow.transform.localScale = new Vector3(4.2f, 1.4f, 0.0f);
        }

        public void InitializeSkillBoxObject(Stage stage, Vector2 spawnPosition)
        {
            base.InitializeAcquirableItemObject(spawnPosition);
            this.transform.localScale = new Vector2(1.0f, 1.0f);
            Body.sortingOrder = (int)(transform.position.y * -100.0f) - 10;
            _effect.SetActive(true);

            var stageSceneUIRoot = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
            _navigationArrow = stageSceneUIRoot.AddNavigationArrow(stage, this.transform, showAlways: false, NAVIGATION_ARROW_PREFAB_PATH, string.Empty);
        }

        public override void OnAcquired(PlayerCharacter owner, Stage stage)
        {
            _effect.SetActive(false);
            _navigationArrow.RemoveNavigationArrow();
            _navigationArrow = null;

            this.RunAcquiredAnimation(owner.Pos, onCompleted: () =>
            {
                stage.RemoveAcquirableItemObject(this);

                var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
                Debug.Assert(stageSceneUI != null);

                if (stageSceneUI.IsSkillBoxPopupOpened)
                {
                    return;
                }

                int selectSkillCount = 0;
                float percent = UnityEngine.Random.Range(0, 1f);
                if (percent <= 0.30f)
                {
                    selectSkillCount = 1;
                }
                else if (0.30f < percent && percent <= 0.80f)
                {
                    selectSkillCount = 3;
                }
                else if (0.8f < percent)
                {
                    selectSkillCount = 5;
                }

                stageSceneUI.AddSkillBoxPopup(owner, selectSkillCount);

                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/8-AcquiredSkillBox_SFX.prefab", owner.Pos);
            });
        }

        public override void OnAcquiredWithSlime(PlayerCharacter owner, Stage stage)
        {
            _effect.SetActive(false);
            _navigationArrow.RemoveNavigationArrow();
            _navigationArrow = null;
            this.RunAcquiredAnimationWithSlime(owner, onCompleted: () =>
            {
                stage.RemoveAcquirableItemObject(this);

                var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
                Debug.Assert(stageSceneUI != null);

                if (stageSceneUI.IsSkillBoxPopupOpened)
                {
                    return;
                }

                int selectSkillCount = 0;
                float percent = UnityEngine.Random.Range(0, 1f);
                if (percent <= 0.30f)
                {
                    selectSkillCount = 1;
                }
                else if (0.30f < percent && percent <= 0.80f)
                {
                    selectSkillCount = 3;
                }
                else if (0.8f < percent)
                {
                    selectSkillCount = 5;
                }

                stageSceneUI.AddSkillBoxPopup(owner, selectSkillCount);

                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/8-AcquiredSkillBox_SFX.prefab", owner.Pos);
            });
        }
    }
}
