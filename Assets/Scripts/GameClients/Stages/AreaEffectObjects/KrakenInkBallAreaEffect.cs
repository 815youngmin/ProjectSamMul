using DG.Tweening;
using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class KrakenInkBallAreaEffect : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _hitAt + _explosionDuration;
        private Monster _owner;
        private float _droptime;        //독구슬 떨어지는 시간
        private float _hitAt;   //독구슬 도착 시간, 데지미 입힐 시간
        private float _damage;          //데미지
        private float _radius;          //공격 범위
        private float _explosionDuration;  //페이드아웃 연출 시간

        private Sequence _dropPoison;   //독구슬 연출 시퀀스
        private SpriteRenderer _inkBallSpriteRenderer;
        private SpriteAnimationHandler _inkBallExplosion;
        private bool _isArrived;

        private readonly float radiusToScale = 0.4f; //radius 2.5f 기준 scale 1이어야됨
        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.KrakenInkBall);

            var poisonSprite = ResourcePool.Instance.InstantiateFromResource("Stages/Characters/SpineSkeletons/Viking/Viking_BossKraken/KrakenInkBall.prefab");
            _inkBallSpriteRenderer = poisonSprite.GetComponent<SpriteRenderer>();
            _inkBallSpriteRenderer.gameObject.SetActive(false);
            _inkBallSpriteRenderer.transform.SetParent(this.transform);

            _inkBallExplosion = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>("Stages/Characters/SpineSkeletons/Viking/Viking_BossKraken/KrakenInkBallExplosion.prefab");
            _inkBallExplosion.InitializeOnly();
            _inkBallExplosion.gameObject.SetActive(false);
            _inkBallExplosion.transform.localPosition = Vector3.zero;
            _inkBallExplosion.transform.localScale = Vector3.one;
            _inkBallExplosion.gameObject.transform.SetParent(this.transform);
        }

        public void Initialize(
            Monster owner,
            Vector2 position,
            float dropTime,
            float damage,
            float radius
            )
        {
            float now = Time.time;
            _owner = owner;
            this.transform.position = position;
            _hitAt = dropTime + now;
            _droptime = dropTime;
            _radius = radius;
            _damage = damage;
            _explosionDuration = _inkBallExplosion.AnimationDuration;

            _inkBallSpriteRenderer.gameObject.SetActive(true);
            _inkBallExplosion.gameObject.SetActive(false);

            Vector3 localScale = Vector3.one * radius * radiusToScale;
            _inkBallSpriteRenderer.transform.localScale = localScale;
            _inkBallExplosion.transform.localScale = localScale;

            _inkBallSpriteRenderer.gameObject.transform.localPosition = new Vector3(0, 30f, 0);
            this.UpdateSortingOrder();

            _inkBallExplosion.SpriteRenderer.color = Color.white;
            this.AllocateDropPoisonSequenceAnimation();
            _dropPoison.Restart();
        }



        //구슬 연출 시간은 외부에서 받아와 처리하기 때문에 초기화 단계에서 할당해준다
        private void AllocateDropPoisonSequenceAnimation()
        {
            _dropPoison = DOTween.Sequence();

            _dropPoison.Append(_inkBallSpriteRenderer.DOFade(0.0f, 0.0f));
            _dropPoison.Append(_inkBallSpriteRenderer.DOFade(1.0f, _droptime));
            _dropPoison.Join(_inkBallSpriteRenderer.gameObject.transform.DOLocalMove(Vector3.zero, _droptime).SetEase(Ease.InQuart));
            _dropPoison.SetRecyclable(true);
            _dropPoison.SetAutoKill(false);
            _dropPoison.Pause();
        }
        private void UpdateSortingOrder()
        {
            _inkBallExplosion.SpriteRenderer.sortingOrder
                = (int)(transform.position.y * -100.0f) - ((int)transform.position.x % 80);
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _isArrived = false;
            _owner = null;
            _inkBallSpriteRenderer.gameObject.SetActive(false);
            _inkBallExplosion.gameObject.SetActive(false);

            _dropPoison.Pause();
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if (_hitAt < now)
            {
                var targetArea = new CircularTargetArea(this.transform.position, _radius);
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, Vector2.zero, knockBackPower: 0f, null, null, null);
                _hitAt = float.MaxValue;

                _inkBallSpriteRenderer.gameObject.SetActive(false);
                _inkBallExplosion.gameObject.SetActive(true);
                _inkBallExplosion.InitializeAndPlay();
            }
        }
    }
}

