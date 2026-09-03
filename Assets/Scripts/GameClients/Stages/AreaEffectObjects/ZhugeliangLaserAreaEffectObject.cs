using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class ZhugeliangLaserAreaEffectObject : AreaEffectObjectBase
    {
        private bool _isAlive;
        public override bool IsAlive => _isAlive;

        private Character _owner;
        private Vector2 _startDirection;
        private float _angle;
        private float _attackDuration;
        private float _distance;
        private float _damage;
        private float _attackTickInterval;
        private GameObject _body;

        private float _accumulatedTime;
        private float _nextHittedClearAt;

        private HashSet<Character> _hittedCharactes = new HashSet<Character>();
        private Sequence _attackReadySequence;
        private Sequence _attackEndSequence;

        private float _rotateAt;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.ZhugeliangLaser);
            _body = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/ZhugeLiang_flag.prefab");
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one * 0.8f;
            _body.GetComponent<SpriteRenderer>().size = new Vector2(13f, 5.652149f);

        }

        public void Initialize(
            Character owner,
            float damage,
            float attackTickInterval,
            Vector2 startPosition,
            Vector2 startDirection,
            float angle,
            float attackDuration,
            float createDuration,
            float waitDuration,
            float endDuration
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _owner = owner;
            _isAlive = true;
            _attackDuration = attackDuration;
            _damage = damage;
            _attackTickInterval = attackTickInterval;
            _accumulatedTime = 0.0f;
            _nextHittedClearAt = 0.0f;

            this.transform.position = startPosition;
            this.transform.right = startDirection;
            _startDirection = startDirection;
            _angle = angle;

            _attackReadySequence = DOTween.Sequence();
            _attackReadySequence.Append(DOTween.To(() => new Vector2(13f, 5.652149f), x => _body.GetComponent<SpriteRenderer>().size = x, new Vector2(30f, 5.652149f), createDuration).SetEase(Ease.OutBack));
            _attackReadySequence.SetAutoKill(true);
            _attackReadySequence.SetRecyclable(false);
            _attackReadySequence.Restart();

            _attackEndSequence = DOTween.Sequence();
            _attackEndSequence.Append(DOTween.To(() => new Vector2(30f, 5.652149f), x => _body.GetComponent<SpriteRenderer>().size = x, new Vector2(13f, 5.652149f), endDuration).SetEase(Ease.OutBack));
            _attackEndSequence.Insert(endDuration / 3f, _body.GetComponentInChildren<SpriteRenderer>().DOFade(0f, endDuration * 0.66f));
            _attackEndSequence.AppendCallback(() =>
            {
                _isAlive = false;
            });

            _attackEndSequence.SetAutoKill(true);
            _attackEndSequence.SetRecyclable(false);
            _attackEndSequence.Pause();

            _distance = 24f;
            _rotateAt = now + createDuration + waitDuration;

        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if (_nextHittedClearAt < now)
            {
                _hittedCharactes.Clear();
                _nextHittedClearAt = now + _attackTickInterval;
            }
            if (_rotateAt < now)
            {
                float t = _accumulatedTime / _attackDuration;

                float angle = Mathf.Lerp(0.0f, _angle, t);
                this.transform.right = Quaternion.AngleAxis(angle, Vector3.forward) * _startDirection;

                Vector3 currentDirection = this.transform.right;

                Vector3 squareCenter = (currentDirection * _distance * 0.5f) + this.transform.position;
                SquareTargetArea attackArea = new SquareTargetArea(squareCenter, new Vector2(_distance, 0.5f), Vector3.SignedAngle(Vector3.right, currentDirection, Vector3.forward));
                CombatSystem.HitOnTargetArea(
                    stage, attackArea, _owner, _damage,
                    CombatSystem.KnockBackType.Pivot, attackArea.Center, 1.0f,
                    _hittedCharactes, _hittedCharactes,
                    hitSoundPrefabPath: string.Empty);

                if (1.0f < t && !_attackEndSequence.IsPlaying())
                {
                    _attackEndSequence.Restart();
                    return;
                }
                _accumulatedTime += deltaTime;
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _body.transform.localPosition = Vector3.zero;
            _body.GetComponentInChildren<SpriteRenderer>().color = Color.white;

            _attackReadySequence.Kill();
        }

    }

}
