using DG.Tweening;
using Shared.GameDataTypes;
using Shared.GameLogics;
using Shared.Localizers;
using System;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using SamMul.GameClients;
using SamMul.GameClients.Stages;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.ResourcePools;
using SamMul.Scenes;
using SamMul.UnityHelpers;

namespace SamMul.UIs.Stages.HUDs
{
    public class PlayerCharacterStatDisplayer : MonoBehaviour
    {
        public static readonly string PREFAB_PATH = "Stages/UIs/HUDs/PlayerCharacterStatDisplayer/PlayerCharacterStatDisplayer.prefab";

        private static readonly float MAX_OPACITY = 0.7f;
        private static readonly float FAID_IN_TIME = 0.2f;
        private static readonly float WAITING_TIME = 0.4f;
        private static readonly float FAID_OUT_TIME = 0.2f;
        private static readonly Vector2 FAID_IN_POSITION = 75.0f * Vector2.up;
        private static readonly Vector2 FAID_OUT_POSITION = 150.0f * Vector2.up;
        private static readonly Vector2 OFFSET = 1.6f * Vector2.up;

        [Header("Player Character Stat")]
        [SerializeField] private RectTransform _playerCharacterStat;
        [SerializeField] private TextMeshProUGUI _playerCharacterAttackPowerText;
        [SerializeField] private TextMeshProUGUI _playerCharacterMaxHpText;

        [Header("Equipment Stat")]
        [SerializeField] private RectTransform _equipmentStat;
        [SerializeField] private TextMeshProUGUI _equipmentAttackPowerText;
        [SerializeField] private TextMeshProUGUI _equipmentMaxHpText;

        [Header("Grade Effect Stat")]
        [SerializeField] private RectTransform _gradeEffectStat;
        [SerializeField] private TextMeshProUGUI _gradeEffectAttackPowerText;
        [SerializeField] private TextMeshProUGUI _gradeEffectMaxHpText;

        [Header("Element Bonus Stat")]
        [SerializeField] private RectTransform _elementBonusStat;
        [SerializeField] private Image _elementIcon;
        [SerializeField] private TextMeshProUGUI _elementBonusStatText;

        private float[] _attackPowers;
        private float[] _maxHps;

