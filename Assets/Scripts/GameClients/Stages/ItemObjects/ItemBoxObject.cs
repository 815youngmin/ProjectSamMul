using Shared.GameDataTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.Stats;
using Z.ResourcePools;
using Random = UnityEngine.Random;

namespace Z.GameClients.Stages.ItemObjects
{
    public class ItemBoxObject : BreakableItemObject
    {
        protected override Vector2 BodySpawnLocalOffset => new Vector2(0.0f, 0.6f);

        // 아이템박스가 깨질 때 드롭할 아이템 후보
        private IReadOnlyDictionary<DropItemType, int> _dropItemCandidates;
        private Animator _animator;
        private Stage _currentStage;

        public static ItemBoxObject Create()
        {
            var gameObject = new GameObject("ItemBoxObject");

            var expObject = gameObject.AddComponent<ItemBoxObject>();
            expObject.AllocateSharedResources(DropItemType.ItemBox);
            gameObject.SetActive(true);

            return expObject;
        }

        public override void AllocateSharedResources(DropItemType dropItemType)
        {
            base.AllocateSharedResources(dropItemType);
            Body.sortingLayerID = SortingLayer.NameToID("Object");

            _animator = Body.GetComponent<Animator>();
            _animator.Play("Idle");

            this.AllocateShadowComponent();
        }

        private void AllocateShadowComponent()
        {
            var shadowObject = new GameObject("ItemboxShadow");
            shadowObject.transform.SetParent(this.transform, worldPositionStays: false);

            var shadow = shadowObject.AddComponent<SpriteRenderer>();

            shadow.sprite = ResourcePool.Instance.LoadResource<Sprite>("Stages/Items/ItemShadow.png");
            shadow.color = new Color(shadow.color.r, shadow.color.g, shadow.color.b, 0.46f);
            shadow.sortingLayerID = SortingLayer.NameToID("LowShadow");
            shadow.drawMode = SpriteDrawMode.Simple;
            shadow.transform.localPosition = new Vector3(0.0f, -0.4f);
            shadow.transform.localScale = new Vector2(2.1f, 1.2f);
        }

        public void InitializeItemBoxObject(IReadOnlyDictionary<DropItemType, int> dropItemCandidates, Vector2 spawnPosition)
        {
            base.InitializeBreakableItemObject(spawnPosition);
            this.gameObject.transform.localScale = Vector3.one;
            _itemCollider.enabled = true;

            Body.sortingOrder = (int)((transform.position.y) * -100.0f);

            Debug.Assert(dropItemCandidates.Count > 0);
            _dropItemCandidates = dropItemCandidates;
        }

        public override void UpdateLogic(Stage stage, float now)
        {
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

        public override void OnBroken(Character attacker, float damage, Stage stage)
        {
            if (!this.gameObject.activeSelf)
            {
                return;
            }

            if (!_itemCollider.enabled)
            {
                return;
            }

            _itemCollider.enabled = false;
            _currentStage = stage;
            _animator.Play("Broken");

            var itemToDrop = this.SelectItemToDrop(stage);
            Debug.Log(itemToDrop.ToString());
            this.SpawnItem(itemToDrop, stage);
            StartCoroutine(checkCompleteAnimationStatus());

            stage.PC.CustomParameters.IncreaseParameterValue(CustomParameterType.BrokenItemBoxes, 1.0f);
        }

        IEnumerator checkCompleteAnimationStatus()
        {
            while (true)
            {
                yield return null;

                AnimatorStateInfo aniamtionInfo = _animator.GetCurrentAnimatorStateInfo(0);
                if (aniamtionInfo.IsName("Broken") && aniamtionInfo.normalizedTime >= 1.0f)
                {
                    _currentStage.RemoveBreakableItemObject(this);
                    _currentStage = null;
                    break;
                }
            }
        }

        private DropItemType SelectItemToDrop(Stage stage)
        {
            DropItemType dropItemType = DropItemType.Fence;//펜스는 드랍될 수 있는 아이템이 아니므로 임시로 씁니다.
            int count = _dropItemCandidates.Count;
            int randomValue = Random.Range(0, 10000);// 0~9999 까지
            foreach (var item in _dropItemCandidates)
            {
                randomValue -= item.Value;
                if (randomValue < 0)
                {
                    dropItemType = item.Key;
                    break;
                }
            }

            if (!_dropItemCandidates.ContainsKey(dropItemType))
            {
                throw new NotImplementedException($"{nameof(SelectItemToDrop)} 함수에서 Drop 아이템이 선택이 되지 않았습니다. 확률 확인해주세요.");
            }

            if (dropItemType == DropItemType.Gold && !stage.HasItemBoxDropGolds())
            {
                dropItemType = DropItemType.HpResorative;
            }

            return dropItemType;
        }

        private void SpawnItem(DropItemType dropItem, Stage stage)
        {
            switch (dropItem)
            {
                case DropItemType.HpResorative:
                    stage.CreateHpResorativeObject(this.transform.position);
                    break;
                case DropItemType.Bomb:
                    stage.CreateBombObject(this.transform.position);
                    break;
                case DropItemType.Gold:
                    long randomAmount = Random.Range(1, 50);
                    long actualAmount = stage.TakeItemBoxDropGolds(randomAmount);
                    if (actualAmount > 0)
                    {
                        stage.CreateGoldObject(actualAmount, this.transform.position);
                    }
                    else
                    {
                        Debug.LogError($"스테이지에 더 이상 드롭가능한 골드가 없어서, {nameof(SpawnItem)} {dropItem} 요청이 실패했습니다. 아무일도 일어나지 않습니다.");
                    }
                    break;
                case DropItemType.ExpMagnet:
                    stage.CreateExpMagnetObject(this.transform.position);
                    break;
                case DropItemType.SkillBox:
                    stage.CreateSkillBoxObject(this.transform.position);
                    break;
                default:
                    Debug.LogError($"{dropItem}은 ItemBox에서 스폰할 수 없음");
                    break;
            }
        }
    }
}
