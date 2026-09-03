using DG.Tweening;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.Loggers;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.Characters.Shields
{
    public class MonsterShield : Shield
    {
        private static readonly string MonsterShieldObjectPath = "Stages/ETCEffects/MonsterShield.prefab";
        private static readonly string MonsterBasicShieldPath = "Stages/ETCEffects/MonsterShield01.png";
        private static readonly string MonsterHitShieldPath = "Stages/ETCEffects/MonsterShield02.png";

        private Sprite _basicShieldSprite;
        private Sprite _hitShieldSprite;
        private GameObject _shieldObject;
        private SpriteRenderer _shieldSpriteRenderer;

        private CharacterAnimationController _ownerAnimationController;

        private static readonly float FadeMaxValue = 1.0f;
        private static readonly float FadeMinValue = 0.3f;

        private static readonly float FadeInDuration = 0.4f;
        private static readonly float FadeOutDuration = 0.7f;
        private static readonly float WaitDuration = 0.1f;

        private float _shieldScale;
        private Sequence _shieldSequence;
        private Sequence _shieldDecreaseSequence;
        private Sequence _shieldDeactivateSequence;

        public MonsterShield(Character owner) : base(owner)
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

            _ownerAnimationController = owner.AnimationController;
            if (_ownerAnimationController == null)
            {
                Log.I.Error($"_ownerAnimationController is null. 게임 로직 이상하게 동작할거임. 수정하세요.");
            }
            
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

            if(shieldAmount > 0)
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

            _shieldSpriteRenderer.flipX = _ownerAnimationController.IsFlippedX;
            _shieldSpriteRenderer.sortingOrder = _ownerAnimationController.SortingOrder + 1;
        }

        public override void Destroy()
        {
            base.Destroy();
            _shieldSequence.Kill();
        }

    }
}
