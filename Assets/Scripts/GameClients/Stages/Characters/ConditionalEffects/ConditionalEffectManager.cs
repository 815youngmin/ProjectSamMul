using Shared.GameDataTypes;
using Shared.StaticDatas;
using System;
using System.Collections.Generic;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.Loggers;

namespace SamMul.GameClients.Stages.Characters.ConditionalEffects
{
    public class ConditionalEffectManager
    {
        private readonly List<ConditionalEffectBase> _conditionalEffects;
        private readonly Dictionary<InstantConditionType, List<ConditionalEffectBase>> _instantConditionListeners;

        private bool _isInStage;

        public ConditionalEffectManager()
        {
            _isInStage = false;

            _conditionalEffects = new List<ConditionalEffectBase>();
            _instantConditionListeners = new Dictionary<InstantConditionType, List<ConditionalEffectBase>>();
            foreach (InstantConditionType condition in Enum.GetValues(typeof(InstantConditionType)))
            {
                _instantConditionListeners.Add(condition, new List<ConditionalEffectBase>());
            }
        }

        public void EnterredIntoStage(Stage stage, PlayerCharacter owner)
        {
            _isInStage = true;
            foreach (var conditionalEffect in _conditionalEffects)
            {
                conditionalEffect.Initialize(stage, owner);
            }
        }

        public void UpdateConditionalEffects(Stage stage, PlayerCharacter owner)
        {
            //스테이지 입장하고나서 update가 돌아야 된다.
            //스테이지 입장하면 초기화 후 true로 변경된다.
            if (!_isInStage)
            {
                return;
            }

            foreach (var conditionalEffect in _conditionalEffects)
            {
                conditionalEffect.Update(stage, owner);
            }
        }

        public void ExitingFromStage(Stage stage, PlayerCharacter owner)
        {
            if (!_isInStage)
            {
                return;
            }

            _isInStage = false;
            foreach (var conditionalEffect in _conditionalEffects)
            {
                conditionalEffect.Destroy(stage, owner);
            }
        }

        public void EnterredIntoBossStageEvent(Stage stage, PlayerCharacter owner, MonsterStaticData bossStaticData)
        {
            var conditionEvent = InstantConditionType.EnterredIntoBossStageEvent;
            var listeners = _instantConditionListeners[conditionEvent];
            foreach (var conditionalEffect in listeners)
            {
                conditionalEffect.EnterredIntoBossStageEvent(stage, owner, bossStaticData);
            }
        }

        public void Clear()
        {
            _isInStage = false;
            _conditionalEffects.Clear();
            //Key값들을 지우진 않고 List만 비워준다. key값까지 지우면 나중에 문제가 생김
            foreach (var kvp in _instantConditionListeners)
            {
                kvp.Value.Clear();
            }
        }

