using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.Actions
{
    public class MonsterDashAction : ActionBase
    {
        private Monster _owner;
        private Character _target;
        private float _prepareDuration;
        private float _dashDuration;
        private float _dashDistance;
        private float _stopDuration;
        private float _dashSpeed;

        private Animation _prepareAnimation;
        private Animation _dashAnimation;
        private Animation _stopAnimation;
        private new SpineMonsterAnimationController _animationController;

        private float _beginAt;
        private float _preparingEndAt => _beginAt + _prepareDuration;
        private float _dashingEndAt => _preparingEndAt + _dashDuration;
        private float _stoppingEndAt => _dashingEndAt + _stopDuration;

        private Vector2 _targetDirection;
        private bool _isRotateToTargetDirection;
        private List<Bone> _rotateBones;
        private Vector2 _dashAttackAreaSize;
        private float _lastRotateAngle;
        private float _lastDashAttackedAt;
        public MonsterDashAction(
            Monster owner,
            Character target,
            SpineMonsterAnimationController animationController,
            Vector2 dashAttackAreaSize,
            float prepareDuration,
            string prepareAnimationName,
            float dashDuration,
            float dashDistance,
            string dashAnimationName,
            float stopDuration,
            string stopAnimationName,
            bool isRotateToTargetDirection,
            string[] rotateBoneNames
            ) :
            base(ActionType.Skill, prepareDuration + dashDuration + stopDuration , animationController)
        {
            _owner = owner;
            _target = target;
            _prepareDuration = prepareDuration;
            _dashDuration = dashDuration;
            _dashDistance = dashDistance;
            _stopDuration = stopDuration;
            _dashSpeed = _dashDistance / _dashDuration;
            _dashAttackAreaSize = dashAttackAreaSize;

            _animationController = animationController;

            _prepareAnimation = _animationController.FindAnimation(prepareAnimationName);
            _dashAnimation = _animationController.FindAnimation(dashAnimationName);
            _stopAnimation = _animationController.FindAnimation(stopAnimationName);

            _isRotateToTargetDirection = isRotateToTargetDirection;
            _rotateBones = new List<Bone>();
            if(rotateBoneNames != null)
            {
                for (int i = 0; i < rotateBoneNames.Length; i++)
                {
                    _rotateBones.Add(_animationController.Body.skeleton.FindBone(rotateBoneNames[i]));
                }
            }

            if (_prepareAnimation == null)
            {
                _prepareAnimation = _animationController.FindAnimation("idle");
            }
            Debug.Assert(_prepareAnimation != null, "준비 애니메이션이 없습니다. 확인 부탁드려요");
            Debug.Assert(_dashAnimation != null, "대쉬 애니메이션이 없습니다. 확인 부탁드려요");

            _animationController.SetAnimationMix(_prepareAnimation, _dashAnimation, 0f);
            if (null != _stopAnimation)
            {
                _animationController.SetAnimationMix(_dashAnimation, _stopAnimation, 0f);
            }
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            _beginAt = now;
            _animationController.SkipHitAnimation(true);

            //대쉬 자체가 이동이자 공격이다, 상하체 애니메이션을 따로 분리하지 않는다.
            if(_stopAnimation != null)
            {
                _animationController.SetAnimation(BodyAnimationTrack.WholeBody, _prepareAnimation, false, _prepareDuration);
                _animationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _dashAnimation, true, 0f);
                _animationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _stopAnimation, false, _dashDuration,_stopDuration);
            }
            else
            {
                _animationController.SetAnimation(BodyAnimationTrack.WholeBody, _prepareAnimation, false,_prepareDuration);
                _animationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _dashAnimation, true, 0f);
            }

            _owner.SetCheckHitOnCollisionArea(false);

            //공격방향을 바라본다. 
            //공격방향 == 플레이어 방향
            //몸체 회전옵션이 켜져있으면 회전, 아니면 스파인에 방향성만 설정해준다.
            _targetDirection = (_target.CenterPos - _owner.CenterPos).normalized;

            float angle = Mathf.Atan2(_targetDirection.y, _targetDirection.x) * Mathf.Rad2Deg;
            stage.AreaIndicators.CreateSquareAttackRangeIndicator(_owner.Pos + _targetDirection * _dashDistance * 0.5f, _dashDistance, 5f, angle, _prepareDuration);
            stage.AreaIndicators.CreateDirectionalIndicator(_owner.Pos , _targetDirection, _dashDistance, 5f, _prepareDuration);

            if (_isRotateToTargetDirection)
            {
                _owner.transform.rotation = Quaternion.identity;
                this.RotateToTargetDirection(_owner,_target);
            }
            else
            {            
                //혹시나 공격방향이 0일경우 이동 방향을 바라본다.
                if (_targetDirection == Vector2.zero)
                {
                    _animationController.UpdateBodyDirectionByMoveDirection(_owner.MoveDir);
                }
                else
                {
                    _animationController.UpdateBodyDirectionByMoveDirection(_targetDirection);
                }
            }
        }

        public override ActionBase End(Stage stage)
        {
            if (_isRotateToTargetDirection)
            {
                _owner.transform.rotation = Quaternion.identity;
            }
            _owner.SetCheckHitOnCollisionArea(true);
            return null;
        }

        public override void Update(Stage stage, float deltaTime, float now)
        {
            if(now < _preparingEndAt)
            {
                //아무것도 하지 않는다. 공격전 준비와 관련해서 처리해야될 일이 있으면 여기다 작성
            }
            else if(now < _dashingEndAt)
            {
                Vector2 dashMovement = _targetDirection * deltaTime * _dashSpeed;
                _owner.transform.Translate(dashMovement.x, dashMovement.y, 0f, Space.World);
                this.HitOnTargetArea(stage, now);

            }
            else if(now < _stoppingEndAt && _stopAnimation != null)
            {
                _animationController.UpdateBodyDirectionByMoveDirection(_targetDirection);
                // 아무것도 하지 않는다. 이후 멈춤 처리와 관련되서 해야될 일이 있으면 여기다 처리
            }
        }

        private void HitOnTargetArea(Stage stage, float now)
        {
            if (_lastDashAttackedAt + _owner.CollisionAttackDuration <= now)
            {
                var collisionArea = new SquareTargetArea(_owner.Pos + new Vector2(0, _dashAttackAreaSize.y * 0.5f), _dashAttackAreaSize, _lastRotateAngle);
                HashSet<Character> targets = new HashSet<Character>();

                CombatSystem.HitOnTargetArea(stage, collisionArea, _owner, _owner.CollisionAttackPower, CombatSystem.KnockBackType.Pivot, collisionArea.Center, knockBackPower: 1.0f, hittedCharacterCollector: targets, exceptedCharacters: null, hitSoundPrefabPath: string.Empty);

                if (targets.Count > 0)
                {
                    _lastDashAttackedAt = now;
                }
                targets.Clear();
            }
        }

        //캐릭터 스파인에 기본 방향은 왼쪽으로 되어있다.
        //계산한 방향에 맞도록 캐릭터 몸 방향을 오른쪽에 맞추고 스파인 본들을 회전시킨다.
        private void RotateToTargetDirection(Monster owner, Character target)
        {
            if(_rotateBones.Count <= 0)
            {
                return;
            }

            _animationController.UpdateBodyDirectionByMoveDirection(Vector2.right);
            for (int i = 0; i < _rotateBones.Count; i++)
            {
                Vector2 boneWorldPosition = _rotateBones[i].GetWorldPosition(owner.Body.transform);
                Vector2 rotateDirection = target.CenterPos - boneWorldPosition;
                float angle = (Mathf.Atan2(rotateDirection.y, rotateDirection.x) * Mathf.Rad2Deg);
                _rotateBones[i].Rotation = angle * -1;  //스파인이 좌우 반전 되어있어서 -1을 곱해줘야 정상적인 각도로 회전한다.
                _lastRotateAngle = angle;
            }
        }
    }
}
