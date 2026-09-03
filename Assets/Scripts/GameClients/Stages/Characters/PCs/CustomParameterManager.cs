using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.ConditionalEffects;
using SamMul.Loggers;

namespace SamMul.GameClients.Stages.Characters.Stats
{
    public interface IReadOnlyCustomParameters
    {
        public float GetParameterValue(CustomParameterType customParameterType);
    }

    public interface ICustomParameterChanger
    {
        public bool ChangeParameterValue(CustomParameterType customParameterType, float newValue);
    }
    public enum CustomParameterType
    {
        HpAbsolbingTenticle_HPDrainAmountPercent,
        HpAbsolbingTenticle_DamagePercent,
        HpAbsolbingTenticle_AttackPeriod,
        IgnatiaSS_StunDuration,
        AddBurnEffectOnIgnitionWave_BurnDuration,
        Ignatia_BurnDamagePercent,
        HalfOffSaleKetchup_TranscendentAttackCountDecreasePercent,
        RecoverHpWhenAttackByRedUmbrella_HPDrainPercent,
        RecoverHpWhenAttackByRedUmbrella_HPDrainAmountPercent,
        DoubleTheJackpot_MultiplyJackpotPercent,
        Dice777_ActivePercent,
        Dice777_Duration,
        Dice777_AddChipAmount,
        UpgradeAlphaGattling_CorrectionAngle,

        AdditionalSkillRefreshCount,    // 추가로 스킬을 재선택할 수 있는 횟수. 기본 1회.

        CleavageGum,        //껌 전용스킬 미니 풍선 생성 개수 파라미터
        WindFieldsRemovePoisonousAreaEffects,   // 라이카가 생성하는 바람장이 독장판을 제거할지 여부. (0.0.f: false, 그 외: true)
        IronFistsCreateWindFields,      // 라이카의 강철 주먹이 바람장을 생성할지 여부. (0.0f: false, 그 외: true)
        TentiSweepAttackRangeRatio,     // 텐티 스윕 공격 범위 비율.
        BattleYoyoAttackRangeRatio,     // 배틀 요요 공격 범위 비율.
        AlphaHomingMissileAttackPeriodRatio,  // 알파 초월 시 유도 미사일 공격 주기 비율.
        SpaceCoinAdditionalMinAmount,   // 스페이스 코인 최소 개수 증가량.
        BubbleGumMoveSpeedChangeDuration,   // 풍선껌 이동 속도 변화 지속 시간.
        BubbleGumMoveSpeedChangeRatio,      // 풍선껌 이동 속도 변화 비율.
        BubbleGumDurationRatio,         // 풍선껌 지속 시간 비율.

        CreatureSavingsKillCount,   //크리처 적금 스킬 몬스터 킬 수 (크리처 적금 스킬 구현을 위해 추가한 파라미터입니다.)
        BrokenItemBoxes,            // 아이템 박스를 부순 횟수.
        AcquiredStarCores,          // 디펜스 스테이지에서 획득한 스타 코어의 개수.
        BroughtStarCores,           // 디펜스 스테이지에서 우주선에 가져다준 스타 코어의 개수.
        AcquiredGoldBoxes,          // 디펜스 스테이지에서 획득한 골드 박스의 개수.
        AcquiredExpMagnets,         // 획득한 자석의 개수.
        AcquiredHpResorative,       // 획득한 체력 회복제의 개수.
        CompletedDefenceAssignments,                // 디펜스 스테이지에서 방어 임무를 완료한 횟수.

        AddBurnEffectOnMambaFlame_BurnDuration,  //MambaFlame 점화 시간
        MambaFlame_BurnDamagePercent,            //MambaFlame 점화 데미지 계수 
        MambaSkill_StunDuration,                 //MambaFlame 스턴 시간
        MambaSkill_RemovePoisonousAreaEffects,    //MambaFlame이 장판 제거할지
        MambaSkill_TranscendentObjectMultiplyDurationUp, //MambaFlame 초월 투사체 지속시간 증가값

        WaterElementBonusMultiplier,      // 물 속성 보너스 계수
        WindElementBonusMultiplier,       // 바람 속성 보너스 계수
        EarthElementBonusMultiplier,      // 땅 속성 보너스 계수
        FireElementBonusMultiplier,       // 불 속성 보너스 계수