        public void AddConditionalEffectByGradeEffect(GradeEffectType gradeEffectType, float parameter1, float parameter2)
        {
            if (gradeEffectType == GradeEffectType.None)
            {
                return;
            }

            ConditionalEffectBase conditionalEffect = gradeEffectType switch
            {
                GradeEffectType.AddMaxHp => new AddMaxHp(maxHpIncrement: parameter1),
                GradeEffectType.MultiplyMaxHp => new MultiplyMaxHp(maxHpIncrementPercentage: parameter1),
                GradeEffectType.AddAttackPower => new AddAttackPower(attackPowerIncrement: parameter1),
                GradeEffectType.MultiplyAttackPower => new MultiplyAttackPower(attackPowerIncrementPercentage: parameter1),
                GradeEffectType.AddDamageReduction => new AddDamageReduction(addDamageRedction: parameter1),
                GradeEffectType.MultiplyDamageReduction => new MultiplyDamageReduction(multiplyDamageRedction: parameter1),
                GradeEffectType.AddMoveSpeed => new AddMoveSpeed(increment: parameter1),
                GradeEffectType.MultiplyMoveSpeed => new MultiplyMoveSpeed(incrementPercentage: parameter1),
                GradeEffectType.AddCharacterAttackSpeed => new AddCharacterAttackSpeed(attackSpeedIncrement: parameter1),
                GradeEffectType.MultiplyCharacterAttackSpeed => new MultiplyCharacterAttackSpeed(attackSpeedIncrementPercentage: parameter1),
                GradeEffectType.MultiplyAttackPowerBelowHp => new MultiplyAttackPowerBelowHp(hpPercent: parameter1, attackPowerIncrementPercentage: parameter2),
                GradeEffectType.RecoverHpWhenHitEnemy => new RecoverHpWhenAttackedEnemy(hpRecoverPercantageByDamage: parameter1),
                GradeEffectType.RecoverHpOnKillEnemy => new RecoverHpOnKillEnemy(recoveryKillCount: (int)parameter1, increment: parameter2),
                GradeEffectType.StackMultiplyMoveSpeedOnKillEnemy => new StackMultiplyMoveSpeedOnKillEnemy(killCountToAddStack: (int)parameter1, incrementPerStack: parameter2),
                GradeEffectType.StackAttackPowerByTime => new StackAttackPowerByTime(incrementMaxTime: parameter1, incrementPercentage: parameter2),
                GradeEffectType.StackAttackPowerOnKillEnemy => new StackAttackPowerOnKillEnemy(killCountToAddStack: (int)parameter1, incrementPerStack: parameter2),
                GradeEffectType.HpAbsolbingTenticle => new HpAbsolbingTenticle(attackDamagePercent: parameter1, hpDrainPercent: parameter2),
                GradeEffectType.IgnatiaSS => new IgnatiaSS(parameter1),
                GradeEffectType.AddCharacterAttackKnockBackPower => new AddCharacterAttackKnockBackPower(parameter1),
                GradeEffectType.DecreaseAttackPowerButIncreaseAttackSpeed => new DecreaseAttackPowerButIncreaseAttackSpeed(parameter1, parameter2),
                GradeEffectType.AddBurnEffectOnIgnitionWave => new AddBurnEffectOnIgnitionWave(parameter1),
                GradeEffectType.HalfOffSaleKetchup => new HalfOffSaleKetchup(parameter1),
                GradeEffectType.RecoverHpWhenAttackByRedUmbrella => new RecoverHpWhenAttackByRedUmbrella(parameter1, parameter2),
                GradeEffectType.BurningLand => throw new NotImplementedException($"{gradeEffectType} 구현 안 됨."),
                GradeEffectType.UpgradeAlphaGattling => new UpgradeAlphaGattling(parameter1),
                GradeEffectType.UpgradeAlphaGuidedMissile => throw new NotImplementedException($"{gradeEffectType} 구현 안 됨."),
                GradeEffectType.AddMiniYoyo => throw new NotImplementedException($"{gradeEffectType} 구현 안 됨."),
                GradeEffectType.Dice777 => new Dice777(),
                GradeEffectType.DoubleTheJackpot => new DoubleTheJackpot(),
                GradeEffectType.MultiplyBurnDamage => new MultiplyBurnDamage(parameter1),
                GradeEffectType.AddDodgeRate => new AddDodgeRate(parameter1),
                GradeEffectType.MultiplyAttackRange => new MultiplyAttackRange(attackRangeIncrementPercentage: parameter1),
                GradeEffectType.MultiplyKnockbackPower => new MultiplyKnockbackPower(knockbackPowerIncrementPercentage: parameter1),
                GradeEffectType.AddAttackPowerOnKillBoss => new AddAttackPowerOnKillBoss(attackPowerIncrement: parameter1),
                GradeEffectType.AddHPOnKillBoss => new AddHPOnKillBoss(maxHpIncrement: parameter1),
                GradeEffectType.AddDamageReductionOnKillBoss => new AddDamageReductionOnKillBoss(addDamageRedction: parameter1),
                GradeEffectType.AddMoveSpeedOnKillBoss => new AddMoveSpeedOnKillBoss(increment: parameter1),
                GradeEffectType.AcquireShieldOnBossAppears => new AcquireShieldOnBossAppears(shieldAmount: (int)parameter1),
                GradeEffectType.AddExpUpOnKillEnemy => new AddExpUpOnKillEnemy(incrementMonsterKillCount: (int)parameter1, incrementExpAmount: (int)parameter2),
                GradeEffectType.MultiplyAttackSpeedOnKillEnemy => new MultiplyAttackSpeedOnKillEnemy(incrementMonsterKillCount: (int)parameter1, incrementAttackSpeedPercentage: parameter2),
                GradeEffectType.AddAttackPowerOnWhenHit => new AddAttackPowerOnWhenHit(incrementAttackPower: parameter1, maxAttackPower: parameter2),
                GradeEffectType.AddAttackSpeedOnWhenHit => new AddAttackSpeedOnWhenHit(incrementAttackSpeedAmount: parameter1, maxAttackSpeedAmount: parameter2),
                GradeEffectType.MultiplyAttackSpeedBelowHp => new MultiplyAttackSpeedBelowHp(hpPercent: parameter1, attackSpeedIncrementPercentage: parameter2),
                GradeEffectType.BelowHPInvincibility => new BelowHPInvincibility(hpPercent: parameter1, duration: parameter2),
                GradeEffectType.AddDodgeRateBelowHP => new AddDodgeRateBelowHP(hpPercent: parameter1, dodgeIncrementPercentage: parameter2),
                GradeEffectType.BelowHPExtraHit => new BelowHPExtraHit(hpPercent: parameter1, additionalAttackDamagePercentage: parameter2),
                GradeEffectType.BelowHPInstantDeath => new BelowHPInstantDeath(hpPercent: parameter1, instantDeathPercentage: parameter2),
                GradeEffectType.MultiplyRecoveryBelowHP => new MultiplyRecoveryBelowHP(hpPercent: parameter1, recoveryIncrementPercentage: parameter2),
                GradeEffectType.MultiplyAttackPowerMoreHP => new MultiplyAttackPowerMoreHP(hpPercent: parameter1, attackPowerIncrementPercentage: parameter2),
                GradeEffectType.MultiplyMoveSpeedMoreHP => new MultiplyMoveSpeedMoreHP(hpPercent: parameter1, moveSpeedIncrementPercentage: parameter2),
                GradeEffectType.MultiplyKnockBackMoreHP => new MultiplyKnockBackMoreHP(hpPercent: parameter1, knockbackPowerIncrementPercentage: parameter2),
                GradeEffectType.MultiplyDurationUpMoreHP => new MultiplyDurationUpMoreHP(hpPercent: parameter1, durationIncrementPercentage: parameter2),
                GradeEffectType.AcquireShieldCertainInterval => new AcquireShieldCertainInterval(shieldDelay: parameter1, shieldAmount: (int)parameter2),
                GradeEffectType.FullAttackOnKillElite => new FullAttackOnKillElite(damagePercentage: parameter1),
                GradeEffectType.DecreaseAttackPowerButIncreaseDamageReduction => new DecreaseAttackPowerButIncreaseDamageReduction(decreaseAttackPowerPercentage: parameter1, increaseDamageRedcutionPercentage: parameter2),
                GradeEffectType.AmbientSlow => new AmbientSlow(slowRange: parameter1, slowPercentage: parameter2),
                GradeEffectType.CleavageGum => new CleavageGum(miniGumAmount: (int)parameter1),
                GradeEffectType.ChewingBag => new ChewingBag(duration: parameter1, slowPercentage: parameter2),
                GradeEffectType.CreateHPBuffer => new CreateHPBuffer(maxHpPercentage: parameter1),
                GradeEffectType.ChargeHPBufferOnKillEnemy => new ChargeHPBufferOnKillEnemy(targetKillCount: (int)parameter1, chargePercentage: parameter2),
                GradeEffectType.IncreaseMoveSpeedOnKillEnemy => new IncreaseMoveSpeedOnKillEnemy(targetKillCount: (int)parameter1, moveSpeedIncreaseAmount: parameter2),
                GradeEffectType.IncreaseDodgeRateOnHighMoveSpeed => new IncreaseDodgeRateOnHighMoveSpeed(moveSpeedThresholdAmount: parameter1, dodgeRateIncreaseAmount: parameter2),
                GradeEffectType.IncreaseAttackSpeedOnShieldOrHPBufferActive => new IncreaseAttackSpeedOnShieldOrHPBufferActive(attackSpeedIncreaseRate: parameter1),
                GradeEffectType.IncreaseHPRecoveryRateOnShieldOrHPBufferActive => new IncreaseHPRecoveryRateOnShieldOrHPBufferActive(hpRecoveryIncreaseRate: parameter1),
                GradeEffectType.RecoverHPOnLevelUp => new RecoverHPOnLevelUp(maxHpPercentage: parameter1),
                GradeEffectType.IncreaseCriticalChance => new IncreaseCriticalChance(criticalChanceIncreaseAmount: parameter1),
                GradeEffectType.IncreaseExpIncreaseRate => new IncreaseExpIncreaseRate(expIcreaseRateIncreaseAmount: parameter1),
                GradeEffectType.IncreaseAttackSpeedOnLevelUp => new IncreaseAttackSpeedOnLevelUp(targetLevel: (int)parameter1, attackSpeedIncreaseRate: parameter2),
                GradeEffectType.IncreaseAttackRangeDistanceRatioOnLevelUp => new IncreaseAttackRangeDistanceRatioOnLevelUp(targetLevel: (int)parameter1, attackRangeDistanceRatioIncreaseRate: parameter2),
                GradeEffectType.IncreaseHPRecoveryRate => new IncreaseHPRecoveryRate(hpRecoveryRateIncreaseAmount: parameter1),
                GradeEffectType.RecoverHpOnLowHpOnlyOnce => new RecoverHpOnLowHpOnlyOnce(hpThresholdRate: parameter1, maxHpPercentage: parameter2),
                GradeEffectType.IncreaseDamageReductionOnHighHp => new IncreaseDamageReductionOnHighHp(hpThresholdRate: parameter1, damageReductionIncreaseAmount: parameter2),
                GradeEffectType.AttackEnemiesNearCharacterOnLevelUp => new AttackEnemiesNearCharacterOnLevelUp(attackRadius: parameter1, attackPowerPercentage: parameter2),
                GradeEffectType.IncreaseDamageReductionOnKillEnemy => new IncreaseDamageReductionOnKillEnemy(targetKillCount: (int)parameter1, damageReductionIncreaseAmount: parameter2),
                GradeEffectType.IncreaseExpIncreaseRateOnAcquiredHeart => new IncreaseExpIncreaseRateOnAcquiredHeart(expIncreaseRateIncreaseAmount: parameter1, maxIncreaseAmount: parameter2),
                GradeEffectType.RecoverHpPerTimeInterval => new RecoverHpPerTimeInterval(timeInterval: parameter1, maxHpPercentage: parameter2),
                GradeEffectType.IncreaseDodgeRateOnAcquiredHeart => new IncreaseDodgeRateOnAcquiredHeart(dodgeRateIncreaseAmount: parameter1, maxIncreaseAmount: parameter2),
                GradeEffectType.IncreaseMoveSpeedOnLevelUp => new IncreaseMoveSpeedOnLevelUp(moveSpeedIncreaseAmount: parameter1, maxIncreaseAmount: parameter2),
                GradeEffectType.IncreaseDamageReductionOnLevelUp => new IncreaseDamageReductionOnLevelUp(damageReductionIncreaseAmount: parameter1, maxIncreaseAmount: parameter2),
                GradeEffectType.IncreaseHPRecoveryRateOnLevelUp => new IncreaseHPRecoveryRateOnLevelUp(hpRecoveryRateIncreaseAmount: parameter1, maxIncreaseAmount: parameter2),
                GradeEffectType.AdditionalDamageForHighHpEnemy => new AdditionalDamageForHighHpEnemy(hpThresholdRate: parameter1, attackPowerPercentage: parameter2),
                GradeEffectType.IncreaseAttackSpeedOnHighHp => new IncreaseAttackSpeedOnHighHp(hpThresholdRate: parameter1, attackSpeedIncreaseRate: parameter2),
                GradeEffectType.IncreaseMoveSpeedOnLowHp => new IncreaseMoveSpeedOnLowHp(hpThresholdRate: parameter1, moveSpeedIncreaseAmount: parameter2),
                GradeEffectType.IncreaseDamageReductionOnLowHp => new IncreaseDamageReductionOnLowHp(hpThresholdRate: parameter1, damageReductionIncreaseAmount: parameter2),
                GradeEffectType.IncreaseMoveSpeedOnHitted => new IncreaseMoveSpeedOnHitted(increaseTime: parameter1, moveSpeedIncreaseAmount: parameter2),
                GradeEffectType.IncreaseDamageReduction => new IncreaseDamageReduction(damageReductionIncreaseAmount: parameter1),
                GradeEffectType.IncreaseAttackRangeDistanceRatioOnLowHp => new IncreaseAttackRangeDistanceRatioOnLowHp(hpThresholdRate: parameter1, attackRangeDistanceRatioIncreaseRate: parameter2),
                GradeEffectType.ResurrectAndRecoverHp => new ResurrectAndRecoverHp(increaseResurrectHpPercentage: parameter1, increaseResurrectionCount: (int)parameter2),
                GradeEffectType.ChargeHPBufferPerTimeInterval => new ChargeHPBufferPerTimeInterval(timeInterval: parameter1, hpBufferPercentage: parameter2),
                GradeEffectType.InvincibleOnHpBufferDeactivated => new InvincibleOnHpBufferDeactivated(invincibilityTime: parameter1),
                GradeEffectType.IncreaseAttackPowerOnShieldOrHPBufferActive => new IncreaseAttackPowerOnShieldOrHPBufferActive(attackPowerIncreaseRate: parameter1),
                GradeEffectType.AdditionalDamageForBossAndElite => new AdditionalDamageForBossAndElite(attackPowerPercentage: parameter1),
                GradeEffectType.IncreaseMoveSpeedAndDodgeRate => new IncreaseMoveSpeedAndDodgeRate(moveSpeedIncreaseAmount: parameter1, dodgeRateIncreaseAmount: parameter2),
                GradeEffectType.IncreaseDamageReductionOnResurrected => new IncreaseDamageReductionOnResurrected(damageReductionIncreaseAmount: parameter1),
                GradeEffectType.IncreaseHPRecoveryRateOnResurrected => new IncreaseHPRecoveryRateOnResurrected(hpRecoveryRateIncreaseAmount: parameter1),
                GradeEffectType.StunEnemyOnAttack => new StunEnemyOnAttack(stunProbability: parameter1, stunTime: parameter2),
                GradeEffectType.AdditionalDamageForStunnedEnemy => new AdditionalDamageForStunnedEnemy(attackPowerPercentage: parameter1),
                GradeEffectType.IncreaseAttackRangeDistanceRatioPerTimeInterval => new IncreaseAttackRangeDistanceRatioPerTimeInterval(attackRangeDistanceRatioIncreaseRate: parameter1, maxIncreaseRate: parameter2),
                GradeEffectType.IncreaseAttackSpeedPerTimeInterval => new IncreaseAttackSpeedPerTimeInterval(attackSpeedIncreaseRate: parameter1, maxIncreaseRate: parameter2),
                GradeEffectType.IncreaseDurationIncreaseRatePerTimeInterval => new IncreaseDurationIncreaseRatePerTimeInterval(durationIncreaseRateIncreaseRate: parameter1, maxIncreaseRate: parameter2),
                GradeEffectType.IncreaseAttackPowerPerTimeInterval => new IncreaseAttackPowerPerTimeInterval(attackPowerIncreaseRate: parameter1, maxIncreaseRate: parameter2),
                GradeEffectType.IncreaseCriticalCoefficient => new IncreaseCriticalCoefficient(criticalCoefficientIncreaseAmount: parameter1),
                GradeEffectType.StunEnemyPerTimeInterval => new StunEnemyPerTimeInterval(timeInterval: parameter1, stunTime: parameter2),
                GradeEffectType.IncreaseAttackSpeedOnHitted => new IncreaseAttackSpeedOnHitted(attackSpeedIncreaseRate: parameter1, maxIncreaseRate: parameter2),
                GradeEffectType.DecreaseAttackPowerAndIncreaseAttackRange => new DecreaseAttackPowerAndIncreaseAttackRange(attackPowerDecreaseRate: parameter1, attackRangeDistanceRatioIncreaseRate: parameter2),
                GradeEffectType.WindFieldsRemovePoisonousAreaEffects => new WindFieldsRemovePoisonousAreaEffects(),
                GradeEffectType.StartWithRangeUpSkill => new StartWithSkill(SkillId.RangeUp),
                GradeEffectType.IronFistsCreateWindFields => new IronFistsCreateWindFields(),
                GradeEffectType.StartWithBasicSkillLevel => new StartWithBasicSkillLevel(skillLevel: (int)parameter1),
                GradeEffectType.IncreaseTentiSweepAttackRange => new IncreaseTentiSweepAttackRange(attackRangeIncreaseRate: parameter1),
                GradeEffectType.IncreaseBattleYoyoAttackRange => new IncreaseBattleYoyoAttackRange(attackRangeIncreaseRate: parameter1),
                GradeEffectType.DecreaseAlphaHomingMissileAttackPeriod => new DecreaseAlphaHomingMissileAttackPeriod(attackPeriodDecreaseRate: parameter1),
                GradeEffectType.IncreaseSpaceCoinMinAmount => new IncreaseSpaceCoinMinAmount(additionalMinAmount: (int)parameter1),
                GradeEffectType.StartWithAttackSpeedUpSkill => new StartWithSkill(SkillId.AttackSpeedUp),
                GradeEffectType.AddResurrectionAmountOnKillEnemy => new AddResurrectionAmountOnKillEnemy(targetKillCount: (int)parameter1, additionalResurrectionAmount: (int)parameter2),
                GradeEffectType.BubbleGumSlowsEnemies => new BubbleGumSlowsEnemies(moveSpeedChangeDuration: parameter1, moveSpeedChangeRatio: parameter2),
                GradeEffectType.IncreaseBubbleGumDuration => new IncreaseBubbleGumDuration(durationIncreaseRate: parameter1),
                GradeEffectType.AddStunEffectOnMambaSkill => new AddStunEffectOnMambaSkill(parameter1),
                GradeEffectType.MoreDamageToCloseOnMambaSkill => new MoreDamageToCloseOnMambaSkill(),
                GradeEffectType.MambaSkillTranscendentObjectMultiplyDurationUp => new MambaSkillTranscendentObjectMultiplyDurationUp(parameter1),
                GradeEffectType.MambaSkillRemovePoisonousAreaEffects => new MambaSkillRemovePoisonousAreaEffects(),
                GradeEffectType.UndineSkillGetGoldOrGem => new UndineSkillGetGoldOrGem(parameter1),
                GradeEffectType.UndineSkillAcquisitionRangeUp => new UndineSkillAcquisitionRangeUp(parameter1),
                GradeEffectType.UndineTranscendentSkillMoreGetGoldOrGem => new UndineTranscendentSkillMoreGetGoldOrGem(parameter1),
                GradeEffectType.MultiplyMaxHpAndAttackPower => new MultiplyMaxHpAndAttackPower(maxHpIncrementPercentage: parameter1, attackPowerIncrementPercentage: parameter2),
                GradeEffectType.StartWithDurationUpSkill => new StartWithSkill(SkillId.DurationUp),
                _ => throw new NotImplementedException($"{gradeEffectType} 구현 안 됨."),
            };

            this.AddConditionalEffect(conditionalEffect);
        }

