#nullable enable
using Shared.GameDataTypes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SamMul.GameClients.Stages.AreaEffectObjects;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.GameClients.Stages.ItemObjects;

namespace SamMul.GameClients.Stages.Characters.PCs
{
    /// <summary>
    /// UI의 조작 상태에 따라 PlayerCharacter를 조작하는 컨트롤러
    /// 현재 PlayerCharacter를 조작하는 UI로 조이스틱이 있다.
    /// 이후 버튼 등으로 스킬을 조작하게 된다면, 해당 버튼 혹은 버튼의 이벤트를 PlayerController가 수신하여 플레이어캐릭터를 조작하게 구현한다.
    /// </summary>
    public class PlayerCharacterController
    {
        private readonly PlayerCharacter _pc;
        private readonly VariableJoystick _joystick;

        // 플레이어 캐릭터를 조작하고 있는 방향. 조이스틱 방향을 따른다.
        private Vector2 _controlDirection;
        // 플레이어 캐릭터를 조작하고 있는지 여부. 
        public bool IsControlling => _controlDirection != Vector2.zero;

        // 제자리에 위치하기 전에, 마지막으로 이동했던 방향을 기억해둔다. 손 때면 공격할 방향이다.
        private Vector2 _lastControlledDirection;

        private bool _isAutoPlayActivated;
        public bool IsAutoPlayActivated => _isAutoPlayActivated;

        public PlayerCharacterController(PlayerCharacter target, VariableJoystick joystick)
        {
            _pc = target;
            _joystick = joystick;
            _controlDirection = Vector2.zero;
            _lastControlledDirection = Vector2.right;

            _isAutoPlayActivated = false;
        }

        public void SwitchAutoPlay(bool activate)
        {
            _isAutoPlayActivated = activate;
        }
        
        public void Update(Stage stage)
        {
            if (!_pc.gameObject.activeSelf)
            {
                // 등장 연출중에는 PC의 비활성화중이다.
                return;
            }

            if (Time.timeScale <= 0f)
            {
                // 팝업이 띄워져 있다던지 해서 시간이 멈췄다. 업데이트도 멈춘다. 
                return;
            }

            if (_pc.Action.IsAppearing)
            {
                // Appearing 액션중에는 움직이지 않는다. 
                return;
            }

            if(_pc.Action.IsDead)
            {
                return;
            }

            this.ReadJoystickAndControlPlayer(stage);
        }

        private void BeginControl(Stage stage, Vector2 joystickDirection)
        {
            _controlDirection = joystickDirection;

            // 이동을 계속 이렇게 구현할 것인가?
            // Controller는 pc의 Velocity or MovePower만 설정하는게 맞지 않겠나?
            _pc.Move(_controlDirection);

            if (joystickDirection.sqrMagnitude > 0)
            {
                _lastControlledDirection = joystickDirection;
            }
        }

        private void UpdateControl(Stage stage, Vector2 joystickDirection)
        {
            // MoveVector 크기 1로 고정해달라고 요청해주셔서, Normalize한다.
            // (0.0~1.0 값으로 움직이는게아니라 항상 1의 크기로 움직이길 원함)
            _controlDirection = joystickDirection.normalized;
            _pc.Move(_controlDirection);

            if (joystickDirection.sqrMagnitude > 0)
            {
                _lastControlledDirection = joystickDirection;
            }
        }

        private void EndControl(Stage stage)
        {
            _controlDirection = Vector2.zero;
            _pc.StopMovement();
        }

        #region Reading Joystick Logic
        private void ReadJoystickAndControlPlayer(Stage stage)
        {
            var joystickDirection = _joystick.Direction;

            // 개발용 빌드에서만 키보드 사용해서 캐릭터 움직인다.
            if (Debug.isDebugBuild &&
                (joystickDirection == Vector2.zero))
            {
                joystickDirection = ReadKeyboardInputAndMakeJoystickDirection();
            }

            if (_isAutoPlayActivated &&
                joystickDirection == Vector2.zero)
            {
                this.ReadStageAndAutoControlPlayer(stage);
                return;
            }

            if (!IsControlling)
            {
                if (joystickDirection == Vector2.zero)
                {
                    // 조이스틱 조작도 안하면 조기 종료
                    return;
                }

                this.BeginControl(stage, joystickDirection);
            }
            else
            {
                if (joystickDirection != Vector2.zero)
                {
                    this.UpdateControl(stage, joystickDirection);
                }
                else
                {
                    this.EndControl(stage);
                }
            }
        }

