using DG.Tweening;
using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.StatusEffects;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class DancingTriangleObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _duration;

        private Character _owner;
        private float _createdAt;

        private Vector2 _movingDirection;
        private float _damage;
        private float _speed;
        private float _radius;
        private float _knobackPower;
        private float _duration;
        private float _attackPeriod;
        private float _stunDuration;
        private float _stunPeriod;
        private bool _removePoisons;

        private GameObject _body;
        private SkeletonAnimation _skeletonAnimation;
        private Animation _appearAnimation;
        private Animation _repeatAnimation;
        private Animation _endAnimation;

        private HashSet<Character> _hittedCharacters;
        private HashSet<Character> _exceptedCharacters;
        private List<Character> _stunedCharacters;

        private float _clearHittedCharactersAt;
        private float _clearStunedCharactersAt;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.DancingTriangleObject);
            _body = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/MambaSkill/MambaTranscendentSkillObject.prefab");
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;

            _skeletonAnimation = _body.GetComponent<SkeletonAnimation>();

            _appearAnimation = _skeletonAnimation.skeleton.Data.FindAnimation("appear");
            _repeatAnimation = _skeletonAnimation.skeleton.Data.FindAnimation("idle");
            _endAnimation = _skeletonAnimation.skeleton.Data.FindAnimation("stop");

        }

        public void Initialize(
            Character owner,
            Vector2 startPosition,
            Vector2 movingDirection,
            float damage,
            float speed,
            float radius,
            float knobackPower,
            float duration,
            float attackPeriod,
            float stunDuration,
            float stunPeriod,
            bool removePoisons
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _createdAt = now;
            _owner = owner;

            _movingDirection = movingDirection;
            _damage = damage;
            _speed = speed;
            _radius = radius;
            _knobackPower = knobackPower;
            _duration = duration;
            _attackPeriod = attackPeriod;
            _stunDuration = stunDuration;
            _stunPeriod = stunPeriod;
            _removePoisons = removePoisons;

            _body.transform.rotation = Quaternion.Euler(0.0f, 0.0f, Vector2.SignedAngle(Vector2.right, _movingDirection));
            _body.transform.localScale = Vector3.one * radius / 2.5f;

            this.transform.position = startPosition;
            _clearHittedCharactersAt = now;

            _skeletonAnimation.AnimationState.SetAnimation(0, _appearAnimation, false);
            _skeletonAnimation.AnimationState.AddAnimation(0, _repeatAnimation, true, 0f);
            _skeletonAnimation.AnimationState.AddAnimation(1, _endAnimation, false, _duration - _appearAnimation.Duration - _endAnimation.Duration);


            _hittedCharacters = new HashSet<Character>();
            _exceptedCharacters = new HashSet<Character>();
            _stunedCharacters = new List<Character>();
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            Vector2 nextPosition = this.MoveToCurrentPosition(deltaTime);
            this.ReflectionToCameraSize(nextPosition);

            // 초월 공격은 같은 캐릭터를 여러 번 공격할 수 있도록 주기적으로 공격한 캐릭터를 초기화한다.
            if (_clearHittedCharactersAt < now)
            {
                _exceptedCharacters.Clear();
                _clearHittedCharactersAt = now +_attackPeriod;
            }

            // 스턴 걸렸던 캐릭터 리스트 초기화
            if (_clearStunedCharactersAt <= now)
            {
                _stunedCharacters.Clear();
                _clearStunedCharactersAt = now + _stunPeriod;
            }
            _hittedCharacters.Clear();
            
            var targetArea = new CircularTargetArea(this.transform.position, _radius);
            CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Direction, _movingDirection, _knobackPower, _hittedCharacters, _exceptedCharacters, null);
            foreach (var hittedCharacter in _hittedCharacters)
            {
                _exceptedCharacters.Add(hittedCharacter);

                //스턴 적용 가능한지 확인한다.
                if (_stunDuration > 0f &&
                    !hittedCharacter.IsBoss &&
                    !_stunedCharacters.Contains(hittedCharacter))
                {
                    hittedCharacter.StatusEffects.AddOrUpdateStatusEffect(stage, hittedCharacter, StatusEffectType.Stun, duration: _stunDuration, Time.time, 0f);
                    _stunedCharacters.Add(hittedCharacter);
                }
            }

            if (_removePoisons)
            {
                stage.ForAllAliveAreaEffects((areaEffect) =>
                {
                    if (areaEffect.AreaEffectObjectType != AreaEffectType.PoisonousArea)
                    {
                        return true;
                    }

                    var poisonousAreaEffect = areaEffect as PoisonousAreaEffectObject;
                    if (poisonousAreaEffect == null)
                    {
                        return true;
                    }

                    if (!targetArea.Contains(poisonousAreaEffect.Center, poisonousAreaEffect.Radius))
                    {
                        return true;
                    }

                    poisonousAreaEffect.TryDisappear();
                    return true;
                });
            }
        }

        private Vector2 MoveToCurrentPosition(float deltaTime)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _movingDirection * _speed * deltaTime;
            return nextPosition;
        }
        
        private void ReflectionToCameraSize(Vector2 nextMovePosition)
        {
            var worldRect = GameClient.CameraController.GetWorldRectInCamera(0.0f, 0.0f, 0.0f, 500.0f);
            worldRect.xMin += _radius;
            worldRect.xMax -= _radius;
            worldRect.yMin += _radius;
            worldRect.yMax -= _radius;

            Vector2 movePosition = nextMovePosition;
            Vector2 reflectedVector = Vector2.zero;

            if (movePosition.x < worldRect.xMin)
            {
                //왼쪽벽 반사 
                reflectedVector.x = 1.0f;
                //벽 안쪽으로 이동
                movePosition.x = worldRect.xMin;
                //반사각 계산
                if (Vector2.Dot(Vector2.right, _movingDirection) < 0)
                {
                    _movingDirection = Vector2.Reflect(_movingDirection, Vector2.right);
                }
                else
                {
                    _movingDirection = Vector2.Lerp(_owner.MoveDir, _movingDirection, 0.5f);
                }
            }
            else if (movePosition.x > worldRect.xMax)
            {
                //오른쪽벽 반사
                reflectedVector.x = -1.0f;
                //벽 안쪽으로 이동
                movePosition.x = worldRect.xMax;
                //반사각 계산
                if (Vector2.Dot(Vector2.left, _movingDirection) < 0)
                {
                    _movingDirection = Vector2.Reflect(_movingDirection, Vector2.left);
                }
                else
                {
                    _movingDirection = Vector2.Lerp(_owner.MoveDir, _movingDirection, 0.5f);
                }

            }
            else if (movePosition.y < worldRect.yMin)
            {
                //아래 벽 반사
                reflectedVector.y = 1.0f;
                //벽 안쪽으로 이동
                movePosition.y = worldRect.yMin;
                //반사각 계산
                if (Vector2.Dot(Vector2.up, _movingDirection) < 0)
                {
                    _movingDirection = Vector2.Reflect(_movingDirection, Vector2.up);
                }
                else
                {
                    _movingDirection = Vector2.Lerp(_owner.MoveDir, _movingDirection, 0.2f);
                }
            }
            else if (movePosition.y > worldRect.yMax)
            {
                //위쪽 벽 반사
                reflectedVector.y = -1.0f;
                //벽 안쪽으로 이동
                movePosition.y = worldRect.yMax;
                //반사각 계산
                if (Vector2.Dot(Vector2.down, _movingDirection) < 0)
                {
                    _movingDirection = Vector2.Reflect(_movingDirection, Vector2.down);
                }
                else
                {
                    _movingDirection = Vector2.Lerp(_owner.MoveDir, _movingDirection, 0.5f);
                }
            }
            _movingDirection.Normalize();
            this.transform.position = movePosition;
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _skeletonAnimation.AnimationState.SetEmptyAnimation(0, 0f);
            _skeletonAnimation.AnimationState.SetEmptyAnimation(1, 0f);
        }

    }

}