        StunEnemyEvery10thTentiSweepAttack_StunDuration,//텐티스윕 10번째 공격 스턴 지속시간 
        PercentIncreaseHitChanceOnSpaceCoinJackpot_Rate,  //다이스 초월 관통 확률
        SpaceCoinAdditionalMinMaxAmount,   // 스페이스 코인 최소 개수 증가량.
        IncreaseAlphaGatlingDamagePercent,   //알파 개틀링 데미지 증가량
        DecreaseAlphaGatlingDamagePercent,   //알파 개틀링 데미지 감소량
        IncreaseBattleYoYoDamagePercent,   //배틀 요요 데미지 증가량

        CreateMeteorIgnitionWaveStackCount,  // 이그니션웨이브 메테오 생성해야 하는 카운트
        DoubleAttackIgnitionWaveStackCount,  // 이그니션웨이브 더블 어택 되는 카운트

        IncreaseAlphaHomingMissileAmount,  //알파 유도 미사일 개수 증가량
        IncreaseIgnitionWaveDamagePercent, //이그니션 웨이브 데미지 증가량

        UndineBonusItemDropChance,              //운디네 스킬 처치시 아이템 드랍 확률
        UndineSkillItemAcquireAdditionalRange,  //운디네 스킬 아이템 획득 추가 범위
        UndineTranscendentBonusItemDropAdditionalChance,    //운디네 초월 스킬시 아이템 드랍 추가 확률
        
        MultipleCircleFireStickySlimeStackCount, //운디네 스킬 모든방향 공격 생성해야 하는 카운트
        IncreaseStickySlimeSplitAmount,      //운디네 스킬 추가 슬라임 분열 개수
    
        IncreaseSpaceCoinJackpotPercent,        //스페이스 코인 잭팟 추가 확률
        DecreaseSpaceCoinJackpotPercent,        //스페이스 코인 잭팟 감소 확률
        AdditionalSpaceCoinJackpotFireCount,    //스페이스 코인 잭팟 추가 발사 횟수

        IncreaseAlphaGatlingBulletFireAmount,   //알파 게틀링 추가 발사 투사체 
        IncreaseAlphaGatlingHitChance,          //알파 게틀링 추가 관통 횟수

        IncreaseIgnitionWaveBurnSpreadAmount,   //이그니션웨이브 화상 확산량 증가
        DeployYoyoDuration,                     //배틀 요요 공격 시 요요설치 지속시간
        DeployYoyoDamagePercent,                //배틀 요요 공격 시 요요설치 지속시간
        IncreaseTentiSweepDamagePercent,        //TentiSweep 데미지 증가 퍼센트
        DecreaseTentiSweepSizePercent,          //TentiSweep 사이즈 감소 퍼센트

        SpawnTentacleAmount,                    //ActivateSpawnTentacleTentiSweep 문어발 소환량
        SpawnTentacleDamagePercent,             //ActivateSpawnTentacleTentiSweep 문어발 데미지 퍼센트
    }

    public class CustomParameterManager : IReadOnlyCustomParameters, ICustomParameterChanger
    {
        private readonly Dictionary<CustomParameterType, float> _customParameters;

        public CustomParameterManager()
        {
            _customParameters = new Dictionary<CustomParameterType, float>();
            this.InitializeCustomParameterSetting();
        }

