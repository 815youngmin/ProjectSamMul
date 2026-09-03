using DG.Tweening;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using System.Collections.Generic;
using UnityEngine;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.SummonOnDieAIs
{
    public abstract class SummonOnDieMonsterAIStrategyBase : MonsterAIStrategyBase
    {
        private readonly static string SummonEffectPrefabPath = "Stages/ETCEffects/monsterWarning.prefab";
        private readonly static float SummonEffectDuration = 1.0f;

        protected SummonOnDieMonsterAIStrategyBase() 
            : base()
        {
        }
        protected SummonOnDieMonsterAIStrategyBase(MonsterAIBlackboardBase blackboard) 
            :base(blackboard) 
        {
        }

        protected void Summon(Stage stage, Monster owner)
        {
            int summonAmount = (int)owner.StaticData.Param3;
            List<Vector2> summonPositions = new List<Vector2>();
            CharacterType summonMonsterType = owner.StaticData.SpawnMonsterType;
            float summonMonsterRadius = StaticDataRepository.Instance.Monsters.Get(summonMonsterType).ColliderRadius;

            //스탯 가중치를 역산해서 계산
            float hpWeight = owner.MaxHP / owner.StaticData.MaxHP;
            float atkWeight = owner.CollisionAttackPower / owner.StaticData.CollisionAttackPower;

            for (int i = 0; i < summonAmount - 1; i++)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 360f / (summonAmount - 1) * i) * Vector2.up;
                summonPositions.Add(owner.CenterPos + dir * summonMonsterRadius * 3f);
            }
            summonPositions.Add(owner.CenterPos);

            var summonSequence = DOTween.Sequence();
            summonSequence.AppendCallback(() =>
            {
                float spawnPositionEffectScale = 1 / 2.395f * 1.5f;
                for (int i = 0; i < summonAmount; i++)
                {
                    UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(SummonEffectPrefabPath, summonPositions[i], Vector2.one * spawnPositionEffectScale, null);
                }
            });
            summonSequence.AppendInterval(SummonEffectDuration);
            summonSequence.AppendCallback(() =>
            {
                for (int i = 0; i < summonPositions.Count; i++)
                {
                    stage.CreateMonster(owner.Alliance, summonMonsterType,
                    MonsterInstanceInitialData.CreateForStageMonster(summonPositions[i],
                    hpWeight,
                    atkWeight,
                    dropExp: 0,
                    dropGolds: 0,
                    dropItems: new List<DropItemType>()),
                    isBoss: false,
                    isElite: false);
                }
            });
        }
    }
}
