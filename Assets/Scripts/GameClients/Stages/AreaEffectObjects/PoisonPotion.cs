using DG.Tweening;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class PoisonPotion : AreaEffectObjectBase
    {
        private const string BodyPrefabPath = "Stages/Projectiles/PoisonPotionRadius0_4.prefab";
        private static readonly float ThrowDurationToDistance = 0.04f;
        private static readonly float AttackPeriod = 0.25f;
        public override bool IsAlive => !_isHit;

        private Stage _stage;
        private Character _owner;
        private float _damage;
        private float _attackRadius;
        private float _attackDuration;
        private Vector2 _startPosition;
        private Vector2 _endPosition;

        private bool _isHit;
        private GameObject _body;
       

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.PoisonPotionObject);
            _body = ResourcePool.Instance.InstantiateFromResource(BodyPrefabPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;
        }

        public void Initialize(
            Stage stage,
            Monster owner,
            Vector2 startPosition,
            Vector2 endPosition,
            float waitTime,
            float attackRadius,
            float damage,
            float attackDuration
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;

            _stage = stage;
            _owner = owner;
            _attackRadius = attackRadius;
            _damage = damage;
            _attackDuration = attackDuration;
            _isHit = false;

            this.transform.position = startPosition;
            this.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));    //똑바로 날라오는게 어색하다, 자연스럽게 돌려놓고 던지자
            _endPosition = endPosition;
            _body.gameObject.SetActive(false);

            float distance = Vector2.Distance(startPosition, endPosition);

            var sequence = DOTween.Sequence();
            sequence.AppendInterval(waitTime);
            sequence.AppendCallback(() => { _body.gameObject.SetActive(true); });
            sequence.Append(this.transform.DOJump(endPosition, jumpPower: 5f, 1, distance * ThrowDurationToDistance).SetEase(Ease.Linear));
            sequence.Join(this.transform.DOLocalRotate(new Vector3(0,0,540f), distance * ThrowDurationToDistance, RotateMode.LocalAxisAdd));
            sequence.AppendCallback(() =>
            {
                _isHit = true;
                _stage.CreatePoisonousAreaEffect(_owner, _endPosition, _attackDuration, AttackPeriod, _damage, _attackRadius);
            });

            stage.AreaIndicators.CreateBlinkCircularAttackRangeIndicator(endPosition, attackRadius, waitTime);
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _body.gameObject.SetActive(true);
        }

    }

}