        private void InitializeCustomParameterSetting()
        {
            _customParameters.Add(CustomParameterType.HpAbsolbingTenticle_HPDrainAmountPercent, 0f);
            _customParameters.Add(CustomParameterType.HpAbsolbingTenticle_DamagePercent, 0f);
            _customParameters.Add(CustomParameterType.HpAbsolbingTenticle_AttackPeriod, -1f);
            _customParameters.Add(CustomParameterType.IgnatiaSS_StunDuration, 0f);
            _customParameters.Add(CustomParameterType.AddBurnEffectOnIgnitionWave_BurnDuration, 0f);
            _customParameters.Add(CustomParameterType.Ignatia_BurnDamagePercent, 0.25f);
            _customParameters.Add(CustomParameterType.HalfOffSaleKetchup_TranscendentAttackCountDecreasePercent, 0f);
            _customParameters.Add(CustomParameterType.RecoverHpWhenAttackByRedUmbrella_HPDrainPercent, 0f);
            _customParameters.Add(CustomParameterType.RecoverHpWhenAttackByRedUmbrella_HPDrainAmountPercent, 0f);
            _customParameters.Add(CustomParameterType.DoubleTheJackpot_MultiplyJackpotPercent, 1f);

            _customParameters.Add(CustomParameterType.Dice777_ActivePercent, 0f);
            _customParameters.Add(CustomParameterType.Dice777_Duration, 0f);
            _customParameters.Add(CustomParameterType.Dice777_AddChipAmount, 0f);

            _customParameters.Add(CustomParameterType.UpgradeAlphaGattling_CorrectionAngle, 0f);

            _customParameters.Add(CustomParameterType.AdditionalSkillRefreshCount, 1.0f);
            _customParameters.Add(CustomParameterType.CleavageGum, 0f);
            _customParameters.Add(CustomParameterType.WindFieldsRemovePoisonousAreaEffects, 0.0f);
            _customParameters.Add(CustomParameterType.IronFistsCreateWindFields, 0.0f);
            _customParameters.Add(CustomParameterType.TentiSweepAttackRangeRatio, 1.0f);
            _customParameters.Add(CustomParameterType.BattleYoyoAttackRangeRatio, 1.0f);
            _customParameters.Add(CustomParameterType.AlphaHomingMissileAttackPeriodRatio, 1.0f);
            _customParameters.Add(CustomParameterType.SpaceCoinAdditionalMinAmount, 0.0f);
            _customParameters.Add(CustomParameterType.BubbleGumMoveSpeedChangeDuration, 0.0f);
            _customParameters.Add(CustomParameterType.BubbleGumMoveSpeedChangeRatio, 1.0f);
            _customParameters.Add(CustomParameterType.BubbleGumDurationRatio, 1.0f);

            _customParameters.Add(CustomParameterType.CreatureSavingsKillCount, 0f);
            _customParameters.Add(CustomParameterType.BrokenItemBoxes, 0.0f);
            _customParameters.Add(CustomParameterType.AcquiredStarCores, 0.0f);
            _customParameters.Add(CustomParameterType.BroughtStarCores, 0.0f);
            _customParameters.Add(CustomParameterType.AcquiredGoldBoxes, 0.0f);
            _customParameters.Add(CustomParameterType.AcquiredExpMagnets, 0.0f);
            _customParameters.Add(CustomParameterType.AcquiredHpResorative, 0.0f);
            _customParameters.Add(CustomParameterType.CompletedDefenceAssignments, 0.0f);
            _customParameters.Add(CustomParameterType.AddBurnEffectOnMambaFlame_BurnDuration, 0.0f);

            _customParameters.Add(CustomParameterType.MambaFlame_BurnDamagePercent, 0.2f);
            _customParameters.Add(CustomParameterType.MambaSkill_StunDuration, 0.0f);
            _customParameters.Add(CustomParameterType.MambaSkill_RemovePoisonousAreaEffects, 0.0f);
            _customParameters.Add(CustomParameterType.MambaSkill_TranscendentObjectMultiplyDurationUp, 0.0f);

            _customParameters.Add(CustomParameterType.WaterElementBonusMultiplier, 1.0f);
            _customParameters.Add(CustomParameterType.WindElementBonusMultiplier, 1.0f);
            _customParameters.Add(CustomParameterType.EarthElementBonusMultiplier, 1.0f);
            _customParameters.Add(CustomParameterType.FireElementBonusMultiplier, 1.0f);

            _customParameters.Add(CustomParameterType.StunEnemyEvery10thTentiSweepAttack_StunDuration, 0.0f);
            _customParameters.Add(CustomParameterType.PercentIncreaseHitChanceOnSpaceCoinJackpot_Rate, 0.0f);
            _customParameters.Add(CustomParameterType.SpaceCoinAdditionalMinMaxAmount, 0.0f);
            _customParameters.Add(CustomParameterType.IncreaseAlphaGatlingDamagePercent, 0.0f);
            _customParameters.Add(CustomParameterType.DecreaseAlphaGatlingDamagePercent, 0.0f);
            _customParameters.Add(CustomParameterType.IncreaseBattleYoYoDamagePercent, 0.0f);

            _customParameters.Add(CustomParameterType.CreateMeteorIgnitionWaveStackCount, -1.0f);
            _customParameters.Add(CustomParameterType.DoubleAttackIgnitionWaveStackCount, -1.0f);

            _customParameters.Add(CustomParameterType.IncreaseAlphaHomingMissileAmount, 0f);
            _customParameters.Add(CustomParameterType.IncreaseIgnitionWaveDamagePercent, 0f);

            _customParameters.Add(CustomParameterType.UndineBonusItemDropChance, 0f);
            _customParameters.Add(CustomParameterType.UndineSkillItemAcquireAdditionalRange, 0f);
            _customParameters.Add(CustomParameterType.UndineTranscendentBonusItemDropAdditionalChance, 0f);

            _customParameters.Add(CustomParameterType.MultipleCircleFireStickySlimeStackCount, -1.0f);
            _customParameters.Add(CustomParameterType.IncreaseStickySlimeSplitAmount, 0f);

            _customParameters.Add(CustomParameterType.IncreaseSpaceCoinJackpotPercent, 0f);
            _customParameters.Add(CustomParameterType.DecreaseSpaceCoinJackpotPercent, 0f);
            _customParameters.Add(CustomParameterType.AdditionalSpaceCoinJackpotFireCount, 0f);

            _customParameters.Add(CustomParameterType.IncreaseAlphaGatlingBulletFireAmount, 0f);
            _customParameters.Add(CustomParameterType.IncreaseAlphaGatlingHitChance, 0f);

            _customParameters.Add(CustomParameterType.IncreaseIgnitionWaveBurnSpreadAmount, 0f);

            _customParameters.Add(CustomParameterType.DeployYoyoDuration, 0f);
            _customParameters.Add(CustomParameterType.DeployYoyoDamagePercent, 0f);

            _customParameters.Add(CustomParameterType.IncreaseTentiSweepDamagePercent, 0f);
            _customParameters.Add(CustomParameterType.DecreaseTentiSweepSizePercent, 0f);

            _customParameters.Add(CustomParameterType.SpawnTentacleAmount, 0f);
            _customParameters.Add(CustomParameterType.SpawnTentacleDamagePercent, 0f);
        }

