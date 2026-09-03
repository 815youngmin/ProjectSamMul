using Shared.StaticDatas;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class AmbushMonsterSkill : SkillBase
    {
        private float _attackPowerRate;
        private float _areaEffectRadius;
        private int _ambushMonsterObjectTotalAmount;
        private int _leftAmbushMonsterObjectAmount;
        private float _ambushMonsterObjectCreatePeriod;
        private float _lastAmbushMonsterObjectCreateAt;
        private float _playerAttackRangeDistanceRatio;
        private float _targetSearchRadius;

        private readonly IReadOnlyCharacterStatCalculators _characterStats;

        private float _ambushMonsterObjectcreatingAt => _lastAmbushMonsterObjectCreateAt + _ambushMonsterObjectCreatePeriod;

        public override float Duration => base.Duration / _characterStats.SkillAttackSpeedValue;
        public override float Cooltime => base.Cooltime / _characterStats.SkillAttackSpeedValue;

        public AmbushMonsterSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats) : base(staticData)
        {
            _attackPowerRate = staticData.Parameter1;
            _ambushMonsterObjectTotalAmount = (int)staticData.Parameter2;
            _areaEffectRadius = staticData.Parameter3;
            _targetSearchRadius = staticData.Parameter4;

            _characterStats = characterStats;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
           
            _leftAmbushMonsterObjectAmount = _ambushMonsterObjectTotalAmount;
            _playerAttackRangeDistanceRatio = ((PlayerCharacter)owner).Stats.AttackRangeDistanceRatio.Value;
            _ambushMonsterObjectCreatePeriod = Duration / _ambushMonsterObjectTotalAmount;

        }
        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if (_leftAmbushMonsterObjectAmount <= 0)
            {
                return;
            }

            if(_ambushMonsterObjectcreatingAt <= now)
            {
                this.CreateAmbushMonsterObjectAndInitialize(owner, stage);
                _lastAmbushMonsterObjectCreateAt = now;
                _leftAmbushMonsterObjectAmount--;
            }
        }

        private void CreateAmbushMonsterObjectAndInitialize(PlayerCharacter owner, Stage stage)
        {
            Vector2 hitPoint;
            int targetIndex;
            List<Character> targetMonsters = new List<Character>();

            stage.FindAliveCharactersInArea(owner.Alliance.ToEnemyAlliance(), new CircularTargetArea(owner.CenterPos, radius: _targetSearchRadius), result: targetMonsters);

            //몬스터 → 아이템 박스 → 랜덤 순으로 탐색
            //타겟은 주변 범위 내의 몬스터들중 랜덤으로 지정된다.
            if (targetMonsters.Count <= 0)
            {
                var item = owner.FindClosestBreakableItemObjectExceptFence(stage, _targetSearchRadius);
                if (item == null)
                {
                    hitPoint = Random.insideUnitCircle * _targetSearchRadius + owner.CenterPos;
                }
                else
                {
                    hitPoint = (Vector2)item.transform.position;
                }
            }
            else
            {
                targetIndex =  Random.Range(0, targetMonsters.Count);
                hitPoint = targetMonsters[targetIndex].CenterPos;
            }

            float damage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, _attackPowerRate);
            float areaEffectRadius = _areaEffectRadius * _playerAttackRangeDistanceRatio;
            Vector3 ambushMonsterObjectScale = Vector3.one * _playerAttackRangeDistanceRatio;
            PlaySkillSoundEffect(hitPoint);
            var ambushMonsterObject = stage.CreateAmbushMonsterObject(
                owner.Alliance,
                owner,
                areaEffectRadius,
                hitPoint,
                damage,
                this.IsTranscendent,
                StaticData.SkillHitSFXPath);
            ambushMonsterObject.transform.localScale = ambushMonsterObjectScale;
        }
    }
}