        /// <summary>
        /// 플레이어 캐릭터의 스탯을 보여줍니다.
        /// </summary>
        /// <param name="stage">
        /// 현재 플레이하고 있는 스테이지입니다.
        /// </param>
        /// <param name="pc">
        /// 스탯을 보여줄 플레이어 캐릭터입니다.
        /// </param>
        /// <param name="uiRoot">
        /// 이 스탯 디스플레이어를 표시할 스테이지의 UI Root 객체입니다.
        /// </param>
        /// <remarks>
        /// 이 함수는 시퀀스가 종료되면 스스로를 리소스 풀에 되돌리기 때문에 호출 측에서 이를 관리하지 않아도 됩니다.
        /// </remarks>
        /// <returns>
        /// 시퀀스의 재생시간을 반환합니다.
        /// </returns> 
        public float DisplayPlayerCharacterStat(Stage stage, PlayerCharacter pc, StageSceneUIRoot uiRoot)
        {
            // 초기화
            _playerCharacterStat.gameObject.SetActive(false);
            _equipmentStat.gameObject.SetActive(false);
            _gradeEffectStat.gameObject.SetActive(false);
            _elementBonusStat.gameObject.SetActive(false);
            _attackPowers = new float[4];
            _maxHps = new float[4];

            // 변수 캐싱
            var heroData = pc.HeroData;
            var equipmentDatas = pc.EquippedEquipments;

            float elementBonusMultiplier = pc.CustomParameters.GetParameterValue(pc.StaticData.ElementType switch
            {
                ElementType.Water => CustomParameterType.WaterElementBonusMultiplier,
                ElementType.Wind => CustomParameterType.WindElementBonusMultiplier,
                ElementType.Earth => CustomParameterType.EarthElementBonusMultiplier,
                ElementType.Fire => CustomParameterType.FireElementBonusMultiplier,
                _ => throw new NotImplementedException($"원소 종류 {pc.StaticData.ElementType}에 대한 처리가 구현되지 않았습니다."),
            });
            float elementBonusRate = AvatarLogic.CalculateElementBonusRate(pc.StaticData.ElementType, stage.StageElementType);
            float resultElementBonusRate = elementBonusMultiplier * elementBonusRate;

            float attackPower = 0.0f;
            float maxHp = 0.0f;

            // 기본 스탯 계산
            float basicAttackPower = AvatarLogic.CalculateHeroBasicAttackPower(heroData);
            float basicMaxHp = AvatarLogic.CalculateHeroBasicMaxHp(heroData);
            _attackPowers[0] = basicAttackPower;
            _maxHps[0] = basicMaxHp;

            // 장비 스탯 계산
            float attackPowerIncreasedByEquipments = 0.0f;
            float maxHpIncreasedByEquipments = 0.0f;
            foreach (var equipmentData in equipmentDatas)
            {
                var equipmentStat = AvatarLogic.CalculateEquipmentStat(equipmentData);
                switch (equipmentStat.StatType)
                {
                    case StatType.AttackPower:
                        attackPowerIncreasedByEquipments += equipmentStat.CalculatedValue;
                        break;
                    case StatType.MaxHP:
                        maxHpIncreasedByEquipments += equipmentStat.CalculatedValue;
                        break;
                    default:
                        throw new NotImplementedException($"{equipmentStat.StatType} 구현 안 됨");
                }
            }
            _attackPowers[1] = _attackPowers[0] + attackPowerIncreasedByEquipments;
            _maxHps[1] = _maxHps[0] + maxHpIncreasedByEquipments;

            // 등급 효과 스탯 계산
            float attackPowerIncreasedByGradeEffects = pc.Stats.AttackPower.Value - resultElementBonusRate * pc.Stats.AttackPower.ValueAfterSum - _attackPowers[1];
            float maxHpIncreasedByGradeEffects = pc.Stats.MaxHP.Value - _maxHps[1];
            _attackPowers[2] = _attackPowers[1] + attackPowerIncreasedByGradeEffects;
            _maxHps[2] = _maxHps[1] + maxHpIncreasedByGradeEffects;

            // 속성 보너스 스탯 계산
            float attackPowerIncreasedByElementBonus = resultElementBonusRate * pc.Stats.AttackPower.ValueAfterSum;
            float maxHpIncreasedByElementBonus = 0.0f;      // 속성 보너스는 공격력만 올려준다.
            _attackPowers[3] = _attackPowers[2] + attackPowerIncreasedByElementBonus;
            _maxHps[3] = _maxHps[2] + maxHpIncreasedByElementBonus;


            UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>()?.UpdatePCAttackPower((int)_attackPowers[3]);
            UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>()?.UpdatePCHP((int)pc.CurrentHP, (int)_maxHps[3]);

            // 디스플레이 시퀀스 생성
            var displaySequence = DOTween.Sequence();

            // 기본 스탯 페이드 인 시퀀스 추가
            var playerCharacterStatGraphics = _playerCharacterStat.GetComponentsInChildren<Graphic>();
            var basicStatFadeInSequence = DOTween.Sequence()
                .OnStart(() =>
                {
                    _playerCharacterStat.gameObject.SetActive(true);
                    _playerCharacterStat.localPosition = Vector3.zero;
                    _playerCharacterAttackPowerText.text = attackPower.ToString("0");
                    _playerCharacterMaxHpText.text = maxHp.ToString("0");

                    foreach (var graphic in playerCharacterStatGraphics)
                    {
                        graphic.DOFade(MAX_OPACITY, FAID_IN_TIME).From(0.0f).SetEase(Ease.Linear);
                    }
                })
                .AppendInterval(FAID_IN_TIME)
                .Join(DOTween.To(() => attackPower, value =>
                {
                    attackPower = value;
                    _playerCharacterAttackPowerText.text = attackPower.ToString("0");
                }, _attackPowers[0], FAID_IN_TIME).SetEase(Ease.Linear))
                .Join(DOTween.To(() => maxHp, value =>
                {
                    maxHp = value;
                    _playerCharacterMaxHpText.text = maxHp.ToString("0");
                }, _maxHps[0], FAID_IN_TIME).SetEase(Ease.Linear))
                .OnComplete(() => _equipmentStat.gameObject.SetActive(false))
                .AppendInterval(WAITING_TIME);
            displaySequence.Append(basicStatFadeInSequence);

            // 장비 스탯 시퀀스 추가
            if (attackPowerIncreasedByEquipments > 0.0f || maxHpIncreasedByEquipments > 0.0f)
            {
                var equipmentStatGraphics = _equipmentStat.GetComponentsInChildren<Graphic>();
                var equipmentStatSequence = DOTween.Sequence()
                    .OnStart(() =>
                    {
                        _equipmentStat.gameObject.SetActive(true);
                        _equipmentStat.localPosition = FAID_IN_POSITION;
                        _equipmentAttackPowerText.text = attackPowerIncreasedByEquipments.ToString("0");
                        _equipmentMaxHpText.text = maxHpIncreasedByEquipments.ToString("0");

                        foreach (var graphic in equipmentStatGraphics)
                        {
                            graphic.DOFade(MAX_OPACITY, FAID_IN_TIME).From(0.0f).SetEase(Ease.Linear);
                        }
                    })
                    .AppendInterval(FAID_IN_TIME)
                    .AppendInterval(WAITING_TIME)
                    .AppendCallback(() =>
                    {
                        _equipmentStat.DOLocalMove(FAID_OUT_POSITION, FAID_OUT_TIME).SetEase(Ease.Linear);
                        foreach (var graphic in equipmentStatGraphics)
                        {
                            graphic.DOFade(0.0f, FAID_OUT_TIME).From(MAX_OPACITY).SetEase(Ease.Linear);
                        }
                    })
                    .AppendInterval(FAID_OUT_TIME)
                    .Join(DOTween.To(() => attackPower, value =>
                    {
                        attackPower = value;
                        _playerCharacterAttackPowerText.text = attackPower.ToString("0");
                    }, _attackPowers[1], FAID_OUT_TIME).SetEase(Ease.Linear))
                    .Join(DOTween.To(() => maxHp, value =>
                    {
                        maxHp = value;
                        _playerCharacterMaxHpText.text = maxHp.ToString("0");
                    }, _maxHps[1], FAID_OUT_TIME).SetEase(Ease.Linear))
                    .OnComplete(() => _equipmentStat.gameObject.SetActive(false));
                displaySequence.Append(equipmentStatSequence);
            }

            // 등급 효과 스탯 시퀀스 추가
            if (attackPowerIncreasedByGradeEffects > 0.0f || maxHpIncreasedByGradeEffects > 0.0f)
            {
                var gradeEffectStatGraphics = _gradeEffectStat.GetComponentsInChildren<Graphic>();
                var gradeEffectStatSequence = DOTween.Sequence()
                    .OnStart(() =>
                    {
                        _gradeEffectStat.gameObject.SetActive(true);
                        _gradeEffectStat.localPosition = FAID_IN_POSITION;
                        _gradeEffectAttackPowerText.text = attackPowerIncreasedByGradeEffects.ToString("0");
                        _gradeEffectMaxHpText.text = maxHpIncreasedByGradeEffects.ToString("0");

                        foreach (var graphic in gradeEffectStatGraphics)
                        {
                            graphic.DOFade(MAX_OPACITY, FAID_IN_TIME).From(0.0f).SetEase(Ease.Linear);
                        }
                    })
                    .AppendInterval(FAID_IN_TIME)
                    .AppendInterval(WAITING_TIME)
                    .AppendCallback(() =>
                    {
                        _gradeEffectStat.DOLocalMove(FAID_OUT_POSITION, FAID_OUT_TIME).SetEase(Ease.Linear);
                        foreach (var graphic in gradeEffectStatGraphics)
                        {
                            graphic.DOFade(0.0f, FAID_OUT_TIME).From(MAX_OPACITY).SetEase(Ease.Linear);
                        }
                    })
                    .AppendInterval(FAID_OUT_TIME)
                    .Join(DOTween.To(() => attackPower, value =>
                    {
                        attackPower = value;
                        _playerCharacterAttackPowerText.text = attackPower.ToString("0");
                    }, _attackPowers[2], FAID_OUT_TIME).SetEase(Ease.Linear))
                    .Join(DOTween.To(() => maxHp, value =>
                    {
                        maxHp = value;
                        _playerCharacterMaxHpText.text = maxHp.ToString("0");
                    }, _maxHps[2], FAID_OUT_TIME).SetEase(Ease.Linear))
                    .OnComplete(() => _gradeEffectStat.gameObject.SetActive(false));
                displaySequence.Append(gradeEffectStatSequence);
            }

            // 속성 보너스 스탯 시퀀스 추가
            var elementBonusStatGraphics = _elementBonusStat.GetComponentsInChildren<Graphic>();
            var elementBonusStatSequence = DOTween.Sequence()
                .OnStart(() =>
                {
                    _elementBonusStat.gameObject.SetActive(true);
                    _elementBonusStat.localPosition = FAID_IN_POSITION;
                    _elementIcon.sprite = ResourcePool.Instance.LoadResource<Sprite>(pc.StaticData.ElementType.IconPath());
                    _elementBonusStatText.text = string.Format(Localizer.Instance.GetText("UI_ELEMENT_BONUS_ATTACK_POWER"), attackPowerIncreasedByElementBonus.ToString("0"));

                    foreach (var graphic in elementBonusStatGraphics)
                    {
                        graphic.DOFade(MAX_OPACITY, FAID_IN_TIME).From(0.0f).SetEase(Ease.Linear);
                    }
                })
                .AppendInterval(FAID_IN_TIME)
                .AppendInterval(WAITING_TIME)
                .AppendCallback(() =>
                {
                    _elementBonusStat.DOLocalMove(FAID_OUT_POSITION, FAID_OUT_TIME).SetEase(Ease.Linear);
                    foreach (var graphic in elementBonusStatGraphics)
                    {
                        graphic.DOFade(0.0f, FAID_OUT_TIME).From(MAX_OPACITY).SetEase(Ease.Linear);
                    }
                })
                .AppendInterval(FAID_OUT_TIME)
                .Join(DOTween.To(() => attackPower, value =>
                {
                    attackPower = value;
                    _playerCharacterAttackPowerText.text = attackPower.ToString("0");
                }, _attackPowers[3], FAID_OUT_TIME).SetEase(Ease.Linear))
                .Join(DOTween.To(() => maxHp, value =>
                {
                    maxHp = value;
                    _playerCharacterMaxHpText.text = maxHp.ToString("0");
                }, _maxHps[3], FAID_OUT_TIME).SetEase(Ease.Linear))
                .OnComplete(() => _elementBonusStat.gameObject.SetActive(false));
            displaySequence.Append(elementBonusStatSequence);

            // 최종 스탯 페이드 아웃 시퀀스 추가
            var finalStatFadeOutSequence = DOTween.Sequence()
                .AppendInterval(WAITING_TIME)
                .AppendCallback(() =>
                {
                    foreach (var graphic in playerCharacterStatGraphics)
                    {
                        graphic.DOFade(0.0f, FAID_OUT_TIME).From(MAX_OPACITY).SetEase(Ease.Linear);
                    }
                })
                .AppendInterval(FAID_OUT_TIME)
                .OnComplete(() => _playerCharacterStat.gameObject.SetActive(false));
            displaySequence.Append(finalStatFadeOutSequence);

            // 디스플레이 시퀀스 재생
            var rectTransfrom = this.GetComponent<RectTransform>();
            displaySequence
                .OnUpdate(() =>
                {
                    var screenPoint = GameClient.CameraController.MainCamera.WorldToScreenPoint(pc.Pos + OFFSET);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRoot.RectTransform, screenPoint, GameClient.CameraController.MainCamera, out var resultPoint);
                    rectTransfrom.localPosition = resultPoint;
                })
                .OnComplete(() =>
                {
                    ResourcePool.Instance.PutBackInstance(PREFAB_PATH, gameObject);
                })
                .Play();

            return displaySequence.Duration();
        }
    }
}
