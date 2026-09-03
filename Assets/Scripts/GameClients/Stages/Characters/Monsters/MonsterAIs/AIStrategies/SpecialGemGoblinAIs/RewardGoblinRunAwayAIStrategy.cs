using UnityEngine;
using Z.GameClients.Stages.Characters.Actions;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Stats;
using Z.Loggers;
using Debug = System.Diagnostics.Debug;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.SpecialGemGoblinAIs
{
    public class RewardGoblinRunAwayAIStrategy : MonsterAIStrategyBase
    {
        public new RewardGoblinAIBlackboard _blackboard => (RewardGoblinAIBlackboard)base._blackboard!;

        // 나를 따라오는 적군 캐릭터. 이 캐릭터를 피해 도망간다.
        // 일정 거리 이상 멀어지면 다시 Idle로 돌아간다.
        private readonly Character _chaser;

        private Vector2? _moveDirection;

        // 추격자의 위치를 마지막으로 확인한 시각
        private float _trackedChaserAt;

        // 걷거나 뛰거나
        private bool _isRunning;

        private float? _movingStartedAt;
        private float? _recoveringStartedAt;

        // 추격자로부터 이만큼 벗어나면 다시 Idle로 돌아간다.
        private static readonly float SAFE_DISTANCE_FROM_CHASER = 12f;
        private static readonly float SQUARED_SAFE_DISTANCE_FROM_CHASER = (SAFE_DISTANCE_FROM_CHASER * SAFE_DISTANCE_FROM_CHASER);

        // 도망 움직임을 지속하는 시간
        private static readonly float MOVING_DURATION = 3f;
        // 도망 움직임 뒤에, 이만큼은 쉬었다가 다시 도망간다.
        private static readonly float RECOVERING_DURATION = 1f;

        // 추격자의 방향에 따라 움직임 변경할 주기
        private static readonly float CHASER_TRACKING_PERIOD = 0.33f;
        // 뛰어서 도망갈 때 추가해줄 속력
        private static readonly StatModifier RUNNING_SPEED_ADD_MODIFIER = new StatModifier(2.75f, StatModType.Flat);

        public RewardGoblinRunAwayAIStrategy(Character chaser, RewardGoblinAIBlackboard blackboard) : base(blackboard)
        {
            Debug.Assert(chaser != null);
            _chaser = chaser;
            _moveDirection = null;
            _trackedChaserAt = 0f;
            _isRunning = false;
            _movingStartedAt = null;
            _recoveringStartedAt = null;

        }

        public override void Begin(Stage stage, Monster owner)
        {
            var direction = _chaser.Pos - owner.Pos;

            this.Walk(stage, owner);
            owner.Move(direction);
        }

        public override void End(Stage stage, Monster owner)
        {
            this.Walk(stage, owner);
            owner.StopMovement();
        }

        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            if (_chaser.Action.IsDead)
            {
                return new RewardGoblinIdleAIStrategy(_blackboard);
            }

            if (_moveDirection != null)
            {
                owner.Move(_moveDirection.Value);
            }

            var now = Time.time;
            if (_trackedChaserAt + CHASER_TRACKING_PERIOD > now )
            {
                return null;
            }

            var nextStrategy = this.TrackAndRunAwayFromChaser(stage, owner);
            return nextStrategy;
        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            // 보상을 드롭한다.
            if (_blackboard.DecreaseHitPointAndDropRewards(stage, owner.Pos) <= 0)
            {
                Log.I.Warn($"RewardGoblin의 보상 드롭 실패. 스테이지에서 발급할 수 있는 수량을 넘어서 지급하지 못했습니다. StageNumber[{stage.StaticData.StageNumber}] [{_blackboard.GoblinType}]");
            }

            if (_blackboard.LeftHitPoint <= 0)
            {
                owner.ForceKillSelf(stage);
                return null;
            }
            
            // 맞으면, 일정시간동안 좀 더 빨리 도망간다. (걷지않고 뛴다)
            this.Run(stage, owner);

            var chasingVector = (owner.Pos - _chaser.Pos);
            this.ChangeRunawayDirection(chasingVector);

            owner.StopMovement();
            owner.Move(chasingVector);
            return null;
        }

        private void StartToRunAway(Stage stage, Monster owner, Vector2 direction)
        {
            _movingStartedAt = Time.time;
            _recoveringStartedAt = null;
            _moveDirection = direction;

            this.Walk(stage, owner);
        }

        private void ChangeRunawayDirection(Vector2 direction)
        {
            _moveDirection = direction;
        }

        private void StartToRecover(Stage stage, Monster owner)
        {
            _movingStartedAt = null;
            _recoveringStartedAt = Time.time;
            _moveDirection = null;
            _isRunning = false;

            this.Walk(stage, owner);
            owner.StopMovement();
            owner.Action.ChangeTo(stage, new IdleAction(owner.AnimationController));

            var animationController = owner.AnimationController as SpineMonsterAnimationController;
            if (animationController != null)
            {
                var drainedAnimation = animationController.FindAnimation("drained");
                animationController.SetAnimation(BodyAnimationTrack.WholeBody, drainedAnimation, loop: true);
            }
        }

        private void Run(Stage stage, Monster owner)
        {
            if (_isRunning)
            {
                return;
            }

            _isRunning = true;
            owner.Stats.MoveSpeed.AddModifier(RUNNING_SPEED_ADD_MODIFIER);

            var animationController = owner.AnimationController as SpineMonsterAnimationController;
            if (animationController != null)
            {
                var animation = animationController.FindAnimation("run");
                if (animation != null)
                {
                    animationController.ChangeMoveAnimation(animation);
                }
            }
        }

        private void Walk(Stage stage, Monster owner)
        {
            owner.Stats.MoveSpeed.RemoveModifier(RUNNING_SPEED_ADD_MODIFIER);

            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;

            var animationController = owner.AnimationController as SpineMonsterAnimationController;
            if (animationController != null)
            {
                var animation = animationController.FindAnimation("move");
                if (animation != null)
                {
                    animationController.ChangeMoveAnimation(animation);
                }
            }
        }

        private MonsterAIStrategyBase TrackAndRunAwayFromChaser(Stage stage, Monster owner)
        {
            var now = Time.time;
            _trackedChaserAt = now;

            var chasingVector = (owner.Pos - _chaser.Pos);
            // 충분히 도망가서 멀어졋으면 Idle로 
            var squaredDistance = chasingVector.sqrMagnitude;
            if (squaredDistance >= SQUARED_SAFE_DISTANCE_FROM_CHASER)
            {
                return new RewardGoblinIdleAIStrategy(_blackboard);
            }

            if (_movingStartedAt == null &&
                _recoveringStartedAt == null)
            {
                this.StartToRunAway(stage, owner, chasingVector);
            }
            else
            {
                if (_movingStartedAt != null)
                {
                    if (_movingStartedAt + MOVING_DURATION <= now)
                    {
                        // 충분히 뛰었다. 이제 쉰다.
                        this.StartToRecover(stage, owner);
                    }
                    else
                    {
                        // 안전거리 1/3 만큼 가까워졌으면 더 빨리 뛴다.
                        if (squaredDistance <= (SQUARED_SAFE_DISTANCE_FROM_CHASER / 9f))
                        {
                            this.Run(stage, owner);
                        }

                        this.ChangeRunawayDirection(chasingVector);
                    }
                }
                
                if ((_recoveringStartedAt != null) &&
                    ((_recoveringStartedAt + RECOVERING_DURATION) <= now))
                {
                    // 충분히 쉬었다. 이제 뛴다.
                    this.StartToRunAway(stage, owner, chasingVector);
                }
            }
           
            return null;
        }

    }

}

