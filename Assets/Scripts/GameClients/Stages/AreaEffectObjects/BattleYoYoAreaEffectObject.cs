using System;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class BattleYoYoAreaEffectObject : AreaEffectObjectBase
    {
        enum State
        {
            MoveToTarget,
            Waiting,
            MoveBackToOwner,
            End,
        }

        private State _currentState;
        public override bool IsAlive => _currentState != State.End;

        private PlayerCharacter _owner;
        private float _damage;

        private GameObject _body;
        private GameObject _startLineObject;
        private GameObject _backLineObject;

        private SpriteRenderer _bodySpriteRenderer;
        private LineRenderer _startLineRenderer;
        private LineRenderer _backLineRenderer;
        private LineRenderer _currentLineRenderer;

        private int _lineCount = 15;

        private float _waitingDuration;
        private float _moveSpeed;
        private float _acceleration;
        private float _attackRadius;
        private float _knockbackPower;
        private float _deployYoyoDuration;
        private float _deployYoyoDamage;
        private float _deployYoyoKnobackPower;

        private Vector2 _targetPosition;
        private Vector2 _startPosition;
        // 타겟으로 향해 이동할 방향벡터 (Normalized)
        private Vector2 _moveDirection;
        private float _leftMoveDistance;

        private float _waitTime;
        private static readonly float HITTED_CHARACTER_CLEAR_PERIOD = 0.35f;
        private float _hittedCharacterClearAt;

        private HashSet<Character> _hittedCharacters;
        private string _hitSoundPrefabPath;

        public void AllocateSharedResources(AreaEffectType areaEffectType)
        {
            Debug.Assert(areaEffectType == AreaEffectType.BattleYoYo_Default || areaEffectType == AreaEffectType.BattleYoYo_EggKim);
            base.AllocateSharedResourcesForBase(areaEffectType);

            var bodyPrefabPath = areaEffectType switch
            {
                AreaEffectType.BattleYoYo_Default => "Stages/AreaEffects/Yoyo_w_0.prefab",
                AreaEffectType.BattleYoYo_EggKim => "Stages/AreaEffects/Yoyo_EggKim.prefab",
                _ => throw new NotSupportedException($"AreaEffectType {areaEffectType}이 배틀 요요가 아닙니다."),
            };
            _body = ResourcePool.Instance.InstantiateFromResource(bodyPrefabPath);
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one * 0.5f;
            _bodySpriteRenderer = _body.GetComponent<SpriteRenderer>();


            _startLineObject = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/Yoyo_StartLine.prefab");
            _startLineObject.transform.SetParent(this.transform);
            _startLineObject.transform.localPosition = Vector2.zero;
            _startLineObject.transform.localScale = Vector2.one;
            _startLineRenderer = _startLineObject.GetComponent<LineRenderer>();


            _backLineObject = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/Yoyo_BackLine.prefab");
            _backLineObject.transform.SetParent(this.transform);
            _backLineObject.transform.localPosition = Vector2.zero;
            _backLineObject.transform.localScale = Vector2.one;
            _backLineRenderer = _backLineObject.GetComponent<LineRenderer>();

            _startLineRenderer.positionCount = _lineCount;
            _backLineRenderer.positionCount = _lineCount;

            _startLineRenderer.gameObject.SetActive(false);
            _backLineRenderer.gameObject.SetActive(false);

            _hittedCharacters = new HashSet<Character>();

        }

        public void Initialize(
            PlayerCharacter owner,
            Vector2 targetPosition,
            float moveDistance,
            float waitingDuration,
            float acceleration,
            float moveSpeed,
            float attackRadius,
            float knockbackPower,
            float damage,
            float deployYoyoDuration,
            float deployYoyoKnockbackPower,
            float deployYoyoDamage,
            string hitSoundPrefabPath
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            _damage = damage;
            _owner = owner;

            this.transform.position = owner.CenterPos;
            _currentState = State.MoveToTarget;

            _targetPosition = targetPosition;
            _waitingDuration = waitingDuration;
            _moveSpeed = moveSpeed;
            _waitTime = 0f;
            _acceleration = acceleration;
            _knockbackPower = knockbackPower;
            _deployYoyoDuration = deployYoyoDuration;
            _deployYoyoKnobackPower = deployYoyoKnockbackPower;
            _deployYoyoDamage = deployYoyoDamage;

            _startPosition = owner.Pos;
            _hittedCharacters.Clear();

            _leftMoveDistance = moveDistance;

            _moveDirection = (_targetPosition - _startPosition).normalized;
            if (_moveDirection.x > 0)
            {
                _body.transform.localScale = new Vector2(1, 1) * 0.5f * (attackRadius / 0.8f); //반지름 0.8f가 기본 사이즈 그보다 크면 이미지 크기를 키워준다

            }
            else
            {
                _body.transform.localScale = new Vector2(-1, 1) * 0.5f * (attackRadius / 0.8f); //반지름 0.8f가 기본 사이즈 그보다 크면 이미지 크기를 키워준다
            }

            _attackRadius = attackRadius;

            _startLineRenderer.gameObject.SetActive(true);
            _backLineRenderer.gameObject.SetActive(false);
            _currentLineRenderer = _startLineRenderer;

            _hitSoundPrefabPath = hitSoundPrefabPath;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            if (_currentState == State.MoveToTarget)
            {
                float deltaDistance = deltaTime * _moveSpeed;
                if (deltaDistance > _leftMoveDistance)
                {
                    deltaDistance = _leftMoveDistance;
                }
                _leftMoveDistance -= deltaDistance;

                this.transform.position += (Vector3)(_moveDirection * deltaDistance);
                _moveSpeed -= _acceleration * deltaTime;
                if (_moveSpeed <= 0.2f)
                {
                    _moveSpeed = 0.2f;
                }

                if (_leftMoveDistance <= 0)
                {
                    _moveSpeed = 0f;
                    _waitTime = 0f;
                    _currentState = State.Waiting;
                }
            }
            else if (_currentState == State.Waiting)
            {
                _waitTime += deltaTime;
                if (_waitTime >= _waitingDuration)
                {
                    _currentState = State.MoveBackToOwner;
                    _startLineRenderer.gameObject.SetActive(false);
                    _backLineRenderer.gameObject.SetActive(true);
                    _currentLineRenderer = _backLineRenderer;

                    if(_deployYoyoDuration > 0)
                    {
                        stage.CreateDeployYoyoAreaEffectObject(_owner, this.transform.position, _moveDirection, _deployYoyoDuration, _attackRadius, _deployYoyoKnobackPower, _deployYoyoDamage);
                    }
                }
            }
            else if (_currentState == State.MoveBackToOwner)
            {
                _moveSpeed += _acceleration * deltaTime;
                Vector3 dir = new Vector3(_owner.CenterPos.x, _owner.CenterPos.y) - this.transform.position;
                dir.Normalize();
                if (Vector2.Distance(_owner.CenterPos, this.transform.position) <= _moveSpeed * deltaTime)
                {
                    this.transform.position = _owner.CenterPos;
                    _currentState = State.End;
                }
                else
                {
                    this.transform.position += dir * deltaTime * _moveSpeed;
                }
            }

            if (_hittedCharacterClearAt < Time.time)
            {
                _hittedCharacters.Clear();
                _hittedCharacterClearAt = Time.time + HITTED_CHARACTER_CLEAR_PERIOD;
            }


            CircularTargetArea area = new CircularTargetArea(this.transform.position, _attackRadius);
            CombatSystem.HitOnTargetArea(
                stage, area, _owner, _damage, CombatSystem.KnockBackType.Pivot,
                _owner.CenterPos, _knockbackPower, _hittedCharacters, _hittedCharacters,
                _hitSoundPrefabPath);


            for (int i = 0; i < _currentLineRenderer.positionCount; i++)
            {
                _currentLineRenderer.SetPosition(i, Vector3.Lerp(_owner.CenterPos, this.transform.position, (float)i / (float)(_currentLineRenderer.positionCount - 1)));
            }
        }


        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _hittedCharacters.Clear();
        }

    }

}
