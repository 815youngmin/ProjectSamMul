using DG.Tweening;
using Shared.GameDataTypes;
using System;
using UnityEngine;
using Z.Scenes;
using Z.UIs.Stages.HUDs;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.SpecialGemGoblinAIs
{
    public class RewardGoblinAIBlackboard : MonsterAIBlackboardBase
    {
        public readonly RewardGoblinType GoblinType;

        public int MaxHpCount { get; private set; }
        public int LeftHitPoint { get; private set; }

        private readonly int _hitRewardMin;
        private readonly int _hitRewardMax;
        private readonly int _deadRewardMin;
        private readonly int _deadRewardMax;

        private float _lastHittedAt;

		private NavigationArrow _navigationArrow;
        
        public static readonly string GEM_GOBLIN_NAVIGATION_ARROW_PATH = "Stages/UIs/HUDs/RewardGoblinNavigationArrow/GemGoblinNavigationArrow.prefab";
        public static readonly string GOLD_GOBLIN_NAVIGATION_ARROW_PATH = "Stages/UIs/HUDs/RewardGoblinNavigationArrow/GoldGoblinNavigationArrow.prefab";

        public RewardGoblinAIBlackboard(RewardGoblinType goblinType, int maxHpCount, int hitRewardMin, int hitRewardMax, int deadRewardMin, int deadRewardMax)
        {
            this.GoblinType = goblinType;

            this.MaxHpCount = maxHpCount;
            this.LeftHitPoint = maxHpCount;

            _hitRewardMin = hitRewardMin;
            _hitRewardMax = hitRewardMax;

            _deadRewardMin = deadRewardMin;
            _deadRewardMax = deadRewardMax;
            
            _lastHittedAt = 0f;
        }

		public override void OnEnterredIntoStage(Stage stage, Monster owner)
        {
            base.OnEnterredIntoStage(stage, owner);

            var navigationArrowPath = GoblinType switch
            {
                RewardGoblinType.Gem => GEM_GOBLIN_NAVIGATION_ARROW_PATH,
                RewardGoblinType.Gold => GOLD_GOBLIN_NAVIGATION_ARROW_PATH,
				_ => throw new NotImplementedException($"RewardGoblinType {GoblinType} is NOT implemented.")
			};

			var stageSceneUIRoot = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
			_navigationArrow = stageSceneUIRoot.AddNavigationArrow(stage, owner.transform, showAlways: false, navigationArrowPath, string.Empty);
            _navigationArrow.Show = false;

			DOTween.Sequence(_navigationArrow)
                .AppendInterval(2.5f)
                .AppendCallback(() =>
				{
					_navigationArrow.Show = true;
				});

            owner.AddEmphasisCircle(EmphasisCircle.Color.Blue);
            owner.AddMonsterNameDisplayer(MonsterNameDisplayer.Color.Blue);
        }

        /// <summary>
        /// 보석고블린은 기본 체력을 999999로 설정해두고 (안죽게)
        /// AI에서 히트 판정에 따라 타격 횟수만큼 맞으면 죽도록 처리한다.
        /// </summary>
        public long DecreaseHitPointAndDropRewards(Stage stage, Vector2 ownerPosition)
		{
			var now = Time.time;
			// 마지막 타격 받은 시점으로부터 0.25초 내에는 타격 인정안해줌
			if (_lastHittedAt + 1.0f > now && LeftHitPoint <= 0)
			{
				return 0;
            }

            _lastHittedAt = now;

            --LeftHitPoint;

			if (LeftHitPoint <= 0)
			{
                if (_navigationArrow != null)
                {
                    DOTween.Kill(_navigationArrow);
                    _navigationArrow.RemoveNavigationArrow();
                    _navigationArrow = null;
                }

				return DropRewardsOnDead(stage, ownerPosition);
            }

            return DropRewardsOnHitted(stage, ownerPosition);
        }

        private long DropRewardsOnHitted(Stage stage, Vector2 ownerPosition)
        {
            int requestAmount = UnityEngine.Random.Range(minInclusive: _hitRewardMin, maxExclusive: _hitRewardMax + 1);

            return this.DropRewards(stage, ownerPosition, requestAmount);
        }

        private long DropRewardsOnDead(Stage stage, Vector2 ownerPosition)
        {
            int requestAmount = UnityEngine.Random.Range(minInclusive: _deadRewardMin, maxExclusive: _deadRewardMax + 1);
            
            return this.DropRewards(stage, ownerPosition, requestAmount);
        }

        private long DropRewards(Stage stage, Vector2 ownerPosition, long requestedAmount)
        {
            if (this.GoblinType == RewardGoblinType.Gem)
            {
                long gemsToDrop = stage.TakeMonsterDropGems(requestedAmount);
                for (long i = 0; i < gemsToDrop; ++i)
                {
                    // 최소 획득 거리가 1.5 유닛이기 때문에, 캐릭터가 설 수 있는 곳에서 1.5유닛 내에 드롭해야 한다. (추가배치한 콜라이더 위에 떨어졌을 때 먹지 못하는 경우를 방지하기 위함)
                    stage.CreateGemObject(amount: 1, ownerPosition + new Vector2(UnityEngine.Random.Range(-1.40f, 1.40f), UnityEngine.Random.Range(-1.40f, 1.40f)));
                }
                return gemsToDrop;
            }
            else if (this.GoblinType == RewardGoblinType.Gold)
            {
                long goldsToDrop = stage.TakeRewardMonsterDropGolds(requestedAmount);
                if (goldsToDrop > 0)
                {
                    stage.CreateGoldObject(amount: goldsToDrop, ownerPosition + new Vector2(UnityEngine.Random.Range(-1.40f, 1.40f), UnityEngine.Random.Range(-1.40f, 1.40f)));
                }
                return goldsToDrop;
            }
            else
            {
                throw new NotImplementedException($"{this.GoblinType} 구현 안 됨");
            }

        }
    }

}