        // 개발용 빌드에서 사용할 키보드 조작
        private Vector2 ReadKeyboardInputAndMakeJoystickDirection()
        {
            Vector2 joystickDirection = Vector2.zero;
            if (Input.anyKey)
            {
                if (Input.GetKey(KeyCode.A))
                {
                    joystickDirection.x += -1f;
                }

                if (Input.GetKey(KeyCode.S))
                {
                    joystickDirection.y += -1f;
                }

                if (Input.GetKey(KeyCode.D))
                {
                    joystickDirection.x += 1f;
                }

                if (Input.GetKey(KeyCode.W))
                {
                    joystickDirection.y += 1f;
                }
                joystickDirection.Normalize();
            }

            return joystickDirection;
        }

        #endregion

        #region AutoPlay AI Logic
        
        // 몬스터 사냥에 대해서만 마지막 방향을 기록한다. (경험치를 향한 방향은 기록하지 않는다)
        private Vector2? _lastAutoHuntingDirection = null;
        private float _autoHuntingDecidedAt = 0f;
        private long _avoidingDirectionCounter = 0;
        private float _lastAvoidingDirectionDecidedAt = 0f;

        private readonly List<(FenceObject Fence, float DistanceSquared)> v_collidingFenceObjects = new ();
        private readonly List<(AreaEffectObjectBase AreaEffect, float DistanceSquared)> v_collidingAreaEffectObjects = new();

        private void ReadStageAndAutoControlPlayer(Stage stage)
        {
            // 1. 엘리트 회피 : 일정거리내에 엘리트 몬스터가 있다면, 앨리트 몬스터로부터 멀어지는 방향으로 이동.
            // 2. 몬스터 회피 : 가장 가까운 몬스터와 충돌하기까지의 거리가 0.5m이내라면, 해당 몬스터와 멀어지는 방향으로 이동
            // 3. 아이템 탐색 : 20m 이내에 경험치/아이템이 있다면, 가장 가까운 아이템상자로 이동
            // - 단, 보스전이라면 3.단계(아이템 탐색)은 생략하고, 몬스터 탐색으로
            // 4. 몬스터 탐색 : 가장 가까운 몬스터와 충돌하지 않을 정도의 거리까지 근접해서 공격

            var now = Time.time;
            
            Vector2? direction = null;
            if (_lastAutoHuntingDirection != null &&
                (now < _autoHuntingDecidedAt + 0.3f))
            {
                direction = _lastAutoHuntingDirection.Value;
            }

            if (now > _lastAvoidingDirectionDecidedAt + 3f)
            {
                _lastAvoidingDirectionDecidedAt = now;
                ++_avoidingDirectionCounter;
            }

            var enemyAlliance = _pc.Alliance.ToEnemyAlliance();
            Character? bossMonster = null;
            Character? eliteMonster = null;
            Character? monster = null;
            if (direction == null)
            {
                // 1 엘리트 회피
                // 2 몬스터 회피
                // 3 보스 회피
                (bossMonster, eliteMonster, monster) = FindClosestMonsterInDistance(stage, enemyAlliance, _pc.Pos, distance: 5f);
                if (monster == null && eliteMonster == null && bossMonster == null)
                {
                    (bossMonster, eliteMonster, monster) = FindClosestMonsterInDistance(stage, enemyAlliance, _pc.Pos, distance: 11f);
                }
                if (monster == null && eliteMonster == null && bossMonster == null)
                {
                    (bossMonster, eliteMonster, monster) = FindClosestMonsterInDistance(stage, enemyAlliance, _pc.Pos, distance: 40f);
                }

                if (eliteMonster != null)
                {
                    var gap = (_pc.Pos - eliteMonster.Pos);

                    if (eliteMonster.Action.IsSkilling)
                    {
                        // 엘리트가 스킬을 사용하고 있다면, 더 멀리 + 살짝 비껴서 도망간다.
                        if (gap.sqrMagnitude <= 6f * 6f)
                        {
                            var orthogonalVector = new Vector2(-gap.y, gap.x);
                            orthogonalVector = orthogonalVector * ((_avoidingDirectionCounter % 2) == 0 ? -1f : 1f);
                            var avoidingDirection = gap + (orthogonalVector * 1f);

                            direction = avoidingDirection.normalized;
                        }
                    }
                    else
                    {
                        // 스킬을 사용하고 있지 않다면 적당히 멀리 도망간다
                        if (gap.sqrMagnitude <= 4f * 4f)
                        {
                            direction = gap.normalized;
                        }
                    }
                }
                else if (monster != null)
                {
                    float collisionDistance = _pc.ColliderRadius + monster.CollisionAttackRadius;
                    var gap = (_pc.Pos - monster.Pos);
                    
                    if (monster.Action.IsSkilling || monster.Action.IsAttacking)
                    {
                        var safeDistance = (monster == bossMonster) ? Random.Range(2f, 4f) : 0.75f;
                        if (gap.sqrMagnitude <= ((safeDistance + collisionDistance) * (safeDistance + collisionDistance)))
                        {
                            var orthogonalVector = new Vector2(-gap.y, gap.x);
                            orthogonalVector = orthogonalVector * ((_avoidingDirectionCounter % 2) == 0 ? -1f : 1f);
                            var avoidingDirection = gap + (orthogonalVector * 1f);

                            direction = avoidingDirection.normalized;
                        }
                    }
                    else
                    {
                        var safeDistance = (monster == bossMonster) ? Random.Range(2f, 4f) : 0.75f;
                        if (gap.sqrMagnitude <= ((safeDistance + collisionDistance) * (safeDistance + collisionDistance)))
                        {
                            direction = gap.normalized;
                        }
                    }
                }

                _lastAutoHuntingDirection = direction;
                _autoHuntingDecidedAt = now;
            }
            
            if (direction == null &&
                bossMonster == null)    // 아이템탐색은 보스전에서는 생략한다.
            {
                // 3 아이템 탐색
                (ExpObject? expObject, AcquirableItemObject? acquirableItem) = FindExpAndAcquirableItemObjectInDistance(stage, _pc.Pos, distance: 8f);

                if (expObject == null && acquirableItem == null)
                {
                    (expObject, acquirableItem) = FindExpAndAcquirableItemObjectInDistance(stage, _pc.Pos, distance: 20f);
                }

                if (acquirableItem != null)
                {
                    direction = ((Vector2)acquirableItem.transform.position - _pc.Pos).normalized;
                }
                else if (expObject != null)
                {
                    direction = ((Vector2)expObject.transform.position - _pc.Pos).normalized;
                }
                else
                {
                    direction = null;
                }
            }

            if (direction == null)
            {
                // 몬스터를 향해 가거나, 몬스터가 없으면 적당히 움직인다. 
                if (monster == null)
                {
                    (bossMonster, eliteMonster, monster) = FindClosestMonsterInDistance(stage, enemyAlliance, _pc.Pos, distance: 25f);
                }
                
                if (monster != null)
                {
                    var toMonster = (monster.Pos - _pc.Pos);
                    if (toMonster.sqrMagnitude >= 9f)
                    {
                        direction = toMonster.normalized;
                    }
                }
                else
                {
                    direction = (_lastControlledDirection + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f))).normalized;
                    _lastAutoHuntingDirection = direction;
                    _autoHuntingDecidedAt = now;
                }

            }

