#nullable enable
using Shared.StaticDatas;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.AreaEffectObjects;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    // 텐티의 기본 스킬. 텐티를 성장시키고, 촉수를 사용한 기본공격에 배리에이션을 준다.
    public class TentiSweepSkill : SkillBase
    {
        private readonly float _attackPowerRate;
        private readonly StatModifier _attackSpeedIncreaser;
        private readonly int _tentiSweepCount;
        private readonly float _knockBackPower;
        private readonly float _attackEffectiveRange;
        private readonly List<(float Angle, Vector2 Direction)> _attackDirections;
        private readonly IReadOnlyCharacterStatCalculators _characterStats;
        private float _lastAttackedAt;

        // TentiSweepDamageUpAndSizeDown 등급 효과
        // 아래 형식대로 구현되도록 되어야 합니다.
        // 대미지 = {기본 데미지 + a + b + c} x {0}
        // 공격범위 = (기본 범위 x {1}) x a x b x c
        private readonly float _increaseTentiSweepAttackRangeRatio;
        private readonly float _increaseTentiSweepDamagePercent;
        private readonly float _decreaseTentiSweepSizePercent;

        // HpAbsolbingTenticle 등급 효과
        private readonly float _gradeEffectAttackDamagePercent;
        private readonly float _gradeEffectAttackHPDrainPercent;
        private readonly float _gradeEffectAttackPeriod;
        private readonly List<Character> _gradeEffectAttackTargets;
        private float _gradeEffectAttackLastAttackedAt;

        // ActivateSpawnTentacleTentiSweep 변장 효과
        private readonly int _spawnTentacleAmount;
        private readonly float _spawnTentacleDamagePercent;
        private readonly HashSet<Character> _spawnTentacleHittedCharacters;
        private int _spawnTentacleCurrentCount;
        private float _spawnTentacleAt;

        public TentiSweepSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats, IReadOnlyCustomParameters parameters) : base(staticData)
        {
            _attackPowerRate = staticData.Parameter1;
            _attackSpeedIncreaser = new StatModifier(staticData.Parameter2, StatModType.Flat);
            _tentiSweepCount = (int)staticData.Parameter3;
            _knockBackPower = staticData.Parameter4;
            _attackEffectiveRange = staticData.Parameter5;
            _attackDirections = new List<(float, Vector2)>();
            _characterStats = characterStats;
            _lastAttackedAt = 0.0f;

            _increaseTentiSweepAttackRangeRatio = parameters.GetParameterValue(CustomParameterType.TentiSweepAttackRangeRatio);
            _increaseTentiSweepDamagePercent = parameters.GetParameterValue(CustomParameterType.IncreaseTentiSweepDamagePercent);
            _decreaseTentiSweepSizePercent = parameters.GetParameterValue(CustomParameterType.DecreaseTentiSweepSizePercent);

            _gradeEffectAttackDamagePercent = parameters.GetParameterValue(CustomParameterType.HpAbsolbingTenticle_DamagePercent);
            _gradeEffectAttackHPDrainPercent = parameters.GetParameterValue(CustomParameterType.HpAbsolbingTenticle_HPDrainAmountPercent);
            _gradeEffectAttackPeriod = parameters.GetParameterValue(CustomParameterType.HpAbsolbingTenticle_AttackPeriod);
            _gradeEffectAttackTargets = new List<Character>();
            _gradeEffectAttackLastAttackedAt = 0.0f;

            _spawnTentacleAmount = (int)parameters.GetParameterValue(CustomParameterType.SpawnTentacleAmount);
            _spawnTentacleDamagePercent = parameters.GetParameterValue(CustomParameterType.SpawnTentacleDamagePercent);
            _spawnTentacleHittedCharacters = new HashSet<Character>();
            _spawnTentacleCurrentCount = 0;
            _spawnTentacleAt = 0.0f;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            owner.Stats.CharacterAttackSpeed.AddModifier(_attackSpeedIncreaser);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_attackSpeedIncreaser);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if (owner.Action.IsDead)
            {
                return;
            }

            this.UpdateBasicAttack(owner, stage, now);

            if (_gradeEffectAttackPeriod > 0)
            {
                this.UpdateGradeEffectAttack(owner, stage, now);
            }

            if (_spawnTentacleAmount > 0)
            {
                this.UpdateSpawnTentacle(owner, stage, now);
            }
        }

        private void UpdateBasicAttack(PlayerCharacter owner, Stage stage, float now)
        {
            const float BASIC_ATTACI_DIRECTION_GAP = 20.0f;
            const float TRANSCENDENT_ATTACK_DIRECTION_GAP = 30.0f;

            // 공격 속도 영향 받도록 처리
            float rangeAttackDuration = 1.0f / _characterStats.CharacterAttackSpeedValue;
            if (now < _lastAttackedAt + rangeAttackDuration)
            {
                return;
            }

            float searchRadius = this.GetMaxAttackDistance(owner) - 0.5f;
            var target = owner.FindBasicAttackTarget(stage, searchRadius);

            // 몬스터 타겟이 존재하지 않으면 아이템을 타겟으로 잡는다.
            Vector2 attackDirection;
            if (target == null)
            {
                var item = owner.FindClosestBreakableItemObjectExceptFence(stage, searchRadius);
                if (item == null)
                {
                    return;
                }
                attackDirection = (Vector2)item.transform.position - owner.Pos;
            }
            else
            {
                attackDirection = target.Pos - owner.Pos;
            }

            var startAngle = Vector2.SignedAngle(Vector2.right, attackDirection) - ((_tentiSweepCount - 1) % 3) * (BASIC_ATTACI_DIRECTION_GAP * 0.5f);
            float attackDirectionGap = IsTranscendent ? TRANSCENDENT_ATTACK_DIRECTION_GAP : BASIC_ATTACI_DIRECTION_GAP;

            _attackDirections.Clear();
            for (int i = 0; i < _tentiSweepCount; ++i)
            {
                var angle = (i < 3)
                    ? startAngle + attackDirectionGap * i
                    : startAngle + 180 + attackDirectionGap * (i % 3);
                var direction = new Vector2(Mathf.Cos(Mathf.Deg2Rad * angle), Mathf.Sin(Mathf.Deg2Rad * angle));
                _attackDirections.Add((angle, direction));
            }

            float damage = this.GetDefaultAttackDamage(owner);
            float areaRatio = this.GetDefaultAreaRatio(owner, IsTranscendent);
            float knockbackPower = CombatSystem.CalculateCharacterAttackKnockBackPower(owner.Stats, _knockBackPower);
            float stunEnemyEvery10thTentiSweepAttackStunDuration = owner.CustomParameters.GetParameterValue(CustomParameterType.StunEnemyEvery10thTentiSweepAttack_StunDuration);
            if (IsTranscendent)
            {
                stage.CreateTentiSweepTranscendentObject(
                    owner.Alliance,
                    owner,
                    _attackDirections[1].Direction,
                    damage,
                    areaRatio,
                    knockbackPower,
                    _attackDirections,
                    stunEnemyEvery10thTentiSweepAttackStunDuration,
                    StaticData.SkillHitSFXPath);
                this.PlaySkillSoundEffect(owner.Pos);
            }
            else
            {
                foreach (var (_, direction) in _attackDirections)
                {
                    stage.CreateTentiSweepObject(
                        owner.Alliance,
                        owner,
                        direction,
                        damage,
                        areaRatio,
                        knockbackPower,
                        stunEnemyEvery10thTentiSweepAttackStunDuration,
                        StaticData.SkillHitSFXPath);
                }
                this.PlaySkillSoundEffect(owner.Pos);
            }

            owner.ConditionalEffects.OnBasicSkillUsed(stage, owner);
            _lastAttackedAt = now;
        }

        private void UpdateGradeEffectAttack(PlayerCharacter owner, Stage stage, float now)
        {
            const float TARGET_SEARCH_RADIUS = 8.0f;

            if (now < _gradeEffectAttackLastAttackedAt + _gradeEffectAttackPeriod)
            {
                return;
            }

            _gradeEffectAttackTargets.Clear();
            stage.FindAliveCharactersInArea(owner.Alliance.ToEnemyAlliance(), new CircularTargetArea(owner.CenterPos, radius: TARGET_SEARCH_RADIUS), result: _gradeEffectAttackTargets);

            Vector2 hitPoint;
            if (_gradeEffectAttackTargets.Count <= 0)
            {
                // 타겟이 없을 때는 타겟 검색 반지름 범위 내에 랜덤 위치로 지정된다.
                hitPoint = owner.CenterPos + Random.insideUnitCircle * TARGET_SEARCH_RADIUS;
            }
            else
            {
                // 타겟은 주변 범위 내의 몬스터들 중 랜덤으로 지정된다.
                int targetIndex = Random.Range(0, _gradeEffectAttackTargets.Count);
                hitPoint = _gradeEffectAttackTargets[targetIndex].CenterPos + new Vector2(0, -2f);
            }

            float damage = _gradeEffectAttackDamagePercent * this.GetDefaultAttackDamage(owner);
            float areaRatio = this.GetDefaultAreaRatio(owner, isTranscendent: false);
            stage.CreateTentiSweepVerticalObject(owner, hitPoint, damage, areaRatio, _knockBackPower, _gradeEffectAttackHPDrainPercent, StaticData.SkillHitSFXPath);
            this.PlaySkillSoundEffect(owner.Pos);

            _gradeEffectAttackLastAttackedAt = now;
        }

        private void UpdateSpawnTentacle(PlayerCharacter owner, Stage stage, float now)
        {
            const float SPAWN_TENTACLE_ATTACK_SEARCH_RADIUS = 5.0f;
            const float SPAWN_TENTACLE_ATTACK_INTERVAL = 0.2f;
            const float SPAWN_TENTACLE_ATTACK_PERIOD = 4.0f;

            if (now < _spawnTentacleAt)
            {
                return;
            }

            // 1차 공격 범위 내에 공격하지 않은 타겟 위주로 검색
            var target = stage.FindClosestCharacter(
                owner.Alliance.ToEnemyAlliance(),
                owner.CenterPos,
                limitDistance: SPAWN_TENTACLE_ATTACK_SEARCH_RADIUS,
                condition: character => !character.Action.IsDead && !_spawnTentacleHittedCharacters.Contains(character));

            // 2차 타겟 없으면 공격했던 타겟도 포함해서 재검색
            if (target == null)
            {
                target = stage.FindClosestCharacter(
                    owner.Alliance.ToEnemyAlliance(),
                    owner.CenterPos,
                    limitDistance: SPAWN_TENTACLE_ATTACK_SEARCH_RADIUS,
                    condition: character => !character.Action.IsDead);
            }

            Vector2 hitPoint;
            if (target == null)
            {
                // 탐색 범위 내에 몬스터가 없는 경우 랜덤으로 처리
                hitPoint = owner.CenterPos + Random.insideUnitCircle * SPAWN_TENTACLE_ATTACK_SEARCH_RADIUS;
            }
            else
            {
                hitPoint = target.CenterPos + new Vector2(0, -2f);
                _spawnTentacleHittedCharacters.Add(target);
            }

            float damage = _spawnTentacleDamagePercent * this.GetDefaultAttackDamage(owner);
            float areaRatio = this.GetDefaultAreaRatio(owner, isTranscendent: false);
            stage.CreateTentiSweepVerticalObject(owner, hitPoint, damage, areaRatio, knockBackPower: 0.0f, hpDrainPercent: 0.0f, null);

            if (_spawnTentacleCurrentCount < _spawnTentacleAmount)
            {
                ++_spawnTentacleCurrentCount;
                _spawnTentacleAt = now + SPAWN_TENTACLE_ATTACK_INTERVAL;

            }
            else
            {
                _spawnTentacleCurrentCount = 0;
                _spawnTentacleAt = now + SPAWN_TENTACLE_ATTACK_PERIOD;
                _spawnTentacleHittedCharacters.Clear();
            }
        }

        private float GetDefaultAttackDamage(PlayerCharacter owner)
        {
            float attackDamage = CombatSystem.CalculateCharacterBasicAttackDamage(owner.Stats, _attackPowerRate);
            return (1.0f + _increaseTentiSweepDamagePercent) * attackDamage;
        }

        private float GetMaxAttackDistance(PlayerCharacter owner)
        {
            return _increaseTentiSweepAttackRangeRatio * (1.0f - _decreaseTentiSweepSizePercent) * _attackEffectiveRange * owner.Stats.AttackRangeDistanceRatio.Value;
        }

        private float GetDefaultAreaRatio(PlayerCharacter owner, bool isTranscendent)
        {
            float maxAttackDistance = this.GetMaxAttackDistance(owner);
            return maxAttackDistance / (isTranscendent ? TentiSweepTranscandentObject.BaseAttackAreaWidth : TentiSweepAreaEffectObject.BaseAttackAreaWidth);
        }
    }
}
