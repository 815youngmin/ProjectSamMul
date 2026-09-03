using Shared.DataTables;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.Loggers;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public sealed class SkillSet
    {
        public string AcquiredSkills => string.Join(", ", _skills.Values.Select(x => $"({x.Id} {x.Level})"));

        private static readonly int MAX_ACTIVE_SKILLS = 5;
        private static readonly int MAX_PASSIVE_SKILLS = 5;

        // 캐릭터가 현재 습득한 스킬들
        private readonly Dictionary<SkillId, SkillBase> _skills;
        public int NumberOfAcquiredSkills => _skills.Count;
        public int NumberOfTranscendentSkills => _skills.Values.Count(x => x.IsTranscendent);
        public int NumberOfMaxLevelSkills => _skills.Values.Count(x => x.Level >= GameConstants.SKILL_MAX_LEVEL);

        /// <summary>
        /// 캐릭터가 현재 습득한 스킬이 <see cref="MaxSkills"/>를 넘길 수 없다.
        /// </summary>
        public bool IsSlotFull => NumberOfAcquiredSkills >= MaxSkills;
        public bool IsActiveSlotFull => _maxActiveSkills <= _skills.Count(x => x.Value.SkillType == SkillType.Active);
        public bool IsPassiveSlotFull => _maxPassiveSkills <= _skills.Count(x => x.Value.SkillType == SkillType.Passive);

        private int MaxSkills => _maxActiveSkills + _maxPassiveSkills;
        private int _maxActiveSkills;
        private int _maxPassiveSkills;

        public SkillSet()
        {
            _skills = new Dictionary<SkillId, SkillBase>();
            _maxActiveSkills = 0;
            _maxPassiveSkills = 0;
        }

        public void Initialize(PlayerCharacter owner, Stage stage)
        {
            _skills.Clear();
            _maxActiveSkills = MAX_ACTIVE_SKILLS;
            _maxPassiveSkills = MAX_PASSIVE_SKILLS;
            this.AcquireOrUpgradeSkill(owner.StaticData.BasicSkill, owner, stage);
        }

        public void ClearBeforeExitFromStage(PlayerCharacter owner, Stage stage)
        {
            float now = Time.time;
            foreach (var skill in _skills.Values)
            {
                if (skill.IsActivated)
                {
                    skill.Deactivate(owner, stage, now);
                }
            }
            _skills.Clear();
        }

        public void Clear(Character owner)
        {
            _skills.Clear();
        }

        public bool HasSkill(SkillId skillId)
        {
            return _skills.ContainsKey(skillId);
        }

        // 최대레벨 달성했거나, 그 이상 초월했거나
        public bool IsMaxLevelOrTranscended(SkillId skillId)
        {
            if (!_skills.TryGetValue(skillId, out var skill))
            {
                return false;
            }

            return skill.Level >= GameConstants.SKILL_MAX_LEVEL;
        }

        // 지정된 스킬이 초월 가능한지 여부. 
        // 이미 초월했거나, 초월조건을 달성 못했으면 false임
        public bool IsAbleToTranscend(SkillId skillId)
        {
            if (!_skills.TryGetValue(skillId, out var skill))
            {
                return false;
            }

            if (skill.Level != GameConstants.SKILL_MAX_LEVEL)
            {
                return false;
            }

            if (!skill.HasTranscendCondition)
            {
                return false;
            }

            // 초월(돌파조합)을 위해 필요한 스킬을 보유하고 있어야만 초월 가능하다.
            if (!_skills.ContainsKey(skill.TranscendCondition))
            {
                return false;
            }

            return true;
        }

        public bool IsAcquirableSkill(SkillId skillId)
        {
            if (HasSkill(skillId))
            {
                if (!IsMaxLevelOrTranscended(skillId) || IsAbleToTranscend(skillId))
                {
                    return true;
                }

                return false;
            }

            SkillStaticData staticData = StaticDataRepository.Instance.Skills.Get(new SkillKey(skillId, 1));
            switch (staticData.skillType)
            {
                case SkillType.Active:
                    return !IsActiveSlotFull;
                case SkillType.Passive:
                    return !IsPassiveSlotFull;
            }

            return false;
        }

        public int GetSkillLevel(SkillId skillId)
        {
            if (!_skills.TryGetValue(skillId, out var skill))
            {
                return 0;
            }

            return skill.Level;
        }

        public void AcquireOrUpgradeSkill(SkillId skillId, PlayerCharacter owner, Stage stage)
        {
            if (!this.HasSkill(skillId))
            {
                this.AcquireSkill(skillId, skillLevel: 1, owner, stage);
                return;
            }

            int skillLevel = this.GetSkillLevel(skillId);
            if (!this.IsMaxLevelOrTranscended(skillId))
            {
                this.AcquireSkill(skillId, skillLevel + 1, owner, stage);
                return;
			}

			if (!this.IsAbleToTranscend(skillId))
			{
				Log.I.Error($"초월 불가능한데 스킬 초월하려 했음. 요청된 스킬 [{skillId}] 현재레벨 [{skillLevel}]");
				return;
			}

			this.AcquireSkill(skillId, skillLevel + 1, owner, stage);
		}

		private void AcquireSkill(SkillId skillId, int skillLevel, PlayerCharacter owner, Stage stage)
        {
            // SkillLevel은 1부터 시작한다. 0 아님
            Debug.Assert(skillLevel > 0);
            Debug.Assert(skillLevel <= GameConstants.SKILL_TRANSCENDENT_LEVEL);
            if (skillLevel > 1)
            {
                if (!_skills.TryGetValue(skillId, out var previousSkill))
                {
                    Debug.LogError($"{skillId}를 습득한 적이 없는데 {skillLevel}레벨 스킬을 획득하려 함. 코드 수정해주세요.");
                    return;
                }

                if (previousSkill.Level != skillLevel - 1)
                {
                    Debug.LogError($"{skillId}의 레벨이 {previousSkill.Level}인데, {skillLevel}레벨 스킬을 획득하려 함. 무시합니다. 코드 수정해주세요.");
                    return;
                }

                _skills.Remove(skillId);
                if (previousSkill.IsActivated)
                {
                    previousSkill.Deactivate(owner, stage, Time.time);
                }
            }

            if (this.NumberOfAcquiredSkills >= MaxSkills)
            {
                Log.I.Error($"스킬을 이미 {this.NumberOfAcquiredSkills}개 배웠는데, 추가로 다른 스킬을 배우려 했음. 요청된 스킬 [{skillId}] [{skillLevel}]");
                return;
            }

            var skill = CreateSkill(skillId, skillLevel, owner.Stats, owner.CustomParameters, owner.CustomParameters);
            _skills.Add(skillId, skill);

            if (!owner.Action.IsDead)
            {
                skill.Activate(owner, stage, Time.time);
            }

            Debug.Log($"스킬획득함 [{skillId}][{skillLevel}]");
        }

        public void Update(PlayerCharacter owner, Stage stage, float now)
        {
            foreach (var kvp in _skills)
            {
                var skill = kvp.Value;
                if (skill.IsActivated)
                {
                    if (skill.DeactivatingAt <= now)
                    {
                        skill.Deactivate(owner, stage, now);
                    }
                    else
                    {
                        skill.Update(owner, stage, now);
                    }
                }
                else
                {
                    if (skill.ActivatingAt <= now)
                    {
                        skill.Activate(owner, stage, now);
                    }
                }
            }
        }

        public void OnPlayerCharacterDead(PlayerCharacter owner, Stage stage, float now)
        {
            foreach (var (key, skill) in _skills)
            {
                skill.OnPlayerCharacterDead(owner, stage, now);
            }
        }

        public void OnPlayerCharacterResurrected(PlayerCharacter owner, Stage stage, float now)
        {
            foreach (var (key, skill) in _skills)
            {
                skill.OnPlayerCharacterResurrected(owner, stage, now);
            }
        }

        public void ReApplyStatsToAcquiredSkills(PlayerCharacterStatCalculators ownerStats)
        {
            foreach (var kvp in _skills)
            {
                var skill = kvp.Value;
                skill.ReApplyStat(ownerStats);
            }
        }

        public void AttackedEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            foreach (var kvp in _skills)
            {
                var skill = kvp.Value;
                skill.AttackedEnemy(stage, owner, enemy, damage);
            }
        }



        public SkillId[] GetAcquiredSkillIds()
        {
            return _skills.Keys.ToArray();
        }

        public SkillKey[] GetAcquiredSkillKeys()
        {
            List<SkillKey> keys = new List<SkillKey>(_skills.Count);
            foreach (var kvp in _skills)
            {
                keys.Add(new SkillKey(kvp.Key, kvp.Value.Level));
            }

            return keys.ToArray();
        }

        private static SkillBase CreateSkill(SkillId skillId, int skillLevel, IReadOnlyCharacterStatCalculators characterStats, IReadOnlyCustomParameters customParametersReader, ICustomParameterChanger customParamtersChanger)
        {
            var staticData = StaticDataRepository.Instance.Skills.Get(new SkillKey(skillId, skillLevel));
            return skillId switch
            {
                SkillId.Glutton => new GluttonSkill(staticData, characterStats),
                SkillId.SpinBlade => new SpinBladeSkill(staticData, characterStats),
                SkillId.AttackSpeedUp => new AttackSpeedUpSkill(staticData),
                SkillId.IncommingDamageDown => new IncommingDamageDownSkill(staticData),
                SkillId.DeathTouch => new DeathTouchSkill(staticData, characterStats),
                SkillId.AcquisitionDistanceUp => new AcquisitionDistanceUpSkill(staticData),
                SkillId.AmbushMonster => new AmbushMonsterSkill(staticData, characterStats),
                SkillId.DurationUp => new DurationUpSkill(staticData),
                SkillId.RangeUp => new RangeUpSkill(staticData),
                SkillId.HealOverTime => new HealOverTimeSkill(staticData),
                SkillId.ExpUp => new ExpUpSkill(staticData),
                SkillId.ProjectileSpeedUp => new ProjectileSpeedUpSkill(staticData),
                SkillId.GoldAmountUp => new GoldAmountUpSkill(staticData),
                SkillId.PlasmaDrill => new PlasmaDrillSkill(staticData, characterStats),
                SkillId.RuneTrap => new RuneTrapSkill(staticData, characterStats),
                SkillId.DefensiveField => new DefensiveFieldSkill(staticData, characterStats),
                SkillId.Meteor => new MeteorSkill(staticData, characterStats),
                SkillId.TentiSweep => new TentiSweepSkill(staticData, characterStats, customParametersReader),
                SkillId.IgnitionWave => new IgnitionWaveSkill(staticData, characterStats, customParametersReader),
                SkillId.AlphaGatling => new AlphaGatlingSkill(staticData, characterStats, customParametersReader),
                SkillId.BloodRapier => new BloodRapierSkill(staticData, characterStats, customParametersReader),
                SkillId.BattleYoYo => new BattleYoYoSkill(staticData, characterStats, customParametersReader),
                SkillId.SpaceCoin => new SpaceCoinSkill(staticData, characterStats, customParametersReader),
                SkillId.BubbleGum => new BubbleGumSkill(staticData, characterStats, customParametersReader),
                SkillId.ShockBomb => new ShockBombSkill(staticData, characterStats),
                SkillId.BouncingClaw => new BouncingClawSkill(staticData, characterStats),
                SkillId.ShootingStar => new ShootingStarSkill(staticData, characterStats),
                SkillId.IronFist => new IronFistSkill(staticData, characterStats, customParametersReader),
                SkillId.MoveSpeedUp => new MoveSpeedUp(staticData),
                SkillId.MaxHPUp => new MaxHPUp(staticData),
                SkillId.DamageUp => new DamageUp(staticData),
                SkillId.Tornado => new TornadoSkill(staticData, characterStats),
                SkillId.DodgeRateUp => new DodgeRateUpSkill(staticData),
                SkillId.ChargeHPBufferOverTime => new ChargeHPBufferOverTimeSkill(staticData),
                SkillId.SpaceShip => new SpaceShipSkill(staticData, characterStats),
                SkillId.CreatureSavings => new CreatureSavingsSkill(staticData, customParametersReader, customParamtersChanger),
                SkillId.EnchantingGlow => new EnchantingGlowSkill(staticData, characterStats, customParametersReader),
                SkillId.LandWhale => new LandWhaleSkill(staticData, characterStats),
                SkillId.BossDamageUp => new BossDamageUpSkill(staticData),
                SkillId.StickySlime => new StickySlimeSkill(staticData, characterStats, customParametersReader),
                _ => throw new NotImplementedException($"{skillId} {skillLevel} 구현해주세요."),
            };
        }
    }
}