            if (bossMonster != null)
            {
                // 보스전일경우 울타리로는 가지 않는다. 울타리 방향과 내적해서 이동한다. 
                v_collidingFenceObjects.Clear();
                stage.ForAllFenceObjects((fenceObject) =>
                {
                    // 지금 진행방향으로 일정거리의 선분이 울타리랑 충돌하는지 확인,
                    // 충돌한다면 진행방향을 울타리로 내적하고, 반대방향으로 이동하도록 벡터를 추가
                    var distanceVector = ((Vector2)fenceObject.transform.position) - _pc.Pos;
                    var distanceSquared = distanceVector.sqrMagnitude;
                    if (distanceSquared <= 8f)
                    {
                        v_collidingFenceObjects.Add((fenceObject, distanceSquared));
                    }

                    return true;
                });

                if (v_collidingFenceObjects.Count >= 2)
                {
                    // 가까운 거리 순으로 정렬하고, 제일 가까운 두개 울타리를 이은 벡터를 사용한다. 울타리로부터 멀어지는 방향으로
                    FenceObject firstFence = null;
                    FenceObject secondFence = null;

                    foreach(var fence in v_collidingFenceObjects.OrderBy(fenceInfo => fenceInfo.DistanceSquared).Take(2))
                    {
                        if (firstFence == null)
                        {
                            firstFence = fence.Fence;
                            continue;
                        }

                        if (secondFence == null)
                        {
                            secondFence = fence.Fence;
                        }
                    }

                    Debug.Assert(firstFence != null);
                    Debug.Assert(secondFence != null);

                    var fenceVector = (Vector2)secondFence.transform.position - (Vector2)firstFence.transform.position;
                    var oppositeToFenceVector = (_pc.Pos - (Vector2)firstFence.transform.position);

                    direction = (fenceVector.normalized + oppositeToFenceVector.normalized).normalized;
                }
            }

