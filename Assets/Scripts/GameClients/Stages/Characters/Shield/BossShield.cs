using DG.Tweening;
using SamMul.Animations.Placeholder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.Characters.Shields
{
    public class BossShield : Shield
    {
        private static readonly string MonsterShieldObjectPath = "Stages/ETCEffects/MonsterShield.prefab";
        private static readonly string MonsterBasicShieldPath = "Stages/ETCEffects/MonsterShield01.png";
        private static readonly string MonsterHitShieldPath = "Stages/ETCEffects/MonsterShield02.png";

        private Sprite _basicShieldSprite;
        private Sprite _hitShieldSprite;
        private GameObject _shieldObject;
        private SpriteRenderer _shieldSpriteRenderer;
        private SkeletonAnimation _ownerBodySkeletonAnimation;
        private MeshRenderer _ownerBodyMeshRenderer;

        private static readonly float FadeMaxValue = 1.0f;
        private static readonly float FadeMinValue = 0.3f;

        private static readonly float FadeInDuration = 0.4f;
        private static readonly float FadeOutDuration = 0.7f;
        private static readonly float WaitDuration = 0.1f;

        private float _shieldScale;
        private Sequence _shieldSequence;
        private Sequence _shieldDecreaseSequence;
        private Sequence _shieldDeactivateSequence;

        public BossShield(Character owner) : base(owner)
        {
            _shieldObject = ResourcePool.Instance.InstantiateFromResource(MonsterShieldObjectPath);
            _shieldSpriteRenderer = _shieldObject.GetComponent<SpriteRenderer>();

            _basicShieldSprite = ResourcePool.Instance.LoadResource<Sprite>(MonsterBasicShieldPath);
            _hitShieldSprite = ResourcePool.Instance.LoadResource<Sprite>(MonsterHitShieldPath);

            _shieldSpriteRenderer.sprite = _basicShieldSprite;
            _shieldObject.transform.SetParent(_shieldToggleObject.transform, false);
            _shieldObject.transform.localPosition = owner.CenterPos - owner.Pos;
            _shieldScale = owner.GetHitBoxSize().x * 0.4f;
            _shieldObject.transform.localScale = Vector3.one * _shieldScale;
            _shieldObject.transform.localRotation = Quaternion.identity;

            _ownerBodySkeletonAnimation = owner.Body.GetComponent<SkeletonAnimation>();
            _ownerBodyMeshRenderer = owner.Body.GetComponent<MeshRenderer>();

            //기본적인 페이드인 페이드아웃 연출
            _shieldSequence = DOTween.Sequence(this);
            _shieldSequence.Append(_shieldSpriteRenderer.DOFade(FadeMinValue, FadeOutDuration).SetEase(Ease.InQuad));
            _shieldSequence.Append(_shieldSpriteRenderer.DOFade(FadeMaxValue, FadeInDuration).SetEase(Ease.OutQuad));
            _shieldSequence.AppendInterval(WaitDuration);
            _shieldSequence.SetLoops(-1);
            _shieldSequence.SetAutoKill(false);
            _shieldSequence.Pause();

            //실드 감소시 나오는 연출
            _shieldDecreaseSequence = DOTween.Sequence(this);
            _shieldDecreaseSequence.AppendCallback(() =>
            {
                _shieldSpriteRenderer.sprite = _hitShieldSprite;
            });
            _shieldDecreaseSequence.Append(_shieldObject.transform.DOScale(_shieldScale * 1.2f, 0.1f));
            _shieldDecreaseSequence.Append(_shieldObject.transform.DOScale(_shieldScale, 0.1f));
            _shieldDecreaseSequence.AppendCallback(() =>
            {
                _shieldSpriteRenderer.sprite = _basicShieldSprite;
            });
            _shieldDecreaseSequence.SetAutoKill(false);
            _shieldDecreaseSequence.Pause();

            //실드 제거될때 나오는 연출
            _shieldDeactivateSequence = DOTween.Sequence(this);
            _shieldDeactivateSequence.AppendCallback(() =>
            {
                _shieldSpriteRenderer.sprite = _hitShieldSprite;
            });
            _shieldDeactivateSequence.Append(_shieldObject.transform.DOScale(_shieldScale * 1.2f, 0.1f));
            _shieldDeactivateSequence.Append(_shieldObject.transform.DOScale(_shieldScale, 0.1f));
            _shieldDeactivateSequence.Join(_shieldSpriteRenderer.DOFade(0, 0.1f).SetEase(Ease.InQuad));
            _shieldDeactivateSequence.AppendCallback(() =>
            {
                _shieldToggleObject.SetActive(false);
            });
            _shieldDeactivateSequence.SetAutoKill(false);
            _shieldDeactivateSequence.Pause();
        }

        protected override void ActivateShiled()
        {
            base.ActivateShiled();

            _shieldSpriteRenderer.color = Color.white;
            _shieldSpriteRenderer.sprite = _basicShieldSprite;
            _shieldSequence.Restart();
        }

        protected override void DeactivateShiled()
        {
            _shieldSequence.Pause();
            _shieldDecreaseSequence.Pause();

            //아래 시퀀스에서 _shieldToggleObject꺼주는 역할까지 같이 한다.
            _shieldDeactivateSequence.Restart();
        }

        protected override void DecreaseShield(int shieldAmount)
        {
            base.DecreaseShield(shieldAmount);

            if (shieldAmount > 0)
            {
                _shieldDecreaseSequence.Restart();
            }

        }

        public override void UpdateLogic()
        {
            if (!IsActive)
            {
                return;
            }
            _shieldSpriteRenderer.flipX = _ownerBodySkeletonAnimation.skeleton.ScaleX > 1.0f;
            _shieldSpriteRenderer.sortingOrder =  _ownerBodyMeshRenderer.sortingOrder + 1;
        }

        public override void Destroy()
        {
            base.Destroy();
            _shieldSequence.Kill();
        }

    }
}
