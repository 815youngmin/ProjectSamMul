using DG.Tweening;
using Shared.GameDataTypes;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.ItemObjects
{
    public class BombObject : AcquirableItemObject
    {
        private GameObject _cover;
        private Sequence _fadeOut;
        private Sequence _fadeIn;
        private Stage _currentStage;

        public static BombObject Create()
        {
            var gameObject = new GameObject("BombObject");

            var bombObject = gameObject.AddComponent<BombObject>();

            bombObject.AllocateSharedResources(DropItemType.Bomb);
            gameObject.SetActive(true);

            return bombObject;
        }

        public override void AllocateSharedResources(DropItemType dropItemType)
        {
            base.AllocateSharedResources(dropItemType);
            SpriteRenderer[] spriteRenderers = this.GetComponentsInChildren<SpriteRenderer>();
            SpriteRenderer spriteRenderer = null;
            foreach(var target in spriteRenderers)
            {
                if(target.gameObject.name == "BombScreenCover")
                {
                    spriteRenderer = target;
                    _cover = target.gameObject;
                    break;
                }
            }

            Debug.Assert(null != _cover);
            Debug.Assert(null != spriteRenderer);
            _cover.SetActive(false);
            _fadeIn = DOTween.Sequence(spriteRenderer)
                .Append(spriteRenderer.DOFade(0.0f, 0.15f))
                .OnComplete(() =>
                {
                    this.OnComplete();
                });
            _fadeIn.SetAutoKill(false);
            _fadeIn.SetRecyclable(true);
            _fadeIn.Pause();

            _fadeOut = DOTween.Sequence(spriteRenderer)
                .Append(spriteRenderer.DOFade(1.0f, 0.15f))
                .OnComplete(() =>
                {
                    _fadeIn.Restart();
                });
            _fadeOut.SetAutoKill(false);
            _fadeOut.SetRecyclable(true);
            _fadeOut.Pause();

            spriteRenderer.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        }

        public void InitializeBombObject(Vector2 spawnPosition)
        {
            base.InitializeAcquirableItemObject(spawnPosition);
            _cover.SetActive(false);
        }

        public override void PuttingBackToPool()
        {
            _cover.transform.SetParent(this.transform);
            base.PuttingBackToPool();
            _fadeIn.Pause();
            _fadeOut.Pause();
            _currentStage = null;
        }

        public override void OnAcquired(PlayerCharacter owner, Stage stage)
        {
            _currentStage = stage;
            _cover.transform.parent = null; // NOTE: 아이템 획득시 시퀀서 크기의 영향을 받지 않게 하기 위해 parent는 null 처리. 반납시 다시 Parent처리.
            _cover.transform.position = owner.Pos;

            this.RunAcquiredAnimation(owner.Pos, onCompleted: () =>
            {
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/12-Bomb_SFX.prefab", owner.Pos);
                
                _cover.SetActive(true);
                float damage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, 6f); // 폭탄 데미지 비율
                float effectiveRange = 20f * GameClient.CameraController.OrthographicSize / 18f;
                CircularTargetArea areaAttack = new CircularTargetArea(owner.Pos, effectiveRange); // 폭탄 터지는 범위.

                List<Character> characters = new List<Character>();
                stage.FindAliveCharactersInArea(owner.Alliance.ToEnemyAlliance(), areaAttack, characters);
                foreach (Character character in characters)
                {
                    character.Hitted(stage, owner, damage, Vector2.zero, character.CenterPos, hitSoundPrefabPath: string.Empty);
                }

                _fadeOut.Restart();
            });
        }
        public override void OnAcquiredWithSlime(PlayerCharacter owner, Stage stage)
        {
            _currentStage = stage;
            _cover.transform.parent = null; // NOTE: 아이템 획득시 시퀀서 크기의 영향을 받지 않게 하기 위해 parent는 null 처리. 반납시 다시 Parent처리.
            _cover.transform.position = owner.Pos;
            this.RunAcquiredAnimationWithSlime(owner, onCompleted: () =>
            {
                UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/CommonSkills/12-Bomb_SFX.prefab", owner.Pos);

                _cover.SetActive(true);
                float damage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, 6f); // 폭탄 데미지 비율
                float effectiveRange = 20f * GameClient.CameraController.OrthographicSize / 18f;
                CircularTargetArea areaAttack = new CircularTargetArea(owner.Pos, effectiveRange); // 폭탄 터지는 범위.

                List<Character> characters = new List<Character>();
                stage.FindAliveCharactersInArea(owner.Alliance.ToEnemyAlliance(), areaAttack, characters);
                foreach (Character character in characters)
                {
                    character.Hitted(stage, owner, damage, Vector2.zero, character.CenterPos, hitSoundPrefabPath: string.Empty);
                }

                _fadeOut.Restart();
            });
        }

        private void OnComplete()
        {
            Debug.Assert(_currentStage != null);
            _currentStage.RemoveAcquirableItemObject(this);
        }
    }
}
