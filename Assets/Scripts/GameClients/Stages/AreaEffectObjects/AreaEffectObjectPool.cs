using Shared.StaticDatas;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Z.ObjectPools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class AreaEffectPool
    {
        private readonly ObjectPool<AreaEffectType, AreaEffectObjectBase> _pool;

        private readonly StaticDataRepository _staticDataRepository;

        public AreaEffectPool(StaticDataRepository staticDataRepository)
        {
            _pool = new ObjectPool<AreaEffectType, AreaEffectObjectBase>(objectFactory: AllocateObject);
            _staticDataRepository = staticDataRepository;
        }

        public void Init()
        {
        }

        // 씬이 정리될 때 호출된다.
        // 씬을 넘어갈 때 유효하지 않은 Pooling Object들을 모두 Destroy해준다. (prefab은 남김)
        public void ClearBeforeChangingScene(Scene relatedScene)
        {
            _pool.DestroyAll(relatedScene, destroyFunction: (AreaEffectObjectBase element) =>
            {
                GameObject.Destroy(element.gameObject);
            });
        }

        public TAreaObject TakeOneFromPool<TAreaObject>(AreaEffectType key) where TAreaObject : AreaEffectObjectBase
        {
            var projectile = this._pool.TakeOne<TAreaObject>(key);
            return projectile;
        }

        public void PutBack(AreaEffectObjectBase areaEffect)
        {
            _pool.PutBack(areaEffect);
        }

        private AreaEffectObjectBase AllocateObject(AreaEffectType areaEffectType)
        {
            var areaEffectObject = new GameObject(areaEffectType.ToString());

            switch (areaEffectType)
            {
                case AreaEffectType.Glutton:
                    var gluttonAreaEffect = areaEffectObject.AddComponent<GluttonObject>();
                    gluttonAreaEffect.AllocateSharedResources();
                    return gluttonAreaEffect;
                case AreaEffectType.SpinBlade:
                    var spinBladeAreaEffect = areaEffectObject.AddComponent<SpinBladeObject>();
                    spinBladeAreaEffect.AllocateSharedResources();
                    return spinBladeAreaEffect;
                case AreaEffectType.DeathTouch:
                    var deathTouchAreaEffect = areaEffectObject.AddComponent<DeathTouchAreaEffectObject>();
                    deathTouchAreaEffect.AllocateSharedResources();
                    return deathTouchAreaEffect;
                case AreaEffectType.AmbushMonster:
                    var ambushMonsterAreaEffect = areaEffectObject.AddComponent<AmbushMonsterObject>();
                    ambushMonsterAreaEffect.AllocateSharedResources();
                    return ambushMonsterAreaEffect;
                case AreaEffectType.IgnitionWaveTranscend_Default:
                case AreaEffectType.IgnitionWaveTranscend_Siyeon:
                case AreaEffectType.IgnitionWaveTranscend_Bongjun:
                {
                        var effect = areaEffectObject.AddComponent<IgnitionWaveTranscendAreaEffectObject>();
                        effect.AllocateSharedResources(areaEffectType);
                        return effect;
                    }
                case AreaEffectType.PlasmaDrill:
                    {
                        var effect = areaEffectObject.AddComponent<PlasmaDrillObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.TrapBomb:
                    {
                        var effect = areaEffectObject.AddComponent<RuneTrapBombAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.DefensiveField:
                    {
                        var effect = areaEffectObject.AddComponent<DefensiveFieldAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.GymBall:
                    {
                        var effect = areaEffectObject.AddComponent<GymBallObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.Meteor:
                    {
                        var effect = areaEffectObject.AddComponent<MeteorAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.LavaReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.LavaReflectionObject, "Stages/AreaEffects/Lava_AttackObject_Bound.prefab");
                        return effect;
                    }
                case AreaEffectType.TentiSweep_Default:
                case AreaEffectType.TentiSweep_Hina:
                case AreaEffectType.TentiSweep_Bongjun:
                    {

                        var effect = areaEffectObject.AddComponent<TentiSweepAreaEffectObject>();
                        effect.AllocateSharedResources(areaEffectType);
                        return effect;
                    }
                case AreaEffectType.TentiSweepVertical_Default:
                case AreaEffectType.TentiSweepVertical_Hina:
                case AreaEffectType.TentiSweepVertical_Bongjun:
                    {
                        var effect = areaEffectObject.AddComponent<TentiSweepVerticalObject>();
                        effect.AllocateSharedResources(areaEffectType);
                        return effect;
                    }
                case AreaEffectType.TentiSweepTranscendent_Default:
                case AreaEffectType.TentiSweepTranscendent_Hina:
                case AreaEffectType.TentiSweepTranscendent_Bongjun:
                {
                        var effect = areaEffectObject.AddComponent<TentiSweepTranscandentObject>();
                        effect.AllocateSharedResources(areaEffectType);
                        return effect;
                }
                case AreaEffectType.IceThreeLeapsBugPoison:
                    {
                        var effect = areaEffectObject.AddComponent<IceThreeLeapsBugPoisonAreaEffect>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.RuneTrap:
                    {
                        var effect = areaEffectObject.AddComponent<RuneTrapTranscendentAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.PoisonousArea:
                    {
                        var effect = areaEffectObject.AddComponent<PoisonousAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.IceGolemShockWave:
                    {
                        var effect = areaEffectObject.AddComponent<IceGolemShockWaveAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.IgnitionWave_Default:
                case AreaEffectType.IgnitionWave_Siyeon:
                case AreaEffectType.IgnitionWave_Bongjun:
                    {
                        var effect = areaEffectObject.AddComponent<IgnitionWaveAreaEffectObject>();
                        effect.AllocateSharedResources(areaEffectType);
                        return effect;
                    }
                case AreaEffectType.EgyptSnakeDruidSnakeReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<EgyptSnakeDruidSnakeReflectionAreaEffect>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.EgyptSphinxVerticalObject:
                    {
                        var effect = areaEffectObject.AddComponent<EgyptSphinxVerticalAreaEffect>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.AlphaGatlingHoming_Default:
                case AreaEffectType.AlphaGatlingHoming_Magenta:
                    {
                        var effect = areaEffectObject.AddComponent<AlphaGatlingHomingAreaEffectObject>();
                        effect.AllocateSharedResources(areaEffectType);
                        return effect;
                    }
                case AreaEffectType.RaAreaEffectObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpineBodyAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.RaAreaEffectObject);
                        return effect;
                    }
                case AreaEffectType.AresSpear:
                    {
                        var effect = areaEffectObject.AddComponent<AresSpearAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.ZeusLightning:
                    {
                        var effect = areaEffectObject.AddComponent<ZeusLightningAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.BloodRapier:
                    {
                        var effect = areaEffectObject.AddComponent<BloodRapierAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.BloodRapierTranscendent:
                    {
                        var effect = areaEffectObject.AddComponent<BloodRapierTranscendentObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.BattleYoYo_Default:
                case AreaEffectType.BattleYoYo_EggKim:
                    {
                        var effect = areaEffectObject.AddComponent<BattleYoYoAreaEffectObject>();
                        effect.AllocateSharedResources(areaEffectType);
                        return effect;
                    }
                case AreaEffectType.BattleYoYoTranscendent_Default:
                case AreaEffectType.BattleYoYoTranscendent_EggKim:
                    {
                        var effect = areaEffectObject.AddComponent<BattleYoYoTranscendentObject>();
                        effect.AllocateSharedResources(areaEffectType);
                        return effect;
                    }
                case AreaEffectType.LaserAreaEffect:
                    {
                        var effect = areaEffectObject.AddComponent<LaserAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.LaserAreaEffect, "Stages/AreaEffects/Laser/Laser.prefab");
                        return effect;
                    }
                case AreaEffectType.ZhangjueLaser:
                    {
                        var effect = areaEffectObject.AddComponent<ZhangjuieLaserAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.ThreekingdomArcher:
                    {
                        var effect = areaEffectObject.AddComponent<ThreekingdomArcherAttackObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.AxeBoomerang:
                    {
                        var effect = areaEffectObject.AddComponent<AxeBoomerangAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.VikingSmallAxe:
                    {
                        var effect = areaEffectObject.AddComponent<VikingSmallAxeAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.VikingThunderHammer:
                    {
                        var effect = areaEffectObject.AddComponent<VikingThunderHammerAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.VikingThunderWarriorLightning:
                    {
                        var effect = areaEffectObject.AddComponent<VikingThunderWarriorLightningAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.FallingRock:
                    {
                        var effect = areaEffectObject.AddComponent<FallingRockAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.FirePillar:
                    {
                        var effect = areaEffectObject.AddComponent<FirePillarAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.ZhugeliangLaser:
                    {
                        var effect = areaEffectObject.AddComponent<ZhugeliangLaserAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.TrickWarriorSpear:
                    {
                        var effect = areaEffectObject.AddComponent<TrickWarriorSpearAreaEffect>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.LokiSpear:
                    {
                        var effect = areaEffectObject.AddComponent<LokiSpearAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.KrakenInkBall:
                    {
                        var effect = areaEffectObject.AddComponent<KrakenInkBallAreaEffect>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.MjolnirBoomerang:
                    {
                        var effect = areaEffectObject.AddComponent<MjolnirBoomerangAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.HomingProjectile:
                    {
                        var effect = areaEffectObject.AddComponent<HomingProjectileAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.FreyaMagicCircle:
                    {
                        var effect = areaEffectObject.AddComponent<SpineBodyAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.FreyaMagicCircle);
                        return effect;
                    }
                case AreaEffectType.OdinGungnir:
                    {
                        var effect = areaEffectObject.AddComponent<OdinGungnirAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.LavaMachineReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.LavaMachineReflectionObject, "Stages/Projectiles/LavaBallRadius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.XxperManReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.XxperManReflectionObject, "Stages/Projectiles/LaserBallRadius0_7.prefab");
                        return effect;
                    }
                case AreaEffectType.XxperManLaser:
                    {
                        var effect = areaEffectObject.AddComponent<LaserAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.XxperManLaser, "Stages/AreaEffects/Laser/XxperManLaser.prefab");
                        return effect;
                    }
                case AreaEffectType.BubbleGum_Default:
                case AreaEffectType.BubbleGum_Spy:
                    {
                        var effect = areaEffectObject.AddComponent<BubbleGumAreaEffectObject>();
                        effect.AllocateSharedResources(areaEffectType);
                        return effect;
                    }
                case AreaEffectType.ChewingBag:
                    {
                        var effect = areaEffectObject.AddComponent<ChewingBagAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.KennyoSpinBeads:
                    {
                        var effect = areaEffectObject.AddComponent<KennyoSpinBeads>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.ShockBomb:
                    {
                        var effect = areaEffectObject.AddComponent<ShockBombAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.BouncingClaw:
                    {
                        var effect = areaEffectObject.AddComponent<BouncingClawAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.IronFist:
                    {
                        var effect = areaEffectObject.AddComponent<IronFistAreaEffectObject>();
                        effect.AllocateSharedResources(isTranscendent: false);
                        return effect;
                    }
                case AreaEffectType.IronFistTranscendent:
                    {
                        var effect = areaEffectObject.AddComponent<IronFistAreaEffectObject>();
                        effect.AllocateSharedResources(isTranscendent: true);
                        return effect;
                    }
                case AreaEffectType.WindField:
                    {
                        var effect = areaEffectObject.AddComponent<WindFieldAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.ShootingStar:
                    {
                        var effect = areaEffectObject.AddComponent<ShootingStarAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.ShootingStarExplosion:
                    {
                        var effect = areaEffectObject.AddComponent<ShootingStarExplosionAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.SusanooSpinBead:
                    {
                        var effect = areaEffectObject.AddComponent<SusanooSpinBead>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.SusanooLightning:
                    {
                        var effect = areaEffectObject.AddComponent<SusanooLightningAreaEffect>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.SolderManLaser:
                    {
                        var effect = areaEffectObject.AddComponent<LaserAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.SolderManLaser, "Stages/AreaEffects/Laser/XxperManLaser.prefab");
                        return effect;
                    }
                case AreaEffectType.SolderManReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.SolderManReflectionObject, "Stages/Projectiles/LaserBallRadius0_7.prefab");
                        return effect;
                    }
                case AreaEffectType.IceDireWolfReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.IceDireWolfReflectionObject, "Stages/AreaEffects/IceGolem_Projectile.prefab");
                        return effect;
                    }
                case AreaEffectType.IceThreeLeapsBugReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<IceThreeLeapsBugReflectionAreaEffect>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.EgyptSnakeDruidReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.EgyptSnakeDruidReflectionObject, "Stages/AreaEffects/SnakeDruidReflectionObject.prefab");
                        return effect;
                    }
                case AreaEffectType.EgyptSphinxReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.EgyptSphinxReflectionObject, "Stages/AreaEffects/SnakeDruidReflectionObject.prefab");
                        return effect;
                    }
                case AreaEffectType.ReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.DelayAreaEffectObject:
                    {
                        var effect = areaEffectObject.AddComponent<DelayAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.DrLeeReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.DrLeeReflectionObject, "Stages/Projectiles/LavaBallRadius0_9.prefab");
                        return effect;
                    }
                case AreaEffectType.CaptainMilitaryBoomerang:
                    {
                        var effect = areaEffectObject.AddComponent<BoomerangAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.CaptainMilitaryBoomerang, "Stages/Projectiles/LaserBallRadius0_7.prefab");
                        return effect;
                    }
                case AreaEffectType.LavaCrossSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.LavaCrossSplitObject, "Stages/AreaEffects/LavaMachineSplitObject.prefab", "Stages/Projectiles/LavaOvalBallRadius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.TrampReflectionCrossSplitObjet:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionSplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.TrampReflectionCrossSplitObjet, "Stages/Projectiles/StarRadius0_6.prefab", "Stages/Projectiles/StarSmallRadius0_3.prefab");
                        return effect;
                    }
                case AreaEffectType.AssaultLeaderReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.AssaultLeaderReflectionObject, "Stages/Projectiles/RedSlash2Radius0_9.prefab");
                        return effect;
                    }
                case AreaEffectType.SolderManSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.SolderManSplitObject, "Stages/Projectiles/LaserBallRadius0_7.prefab", "Stages/Projectiles/LaserBallRadius0_3.prefab");
                        return effect;
                    }
                case AreaEffectType.HectorReflectiveShield:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.HectorReflectiveShield, "Stages/Projectiles/HectorReflectiveShield.prefab");
                        return effect;
                    }
                case AreaEffectType.ZeusReflectionHomingObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.ZeusReflectionHomingObject, "Stages/Projectiles/LightningBall2Radius1_8.prefab");
                        return effect;
                    }
                case AreaEffectType.OdinReflectionHomingObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.OdinReflectionHomingObject, "Stages/Projectiles/LightningBall2Radius1_8.prefab");
                        return effect;
                    }
                case AreaEffectType.ViracochaReflectionHomingObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.ViracochaReflectionHomingObject, "Stages/Projectiles/LightningBall2Radius1_8.prefab");
                        return effect;
                    }
                case AreaEffectType.JangGakReflectionSpltObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionSplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.JangGakReflectionSpltObject, "Stages/Projectiles/ThrowingKnifeRadius0_7.prefab", "Stages/Projectiles/BlackThorn_Radius0_2.prefab");
                        return effect;
                    }
                case AreaEffectType.GreekGeneralReturningSpear:
                    {
                        var effect = areaEffectObject.AddComponent<ReturningAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.GreekGeneralReturningSpear, "Stages/Projectiles/GreekGeneralSpear.prefab");
                        return effect;
                    }
                case AreaEffectType.CaoCaoReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.CaoCaoReflectionObject, "Stages/Projectiles/RedSlashObject_Radius1.prefab");
                        return effect;
                    }
                case AreaEffectType.JyyuReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.JyyuReflectionObject, "Stages/Projectiles/Fire_Radius1.prefab");
                        return effect;
                    }
                case AreaEffectType.AxeLeaderReflectingAxe:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.AxeLeaderReflectingAxe, "Stages/AreaEffects/VikingAxeLeader/VikingSmallAxe.prefab");
                        return effect;
                    }
                case AreaEffectType.AxeLeaderReturningAxe:
                    {
                        var effect = areaEffectObject.AddComponent<ReturningAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.AxeLeaderReturningAxe, "Stages/AreaEffects/VikingAxeLeader/VikingSmallAxe.prefab");
                        return effect;
                    }
                case AreaEffectType.DrHomingMissileObject:
                    {
                        var effect = areaEffectObject.AddComponent<DrHomingMissileAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.DrHomingSmallMissileObject:
                    {
                        var effect = areaEffectObject.AddComponent<DrHomingSmallMissileAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.SunquanSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.SunquanSplitObject, "Stages/Projectiles/FireBall_Radius1.prefab", "Stages/Projectiles/FireBallSmall_Radius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.SunquanReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.SunquanReflectionObject, "Stages/Projectiles/RedSlashObject_Radius1.prefab");
                        return effect;
                    }
                case AreaEffectType.ZhugeliangReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.ZhugeliangReflectionObject, "Stages/Projectiles/Fire_Radius1.prefab");
                        return effect;
                    }
                case AreaEffectType.DongZhuoReflectionProjectileObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.DongZhuoReflectionProjectileObject, "Stages/Projectiles/ThrowingKnifeRadius0_7.prefab");
                        return effect;
                    }
                case AreaEffectType.TrickWarriorReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.TrickWarriorReflectionObject, "Stages/Projectiles/TrickWarriorReflectionObject.prefab");
                        return effect;
                    }
                case AreaEffectType.LokiReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.LokiReflectionObject, "Stages/Projectiles/Dark_Radius1.prefab");
                        return effect;
                    }
                case AreaEffectType.PoisonMachinePoisonousAreaEffect:
                    {
                        var effect = areaEffectObject.AddComponent<PoisonMachinePoisonousAreaEffect>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.PoisonMachineHomingProjectile:
                    {
                        var effect = areaEffectObject.AddComponent<PoisonMachineHomingProjectile>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.MitsuhideReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.MitsuhideReflectionObject, "Stages/Projectiles/RedSlashObject_Radius1.prefab");
                        return effect;
                    }
                case AreaEffectType.KojiroReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.KojiroReflectionObject, "Stages/Projectiles/RedSlashObject_Radius1.prefab");
                        return effect;
                    }
                case AreaEffectType.RanmaruReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.RanmaruReflectionObject, "Stages/Projectiles/RedSlashObject_Radius0_75.prefab");
                        return effect;
                    }
                case AreaEffectType.KennyoSplitingProjectile:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.KennyoSplitingProjectile, "Stages/Projectiles/KennyoBigProjectile.prefab", "Stages/Projectiles/KennyoSmallProjectile.prefab");
                        return effect;
                    }
                case AreaEffectType.IeyasuReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.IeyasuReflectionObject, "Stages/Projectiles/StoneSmall_Radius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.IeyasuSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.IeyasuSplitObject, "Stages/Projectiles/IeyasuSplitProjectile.prefab", "Stages/Projectiles/IeyasuSplitedProjectile.prefab");
                        return effect;
                    }
                case AreaEffectType.ThorReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.RanmaruReflectionObject, "Stages/Projectiles/LightningRadius0_4.prefab");
                        return effect;
                    }
                case AreaEffectType.SusanooBeadObject:
                    {
                        var effect = areaEffectObject.AddComponent<SusanooBeadObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.FreyaReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.FreyaReflectionObject, "Stages/Projectiles/Dark_Radius1.prefab");
                        return effect;
                    }
                case AreaEffectType.TornadoObject:
                    {
                        var effect = areaEffectObject.AddComponent<TornadoAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.PoisonPotionObject:
                    {
                        var effect = areaEffectObject.AddComponent<PoisonPotion>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.PoisonBall:
                    {
                        var effect = areaEffectObject.AddComponent<PoisonBall>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.EasternEmpireSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.EasternEmpireSplitObject, "Stages/Projectiles/Crusades_EliteBallista_misile.prefab", "Stages/Projectiles/BlackThorn_Radius0_4.prefab");
                        return effect;
                    }
                case AreaEffectType.EasternEmpireReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.EasternEmpireReflectionObject, "Stages/Projectiles/LavaBallRadius0_3.prefab");
                        return effect;
                    }
                case AreaEffectType.CrusadesCaptainBoomerangObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReturningAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.CrusadesCaptainBoomerangObject, "Stages/Projectiles/ArrowRadius0_3.prefab");
                        return effect;
                    }
                case AreaEffectType.HenriIIArrowObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReturningAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.HenriIIArrowObject, "Stages/Projectiles/ArrowRadius0_3.prefab");
                        return effect;
                    }
                case AreaEffectType.HenriIISandAreaEffect:
                    {
                        var effect = areaEffectObject.AddComponent<SpineBodyAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.HenriIISandAreaEffect);
                        return effect;
                    }
                case AreaEffectType.HenriIIMaceObject:
                    {
                        var effect = areaEffectObject.AddComponent<BoomerangObject>();
                        effect.AllocateSharedResources(AreaEffectType.HenriIIMaceObject, "Stages/Projectiles/HenriIIMace.prefab");
                        return effect;
                    }
                case AreaEffectType.AlAshrafKhalilSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.AlAshrafKhalilSplitObject, "Stages/Projectiles/FireBall_Radius1.prefab", "Stages/Projectiles/FireBallSmall_Radius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.AlAshrafKhalilReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.AlAshrafKhalilReflectionObject, "Stages/Projectiles/Dark_Radius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.SuperSolderBoomerangObject:
                    {
                        var effect = areaEffectObject.AddComponent<BoomerangObject>();
                        effect.AllocateSharedResources(AreaEffectType.SuperSolderBoomerangObject, "Stages/AreaEffects/SuperSoldierBoomerangObject.prefab");
                        return effect;
                    }
                case AreaEffectType.RailGunReflectionObjet:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.RailGunReflectionObjet, "Stages/Projectiles/LaserBallRadius0_7.prefab");
                        return effect;
                    }
                case AreaEffectType.RailGunSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.RailGunSplitObject, "Stages/Projectiles/StarRadius0_6.prefab", "Stages/Projectiles/StarSmallRadius0_3.prefab");
                        return effect;
                    }
                case AreaEffectType.ExplosionAreaEffect:
                    {
                        var effect = areaEffectObject.AddComponent<ExplosionAreaEffect>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.ThunderboltReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.ThunderboltReflectionObject, "Stages/Projectiles/LaserBallRadius0_7.prefab");
                        return effect;
                    }
                case AreaEffectType.HernandoReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.HernandoReflectionObject, "Stages/Projectiles/RedSlashObject_Radius0_75.prefab");
                        return effect;
                    }
                case AreaEffectType.AlmagroSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.AlmagroSplitObject, "Stages/Projectiles/FireBall_Radius1.prefab", "Stages/Projectiles/FireBallSmall_Radius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.TupacAmaruSandMakeObject:
                    {
                        var effect = areaEffectObject.AddComponent<TupacAmaruSandMakeObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.GonzaloPizarroReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.GonzaloPizarroReflectionObject, "Stages/Projectiles/RedSlashObject_Radius0_75.prefab");
                        return effect;
                    }
                case AreaEffectType.Carlos1ReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.Carlos1ReflectionObject, "Stages/AreaEffects/VikingAxeLeader/Carlos1ReflectionObject.prefab");
                        return effect;
                    }
                case AreaEffectType.IntiTentacleObject:
                    {
                        var effect = areaEffectObject.AddComponent<IntiTentacleObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.SuppressionCaptainReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.SuppressionCaptainReflectionObject, "Stages/Projectiles/LightningBallRadius0_75.prefab");
                        return effect;
                    }
                case AreaEffectType.DrKooReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.DrKooReflectionObject, "Stages/Projectiles/LightningBallRadius0_75.prefab");
                        return effect;
                    }
                case AreaEffectType.ValorRangerReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.ValorRangerReflectionObject, "Stages/Projectiles/LightningBallRadius0_75.prefab");
                        return effect;
                    }
                case AreaEffectType.ValorRangerSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.ValorRangerSplitObject, "Stages/Projectiles/LaserBallRadius0_7.prefab", "Stages/Projectiles/LaserBallRadius0_3.prefab");
                        return effect;
                    }
                case AreaEffectType.SpaceShipIceObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpaceShipIceAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.SpaceShipTranscendentIceObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpaceShipTranscendentIceObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.LavaMachineHardBoomerangObject:
                    {
                        var effect = areaEffectObject.AddComponent<BoomerangAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.LavaMachineHardBoomerangObject, "Stages/Projectiles/LavaOvalBallRadius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.SpaceShipIceGroup:
                    {
                        var effect = areaEffectObject.AddComponent<SpaceShipIceGroupAreaEffect>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.DrKimSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.DrKimSplitObject, "Stages/Projectiles/FireBall_Radius1.prefab", "Stages/Projectiles/FireBallSmall_Radius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.GenieSandHomingObject:
                    {
                        var effect = areaEffectObject.AddComponent<GenieSandHomingObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.WitchQueenSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.WitchQueenSplitObject, "Stages/Projectiles/Dark_Radius1.prefab", "Stages/AreaEffects/Freya_Homing.prefab");
                        return effect;
                    }
                case AreaEffectType.BlackCyclopsSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.BlackCyclopsSplitObject, "Stages/Projectiles/BlackCyclopsSplitObject.prefab", "Stages/Projectiles/BlackThorn_Radius0_4.prefab");
                        return effect;
                    }
                case AreaEffectType.HarunAlRashidBoomerangObject:
                    {
                        var effect = areaEffectObject.AddComponent<BoomerangObject>();
                        effect.AllocateSharedResources(AreaEffectType.HarunAlRashidBoomerangObject, "Stages/Projectiles/RedSlashObject_Radius0_75.prefab");
                        return effect;
                    }
                case AreaEffectType.PeribanouSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.PeribanouSplitObject, "Stages/Projectiles/FireBall_Radius1.prefab", "Stages/Projectiles/FireBallSmall_Radius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.SchaibarReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.SchaibarReflectionObject, "Stages/Projectiles/LightningBallRadius0_75.prefab");
                        return effect;
                    }
                case AreaEffectType.DynamiteAreaEffectObject:
                    {
                        var effect = areaEffectObject.AddComponent<ExplosiveAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.DynamiteAreaEffectObject, "Stages/AreaEffects/Dynamite_0.prefab");
                        return effect;
                    }
                case AreaEffectType.MiningKingHomingSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingSplitReflectionObject>();
                        effect.AllocateSharedResources(AreaEffectType.MiningKingHomingSplitObject, "Stages/Projectiles/GoldBigProjectile.prefab");
                        return effect;
                    }
                case AreaEffectType.GoldReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.GoldReflectionObject, "Stages/Projectiles/GoldSmallProjectileRadius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.WesternBigBulletReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.WesternBigBulletReflectionObject, "Stages/Projectiles/BulletBigRadius0_4.prefab");
                        return effect;
                    }
                case AreaEffectType.WyattEarpReflectionSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionSplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.WyattEarpReflectionSplitObject, "Stages/Projectiles/BulletBigRadius0_4.prefab", "Stages/Projectiles/BulletSmallRadius0_2.prefab");
                        return effect;
                    }
                case AreaEffectType.TrampHardReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.TrampHardReflectionObject, "Stages/Projectiles/LaserBallRadius0_7.prefab");
                        return effect;
                    }
                case AreaEffectType.TrampHardSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.TrampHardSplitObject, "Stages/Projectiles/StarRadius0_6.prefab", "Stages/Projectiles/StarSmallRadius0_3.prefab");
                        return effect;
                    }
                case AreaEffectType.DrLeeHardHomingReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.DrLeeHardHomingReflectionObject, "Stages/Projectiles/LavaBallRadius1_3.prefab");
                        return effect;
                    }
                case AreaEffectType.CaptainLoveDashReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.CaptainLoveDashReflectionObject, "Stages/Projectiles/LaserBallRadius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.CaptainLoveSectorReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.CaptainLoveSectorReflectionObject, "Stages/Projectiles/LightningBallRadius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.JesseJameReflectionHomingBoomerangObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingBoomerangObject>();
                        effect.AllocateSharedResources(AreaEffectType.JesseJameReflectionHomingBoomerangObject, "Stages/Projectiles/Boomerang.prefab");
                        return effect;
                    }
                case AreaEffectType.JesseJameBoomerangObject:
                    {
                        var effect = areaEffectObject.AddComponent<BoomerangObject>();
                        effect.AllocateSharedResources(AreaEffectType.JesseJameBoomerangObject, "Stages/Projectiles/Boomerang.prefab");
                        return effect;
                    }
                case AreaEffectType.HeadMinerHardSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.HeadMinerHardSplitObject, "Stages/Projectiles/GoldBigProjectile.prefab", "Stages/Projectiles/GoldSmallProjectileRadius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.KidSplitReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitReflectionObject>();
                        effect.AllocateSharedResources(AreaEffectType.KidSplitReflectionObject, "Stages/Projectiles/BulletBigRadius0_4.prefab");
                        return effect;
                    }
                case AreaEffectType.KidReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.KidReflectionObject, "Stages/Projectiles/BulletSmallRadius0_2.prefab");
                        return effect;
                    }
                case AreaEffectType.WatchManDollReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.WatchManDollReflectionObject, "Stages/Projectiles/LightningRadius0_4.prefab");
                        return effect;
                    }
                case AreaEffectType.JamesWattReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.JamesWattReflectionObject, "Stages/Projectiles/LightningBallRadius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.AbandoneSpecimenSpinAreaEffect:
                    {
                        var effect = areaEffectObject.AddComponent<SpinMoveObject>();
                        effect.AllocateSharedResources(AreaEffectType.AbandoneSpecimenSpinAreaEffect, "Stages/Projectiles/SpeciMenProjectile.prefab");
                        return effect;
                    }
                case AreaEffectType.NicolasFlamelReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.NicolasFlamelReflectionObject, "Stages/Projectiles/LightningBallRadius0_75.prefab");
                        return effect;
                    }
                case AreaEffectType.NicolasFlamelSpinObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpinMoveObject>();
                        effect.AllocateSharedResources(AreaEffectType.NicolasFlamelSpinObject, "Stages/Projectiles/LightningBallRadius0_75.prefab");
                        return effect;
                    }
                case AreaEffectType.BattleGolemBoomerangObject:
                    {
                        var effect = areaEffectObject.AddComponent<BoomerangObject>();
                        effect.AllocateSharedResources(AreaEffectType.BattleGolemBoomerangObject, "Stages/Projectiles/RedSlashObject_Radius0_75.prefab");
                        return effect;
                    }
                case AreaEffectType.SpecialOperationSoldierReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.SpecialOperationSoldierReflectionObject, "Stages/Projectiles/LightningBallRadius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.LeaderBHardReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.LeaderBHardReflectionObject, "Stages/Projectiles/LightningBallRadius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.LeaderBHardSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionSplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.LeaderBHardSplitObject, "Stages/Projectiles/StarRadius0_6.prefab", "Stages/Projectiles/StarSmallRadius0_3.prefab");
                        return effect;
                    }
                case AreaEffectType.WilhelmIIReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.WilhelmIIReflectionObject, "Stages/Projectiles/BulletBigRadius0_4.prefab");
                        return effect;
                    }
                case AreaEffectType.PhilippePetainSpinObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpinMoveObject>();
                        effect.AllocateSharedResources(AreaEffectType.PhilippePetainSpinObject, "Stages/Projectiles/RedSlash2Radius0_9.prefab");
                        return effect;
                    }
                case AreaEffectType.ErnestKingReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.ErnestKingReflectionObject, "Stages/Projectiles/BulletBigRadius0_4.prefab");
                        return effect;
                    }
                case AreaEffectType.JosephStalinReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.JosephStalinReflectionObject, "Stages/Projectiles/CannonballRadius0_85.prefab");
                        return effect;
                    }
                case AreaEffectType.JosephStalinSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.JosephStalinSplitObject, "Stages/Projectiles/CannonballRadius1_0.prefab", "Stages/Projectiles/CannonballRadius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.XxperManHardSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.XxperManHardSplitObject, "Stages/Projectiles/StarRadius0_6.prefab", "Stages/Projectiles/StarSmallRadius0_3.prefab");
                        return effect;
                    }
                case AreaEffectType.XxperManHardSpinObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpinMoveObject>();
                        effect.AllocateSharedResources(AreaEffectType.XxperManHardSpinObject, "Stages/Projectiles/LightningBallRadius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.XxperManHardReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.XxperManHardReflectionObject, "Stages/Projectiles/LightningBallRadius1_5.prefab");
                        return effect;
                    }
                case AreaEffectType.AdolfHitlerReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.AdolfHitlerReflectionObject, "Stages/Projectiles/BulletBigRadius0_4.prefab");
                        return effect;
                    }
                case AreaEffectType.WaffenSSReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.WaffenSSReflectionObject, "Stages/Projectiles/BulletSmallRadius0_2.prefab");
                        return effect;
                    }
                case AreaEffectType.ShootingChampionSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.ShootingChampionSplitObject, "Stages/Projectiles/BulletBigRadius0_4.prefab", "Stages/Projectiles/BulletSmallRadius0_2.prefab");
                        return effect;
                    }
                case AreaEffectType.WeightliftingGiantReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.WeightliftingGiantReflectionObject, "Stages/Projectiles/FitnessDiscRadius1_0.prefab");
                        return effect;
                    }
                case AreaEffectType.GloryStatueReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.GloryStatueReflectionObject, "Stages/Projectiles/Fire_Radius1.prefab");
                        return effect;
                    }
                case AreaEffectType.BeggarKingReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.BeggarKingReflectionObject, "Stages/Projectiles/Stone_Radius1.prefab");
                        return effect;
                    }
                case AreaEffectType.DriverReflectionHomingBoomerangObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingBoomerangObject>();
                        effect.AllocateSharedResources(AreaEffectType.DriverReflectionHomingBoomerangObject, "Stages/Projectiles/HandleRadius0_75.prefab");
                        return effect;
                    }
                case AreaEffectType.InvestmentKingSpinObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpinMoveObject>();
                        effect.AllocateSharedResources(AreaEffectType.InvestmentKingSpinObject, "Stages/Projectiles/MoneyRadius0_6.prefab");
                        return effect;
                    }
                case AreaEffectType.InvestmentKingCircleBoomerangObject:
                    {
                        var effect = areaEffectObject.AddComponent<CircleBoomerangObject>();
                        effect.AllocateSharedResources(AreaEffectType.InvestmentKingCircleBoomerangObject, "Stages/Projectiles/MoneyHammerRadius1_1.prefab");
                        return effect;
                    }
                case AreaEffectType.SnaepCattSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.SnaepCattSplitObject, "Stages/Projectiles/SoundImpulseRadius0_9.prefab", "Stages/Projectiles/SoundImpulseSmallRadius0_45.prefab");
                        return effect;

                    }
                case AreaEffectType.BeggarKingReflectionPoisonousAreaEffectObject:
                    {
                        var effect = areaEffectObject.AddComponent<BeggarKingReflectionPoisonousAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.DriverReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.DriverReflectionObject, "Stages/Projectiles/WheelRadius1_1.prefab");
                        return effect;
                    }
                case AreaEffectType.MileJackReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.MileJackReflectionObject, "Stages/Projectiles/SoundImpulseRadius0_9.prefab");
                        return effect;
                    }
                case AreaEffectType.MileJackReflectionSmallObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.MileJackReflectionSmallObject, "Stages/Projectiles/SoundImpulseSmallRadius0_45.prefab");
                        return effect;
                    }
                case AreaEffectType.SuppressionCaptainSpinObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpinMoveObject>();
                        effect.AllocateSharedResources(AreaEffectType.SuppressionCaptainSpinObject, "Stages/Projectiles/LightningBallRadius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.SolderManHardSpinObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpinMoveObject>();
                        effect.AllocateSharedResources(AreaEffectType.SolderManHardSpinObject, "Stages/Projectiles/LightningBallRadius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.DiagonalProjectileCreateObject:
                    {
                        var effect = areaEffectObject.AddComponent<DiagonalProjectileCreateObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.SpaceHunterReflectionHomingObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.SpaceHunterReflectionHomingObject, "Stages/Projectiles/LightningBall2Radius1_5.prefab");
                        return effect;
                    }
                case AreaEffectType.MushroomHeadReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.MushroomHeadReflectionObject, "Stages/Projectiles/Stone_Radius1.prefab");
                        return effect;
                    }
                case AreaEffectType.DancingTriangleObject:
                    {
                        var effect = areaEffectObject.AddComponent<DancingTriangleObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.GrassEatingFlowerReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.GrassEatingFlowerReflectionObject, "Stages/Projectiles/ThornVineBigRadius0_7.prefab");
                        return effect;
                    }
                case AreaEffectType.GrassEatingFlowerSpinObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpinMoveObject>();
                        effect.AllocateSharedResources(AreaEffectType.GrassEatingFlowerSpinObject, "Stages/Projectiles/ThornVineSmallRadius0_45.prefab");
                        return effect;
                    }
                case AreaEffectType.FirstMateReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.FirstMateReflectionObject, "Stages/Projectiles/MushroomRadius1_0.prefab");
                        return effect;
                    }
                case AreaEffectType.EnchantingGlowNormalAreaEffectObject:
                    {
                        var effect = areaEffectObject.AddComponent<EnchantingGlowNormalAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.EnchantingGlowBodyObject:
                    {
                        var effect = areaEffectObject.AddComponent<EnchantingGlowBodyObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.StageMeteorAreaEffectObject:
                    {
                        var effect = areaEffectObject.AddComponent<StageMeteorAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.StageLightningAreaEffectObject:
                    {
                        var effect = areaEffectObject.AddComponent<StageLightningAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.StageSandAreaEffectObject:
                    {
                        var effect = areaEffectObject.AddComponent<StageSandAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.StageIceAreaEffectObject:
                    {
                        var effect = areaEffectObject.AddComponent<StageIceAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.HackedRobotSpinObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpinMoveObject>();
                        effect.AllocateSharedResources(AreaEffectType.HackedRobotSpinObject, "Stages/Projectiles/LightningBallRadius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.HackedRobotReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.HackedRobotReflectionObject, "Stages/Projectiles/ThornVineBigRadius1_0.prefab");
                        return effect;
                    }
                case AreaEffectType.FirstMateHardReflectionHomingObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.FirstMateHardReflectionHomingObject, "Stages/Projectiles/MushroomRadius1_5_Rotation.prefab");
                        return effect;
                    }
                case AreaEffectType.PepperAssaultTrooperBoomerangObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingBoomerangObject>();
                        effect.AllocateSharedResources(AreaEffectType.PepperAssaultTrooperBoomerangObject, "Stages/Projectiles/PepperRadius0_25.prefab");
                        return effect;
                    }
                case AreaEffectType.PepperAssaultExplosiveObject:
                    {
                        var effect = areaEffectObject.AddComponent<ExplosiveAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.PepperAssaultExplosiveObject, "Stages/Projectiles/PepperRadius0_25.prefab");
                        return effect;
                    }
                case AreaEffectType.PirateCaptainReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.PirateCaptainReflectionObject, "Stages/Projectiles/ThornVineBigRadius1_5.prefab");
                        return effect;
                    }
                case AreaEffectType.EramixMammothChiefReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.EramixMammothChiefReflectionObject, "Stages/Projectiles/Stone_Radius1.prefab");
                        return effect;
                    }
                case AreaEffectType.ThreeLeapsBugPoisonHomingObject:
                    {
                        var effect = areaEffectObject.AddComponent<ThreeLeapsBugPoisonHomingObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.PhilippePetainSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.PhilippePetainSplitObject, "Stages/Projectiles/BlackSlash_Radius2_0.prefab", "Stages/Projectiles/BlackSlash_Radius1.prefab");
                        return effect;
                    }
                case AreaEffectType.EraMixWilhelmIISplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.EraMixWilhelmIISplitObject, "Stages/Projectiles/BlackSlash_Radius2_0.prefab", "Stages/Projectiles/BlackSlash_Radius1.prefab");
                        return effect;

                    }
                case AreaEffectType.EraMixAnubisMaskSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.EraMixAnubisMaskSplitObject, "Stages/Projectiles/Stone_Radius1.prefab", "Stages/Projectiles/StoneSmall_Radius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.EraMixJosephStalinSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.EraMixJosephStalinSplitObject, "Stages/Projectiles/CannonballRadius1_0.prefab", "Stages/Projectiles/CannonballRadius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.EraMixJosephStalinReflectionHomingObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.EraMixJosephStalinReflectionHomingObject, "Stages/Projectiles/CannonballRadius1_0.prefab");
                        return effect;
                    }
                case AreaEffectType.SearchLeaderHardSpinObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpinMoveObject>();
                        effect.AllocateSharedResources(AreaEffectType.SearchLeaderHardSpinObject, "Stages/Projectiles/StarSmallRadius0_3.prefab");
                        return effect;
                    }
                case AreaEffectType.CaptainMilitaryHardReflectionHomingBoomerangObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingBoomerangObject>();
                        effect.AllocateSharedResources(AreaEffectType.CaptainMilitaryHardReflectionHomingBoomerangObject, "Stages/Projectiles/LightningBallRedRadius0_7.prefab");
                        return effect;
                    }
                case AreaEffectType.EraMixBenitoMussoliniReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.EraMixBenitoMussoliniReflectionObject, "Stages/Projectiles/Fire_Radius1.prefab");
                        return effect;
                    }
                case AreaEffectType.EraMixBenitoMussoliniSpinObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpinMoveObject>();
                        effect.AllocateSharedResources(AreaEffectType.EraMixBenitoMussoliniSpinObject, "Stages/Projectiles/BossRa_ProjectileRadius0_7.prefab");
                        return effect;
                    }
                case AreaEffectType.EraMixSnakeDruidReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.EraMixSnakeDruidReflectionObject, "Stages/AreaEffects/SnakeDruidReflectionObjectRadius0_75.prefab");
                        return effect;
                    }
                case AreaEffectType.EraMixHochmeisterRedReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.EraMixHochmeisterRedReflectionObject, "Stages/Projectiles/RedSlashObject_Radius0_75.prefab");
                        return effect;
                    }
                case AreaEffectType.DrBinHardReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.DrBinHardReflectionObject, "Stages/Projectiles/Fire_Radius1.prefab");
                        return effect;
                    }
                case AreaEffectType.CaptainMilitaryHardSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.CaptainMilitaryHardSplitObject, "Stages/Projectiles/LightningBallRadius1_8.prefab", "Stages/Projectiles/LightningBallRedRadius1_0.prefab");
                        return effect;
                    }
                case AreaEffectType.TestReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.TestReflectionObject, "Stages/Projectiles/TestProjectileRadius1_0.prefab");
                        return effect;
                    }
                case AreaEffectType.TestSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.TestSplitObject, "Stages/Projectiles/TestProjectileRadius1_0.prefab", "Stages/Projectiles/TestProjectileRadius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.TestSpinMoveObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpinMoveObject>();
                        effect.AllocateSharedResources(AreaEffectType.TestSpinMoveObject, "Stages/Projectiles/TestProjectileRadius1_0.prefab");
                        return effect;
                    }
                case AreaEffectType.TestReflectionHomingObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.TestReflectionHomingObject, "Stages/Projectiles/TestProjectileRadius1_0.prefab");
                        return effect;
                    }
                case AreaEffectType.TestReflectionHomingBoomerangObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingBoomerangObject>();
                        effect.AllocateSharedResources(AreaEffectType.TestReflectionHomingBoomerangObject, "Stages/Projectiles/TestProjectileRadius1_0.prefab");
                        return effect;
                    }
                case AreaEffectType.EraMixHectorReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.EraMixHectorReflectionObject, "Stages/Projectiles/HectorReflectiveShieldRadius1_2.prefab");
                        return effect;
                    }
                case AreaEffectType.EraMixHectorSpinMoveObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpinMoveObject>();
                        effect.AllocateSharedResources(AreaEffectType.EraMixHectorSpinMoveObject, "Stages/Projectiles/HectorReflectiveShieldRadius0_6.prefab");
                        return effect;
                    }
                case AreaEffectType.EraMixCrusadesReflectionHomingBoomerangObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingBoomerangObject>();
                        effect.AllocateSharedResources(AreaEffectType.EraMixCrusadesReflectionHomingBoomerangObject , "Stages/Projectiles/ArrowRadius0_3.prefab");
                        return effect;
                    }
                case AreaEffectType.EraMixCrusadesReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.EraMixCrusadesReflectionObject, "Stages/Projectiles/FireArrowRadius0_4.prefab");
                        return effect;
                    }
                case AreaEffectType.EraMixRichardISplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.EraMixRichardISplitObject, "Stages/Projectiles/Stone_Radius1_5.prefab", "Stages/Projectiles/StoneSmall_Radius0_5.prefab");
                        return effect;
                    }
                case AreaEffectType.EraMixRichardIReflectionHomingObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingAreaEffectObject>();
                        effect.AllocateSharedResources(AreaEffectType.EraMixRichardIReflectionHomingObject, "Stages/Projectiles/Richard1ShieldRadius3_0.prefab");
                        return effect;
                    }
                case AreaEffectType.EraMixAlAshrafKhalilBoomerangObject:
                    {
                        var effect = areaEffectObject.AddComponent<BoomerangObject>();
                        effect.AllocateSharedResources(AreaEffectType.EraMixAlAshrafKhalilBoomerangObject, "Stages/Projectiles/RedSlashObject_Radius1.prefab");
                        return effect;
                    }
                case AreaEffectType.TestBoomerangObject:
                    {
                        var effect = areaEffectObject.AddComponent<BoomerangObject>();
                        effect.AllocateSharedResources(AreaEffectType.TestBoomerangObject, "Stages/Projectiles/TestProjectileRadius1_0.prefab");
                        return effect;
                    }
                case AreaEffectType.SplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.EraMixZeusSpecialReflectionHomingObject:
                    {
                        var effect = areaEffectObject.AddComponent<EraMixZeusSpecialReflectionHomingObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.ReflectionSplitObject:  //반사 하면서 돌아다니다 일정시간 이후 분열하는 투사체
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionSplitAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.SpinMoveObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpinMoveObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.LightningMachineHardHomingSplitSpinMoveObject:
                    {
                        var effect = areaEffectObject.AddComponent<LightningMachineHardHomingSplitSpinMoveObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.SpecialOperationsSoldierHardReflectionHomingObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpecialOperationsSoldierHardReflectionHomingObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.MonsterInfinitySpinBladeObject:
                    {
                        var effect = areaEffectObject.AddComponent<MonsterInfinitySpinBladeObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.EraMixGuanyuLightningCreateObject:
                    {
                        var effect = areaEffectObject.AddComponent<EraMixGuanyuLightningCreateObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.EraMixMileJackReflectionFireSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<EraMixMileJackReflectionFireSplitObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.ReflectionHomingBoomerangObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingBoomerangObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.ReflectionMultiHitObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionAreaEffectMultiHitObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.HomingSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<HomingSplitObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.ReflectionHomingSplitObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingSplitObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.ReflectionHomingObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionHomingAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.LandWhaleBasicWhaleObject:
                    {
                        var effect = areaEffectObject.AddComponent<LandWhaleBasicWhaleObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.LandWhaleTranscendentWhaleObject:
                    {
                        var effect = areaEffectObject.AddComponent<LandWhaleTranscendentWhaleObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.LandWhaleTranscendentFireObject:
                    {
                        var effect = areaEffectObject.AddComponent<LandWhaleTranscendentFireObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.WoodTentacleObject:
                    {
                        var effect = areaEffectObject.AddComponent<WoodTentacleObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.EraMixKojiroSplitToReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<EraMixKojiroSplitToReflectionObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.StickySlimeNormalObject_Default:
                case AreaEffectType.StickySlimeNormalObject_Bongjun:
                    {
                        var effect = areaEffectObject.AddComponent<StickySlimeNormalObject>();
                        effect.AllocateSharedResources(areaEffectType);
                        return effect;
                    }
                case AreaEffectType.StickySlimeTranscendentObject_Default:
                case AreaEffectType.StickySlimeTranscendentObject_Bongjun:
                    {
                        var effect = areaEffectObject.AddComponent<StickySlimeTranscendentObject>();
                        effect.AllocateSharedResources(areaEffectType);
                        return effect;
                    }
                case AreaEffectType.ReflectionSplitSpinMoveObjectAreaEffectObject:
                    {
                        var effect = areaEffectObject.AddComponent<ReflectionSplitSpinMoveObjectAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.SpinMoveMultiHitObject:
                    {
                        var effect = areaEffectObject.AddComponent<SpinMoveMultiHitObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.IntiTentacleCreateObject:
                    {
                        var effect = areaEffectObject.AddComponent<IntiTentacleCreateObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.WoodTentacleCreateObject:
                    {
                        var effect = areaEffectObject.AddComponent<WoodTentacleCreateObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.SplitReflectionObject:
                    {
                        var effect = areaEffectObject.AddComponent<SplitReflectionObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.DropStoneCreateObject:
                    {
                        var effect = areaEffectObject.AddComponent<DropStoneCreateObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.DeployBattleYoYo_Default:
                case AreaEffectType.DeployBattleYoYo_EggKim:
                    {
                        var effect = areaEffectObject.AddComponent<DeployYoyoAreaEffectObject>();
                        effect.AllocateSharedResources(areaEffectType);
                        return effect;
                    }
                case AreaEffectType.DiesWithOwnerAreaEffectObject:
                    {
                        var effect = areaEffectObject.AddComponent<DiesWithOwnerAreaEffectObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.LightningCreateObject:
                    {
                        var effect = areaEffectObject.AddComponent<LightningCreateObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.EraMixGenieReflectionFollowUpObject:
                    {
                        var effect = areaEffectObject.AddComponent<EraMixGenieReflectionFollowUpObject>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.GumSpecialMine:
                    {
                        var effect = areaEffectObject.AddComponent<GumSpecialMineAreaEffect>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.GumSpecialMineSlowAreaEffect:
                    {
                        var effect = areaEffectObject.AddComponent<GumSpecialMineSlowAreaEffect>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                case AreaEffectType.GumSupportFireAreaEffect:
                    {
                        var effect = areaEffectObject.AddComponent<GumSupportFireAreaEffect>();
                        effect.AllocateSharedResources();
                        return effect;
                    }
                default:
                    throw new NotImplementedException($"{areaEffectType} 구현 안됨. 구현해주세요.");
            }
        }
    }
}
