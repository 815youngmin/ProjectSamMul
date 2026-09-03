#nullable enable
using DG.Tweening;
using Shared.GameDataTypes;
using System;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.PCs;

namespace Z.GameClients.Stages.ItemObjects
{
    public abstract class AcquirableItemObject : ItemObjectBase
    {
        protected static readonly Vector2 BODY_INITIAL_LOCAL_POSITION = Vector2.zero;

        private Sequence? _spawningAnimation;
        private Sequence? _floatingAnimation;
        private Sequence? _acquiredAnimation;



        public override void AllocateSharedResources(DropItemType dropItemType)
        {
            base.AllocateSharedResources(dropItemType);

            gameObject.layer = LayerMask.NameToLayer("AcquirableItem");
            Body.transform.localPosition = BODY_INITIAL_LOCAL_POSITION;

            this.AllocateAnimations();
        }

        private void AllocateAnimations()
        {
            _spawningAnimation = DOTween.Sequence()
                .Append(Body.transform.DOLocalMoveY(BODY_INITIAL_LOCAL_POSITION.y + 2.5f, 0.21f).SetEase(Ease.OutCubic))
                .AppendInterval(0.013f)
                .Append(Body.transform.DOLocalMoveY(BODY_INITIAL_LOCAL_POSITION.y, 0.11f).SetEase(Ease.InSine))
                .AppendInterval(0.009f)
                .Append(Body.transform.DOLocalMoveY(BODY_INITIAL_LOCAL_POSITION.y + 0.9f, 0.09f).SetEase(Ease.OutCubic))
                .AppendInterval(0.010f)
                .Append(Body.transform.DOLocalMoveY(BODY_INITIAL_LOCAL_POSITION.y + 0.1f, 0.07f).SetEase(Ease.InSine))
                .Append(Body.transform.DOLocalMoveY(BODY_INITIAL_LOCAL_POSITION.y, 0.66f))
                .AppendInterval(0.1f)
                .OnComplete(() => _floatingAnimation.Restart())
                .SetAutoKill(false)
                .Pause();

            // 경험치는 흔들리지 않는다.
            _floatingAnimation = IsExpObjectType(DropItemType)
                ? null
                : DOTween.Sequence()
                    .Append(Body.transform.DOLocalMoveY(BODY_INITIAL_LOCAL_POSITION.y + 0.2f, 0.8f))
                    .Append(Body.transform.DOLocalMoveY(BODY_INITIAL_LOCAL_POSITION.y, 0.8f))
                    .SetLoops(-1)
                    .SetAutoKill(false)
                    .Pause();
        }

        protected void InitializeAcquirableItemObject(Vector2 spawnPosition)
        {
            base.InitializeBase(spawnPosition);

            Body.transform.localPosition = BODY_INITIAL_LOCAL_POSITION;
            Body.sortingOrder = (int)(transform.position.y * -100.0f);

            _spawningAnimation.Restart();
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();

            this.StopAllAnimations();

            transform.localScale = Vector3.one;
            Body.transform.localPosition = BODY_INITIAL_LOCAL_POSITION;
            Body.sortingLayerID = SortingLayer.NameToID("Item");

            _acquiredAnimation.Kill();
            _acquiredAnimation = null;
        }

        public abstract void OnAcquired(PlayerCharacter owner, Stage stage);

        //운디네 슬라임으로 획득시 처리
        public abstract void OnAcquiredWithSlime(PlayerCharacter owner, Stage stage);
       

        //아이템 획득 기본 연출
        protected void RunAcquiredAnimation(Vector3 targetPosition, Action onCompleted)
        {
            this.StopAllAnimations();

            _acquiredAnimation = DOTween.Sequence()
                .Append(Body.transform.DOLocalMoveY(BODY_INITIAL_LOCAL_POSITION.y + 2.5f, 0.1f).SetEase(Ease.OutSine))
                .AppendInterval(0.035f)
                .Append(transform.DOMove(targetPosition, 0.2f).SetEase(Ease.InCubic))
                .Join(transform.DOScale(0.1f, 0.2f).SetEase(Ease.InCubic))
                .OnComplete(() => onCompleted.Invoke());
        }

        //아이템 운디네 슬라임으로 획득시 연출
        protected void RunAcquiredAnimationWithSlime(Character targetCharacter, Action onCompleted)
        {
            this.StopAllAnimations();

            Vector3 startPos = transform.position;
            float distance =  Vector3.Distance(startPos, targetCharacter.Pos);
            float moveDuration = Mathf.Clamp(distance / 15f, 0.2f, 0.6f);

            Body.sortingLayerID = SortingLayer.NameToID("HighParticle");

            _acquiredAnimation = DOTween.Sequence()
                .Append(Body.transform.DOLocalMoveY(BODY_INITIAL_LOCAL_POSITION.y + 0.8f, 0.2f).SetEase(Ease.OutQuint))
                .Insert(0f,   Body.DOColor(new Color(1f, 1f, 1f, 0f), 0.08f))
                .Insert(0.08f,Body.DOColor(new Color(1f, 1f, 1f, 1f), 0.08f))
                .Insert(0.16f,Body.DOColor(new Color(1f, 1f, 1f, 0f), 0.08f))
                .Insert(0.24f,Body.DOColor(new Color(1f, 1f, 1f, 1f), 0.08f))
                .Append(DOVirtual.Float(0, 1, moveDuration, t =>
                {
                    Vector3 currentTargetPos = targetCharacter.Pos;
                    transform.position = Vector3.Lerp(startPos, targetCharacter.Pos, t);
                }).SetEase(Ease.InCubic))
                .Join(transform.DOScale(0.8f, moveDuration).SetEase(Ease.InCubic))
                .OnComplete(() => onCompleted.Invoke());
        }

        private void StopAllAnimations()
        {
            _spawningAnimation.Pause();
            _floatingAnimation.Pause();
            _acquiredAnimation.Pause();
        }

        private void OnDestroy()
        {
            _spawningAnimation.Kill();
            _floatingAnimation.Kill();
            _acquiredAnimation.Kill();

            _spawningAnimation = null;
            _floatingAnimation = null;
            _acquiredAnimation = null;
        }

        protected static bool IsExpObjectType(DropItemType itemType)
            => itemType is DropItemType.ExpS or DropItemType.ExpM or DropItemType.ExpL or DropItemType.ExpXL;
    }
}