        public float GetParameterValue(CustomParameterType customParameterType)
        {
            if (_customParameters.ContainsKey(customParameterType))
            {
                return _customParameters[customParameterType];
            }
            else
            {
                Debug.LogError("파라미터 타입을 찾을 수 없습니다. 잘못된 타입입니다.");
                return 0f;
            }
        }

        public bool ChangeParameterValue(CustomParameterType customParameterType, float newValue)
        {
            if (_customParameters.ContainsKey(customParameterType))
            {
                _customParameters[customParameterType] = newValue;
                return true;
            }
            else
            {
                Debug.LogError("파라미터 타입을 찾을 수 없습니다. 잘못된 타입입니다.");
                return false;
            }
        }

        public bool IncreaseParameterValue(CustomParameterType customParameterType, float increaseAmount)
        {
            if (increaseAmount < 0.0f)
            {
                Log.I.Error($"{customParameterType} 타입의 증가량 {increaseAmount}의 값은 음이 아니어야 합니다. 무시하고 진행합니다.");
                return false;
            }

            if (_customParameters.ContainsKey(customParameterType))
            {
                _customParameters[customParameterType] += increaseAmount;
                return true;
            }
            else
            {
                Debug.LogError("파라미터 타입을 찾을 수 없습니다. 잘못된 타입입니다.");
                return false;
            }
        }

        public bool DecreaseParameterValue(CustomParameterType customParameterType, float decreaseAmount)
        {
            if (decreaseAmount < 0.0f)
            {
                Log.I.Error($"{customParameterType} 타입의 감소량 {decreaseAmount}의 값은 음이 아니어야 합니다. 무시하고 진행합니다.");
                return false;
            }

            if (_customParameters.ContainsKey(customParameterType))
            {
                _customParameters[customParameterType] -= decreaseAmount;
                return true;
            }
            else
            {
                Debug.LogError("파라미터 타입을 찾을 수 없습니다. 잘못된 타입입니다.");
                return false;
            }
        }

        public void Clear()
        {
            _customParameters.Clear();
            this.InitializeCustomParameterSetting();
        }

    }

}