        public void AddConditionalEffectByEvolutionEffect(EvolutionType evolutionType, float parameter1, float parameter2)
        {
            ConditionalEffectBase conditionalEffect = evolutionType switch
            {
                EvolutionType.Strength => new EvolutionStrength(attackPowerIncrement: parameter1),
                EvolutionType.Stamina => new EvolutionStamina(maxHpIncrement: parameter1),
                EvolutionType.Tenacity => new EvolutionTenacity(addDamageRedction: parameter1),
                EvolutionType.Restoration => new EvolutionRestoration(eatingHPRecoveryRateIncreaser: parameter1),
                EvolutionType.LetsBringThisToo => new LetsBringThisToo(),
                EvolutionType.OneMoreTime => new OneMoreTime(additionalSkillRefreshCount: (int)parameter1),
                EvolutionType.BurningMeat => new BurningMeat(hpRecoveryIncrementPercentage: parameter1),
                EvolutionType.HAHAHTormentYou => new HAHAHTormentYou(criticalIncrementPercentage: parameter1),
                EvolutionType.GivingGirlAChance => new GivingGirlAChance(additionalSkillRefreshCount: (int)parameter1),
                EvolutionType.SwooshAway => new SwooshAway(projectileSpeedIncrementPercentage: parameter1),
                EvolutionType.ComeHereFood => new ComeHereFood(moveSpeedIncrementPercentage: parameter1),
                EvolutionType.TodayFateGoddess => throw new NotImplementedException($"{evolutionType} 구현 안 됨."),
                EvolutionType.TodayExperimentFun => new TodayExperimentFun(criticalIncrementPercentage: parameter1),
                EvolutionType.PreferFoodOverClothes => throw new NotImplementedException($"{evolutionType} 구현 안 됨."),
                EvolutionType.SweetFragranceLovely => new SweetFragranceLovely(hpRecoveryIncrementPercentage: parameter1),
                EvolutionType.GirlWillTryHarder => throw new NotImplementedException($"{evolutionType} 구현 안 됨."),
                EvolutionType.WeaknessIsHere => new WeaknessIsHere(criticalIncrementPercentage: parameter1),
                EvolutionType.BeautifulOutfit => throw new NotImplementedException($"{evolutionType} 구현 안 됨."),
                EvolutionType.HoldOnRedo => new HoldOnRedo(additionalSkillRefreshCount: (int)parameter1),
                EvolutionType.ComeHereFight => new ComeHereFight(attackRangeIncrementPercentage: parameter1),
                EvolutionType.SeasoningIsDelicious => new SeasoningIsDelicious(hpRecoveryIncrementPercentage: parameter1),
                EvolutionType.GirlWillFinishFaster => new GirlWillFinishFaster(attackSpeedIncrementPercentage: parameter1),
                EvolutionType.YoungLadyOutfitBest => new YoungLadyOutfitBest(equipmentStatIncrementPercentage: parameter1),
                EvolutionType.ThisMeatIsFierce => new ThisMeatIsFierce(increaseResurrectionCount: (int)parameter1),
                EvolutionType.KetchapiDressesWell => new KetchapiDressesWell(addDamageRedctionPerPiece: parameter1, maxAddDamageRedction: parameter2),
                EvolutionType.JACKPOT => new JACKPOT(dropSkillBoxPercentage: parameter1),
                EvolutionType.LotsOfChancesLotsOfMeat => new LotsOfChancesLotsOfMeat(),
                EvolutionType.IncreaseCriticalCoefficient => new IncreaseCriticalCoefficient(criticalCoefficientIncreaseAmount: parameter1),
                EvolutionType.AcquisitionDistance => new AcquisitionDistance(increasePercent: parameter1),
                EvolutionType.GoldIncreaseRate => new GoldIncreaseRate(increasePercent: parameter1),
                _ => throw new NotImplementedException($"{evolutionType} 구현 안 됨."),
            };

            this.AddConditionalEffect(conditionalEffect);
        }

