using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class LokiSpearAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => _currentMoveCount < _moveCount;

        private GameObject _body;

        private readonly string _lokiSpearPath = "Stages/AreaEffects/Loki_Spear.prefab";

        private Sequence _spearFadeOutSequence;

        private float _damage;
        private float _speed;
        private float _moveDuration;
        private float _waitDuration;
        private int _moveCount;
        private int _currentMoveCount;

        private float _movingAt;
        private float _waitingAt;
        private Vector2 _targetDirection;

        private Character _target;
        private Character _owner;
        private HashSet<Character> _hittedCharactes = new HashSet<Character>();

        private bool _canHit;
        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.LokiSpear);

            _body = ResourcePool.Instance.InstantiateFromResource(_lokiSpearPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one * 1f;    
        }

        public void Initialize(
            Character target,
            Character owner,
            Vector3 attackPosition,
            float damage,
            float speed,
            float moveDuration,
            float waitDuration,
            int moveCount)
        {
            base.InitializeAreaObject(owner.Alliance);
            _target = target;
            _owner = owner;
            _damage = damage;
            _speed = speed;
            _moveDuration = moveDuration;
            _waitDuration = waitDuration;
            _moveCount = moveCount;

            this.transform.position = attackPosition;
            _currentMoveCount = 0;

            _targetDirection = _target.CenterPos - (Vector2)this.transform.position;
            _targetDirection.Normalize();
            this.transform.right = _targetDirection;

            //마지막 대기시간에 페이드아웃 하면서 사라진다.
            _spearFadeOutSequence = DOTween.Sequence();
            _spearFadeOutSequence.Append(_body.GetComponentInChildren<SpriteRenderer>().DOFade(0f, _waitDuration));
            _spearFadeOutSequence.Pause();
            _body.GetComponentInChildren<SpriteRenderer>().color = Color.white;

            float now = Time.time;
            _movingAt = now + _moveDuration;
            _waitingAt = _movingAt + _waitDuration;

            _canHit = true;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            //이동 가능한 시간
            if (now < _movingAt)
            {
                this.MoveToCurrentPosition(deltaTime);
            }

            if (_canHit)
            {
                float angle = Vector3.SignedAngle(Vector3.right, _targetDirection, Vector3.forward);
                SquareTargetArea squareTargetArea = new SquareTargetArea(this.transform.position, new Vector2(6f, 0.5f), angle);
                CombatSystem.HitOnTargetArea(stage, squareTargetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, Vector2.zero, knockBackPower: 0f, _hittedCharactes, _hittedCharactes, null);
            }

            //멈춰있어야 한다.
            if (_movingAt <= now && now < _waitingAt)
            {
                if (_currentMoveCount + 1 >= _moveCount && !_spearFadeOutSequence.IsPlaying())
                {
                    //마지막 대기시간에는 공격처리를 끈다(사라지는 처리에 공격 받으니 어색함)
                    _canHit = false;
                    _spearFadeOutSequence.Restart();
                }
                else if (_currentMoveCount + 1 < _moveCount)
                {
                    Vector2 newDirection = _target.CenterPos - (Vector2)this.transform.position;
                    newDirection.Normalize();
                    _targetDirection = Vector2.Lerp(_targetDirection, newDirection, 10f * deltaTime);
                    this.transform.right = _targetDirection;
                }
                return;
            }

            //대기 패턴까지 끝나면 이동시간, 대기시간, 이동횟수를 업데이트 해준다.
            if (now >= _waitingAt)
            {
                _movingAt = now + _moveDuration;
                _waitingAt = now + _moveDuration + _waitDuration;
                _currentMoveCount++;
                _hittedCharactes.Clear();
            }

        }
        private void MoveToCurrentPosition(float deltaTime)
        {
            Vector2 deltaMovement = _targetDirection * deltaTime * _speed;
            this.transform.Translate(deltaMovement.x, deltaMovement.y, 0f, Space.World);
        }


        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _canHit = true;
            _hittedCharactes.Clear();
            _spearFadeOutSequence.Kill();
            _spearFadeOutSequence = null;
        }
    }

}

