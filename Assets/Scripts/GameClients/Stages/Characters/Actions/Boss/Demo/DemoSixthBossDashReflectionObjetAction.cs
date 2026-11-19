using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Demo
{
    /// <summary>
    /// 맵 밖으로 빠져나간 뒤, 밖에서 타겟을 향해 맵을 가로지르는 돌진을 DashCount 번 하고 맵 중앙으로 돌아온다.
    /// 맵 안을 지나는 동안 일정 간격으로 돌진 방향의 좌우로 벽에 튕기는 오브젝트를 쏜다.
    /// 진행 시간이 정해져 있지 않아 서브 액션을 쓰지 않고 상태 머신으로 처리하며, 끝나면 스스로 IdleAction 으로 바꾼다.
    /// </summary>
    public class DemoSixthBossDashReflectionObjetAction : SmartAction<SpriteMonsterAnimationController>
    {
        private static readonly float DAMAGE_COEFFICIENT = 1.0f;
        private enum ActionState { NotStarted, OutsideMove, DashReady, Dash, CenterMoveReady, CenterMove, Finished }

        // 맵 밖으로 나가기 전 제자리에서 대기하는 시간과, 중앙에 돌아온 뒤 대기하는 시간.
        private static readonly float ReadyDuration = 0.5f;
        private static readonly float EndDuration = 0.5f;

        // 돌진이 시작·종료되는 바깥 사각형의 크기 = 맵(울타리) 크기 x 이 값.
        private static readonly float OutsideRectScale = 1.5f;

        private static readonly int DashCount = 2;
        private static readonly float DashSpeed = 30.0f;
        private static readonly float OutSideMoveSpeed = 20f;
        private static readonly float DashIndicatorDuration = 0.5f;

        private const string ProjectileBodyPath = "Stage/Common/Projectiles/MonsterProjectile_Orb2.prefab";
        private static readonly float ProjectileFireDelay = 0.5f;
        private static readonly float ProjectileSpeed = 12;
        private static readonly float ProjectileLifeTime = 8f;
        private static readonly float ProjectileRadius = 0.4f;
        private static readonly float ProjectileKnobackPower = 0.1f;
        private static readonly float ProjectileRotateSpeed = 0f;

        private Monster _owner;
        private Character _target;
        private Collider2D _ownerCollider;

        private Vector2 _moveDirection;
        private Vector2 _dashDirection;
        private ActionState _currentActionState;
        private Rect _outsideCheckRect;
        private Rect _stageRect;
        private float _maxDistance;

        private float _oustsideAt;
        private int _currentDashCount;
        private float _centerMoveAt;
        private float _finishEndAt;
        private float _dashAt;
        private float _projectileFireAt;

        public override void Initialize(Monster owner, Character target, SpriteMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                -1,
                animationController);

            _owner = owner;
            _target = target;
            _ownerCollider = owner.GetComponent<Collider2D>();
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            _ownerCollider.enabled = false;
            _currentActionState = ActionState.NotStarted;
            _stageRect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();
            Vector2 size = _stageRect.size * OutsideRectScale;
            Vector2 position = _stageRect.center + new Vector2(size.x * -0.5f, size.y * -0.5f);
            _outsideCheckRect = new Rect(position, size);

            //대각선 길이를 최대 거리로 놓고 계산 (인디케이터 등을 표현할때 최대 거리만큼 표현)
            _maxDistance = Mathf.Sqrt(_outsideCheckRect.width * _outsideCheckRect.width + _outsideCheckRect.height * _outsideCheckRect.height);

            //대쉬 끝나고 카운트 확인 처리하기 때문에 1부터 시작한다.
            _currentDashCount = 1;
            _projectileFireAt = 0f;
        }

        //해당 패턴은 서브 액션 처리를 사용하지 않는다.
        public override void Update(Stage stage, float deltaTime, float now)
        {
            switch (_currentActionState)
            {
                case ActionState.NotStarted:
                    {
                        AnimationController.PlayIdleAttackActionInfinitely();
                        _currentActionState = ActionState.OutsideMove;

                        // 타겟의 반대쪽으로 빠져나간다.
                        _moveDirection = Quaternion.Euler(0.0f, 0.0f, Random.Range(-30.0f, 30.0f)) * (_owner.Pos.x < _target.Pos.x ? Vector2.left : Vector2.right);
                        _moveDirection.Normalize();
                        AnimationController.UpdateBodyDirectionByMoveDirection(_moveDirection);

                        _oustsideAt = now + ReadyDuration;
                    }
                    break;
                case ActionState.OutsideMove:
                    {
                        if (_oustsideAt > now)
                        {
                            return;
                        }

                        //맵 밖으로 이동했는지 확인
                        if (!_outsideCheckRect.Contains(_owner.Pos))
                        {
                            //가장 가까운 rect 위치로 이동시키고
                            //다시 맵 밖으로 나갈때까지 대쉬 이동
                            _owner.transform.position = this.GetNearRectInsidePosition(_owner.Pos, _outsideCheckRect);
                            _currentActionState = ActionState.DashReady;
                        }
                        else
                        {
                            AnimationController.PlayMoveInfinitely(_moveDirection);
                            _owner.transform.Translate(deltaTime * OutSideMoveSpeed * _moveDirection, Space.World);
                        }
                    }
                    break;
                case ActionState.DashReady:
                    {
                        stage.CreateDashAttackRangeIndicator(_owner, _target, _owner.CollisionAttackRadius + 2.0f, _maxDistance, DashIndicatorDuration);
                        AnimationController.PlayIdleAttackActionInfinitely();
                        AnimationController.UpdateBodyDirectionByMoveDirection(_target.Pos - _owner.Pos);
                        _currentActionState = ActionState.Dash;
                        _dashDirection = Vector2.zero;
                        _dashAt = now + DashIndicatorDuration;
                    }
                    break;
                case ActionState.Dash:
                    {
                        if (_dashAt >= now)
                        {
                            return;
                        }

                        if (!_outsideCheckRect.Contains(_owner.Pos))
                        {
                            if (_currentDashCount < DashCount)
                            {
                                _currentDashCount++;
                                _owner.transform.position = this.GetNearRectInsidePosition(_owner.Pos, _outsideCheckRect);
                                _currentActionState = ActionState.DashReady;
                            }
                            else
                            {
                                _currentActionState = ActionState.CenterMoveReady;
                            }
                        }
                        else
                        {
                            if (_dashDirection == Vector2.zero)
                            {
                                _dashDirection = (_target.Pos - _owner.Pos).normalized;
                                AnimationController.UpdateBodyDirectionByMoveDirection(_dashDirection);
                                AnimationController.PlayMoveInfinitely(_dashDirection);
                            }

                            _owner.transform.Translate(deltaTime * DashSpeed * _dashDirection, Space.World);

                            // 오브젝트는 맵 안을 지나는 동안에만 쏜다.
                            if (_projectileFireAt <= now && _stageRect.Contains(_owner.Pos))
                            {
                                this.FireLeftRightReflectionObject(stage, _owner.CenterPos, _dashDirection);
                                _projectileFireAt = now + ProjectileFireDelay;
                            }
                        }
                    }
                    break;
                case ActionState.CenterMoveReady:
                    {
                        _centerMoveAt = now + DashIndicatorDuration;
                        _dashDirection = (_outsideCheckRect.center - _owner.Pos).normalized;
                        stage.CreateDirectionalSquareRangeIndicator(
                            _owner.CenterPos, _dashDirection, _owner.CollisionAttackRadius + 2.0f, Vector2.Distance(_outsideCheckRect.center, _owner.Pos), DashIndicatorDuration);
                        AnimationController.PlayIdleAttackActionInfinitely();
                        AnimationController.UpdateBodyDirectionByMoveDirection(_dashDirection);
                        _currentActionState = ActionState.CenterMove;
                    }
                    break;
                case ActionState.CenterMove:
                    {
                        if (_centerMoveAt < now)
                        {
                            _dashDirection = (_outsideCheckRect.center - _owner.Pos).normalized;
                            if (Vector2.Distance(_outsideCheckRect.center, _owner.Pos) <= DashSpeed * deltaTime)
                            {
                                _currentActionState = ActionState.Finished;
                                _owner.transform.position = _outsideCheckRect.center;
                                AnimationController.PlayIdleAttackActionInfinitely();
                                _finishEndAt = now + EndDuration;
                            }
                            else
                            {
                                AnimationController.PlayMoveInfinitely(_dashDirection);
                                _owner.transform.Translate(deltaTime * DashSpeed * _dashDirection, Space.World);
                            }
                        }
                    }
                    break;
                case ActionState.Finished:
                    {
                        if (_finishEndAt <= now)
                        {
                            _ownerCollider.enabled = true;
                            _owner.Action.ChangeTo(stage, new IdleAction(_animationController));
                        }
                    }
                    break;
            }
        }

        public override ActionBase End(Stage stage)
        {
            _ownerCollider.enabled = true;
            return null;
        }

        public override bool Cancel(Stage stage)
        {
            this.End(stage);
            return base.Cancel(stage);
        }

        private Vector2 GetNearRectInsidePosition(Vector2 position, Rect rect)
        {
            float clampedX = Mathf.Clamp(position.x, rect.xMin + 0.1f, rect.xMax - 0.1f);
            float clampedY = Mathf.Clamp(position.y, rect.yMin + 0.1f, rect.yMax - 0.1f);

            return new Vector2(clampedX, clampedY);
        }

        private void FireLeftRightReflectionObject(Stage stage, Vector2 firePosition, Vector2 dashDir)
        {
            Vector2 leftDir = Quaternion.Euler(0, 0, 90f) * dashDir;
            Vector2 rightDir = Quaternion.Euler(0, 0, -90f) * dashDir;

            this.FireReflectionObject(stage, firePosition, leftDir);
            this.FireReflectionObject(stage, firePosition, rightDir);
        }

        private void FireReflectionObject(Stage stage, Vector2 firePosition, Vector2 direction)
        {
            stage.CreateReflectionAreaEffectObject(
                _owner,
                ProjectileRadius,
                firePosition,
                direction,
                ProjectileSpeed,
                ProjectileRotateSpeed,
                _owner.SpecialAttackPower * DAMAGE_COEFFICIENT,
                ProjectileKnobackPower,
                ProjectileLifeTime,
                _stageRect,
                ProjectileBodyPath
                );
        }
    }
}