        private void AddConditionalEffect(ConditionalEffectBase conditionalEffect)
        {
            _conditionalEffects.Add(conditionalEffect);
            foreach (var conditionType in conditionalEffect.SubscribingInstantConditions)
            {
                _instantConditionListeners[conditionType].Add(conditionalEffect);
            }

            if (_isInStage)
            {
                // conditionalEffect.Initialize()는 스테이지 입장시점에 한번 호출된다. 
                // 스테이지 입장 전에 AddConditionalEffect 된 것에 대해서만 처리됨.
                Log.I.Warn($"스테이지에 입장한 뒤에 {nameof(AddConditionalEffect)}가 호출되었습니다. 추가된 효과가 정상적으로 초기화되지 않습니다. 코드를 수정해주세요.");
            }
        }


        #region InstantConditionEvents

        /// <summary>
        /// 내가 때림
        /// </summary>
        public void AttackedEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            var conditionEvent = InstantConditionType.AttackedEnemy;
            var listeners = _instantConditionListeners[conditionEvent];
            foreach (var conditionalEffect in listeners)
            {
                conditionalEffect.AttackedEnemy(stage, owner, enemy, damage);
            }
        }

        /// <summary>
        /// 내가 다른애 죽임
        /// </summary>
        public void KilledEnemy(Stage stage, PlayerCharacter owner, Character enemy)
        {
            var conditionEvent = InstantConditionType.KilledEnemy;
            var listeners = _instantConditionListeners[conditionEvent];
            foreach (var conditionalEffect in listeners)
            {
                conditionalEffect.KilledEnemy(stage, owner, enemy);
            }
        }

