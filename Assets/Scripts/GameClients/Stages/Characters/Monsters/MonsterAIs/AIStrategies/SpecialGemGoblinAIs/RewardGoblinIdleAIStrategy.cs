using Shared.GameDataTypes;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Actions;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.Loggers;

namespace SamMul.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.SpecialGemGoblinAIs
{
    public class RewardGoblinIdleAIStrategy : MonsterAIStrategyBase
    {
        public new RewardGoblinAIBlackboard _blackboard => (RewardGoblinAIBlackboard)base._blackboard!;

        // 추격자를 마지막으로 검사한 시각
        private float _searchedAt;
        private static readonly float SEARCHING_PERIOD = 0.1f;
        private static readonly float SEARCHING_DISTANCE = 5f;

        private Vector2 _patrolDirection = Vector2.right;
        private float? _patrolEndAt = null!;
        private float? _jougleEndAt = null!;

        public RewardGoblinIdleAIStrategy(RewardGoblinAIBlackboard blackboard): base(blackboard) { }

        public override void Begin(Stage stage, Monster owner)
        {
            _searchedAt = 0f;

            string skinName = owner.CharacterType switch
            {
                CharacterType.Special_Gem_Goblin => "jewel",
                CharacterType.Special_Gold_Goblin => "gold",
                _ => "gold",
            };

            var animationController = owner.AnimationController as SpineMonsterAnimationController;
            if (animationController != null)
            {
                animationController?.Body.Skeleton.SetSkin(skinName);
            }

            _patrolEndAt = Time.time + 2f;
            _patrolDirection = Vector2.left;
        }

        private void PlayJuggling(Stage stage, Monster owner)
        {
            owner.Action.ChangeTo(stage, new IdleAction(owner.AnimationController));

            var animationController = owner.AnimationController as SpineMonsterAnimationController;
            if (animationController != null)
            {
                var juggleAnimation = animationController.FindAnimation("Aggro");
                animationController.SetAnimation(BodyAnimationTrack.WholeBody, juggleAnimation, loop: true);
            }
        }

        public override void End(Stage stage, Monster owner)
        {
            
        }


        public override MonsterAIStrategyBase Update(Stage stage, Monster owner)
        {
            float now = Time.time;

            if (_searchedAt < now)
            {
                _searchedAt = now + SEARCHING_PERIOD;

                var chaser = stage.FindClosestCharacter(
                    owner.Alliance.ToEnemyAlliance(),
                    owner.Pos,
                    limitDistance: SEARCHING_DISTANCE,
                    condition: character => !character.Action.IsDead);

                if (chaser != null)
                {
                    return new RewardGoblinRunAwayAIStrategy(chaser, _blackboard);
                }
            }

            // 주변에 적이 없으면 좌우로 천천히 어슬렁어슬렁 한다.
            // 일정시간 걸어갔다가, 일정시간 같은자리에서 춤추고. (반복)

            if (_patrolEndAt != null)
            {
                if (_patrolEndAt.Value <= now)
                {
                    _patrolEndAt = null;
                    _jougleEndAt = now + 3f;
                    _patrolDirection = _patrolDirection == Vector2.right ?
                        Vector2.left : Vector2.right;

                    PlayJuggling(stage, owner);
                }
                else
                {
                    owner.Move(_patrolDirection);
                }
            }

            if (_jougleEndAt != null)
            {
                if (_jougleEndAt.Value <= now)
                {
                    _jougleEndAt = null;
                    _patrolEndAt = now + 1.66f;
                }
            }

            return null;
        }

        public override MonsterAIStrategyBase OnHitted(Stage stage, Monster owner, Character attacker)
        {
            if (null == attacker)
            {
                return null;
            }

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

            return new RewardGoblinRunAwayAIStrategy(chaser: attacker, _blackboard);
        }
	}
    
}