            // 이동하려는 방향으로 일정 거리 내에 독장판이 있으면 살짝 비껴 이동하거나, 잠시 대기한다.
            if (direction != null)
            {
                v_collidingAreaEffectObjects.Clear();
                var enemyAllience = _pc.Alliance.ToEnemyAlliance();
                stage.ForAllAliveAreaEffects((areaEffect) =>
                {
                    if (areaEffect.Alliance != enemyAlliance)
                    {
                        return true;
                    }

                    var distanceSquared = (((Vector2)areaEffect.transform.position) - _pc.Pos).sqrMagnitude;
                    if (distanceSquared <= 25f)
                    {
                        // 가까우면서, 내가 이동하려는 방향이랑 같아야 한다.
                        // 벡터 내적이 양의 값이면 같은방향이다.
                        var toAreaEffect = (Vector2)areaEffect.transform.position - _pc.Pos;
                        float dotProduct = Vector2.Dot(toAreaEffect, direction.Value);
                        if (dotProduct > 0)
                        {
                            v_collidingAreaEffectObjects.Add((areaEffect, distanceSquared));
                        }
                    }
                    return true;
                });

                if (v_collidingAreaEffectObjects.Count > 0)
                {
                    // 장판 바깥으로 돌아간다.
                    var closestAreaEffectInfo = v_collidingAreaEffectObjects.OrderBy(areaEffect => areaEffect.DistanceSquared).First();
                    var toAreaEffect = ((Vector2)closestAreaEffectInfo.AreaEffect.transform.position - _pc.Pos).normalized;
                    var orthogonalToAreaEffect = new Vector2(-toAreaEffect.y, toAreaEffect.x);
                    direction = (direction.Value + orthogonalToAreaEffect * 8f).normalized;
                }
            }

            if (!IsControlling)
            {
                if (direction == null)
                {
                    // 가만히 있는 경우
                    return;
                }

                this.BeginControl(stage, direction.Value);
            }
            else
            {
                if (direction != null)
                {
                    this.UpdateControl(stage, direction.Value);
                }
                else
                {
                    this.EndControl(stage);
                }
            }
        }
        
        
        private static (ExpObject? expObject, AcquirableItemObject? acquirableItem) FindExpAndAcquirableItemObjectInDistance(Stage stage, Vector2 position, float distance)
        {
            ExpObject? expObject = null;
            float expObjectSquaredDistance = 99999f;

            // 경험치가 아닌 획득가능한 오브젝트들
            AcquirableItemObject? acquirableItem = null;
            float acquirableObjectSquaredDistance = 99999f;
        
            foreach (var item in stage.ForAcquirableItemObjectsInDistance(position, distance: 10f))
            {
                float squaredDistance = ((Vector2)item.transform.position - position).sqrMagnitude;
                if (item.DropItemType.IsExpItem())
                {
                    if (squaredDistance < expObjectSquaredDistance)
                    {
                        expObject = (ExpObject)item;
                        expObjectSquaredDistance = squaredDistance;
                    }
                }
                else
                {
                    if (squaredDistance < acquirableObjectSquaredDistance)
                    {
                        acquirableItem = item;
                        acquirableObjectSquaredDistance = squaredDistance;
                    }
                }
            }

            return (expObject, acquirableItem);
        }

        private List<Character> v_enemies = new List<Character>();
        private (Character? bossMonster, Character? eliteMonster, Character? monster) FindClosestMonsterInDistance(Stage stage, AllianceType enemyAlliance, Vector2 position, float distance)
        {
            v_enemies.Clear();
            stage.FindAliveCharactersInArea(enemyAlliance, new CircularTargetArea(position, distance), in v_enemies);

            // 보스 몬스터
            Character? bossMonster = null;
            float bossDistanceSquared = 9999999f;
            // 가장가까운 엘리트몬스터
            Character? eliteMonster = null;
            float eliteDistanceSquared = 999999f;
            // 가장가까운 몬스터 (엘리트 포함)
            Character? monster = null;
            float monsterDistanceSquared = 999999f;
                
            foreach (var enemy in v_enemies)
            {
                float distanceSquared = (enemy.Pos - position).sqrMagnitude;
                if (enemy.IsElite)
                {
                    if (distanceSquared < eliteDistanceSquared)
                    {
                        eliteDistanceSquared = distanceSquared;
                        eliteMonster = enemy;
                    }
                }

                if (enemy.IsBoss)
                {
                    if (distanceSquared < bossDistanceSquared)
                    {
                        bossDistanceSquared = distanceSquared;
                        bossMonster = enemy;
                    }
                }

                if (distanceSquared < monsterDistanceSquared)
                {
                    monsterDistanceSquared = distanceSquared;
                    monster = enemy;
                }
            }

            return (bossMonster, eliteMonster, monster);
        }
        #endregion 
        
    }
    
}
