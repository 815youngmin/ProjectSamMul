using Shared.GameDataTypes;
using Shared.StaticDatas;
using SamMul.Animations.Placeholder;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.GameClients.Stages.ItemObjects;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;
using Random = UnityEngine.Random;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class SpaceCoinSkill : SkillBase
    {
        #region Projectile Paths
        private static readonly string[] DEFAULT_PROJECTILE_PATH =
        {
            // 주사위 눈별 칩 리소스 대신 공용 공격 비주얼을 쓴다. 인덱스(주사위 눈)는 그대로 유지한다.
            AreaEffectObjects.PlayerAttackVisual.PREFAB_PATH,
            AreaEffectObjects.PlayerAttackVisual.PREFAB_PATH,
            AreaEffectObjects.PlayerAttackVisual.PREFAB_PATH,
            AreaEffectObjects.PlayerAttackVisual.PREFAB_PATH,
            AreaEffectObjects.PlayerAttackVisual.PREFAB_PATH,
            AreaEffectObjects.PlayerAttackVisual.PREFAB_PATH,
        };
        #endregion

        private readonly float _attackPowerRate;        // Param1 : 일반 공격 대미지 증가 비율 (피해 계수), 최종대미지 = PC공격력 * (1+Param1)
        private StatModifier _attackSpeedIncreaser;     // Param2 : 공격속도 
        private readonly int _fireMinValue;             // Parma3 : 최소 발사 개수
        private readonly int _fireMaxValue;             // Param4 : 최대 바라 새수
        private readonly float _attackRange;            // Param5 : 공격 범위
        private readonly float _jackpotRatio;           // param6 : 잭팟 확률
        private readonly int _jackpotFireRatio;         // 잭팟 발사 비율
        private readonly float _knockbackPower;

        private readonly float _projectileMoveSpeed;    // 발사체 속도
        private readonly float _projectileAliveDistance; //발사체 이동 거리
        private readonly float _collidingRadius;

        private readonly float _throwDicePeriod;        //주사위 던지는 간격 (공속 영향 받음)

        private string[] _projectilePaths;

        private GameObject _diceObject; //주사위 연출 오브젝트 

        private float _diceThrowAt; //랜덤 주사위 던지는 타이밍
        private float _fireAt;      //주사위 연출 종료 후 발사 타이밍
        private int _fireAmount;    //칩 발사 개수
        private int _randomIndex;   //랜덤 주사위 숫자 1~6, 잭팟인 경우 7
        private List<Character> _targets;   //타겟들
        private BreakableItemObject _targetItem;

        private IReadOnlyCharacterStatCalculators _characterStats;

        private SkeletonAnimation _diceSkeletonAnimation;
        private MeshRenderer _diceMeshRenderer;

        // 이 스킬에서 지금까지 던진 횟수, 1레벨의 처음 3개는 1개씩만 던지기는 구현을 위해 사용
        private long _throwActionCount;

        private bool _isPlayerDead;

        private static readonly string DICE_ROLL_SFX_PATH = "Sounds/SoundEffects/PCs/11-SpaceCoin_normal_dice_roll_SFX.prefab";
        private static readonly string DICE_CHIP_SFX_PATH = "Sounds/SoundEffects/PCs/11-SpaceCoin_normal_dice_stop_SFX.prefab";
        private static readonly string DICE_JACKPOT_SFX_PATH = "Sounds/SoundEffects/PCs/12-SpaceCoin_ultimate_SFX.prefab";

        //DoubleTheJackpot 등급 효과
        private float _gradeEffectMultiplyJackpotPercent;

        //Dice777 등급 효과
        private readonly float _gradeEffect_Dice777_ActivePercent;
        private readonly float _gradeEffect_Dice777_Duration;
        private readonly float _gradeEffect_Dice777_AddChipAmount;

        private float _gradeEffectDice777ActiveAt;

        private float _addChipAmount;

        //PercentIncreaseHitChanceOnSpaceCoinJackpot 변장 효과에 사용
        private float _increaseHitChancePercent;

        //AdditionalSpaceCoinJackPotFire 변장 효과 
        private int _additionalJackpotFireCount;
        private int _remainingJackpotFireCount;

        public SpaceCoinSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats, IReadOnlyCustomParameters parameters) : base(staticData)
        {
            _attackPowerRate = staticData.Parameter1;
            _attackSpeedIncreaser = new StatModifier(StaticData.Parameter2, StatModType.Flat);
            _fireMinValue = (int)(staticData.Parameter3 + parameters.GetParameterValue(CustomParameterType.SpaceCoinAdditionalMinAmount)
                                                        + parameters.GetParameterValue(CustomParameterType.SpaceCoinAdditionalMinMaxAmount));

            _fireMaxValue = (int)(staticData.Parameter4 + parameters.GetParameterValue(CustomParameterType.SpaceCoinAdditionalMinMaxAmount));
            _attackRange = staticData.Parameter5;
            _jackpotRatio = staticData.Parameter6;
            _jackpotFireRatio = 5;

            _projectileMoveSpeed = 32f;
            _projectileAliveDistance = 20f;
            _collidingRadius = 0.45f;
            _throwDicePeriod = 1.0f;
            _knockbackPower = 0.1f;

            _throwActionCount = 0;

            _characterStats = characterStats;

            _isPlayerDead = false;

            _targets = new List<Character>();

            _gradeEffectMultiplyJackpotPercent = parameters.GetParameterValue(CustomParameterType.DoubleTheJackpot_MultiplyJackpotPercent);

            //Dice777 일정 시간동안 최대 칩 개수 증가 버프 
            _gradeEffect_Dice777_ActivePercent = parameters.GetParameterValue(CustomParameterType.Dice777_ActivePercent);
            _gradeEffect_Dice777_Duration = parameters.GetParameterValue(CustomParameterType.Dice777_Duration);
            _gradeEffect_Dice777_AddChipAmount = parameters.GetParameterValue(CustomParameterType.Dice777_AddChipAmount);

            //다이스 초월 확률 = (기본확률  + A - B ) * DoubleTheJackpot
            _jackpotRatio += parameters.GetParameterValue(CustomParameterType.IncreaseSpaceCoinJackpotPercent);
            _jackpotRatio -= parameters.GetParameterValue(CustomParameterType.DecreaseSpaceCoinJackpotPercent);
            _jackpotRatio *= _gradeEffectMultiplyJackpotPercent;

            _additionalJackpotFireCount = (int)parameters.GetParameterValue(CustomParameterType.AdditionalSpaceCoinJackpotFireCount);

            _increaseHitChancePercent = parameters.GetParameterValue(CustomParameterType.PercentIncreaseHitChanceOnSpaceCoinJackpot_Rate);
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);

            owner.Stats.CharacterAttackSpeed.AddModifier(_attackSpeedIncreaser);

            _diceObject = ResourcePool.Instance.InstantiateFromResource("Stages/ETCEffects/Dice_Number.prefab");
            _diceObject.transform.SetParent(owner.transform);
            _diceObject.transform.localPosition = Vector2.up * 1.7f;
            _diceObject.transform.localScale = Vector3.one * 0.3f;
            _diceSkeletonAnimation = _diceObject.GetComponent<SkeletonAnimation>();
            _diceMeshRenderer = _diceObject.GetComponent<MeshRenderer>();

            _fireAmount = 0;
            _targets.Clear();
            _targetItem = null;

            if (this.Level <= 1)
            {
                _diceSkeletonAnimation.AnimationState.SetAnimation(0, "appear", false);
                _diceThrowAt = now + _diceSkeletonAnimation.skeleton.Data.FindAnimation("appear").Duration;
            }
            else
            {
                _diceThrowAt = now + 0.2f;
            }

            _gradeEffectDice777ActiveAt = now;
            _addChipAmount = 0;

            _projectilePaths = DEFAULT_PROJECTILE_PATH;
            Debug.Assert(_projectilePaths.Length == 6);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            owner.Stats.CharacterAttackSpeed.RemoveModifier(_attackSpeedIncreaser);

            GameObject.Destroy(_diceObject);

            _targets.Clear();
            _targetItem = null;
        }

        public override void OnPlayerCharacterDead(PlayerCharacter owner, Stage stage, float now)
        {
            base.OnPlayerCharacterDead(owner, stage, now);

            _isPlayerDead = true;
            _diceSkeletonAnimation.AnimationState.SetAnimation(0, "die", false);
        }

        public override void OnPlayerCharacterResurrected(PlayerCharacter owner, Stage stage, float now)
        {
            base.OnPlayerCharacterResurrected(owner, stage, now);

            _isPlayerDead = false;
            _diceSkeletonAnimation.AnimationState.SetAnimation(0, "appear", false);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            _diceMeshRenderer.sortingOrder = (int)(owner.Pos.y * -100 - 5);

            if (_isPlayerDead)
            {
                return;
            }

            //주사위 연출 및 랜덤 숫자 지정
            if (_diceThrowAt <= now)
            {
                float resultAttackRange = owner.Stats.AttackRangeDistanceRatio.Value * _attackRange;
                CircularTargetArea circularTargetArea = new CircularTargetArea(owner.Pos, resultAttackRange);
                _targets.Clear();
                _targetItem = null;
                stage.FindCharactersInArea(owner.Alliance.ToEnemyAlliance(),
                circularTargetArea,
                condition: character => !character.Action.IsDead && !character.IsImmuneToHit, _targets);
                if (_targets.Count <= 0)
                {
                    _targetItem = owner.FindClosestBreakableItemObjectExceptFence(stage, resultAttackRange);
                    if(_targetItem == null)
                    {
                        return;
                    }
                }
                UnityGlobal.Sounds.PlayBySoundPrefab(DICE_ROLL_SFX_PATH, owner.Pos);
                ThrowDice();

                float attackPeriod = _throwDicePeriod / _characterStats.CharacterAttackSpeedValue;
                _diceThrowAt = now + attackPeriod;
                _fireAt = now + 0.2f;  //주사위 연출 시간

                //Dice777 등급효과 처리
                if (_gradeEffectDice777ActiveAt + _gradeEffect_Dice777_Duration <= now)
                {
                    if (Random.value <= _gradeEffect_Dice777_ActivePercent)
                    {
                        _gradeEffectDice777ActiveAt = now;
                        _addChipAmount = _gradeEffect_Dice777_AddChipAmount;
                    }
                    else
                    {
                        _addChipAmount = 0;
                    }
                }
            }

            //칩 발사 
            if (_fireAt < now && _fireAmount != 0)
            {
                ThrowChips(stage, owner);
                owner.ConditionalEffects.OnBasicSkillUsed(stage, owner);
            }

        }

        // 주사위를 던져 숫자를 결정하고, 연출한다.
        private void ThrowDice()
        {
            if (IsTranscendent)
            {
                if(_remainingJackpotFireCount > 0)
                {
                    _fireAmount = _fireMaxValue * _jackpotFireRatio;
                    _randomIndex = 7;   //잭팟
                    _remainingJackpotFireCount--;
                }
                else
                {
                    //잭팟 체크
                    if (Random.value <= _jackpotRatio)
                    {
                        _fireAmount = _fireMaxValue * _jackpotFireRatio;
                        _randomIndex = 7;   //잭팟
                        _remainingJackpotFireCount = _additionalJackpotFireCount;
                    }
                    else
                    {
                        _randomIndex = GetRandomNumberFromDice();
                        _fireAmount = _fireMinValue + (_fireMaxValue - _fireMinValue) * (_randomIndex - 1) / 5;
                    }
                }
            }
            else
            {
                if (Level == 1 && _throwActionCount < 2)
                {
                    // 레벨1은 처음 2개를 반드시 초록색을 던지게 한다. 
                    _randomIndex = 1;
                }
                else
                {
                    _randomIndex = GetRandomNumberFromDice();
                }
                _fireAmount = _fireMinValue + (_fireMaxValue - _fireMinValue) * (_randomIndex - 1) / 5;
            }
            _throwActionCount++;

            switch (_randomIndex)
            {
                case 1:
                    _diceSkeletonAnimation.AnimationState.SetAnimation(0, "1_start", false);
                    _diceSkeletonAnimation.AnimationState.AddAnimation(0, "1_loop", false, 0f);
                    _diceSkeletonAnimation.AnimationState.AddAnimation(0, "appear", false, 1.35f);
                    break;
                case 2:
                    _diceSkeletonAnimation.AnimationState.SetAnimation(0, "2_start", false);
                    _diceSkeletonAnimation.AnimationState.AddAnimation(0, "2_loop", false, 0f);
                    _diceSkeletonAnimation.AnimationState.AddAnimation(0, "appear", false, 1.35f);
                    break;
                case 3:
                    _diceSkeletonAnimation.AnimationState.SetAnimation(0, "3_start", false);
                    _diceSkeletonAnimation.AnimationState.AddAnimation(0, "3_loop", false, 0f);
                    _diceSkeletonAnimation.AnimationState.AddAnimation(0, "appear", false, 1.35f);
                    break;
                case 4:
                    _diceSkeletonAnimation.AnimationState.SetAnimation(0, "4_start", false);
                    _diceSkeletonAnimation.AnimationState.AddAnimation(0, "4_loop", false, 0f);
                    _diceSkeletonAnimation.AnimationState.AddAnimation(0, "appear", false, 1.35f);
                    break;
                case 5:
                    _diceSkeletonAnimation.AnimationState.SetAnimation(0, "5_start", false);
                    _diceSkeletonAnimation.AnimationState.AddAnimation(0, "5_loop", false, 0f);
                    _diceSkeletonAnimation.AnimationState.AddAnimation(0, "appear", false, 1.35f);
                    break;
                case 6:
                    _diceSkeletonAnimation.AnimationState.SetAnimation(0, "6_start", false);
                    _diceSkeletonAnimation.AnimationState.AddAnimation(0, "6_loop", false, 0f);
                    _diceSkeletonAnimation.AnimationState.AddAnimation(0, "appear", false, 1.35f);
                    break;
                case 7:
                    _diceSkeletonAnimation.AnimationState.SetAnimation(0, "jackpot", false);
                    _diceSkeletonAnimation.AnimationState.AddAnimation(0, "jackpot", false, 0f);
                    _diceSkeletonAnimation.AnimationState.AddAnimation(0, "appear", false, 1.35f);
                    break;
            }
        }

        /// <summary>
        /// 주사위를 던져 1~6 사이 숫자중 하나를 반환합니다.
        /// 그런데 육면체 주사위의 확률은 아니고, 다이스를 위해 커스터마이징된 확률입니다.
        /// 
        /// 1이 나올확률이 6이 나올 확률보다 높음
        /// </summary>
        private int GetRandomNumberFromDice()
        {
            int randomNumber = Random.Range(0, 100);
            if (randomNumber < 50)
            {
                return 1;
            }
            if (randomNumber < 60)
            {
                return 2;
            }
            if (randomNumber < 68)
            {
                return 3;
            }
            if (randomNumber < 76)
            {
                return 4;
            }
            if (randomNumber < 85)
            {
                return 5;
            }
            return 6;
        }

        private void ThrowChips(Stage stage, PlayerCharacter owner)
        {
            Vector3 resultProjectileScale = Vector3.one * owner.Stats.AttackRangeDistanceRatio.Value;
            float resultProjectileMoveSpeed = _projectileMoveSpeed * _characterStats.ProjectileMoveSpeedIncreaseRateValue;
            var damage = CombatSystem.CalculateCharacterBasicAttackDamage(owner.Stats, _attackPowerRate);
            float knockbackPower = CombatSystem.CalculateCharacterAttackKnockBackPower(owner.Stats, _knockbackPower);
            int hitChance = 1;

            if (_randomIndex >= 7)
            {
                UnityGlobal.Sounds.PlayBySoundPrefab(DICE_JACKPOT_SFX_PATH, owner.Pos);

                //잭팟 관통 확률 확인
                if (Random.value <= _increaseHitChancePercent)
                {
                    hitChance = 3;
                }

            }
            UnityGlobal.Sounds.PlayBySoundPrefab(DICE_CHIP_SFX_PATH, owner.Pos);
            for (int i = 0; i < _fireAmount + _addChipAmount; i++)
            {
                Vector2 direction;
                if (_targets.Count == 0 && _targetItem == null)
                {
                    direction = Random.insideUnitCircle;
                }
                else if(_targets.Count == 0 && _targetItem != null)
                {
                    direction = (Vector2)_targetItem.transform.position - owner.Pos;
                    _targetItem = null;
                }
                else
                {
                    var target = _targets[Random.Range(0, _targets.Count)];
                    // 하나는 랜덤 offset 없이, 꼭 맞는 방향으로 던지고, 숫자가 많아질수록 산개시킨다.
                    direction = (target.Pos + Random.insideUnitCircle * (float)(i * 0.95f)) - owner.Pos;
                    direction.Normalize();
                }

                string projectilePath;
                if (_randomIndex < 7)
                {
                    projectilePath = _projectilePaths[_randomIndex - 1];
                }
                else
                {
                    projectilePath = _projectilePaths[Random.Range(0, _projectilePaths.Length)];
                }

                var projectile = stage.CreateProjectile(
                    projectilePath,
                    owner.Alliance,
                    owner,
                    damage,
                    knockbackPower,
                    owner.Pos + Random.insideUnitCircle * 0.2f,
                    direction,
                    resultProjectileMoveSpeed,
                    acceleration: 0f,
                    _collidingRadius,
                    _projectileAliveDistance,
                    hitChance,
                    splitCount: 0,
                    StaticData.SkillHitSFXPath);
                projectile.transform.localScale = resultProjectileScale;

            }
            _fireAmount = 0;
        }
    }
}
