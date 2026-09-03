using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class ZhangjuieLaserAreaEffectObject : AreaEffectObjectBase
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
        private List<GameObject> _bodies;

        private float _accumulatedTime;
        private float _nextHittedClearAt;

        private HashSet<Character> _hittedCharactes = new HashSet<Character>();
        private Sequence _attackReadySequence;
        private Sequence _attackEndSequence;

        private float _rotateAt;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.ZhangjueLaser);
            _bodies = new List<GameObject>();
            GameObject laser1 = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/ZhangGakLaser.prefab");
            GameObject laser2 = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/ZhangGakLaser.prefab");
            GameObject laser3 = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/ZhangGakLaser.prefab");

            laser1.transform.SetParent(this.transform);
            laser2.transform.SetParent(laser1.transform);
            laser3.transform.SetParent(laser2.transform);

            laser1.transform.localPosition = Vector2.zero;
            laser1.transform.localScale = Vector2.one;
            laser1.GetComponentInChildren<SpriteRenderer>().sortingOrder = 3;

            laser2.transform.localPosition = Vector2.zero;
            laser2.transform.localScale = Vector2.one;
            laser2.GetComponentInChildren<SpriteRenderer>().sortingOrder = 2;

            laser3.transform.localPosition = Vector2.zero;
            laser3.transform.localScale= Vector2.one;
            laser3.GetComponentInChildren<SpriteRenderer>().sortingOrder = 1;

            _bodies.Add(laser1);
            _bodies.Add(laser2);
            _bodies.Add(laser3);
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
            _attackReadySequence.AppendCallback(() =>
            {
                _bodies[0].GetComponentInChildren<TrailRenderer>().enabled = false;
                _bodies[1].GetComponentInChildren<TrailRenderer>().enabled = false;
                _bodies[2].GetComponentInChildren<TrailRenderer>().enabled = false;
            });
            _attackReadySequence.Append(_bodies[0].transform.DOLocalMove(Vector3.right * 7, createDuration / 3f).SetEase(Ease.OutBack));
            _attackReadySequence.Append(_bodies[1].transform.DOLocalMove(Vector3.right * 7, createDuration / 3f).SetEase(Ease.OutBack));
            _attackReadySequence.Append(_bodies[2].transform.DOLocalMove(Vector3.right * 7, createDuration / 3f).SetEase(Ease.OutBack));
            _attackReadySequence.AppendCallback(() =>
            {
                _bodies[0].GetComponentInChildren<TrailRenderer>().enabled = true;
                _bodies[1].GetComponentInChildren<TrailRenderer>().enabled = true;
                _bodies[2].GetComponentInChildren<TrailRenderer>().enabled = true;
            });
            _attackReadySequence.SetAutoKill(true);
            _attackReadySequence.SetRecyclable(false);
            _attackReadySequence.Restart();

            _attackEndSequence = DOTween.Sequence();
            _attackEndSequence.AppendCallback(() =>
            {
                _bodies[0].GetComponentInChildren<TrailRenderer>().enabled = false;
                _bodies[1].GetComponentInChildren<TrailRenderer>().enabled = false;
                _bodies[2].GetComponentInChildren<TrailRenderer>().enabled = false;
            });
            _attackEndSequence.Append(_bodies[2].transform.DOLocalMove(Vector3.zero, endDuration / 3f).SetEase(Ease.OutBack));
            _attackEndSequence.Append(_bodies[1].transform.DOLocalMove(Vector3.zero, endDuration / 3f).SetEase(Ease.OutBack));
            _attackEndSequence.Append(_bodies[0].transform.DOLocalMove(Vector3.zero, endDuration / 3f).SetEase(Ease.OutBack));

            _attackEndSequence.Insert(endDuration / 3f, _bodies[2].GetComponentInChildren<SpriteRenderer>().DOFade(0f, endDuration * 0.66f));
            _attackEndSequence.Join(_bodies[1].GetComponentInChildren<SpriteRenderer>().DOFade(0f, endDuration * 0.66f));
            _attackEndSequence.Join(_bodies[0].GetComponentInChildren<SpriteRenderer>().DOFade(0f, endDuration * 0.66f));

            _attackEndSequence.AppendCallback(() =>
            {
                _isAlive = false;
            });

            _attackEndSequence.SetAutoKill(true);
            _attackEndSequence.SetRecyclable(false);
            _attackEndSequence.Pause();

            _distance = 25f;
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

            if(_rotateAt < now)
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

            for(int i = 0; i < _bodies.Count; i++)
            {
                _bodies[i].transform.localPosition = Vector3.zero;
                _bodies[i].GetComponentInChildren<SpriteRenderer>().color = Color.white;
            }
            _attackReadySequence.Kill();
        }

    }

}