        /// <summary>
        /// 내가 보스를 죽임
        /// </summary>
        public void KilledBoss(Stage stage, PlayerCharacter owner, Monster boss)
        {
            var conditionEvent = InstantConditionType.KilledBoss;
            var listeners = _instantConditionListeners[conditionEvent];
            foreach (var conditionalEffect in listeners)
            {
                conditionalEffect.KilledBoss(stage, owner, boss);
            }
        }

        /// <summary>
        /// 내가 맞기 전 (데미지 아직 안입음)
        /// </summary>
        public BeingHittedResultData BeingHittedByEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
            var conditionEvent = InstantConditionType.BeingHittedByEnemy;
            var listeners = _instantConditionListeners[conditionEvent];
            BeingHittedResultData begingHittedResultData = new BeingHittedResultData(false, damage);

            foreach (var conditionalEffect in listeners)
            {
                begingHittedResultData = conditionalEffect.BeingHittedByEnemy(stage, owner, enemy, begingHittedResultData._calculatedDamage);
                if (begingHittedResultData._isEnemyAttackIgnored)
                {
                    return begingHittedResultData;
                }
            }

            return begingHittedResultData;
        }

        /// <summary>
        /// 내가 맞음
        /// </summary>
        public void HittedByEnemy(Stage stage, PlayerCharacter owner, Monster enemy, float damage)
        {
            var conditionEvent = InstantConditionType.HittedByEnemy;
            var listeners = _instantConditionListeners[conditionEvent];
            foreach (var conditionalEffect in listeners)
            {
                conditionalEffect.HittedByEnemy(stage, owner, enemy, damage);
            }
        }

        /// <summary>
        /// 내가 부활함
        /// </summary>
        public void Resurrected(Stage stage, PlayerCharacter owner)
        {
            var conditionEvent = InstantConditionType.Resurrected;
            var listeners = _instantConditionListeners[conditionEvent];
            foreach (var conditionalEffect in listeners)
            {
                conditionalEffect.Resurrected(stage, owner);
            }
        }

        /// <summary>
        /// 내 레벨이 오름
        /// </summary>
        public void OnLevelUp(Stage stage, PlayerCharacter owner)
        {
            var conditionEvent = InstantConditionType.OnLevelUp;
            var listeners = _instantConditionListeners[conditionEvent];
            foreach (var conditionalEffect in listeners)
            {
                conditionalEffect.OnLevelup(stage, owner);
            }
        }

        /// <summary>
        /// 하트를 획득함
        /// </summary>
        public void AcquiredHeart(Stage stage, PlayerCharacter owner)
        {
            var conditionEvent = InstantConditionType.AcquiredHeart;
            var listeners = _instantConditionListeners[conditionEvent];
            foreach (var conditionalEffect in listeners)
            {
                conditionalEffect.AcquiredHeart(stage, owner);
            }
        }

        /// <summary>
        /// 스킬 셋 초기화 직후
        /// </summary>
        public void OnSkillSetInitialized(Stage stage, PlayerCharacter owner)
        {
            var conditionEvent = InstantConditionType.OnSkillSetInitialized;
            var listeners = _instantConditionListeners[conditionEvent];
            foreach (var conditionalEffect in listeners)
            {
                conditionalEffect.OnSkillSetInitialized(stage, owner);
            }
        }

        /// <summary>
        /// 캐릭터 기본 공격이 나간 직후
        /// 스킬의 Activated 상태가 아닌 공격 객체가 생성된 시점에 호출되어야 함
        /// </summary>
        public void OnBasicSkillUsed(Stage stage, PlayerCharacter owner)
        {
            var conditionEvent = InstantConditionType.OnBasicSkillUsed;
            var listeners = _instantConditionListeners[conditionEvent];
            foreach (var conditionalEffect in listeners)
            {
                conditionalEffect.OnBasicSkillUsed(stage, owner);
            }
        }

        /// <summary>
        /// 내가 적을 스턴 상태일때 죽인경우
        /// </summary>
        public void KilledStunnedEnemy(Stage stage, PlayerCharacter owner, Character enemy)
        {
            var conditionEvent = InstantConditionType.KilledStunnedEnemy;
            var listeners = _instantConditionListeners[conditionEvent];
            foreach (var conditionalEffect in listeners)
            {
                conditionalEffect.KilledStunnedEnemy(stage, owner, enemy);
            }
        }

        #endregion
    }
}