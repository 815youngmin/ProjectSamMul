using Shared.GameDataTypes;
using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.AreaEffectOnDieAIs;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.DashAIs;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.ForwardAreaAttackAIs;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.HealAIs;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.InvisibleAIs;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.MeleeAIs;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.MoveAndStopAIs;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.MoveStopAttackAIs;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.PassByAIs;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.PoisonousAreaAI;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.RangeAIs;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.ShieldAIs;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.SpecialGemGoblinAIs;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.SummonAIs;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies.SummonOnDieAIs;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Arabian;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Steampunk;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.BossAIStrategies.Western;
using Z.Loggers;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs
{

    public class MonsterAIController : IMonsterAIEvent
    {
        protected Monster Owner { get; private set; }
        private MonsterAIStrategyBase aiStrategy;
        /// <summary>
        /// AIController가 몬스터에 연결된 직후 호출된다.
        /// 몬스터 초기화 시점으로, AIController의 초기화 코드를 넣으면 된다.
        /// </summary>
        public void InitializeAIStrategy(Monster owner, Stage stage, MonsterAIBlackboardBase? blackboard)
        {
            this.Owner = owner;

            this.CreateAIStrategy(owner.StaticData.AIType, blackboard);

            if (this.aiStrategy != null)
            {
                this.aiStrategy.Begin(stage, owner);
            }
        }

        public ISummonedMonsterCommandSender InitializeSummonMonsterAIStrategy(Monster owner, Stage stage)
        {
            this.Owner = owner;
            ISummonedMonsterCommandSender summonedMonsterCommandSender;

            this.aiStrategy = new SummonedMonsterAIStrategy();
            summonedMonsterCommandSender = (ISummonedMonsterCommandSender)this.aiStrategy;
            this.aiStrategy.Begin(stage, owner);

            return summonedMonsterCommandSender;
        }


        private void CreateAIStrategy(MonsterAIType aiType, MonsterAIBlackboardBase? baseBlackboard)
        {
            switch (aiType)
            {
                case MonsterAIType.MeleeAttackAI:
                    {
                        this.aiStrategy = new MeleeIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.RangeAttackAI:
                    {
                        this.aiStrategy = new RangeIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.EliteRangeAttackAI:
                    {
                        this.aiStrategy = new MultipleHorizontalRangeAttackIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.SummonMeleeAttackAI:
                    {
                        this.aiStrategy = new SummonMeleeIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.PassByAI:
                    {
                        this.aiStrategy = new PassByIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.PoisonousAreaAI:
                    {
                        this.aiStrategy = new PoisonousAreaIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.RewardGoblinAI:
                    {
                        var blackboard = baseBlackboard as RewardGoblinAIBlackboard;
                        if (blackboard == null)
                        {
                            Log.I.Error($"Failed To CreateMonsterAIController. AIType[{aiType}] requires Blackboard type of [{nameof(RewardGoblinIdleAIStrategy)}].");
                            blackboard = new RewardGoblinAIBlackboard(RewardGoblinType.Gold, maxHpCount: 5, hitRewardMin: 1, hitRewardMax: 10, deadRewardMin: 1, deadRewardMax
                                : 10);
                        }

                        this.aiStrategy = new RewardGoblinIdleAIStrategy(blackboard);
                        break;
                    }
                case MonsterAIType.DashAttackAI:
                    {
                        this.aiStrategy = new DashIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.MoveAndStopAI:
                    {
                        this.aiStrategy = new MoveAndStopIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.MoveStopAttackAI:
                    {
                        this.aiStrategy = new MoveStopAttackIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.HealAI:
                    {
                        var blackboard = (baseBlackboard as HealAIBlackboard)
                                      ?? new HealAIBlackboard(healRange: Owner.StaticData.Param1, healPercent: Owner.StaticData.Param2, healPeriod: 0.5f, maxProximityDistance: 6f, maxEscapeDistance: 8f, moveTime: 2f, stopTime: 1f);
                        this.aiStrategy = new HealIdleAIStrategy(blackboard);
                        break;
                    }
                case MonsterAIType.SummonerAI:
                    {
                        this.aiStrategy = new SummonerIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.TurretAttackAI:
                    {
                        this.aiStrategy = new TurretIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.TurretReflectionRangeAttackAI:
                    {
                        this.aiStrategy = new TurretReflectionRangeIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.MultipleVerticalRangeAttackAI:
                    {
                        this.aiStrategy = new MultipleVerticalRangeAttackIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.MultipleHorizontalRangeAttackAI:
                    {
                        this.aiStrategy = new MultipleHorizontalRangeAttackIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.MultipleVerticalEliteRangeAttackAI:
                    {
                        this.aiStrategy = new MultipleVerticalEliteRangeAttackIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.MultipleHorizontalEliteRangeAttackAI:
                    {
                        this.aiStrategy = new MultipleHorizontalEliteRangeAttackIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.IncubatorAI:
                    {
                        this.aiStrategy = new IncubatorIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.EliteReflectionRangeAttackAI:
                    {
                        switch (Owner.CharacterType)
                        {
                            case CharacterType.Egypt_EliteScorpion:
                                {
                                    var blackboard = (baseBlackboard as EliteReflectionRangeAttackAIBlackboard)
                                            ?? new EliteReflectionRangeAttackAIBlackboard(projectileAmount: 6, projectileSpeed: 8, projectileLifeTime: 10f, projectileRadius: 0.5f, projectileRotateSpeed: 0f, projectilePrefabPath: Owner.StaticData.SpecialAttack1ResourcePath);
                                    this.aiStrategy = new EliteReflectionRangeIdleAIStrategy(blackboard);
                                    break;
                                }
                            case CharacterType.Summon_Egypt_Archer:
                                {
                                    var blackboard = (baseBlackboard as EliteReflectionRangeAttackAIBlackboard)
                                            ?? new EliteReflectionRangeAttackAIBlackboard(projectileAmount: 6, projectileSpeed: 8, projectileLifeTime: 10f, projectileRadius: 0.5f, projectileRotateSpeed: 0f, projectilePrefabPath: Owner.StaticData.SpecialAttack1ResourcePath);
                                    this.aiStrategy = new EliteReflectionRangeIdleAIStrategy(blackboard);
                                    break;
                                }
                            case CharacterType.Troy_ElitePhalanx:
                                {
                                    var blackboard = (baseBlackboard as EliteReflectionRangeAttackAIBlackboard)
                                            ?? new EliteReflectionRangeAttackAIBlackboard(projectileAmount: 6, projectileSpeed: 8, projectileLifeTime: 10f, projectileRadius: 0.5f, projectileRotateSpeed: 0f, projectilePrefabPath: Owner.StaticData.SpecialAttack1ResourcePath);
                                    this.aiStrategy = new EliteReflectionRangeIdleAIStrategy(blackboard);
                                    break;
                                }
                            case CharacterType.Sengoku_EliteWindGod:
                                {
                                    var blackboard = (baseBlackboard as EliteReflectionRangeAttackAIBlackboard)
                                            ?? new EliteReflectionRangeAttackAIBlackboard(projectileAmount: 6, projectileSpeed: 8, projectileLifeTime: 10f, projectileRadius: 0.5f, projectileRotateSpeed: 0f, projectilePrefabPath: Owner.StaticData.SpecialAttack1ResourcePath);
                                    this.aiStrategy = new EliteReflectionRangeIdleAIStrategy(blackboard);
                                    break;
                                }
                            case CharacterType.Susanoo_Summon_WindGod:
                                {
                                    var blackboard = (baseBlackboard as EliteReflectionRangeAttackAIBlackboard)
                                            ?? new EliteReflectionRangeAttackAIBlackboard(projectileAmount: 6, projectileSpeed: 8, projectileLifeTime: 10f, projectileRadius: 0.5f, projectileRotateSpeed: 0f, projectilePrefabPath: Owner.StaticData.SpecialAttack1ResourcePath);
                                    this.aiStrategy = new EliteReflectionRangeIdleAIStrategy(blackboard);
                                    break;
                                }
                            case CharacterType.EarthGuardian_Elite_FixingMan:
                                {
                                    var blackboard = (baseBlackboard as EliteReflectionRangeAttackAIBlackboard)
                                            ?? new EliteReflectionRangeAttackAIBlackboard(projectileAmount: 6, projectileSpeed: 8, projectileLifeTime: 10f, projectileRadius: 0.5f, projectileRotateSpeed: 0f, projectilePrefabPath: Owner.StaticData.SpecialAttack1ResourcePath);
                                    this.aiStrategy = new EliteReflectionRangeIdleAIStrategy(blackboard);
                                    break;
                                }
                            case CharacterType.Sengoku_EliteGhostWarrior:
                                {
                                    var blackboard = (baseBlackboard as EliteReflectionRangeAttackAIBlackboard)
                                                    ?? new EliteReflectionRangeAttackAIBlackboard(projectileAmount: 6, projectileSpeed: 8, projectileLifeTime: 10f, projectileRadius: 0.5f, projectileRotateSpeed: 0f, projectilePrefabPath: Owner.StaticData.SpecialAttack1ResourcePath);
                                    this.aiStrategy = new EliteReflectionRangeIdleAIStrategy(blackboard);
                                    break;
                                }
                            default:
                                {
                                    var blackboard = (baseBlackboard as EliteReflectionRangeAttackAIBlackboard)
                                            ?? new EliteReflectionRangeAttackAIBlackboard(projectileAmount: 6, projectileSpeed: 8, projectileLifeTime: 10f, projectileRadius: 0.5f, projectileRotateSpeed: 720f, projectilePrefabPath: Owner.StaticData.SpecialAttack1ResourcePath);
                                    this.aiStrategy = new EliteReflectionRangeIdleAIStrategy(blackboard);
                                }
                                break;
                        }
                        break;
                    }
                case MonsterAIType.AreaEffectOnDieAI:
                    {
                        this.aiStrategy = new AreaEffectOnDieIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.EliteSummonAI:
                    {
                        this.aiStrategy = new EliteSummonerIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.ForwardAreaAttackAI:
                    {
                        this.aiStrategy = new ForwardAreaAttackIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.ForwardAreaAttackEliteAI:
                    {
                        this.aiStrategy = new ForwardAreaAttackEliteIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.PassByFastAndNormalMoveAI:
                    {
                        this.aiStrategy = new PassByFastAndNormalMoveIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.OrthogonalMoveAI:
                    {
                        this.aiStrategy = new OrthogonalMoveAIIdleStrategy();
                        break;
                    }
                case MonsterAIType.DropShipSummonAI:
                    {
                        this.aiStrategy = new DropShipSummonerIdleAIStrategy(Time.time);
                        break;
                    }
                case MonsterAIType.Arabian_AlibabaAI:
                    {
                        this.aiStrategy = new AlibabaIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Arabian_GenieAI:
                    {
                        this.aiStrategy = new GenieIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Arabian_AladdinAI:
                    {
                        this.aiStrategy = new AladdinIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Arabian_WitchQueenAI:
                    {
                        this.aiStrategy = new WitchQueenIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Arabian_BlackCyclopsAI:
                    {
                        this.aiStrategy = new BlackCyclopsIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Arabian_SinbadAI:
                    {
                        this.aiStrategy = new SinbadIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Arabian_HarunAlRashidAI:
                    {
                        this.aiStrategy = new HarunAlRashidIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Arabian_PeribanouAI:
                    {
                        this.aiStrategy = new PeribanouIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Arabian_SchaibarAI:
                    {
                        this.aiStrategy = new SchaibarIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.WesternBombMasterAI:
                    {
                        this.aiStrategy = new WesternBombMasterIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.WesternHeadMinerAI:
                    {
                        this.aiStrategy = new WesternHeadMinerIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.WesternMiningKingAI:
                    {
                        this.aiStrategy = new WesternMiningKingIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.DashAttackSummonOnDieAI:
                    {
                        this.aiStrategy = new DashAttackSummonOnDieIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.EliteReflectionRangeAttackSummonOnDieAI:
                    {
                        switch (Owner.CharacterType)
                        {
                            default:
                                {
                                    var blackboard = (baseBlackboard as EliteReflectionRangeAttackAIBlackboard)
                                            ?? new EliteReflectionRangeAttackAIBlackboard(projectileAmount: 6, projectileSpeed: 8, projectileLifeTime: 10f, projectileRadius: 0.5f, projectileRotateSpeed: 0f, projectilePrefabPath: Owner.StaticData.SpecialAttack1ResourcePath);
                                    this.aiStrategy = new EliteReflectionRangeSummonOnDieIdleAIStrategy(blackboard);
                                    break;
                                }
                        }
                        break;
                    }
                case MonsterAIType.MultipleHorizontalEliteRangeAttackSummonOnDieAI:
                    {
                        this.aiStrategy = new MultipleHorizontalEliteRangeAttackSummonOnDieIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.MultipleVerticalEliteRangeAttackSummonOnDieAI:
                    {
                        this.aiStrategy = new MultipleVerticalEliteRangeAttackSummonOnDieIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.ShieldMeleeAI:
                    {
                        this.aiStrategy = new ShieldMeleeIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.WesternBillCodyAI:
                    {
                        this.aiStrategy = new WesternBillCodyIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.WesternBombMasterHardAI:
                    {
                        this.aiStrategy = new WesternBombMasterHardIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.WesternWyattEarpAI:
                    {
                        this.aiStrategy = new WesternWyattEarpIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.WesternJesseJamesAI:
                    {
                        this.aiStrategy = new WesternJesseJamesIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.WesternHeadMinerHardAI:
                    {
                        this.aiStrategy = new WesternHeadMinerHardIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.WesternKidAI:
                    {
                        this.aiStrategy = new WesternKidIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.ShieldRangeAttackAI:
                    {
                        this.aiStrategy = new ShieldRangeIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.ShieldEliteSummonAI:
                    {
                        this.aiStrategy = new ShieldEliteSummonIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.MeleeSummonOnDieAI:
                    {
                        this.aiStrategy = new MeleeSummonOnDieIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Steampunk_WatchManAI:
                    {
                        this.aiStrategy = new Steampunk_WatchManIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Steampunk_AlchemyGuildLeaderAI:
                    {
                        this.aiStrategy = new Steampunk_AlchemyGuildLeaderIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Steampunk_JamesWattAI:
                    {
                        this.aiStrategy = new Steampunk_JamesWattIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Steampunk_ThomasEdisonAI:
                    {
                        this.aiStrategy = new Steampunk_ThomasEdisonIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Steampunk_AbandonedSpeciMenAI:
                    {
                        this.aiStrategy = new Steampunk_AbandonedSpeciMenIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Steampunk_AlchemyGuildLeaderHardAI:
                    {
                        this.aiStrategy = new Steampunk_AlchemyGuildLeaderHardIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Steampunk_NicolasFlamelAI:
                    {
                        this.aiStrategy = new Steampunk_NicolasFlamelIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Steampunk_BattleGolemAI:
                    {
                        this.aiStrategy = new Steampunk_BattleGolemIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Steampunk_NicolasFlamelHardAI:
                    {
                        this.aiStrategy = new Steampunk_NicolasFlamelHardIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.Steampunk_NikolaTeslaAI:
                    {
                        this.aiStrategy = new Steampunk_NikolaTeslaIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.PoisonousRangeAttackAI:
                    {
                        this.aiStrategy = new PoisonousRangeIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.ShieldHealAI:
                    {
                        var blackboard = (baseBlackboard as HealAIBlackboard)
                                      ?? new HealAIBlackboard(healRange: Owner.StaticData.Param1, healPercent: Owner.StaticData.Param2, healPeriod: 0.5f, maxProximityDistance: 6f, maxEscapeDistance: 8f, moveTime: 2f, stopTime: 1f);
                        this.aiStrategy = new ShieldHealIdleAIStrategy(blackboard);
                        break;
                    }
                case MonsterAIType.OrthogonalMoveAndStopAI:
                    {
                        this.aiStrategy = new OrthogonalMoveAndStopAIIdleStrategy();
                        break;
                    }
                case MonsterAIType.MultipleRangeAttackOnDieAI:
                    {
                        this.aiStrategy = new MultipleRangeAttackOnDieIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.EliteReflectionRangeAttackAndSummonerAI:
                    {
                        this.aiStrategy = new EliteReflectionRangeAttackAndSummonerIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.ShieldMultipleVerticalRangeAttackAI:
                    {
                        this.aiStrategy = new ShieldMultipleVerticalRangeAttackIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.ShieldMultipleVerticalEliteRangeAttackAI:
                    {
                        this.aiStrategy = new ShieldMultipleVerticalEliteRangeAttackIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.ShieldMultipleHorizontalRangeAttackAI:
                    {
                        this.aiStrategy = new ShieldMultipleHorizontalRangeAttackIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.ShieldMultipleHorizontalEliteRangeAttackAI:
                    {
                        this.aiStrategy = new ShieldMultipleHorizontalEliteRangeAttackIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.InvisibleMoveAndStopAI:
                    {
                        this.aiStrategy = new InvisibleMoveAndStopIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.InvisibleDashAI:
                    {
                        this.aiStrategy = new InvisibleDashIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.PassByMultipleRangeAttackOnDie:
                    {
                        this.aiStrategy = new PassByMultipleRangeAttackOnDieIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.InvisibleMultipleHorizontalEliteRangeAttackAI:
                    {
                        this.aiStrategy = new InvisibleMultipleHorizontalEliteRangeAttackIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.InvisibleMultipleVerticalEliteRangeAttackAI:
                    {
                        this.aiStrategy = new InvisibleMultipleVerticalEliteRangeAttackIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.InvisibleReflectionRangeAttackAI:
                    {
                        var blackboard = (baseBlackboard as EliteReflectionRangeAttackAIBlackboard)
                        ?? new EliteReflectionRangeAttackAIBlackboard(projectileAmount: 6, projectileSpeed: 8, projectileLifeTime: 10f, projectileRadius: 0.5f, projectileRotateSpeed: 0f, projectilePrefabPath: Owner.StaticData.SpecialAttack1ResourcePath);
                        this.aiStrategy = new InvisibleEliteReflectionRangeIdleAIStrategy(blackboard);
                        break;
                    }
                case MonsterAIType.MultipleVerticalEliteRangeAttackAndSummonerAI:
                    {
                        this.aiStrategy = new MultipleVerticalEliteRangeAttackAndSummonerIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.MultipleHorizontalEliteRangeAttackAndSummonerAI:
                    {
                        this.aiStrategy = new MultipleHorizontalEliteRangeAttackAndSummonerIdleAIStrategy();
                        break;
                    }
                case MonsterAIType.DashAttackAndSummonerAI:
                    {
                        this.aiStrategy = new DashAndSummonerIdleAIStrategy();
                        break;
                    }
                default:
                    {
                        Debug.LogError($"{aiType} 작업 안 됨. 구현해주세요.");
                        this.aiStrategy = new MeleeIdleAIStrategy();
                        break;
                    }
            }
        }


        public void Update(Stage stage)
        {
            if (this.Owner.Action.IsDead ||
                this.Owner.Action.IsAppearing)
            {
                return;
            }

            var nextStrategy = this.aiStrategy.Update(stage, this.Owner);
            if (nextStrategy == null)
            {
                return;
            }

            this.ChangeTo(stage, nextStrategy);
        }

        void IMonsterAIEvent.OnDead(Stage stage)
        {
            var nextStrategy = this.aiStrategy.OnDead(stage, this.Owner);
            if (nextStrategy != null)
            {
                this.ChangeTo(stage, nextStrategy);
            }
        }

        void IMonsterAIEvent.OnEnterredIntoStage(Stage stage)
        {
            aiStrategy.Blackboard?.OnEnterredIntoStage(stage, this.Owner);
        }

        void IMonsterAIEvent.OnDisappearing(Stage stage)
        {
            var nextStrategy = this.aiStrategy.OnDisappearing(stage, this.Owner);
            if (nextStrategy != null)
            {
                this.ChangeTo(stage, nextStrategy);
            }
        }
        void IMonsterAIEvent.OnRePosition(Stage stage)
        {
            var nextStrategy = this.aiStrategy.OnRePosition(stage, this.Owner);
            if (nextStrategy != null)
            {
                this.ChangeTo(stage, nextStrategy);
            }
        }
        void IMonsterAIEvent.OnHitted(Stage stage, Character attacker)
        {
            var nextStrategy = this.aiStrategy.OnHitted(stage, this.Owner, attacker: attacker);
            if (nextStrategy != null)
            {
                this.ChangeTo(stage, nextStrategy);
            }
        }

        private void ChangeTo(Stage stage, MonsterAIStrategyBase nextStrategy)
        {
            Debug.Assert(nextStrategy != null);

            this.aiStrategy.End(stage, this.Owner);
            this.aiStrategy = nextStrategy;
            this.aiStrategy.Begin(stage, this.Owner);
        }
    }
}
