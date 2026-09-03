using DG.Tweening;
using Shared.StaticDatas;
using UnityEngine;
using UnityEngine.UI;
using Z.GameClients.Stages;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.PCs;
using Z.GameClients.Stages.Characters.PCs.Skills;
using Z.GameClients.Stages.Characters.Stats;
using Z.GameClients.Stages.CombatSystems;
using Z.GameClients.Stages.ItemObjects;
using Z.ResourcePools;

public class EnchantingGlowSkill : SkillBase
{
    private static readonly string TargetEffectPath = "Stages/AreaEffects/MambaSkill/mamba_targeting.prefab";
    private readonly string MAMBA_SLIDER_PREFAB_PATH = "Stages/UIs/HUDs/Aims/MambaSlider.prefab";

    private IReadOnlyCharacterStatCalculators _characterStats;

    private readonly float _normalAttackPowerRate;
    private StatModifier _attackSpeedIncreaser;     // Param2 : 공격속도 
    private readonly int _attackAmount;
    private readonly float _attackRadius;
    private readonly float _attackDuration;

    private readonly float _fireTargetSearchRange;
    private readonly float _knobackPower;
    private readonly float _attackPeriod;
    private readonly float _stunPeriod;
    private readonly float _stunDuration;

    // 대미지  = 1.0f + _transcendentAttackPowerAdditionalRate
    private readonly float _transcendentAttackPowerAdditionalRate;
    private readonly float _transcendentAttackRadius;
    private readonly float _transcendentAttackPeriod;
    private readonly float _transcendentMoveSpeed;
    private readonly float _transcendentDuration;

    private readonly float _transcendentObjectMultiplyDurationUp;
    private readonly bool _removePoisonousAreaEffects;

    private float _fireAt;
    private int _transcendentCounter;
    private bool _fireCheck;

    private GameObject _mambaTargetingEffect;
    private SpriteRenderer _targetEffectSpriteRenderer;
    private Character _attackTarget;
    private BreakableItemObject _targetItem;

    private Sequence _prevSequence;

    private Slider _mambaSlider;

    public EnchantingGlowSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats, IReadOnlyCustomParameters parameters) : base(staticData)
    {
        _attackTarget = null;

        _characterStats = characterStats;
        _fireTargetSearchRange = 15f;
        _knobackPower = 0.0f;
        _attackPeriod = 0.6f;
        _stunPeriod = 0.6f;
        _transcendentCounter = 0;
        _transcendentAttackRadius = 2.5f;
        _transcendentAttackPeriod = 0.25f;
        _transcendentMoveSpeed = 16f;
        _transcendentDuration = 10f;

        _normalAttackPowerRate = StaticData.Parameter1;
        _attackSpeedIncreaser = new StatModifier(StaticData.Parameter2, StatModType.Flat);
        _attackAmount = (int)StaticData.Parameter3;
        _attackRadius = StaticData.Parameter4;
        _attackDuration = StaticData.Parameter5;
        _transcendentAttackPowerAdditionalRate = StaticData.Parameter6;

        _stunDuration = parameters.GetParameterValue(CustomParameterType.MambaSkill_StunDuration);
        _transcendentObjectMultiplyDurationUp = parameters.GetParameterValue(CustomParameterType.MambaSkill_TranscendentObjectMultiplyDurationUp);
        _removePoisonousAreaEffects = parameters.GetParameterValue(CustomParameterType.MambaSkill_RemovePoisonousAreaEffects) != 0.0f;

    }

    public override void Activate(PlayerCharacter owner, Stage stage, float now)
    {
        base.Activate(owner, stage, now);
        owner.Stats.CharacterAttackSpeed.AddModifier(_attackSpeedIncreaser);

        _fireAt = now;
        _fireCheck = false;

        _mambaTargetingEffect = ResourcePool.Instance.InstantiateFromResource(TargetEffectPath);
        _mambaTargetingEffect.transform.localScale = Vector3.one;
        _targetEffectSpriteRenderer = _mambaTargetingEffect.GetComponent<SpriteRenderer>();
        _targetEffectSpriteRenderer.color = new Color(1f, 1f, 1f, 0f);

        _mambaSlider = ResourcePool.Instance.InstantiateFromResource<Slider>(MAMBA_SLIDER_PREFAB_PATH);
        _mambaSlider.transform.SetParent(owner.transform);
        _mambaSlider.transform.localPosition = new Vector2(0.0f, -0.63f);
        _mambaSlider.transform.localScale = Vector3.one;
        _mambaSlider.value = 0.0f;

    }

    public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
    {
        base.Deactivate(owner, stage, now);
        owner.Stats.CharacterAttackSpeed.RemoveModifier(_attackSpeedIncreaser);

        if(_mambaTargetingEffect != null)
        {
            ResourcePool.Instance.PutBackInstance(TargetEffectPath, _mambaTargetingEffect);
            _mambaTargetingEffect = null;
            _targetEffectSpriteRenderer = null;
        }

        if(_prevSequence != null)
        {
            _prevSequence.Kill();
            _prevSequence = null;
        }

        if(_mambaSlider != null)
        {
            ResourcePool.Instance.PutBackInstance(MAMBA_SLIDER_PREFAB_PATH, _mambaSlider.gameObject);
            _mambaSlider = null;
        }

    }

    public override void Update(PlayerCharacter owner, Stage stage, float now)
    {
        this.UpdateTarget(owner, stage);

        float attackPeriod = 1.0f / _characterStats.CharacterAttackSpeedValue;
        _mambaSlider.value = (attackPeriod + now - _fireAt) / (attackPeriod - 0.1f);

        if (_fireAt <= now)
        {
            if(_attackTarget == null)
            {
                var item = owner.FindClosestBreakableItemObjectExceptFence(stage, _fireTargetSearchRange);
                if(item == null)
                {
                    return;
                }
                _targetItem = item;
            }

            if (IsTranscendent)
            {
                if (_transcendentCounter % 4 == 0)
                {
                    this.TranscendentSkillFire(owner, stage);
                }
                _transcendentCounter++;
            }
            this.NormalSkillFire(owner, stage); 
            _fireAt = now + attackPeriod;
            _fireCheck = true;
            base.PlaySkillSoundEffect(owner.Pos);
            owner.ConditionalEffects.OnBasicSkillUsed(stage, owner);
            _mambaSlider.value = 0.0f;
            _targetItem = null;
        }
    }

    private void TranscendentSkillFire(PlayerCharacter owner, Stage stage)
    {
        Vector2 targetPosition;
        if (_attackTarget != null)
        {
            targetPosition = _attackTarget.CenterPos;
        }
        // 수색 범위 안에 적이 없으면 랜덤 위치를 공격한다.
        else
        {
            if (_targetItem != null)
            {
                targetPosition = _targetItem.transform.position;
            }
            else
            {
                targetPosition = owner.CenterPos + _fireTargetSearchRange * Random.insideUnitCircle;
            }
        }

        var damage = CombatSystem.CalculateCharacterBasicAttackDamage(owner.Stats, _transcendentAttackPowerAdditionalRate);
        float attakRadius = _transcendentAttackRadius * owner.Stats.AttackRangeDistanceRatio.Value;
        float duration = _transcendentDuration * (1 + _transcendentObjectMultiplyDurationUp);
        float moveSpeed = _transcendentMoveSpeed * owner.Stats.ProjectileMoveSpeedIncreaseRate.Value;

        Vector2 fireDirection = (targetPosition - owner.CenterPos).normalized;
        stage.CreateDancingTriangleObject(
            owner,
            owner.CenterPos,
            fireDirection,
            damage,
            moveSpeed,
            attakRadius,
            _knobackPower,
            duration,
            _transcendentAttackPeriod,
            _stunDuration,
            _stunPeriod,
            _removePoisonousAreaEffects
            );

    }

    private void NormalSkillFire(PlayerCharacter owner, Stage stage)
    {
        Vector2 targetPosition;
        if (_attackTarget != null)
        {
            targetPosition = _attackTarget.Pos;
        }
        // 수색 범위 안에 적이 없으면 랜덤 위치를 공격한다.
        else
        {
            if(_targetItem != null)
            {
                targetPosition = _targetItem.transform.position;
            }
            else
            {
                targetPosition = owner.Pos + _fireTargetSearchRange * Random.insideUnitCircle;
            }
        }

        var damage = CombatSystem.CalculateCharacterBasicAttackDamage(owner.Stats, _normalAttackPowerRate);
        float attakRadius = _attackRadius * owner.Stats.AttackRangeDistanceRatio.Value;

        Vector2 fireDirection = (targetPosition - owner.Pos).normalized;
        stage.CreateEnchantingGlowNormalAreaEffectObject(
            owner,
            attackAmount: _attackAmount,
            damage,
            startPosition: owner.Pos - fireDirection * 1f,
            attackDirection: fireDirection,
            knobackPower: _knobackPower,
            attackRadius: attakRadius,
            attackPeriod: _attackPeriod,
            attackDuration: _attackDuration,
            stunDuration: _stunDuration,
            stunPeriod: _stunPeriod,
            removePoisons: _removePoisonousAreaEffects
            );
    }

    private Character SearchTarget(PlayerCharacter owner, Stage stage)
    {
        var target = stage.FindClosestCharacter(
            owner.Alliance.ToEnemyAlliance(),
            owner.CenterPos,
            limitDistance: _fireTargetSearchRange,
            condition: character => !character.Action.IsDead && !character.IsImmuneToHit);

        return target;
    }

    private void UpdateTarget(PlayerCharacter owner, Stage stage)
    {
        //기존 타겟이 없거나 죽은 경우
        if(_attackTarget == null || _attackTarget.Action.IsDead || _attackTarget.IsImmuneToHit)
        {
            _attackTarget = this.SearchTarget(owner, stage);
            if(_attackTarget == null )
            {
                return;
            }

            _mambaTargetingEffect.transform.SetParent(_attackTarget.transform);
            _mambaTargetingEffect.gameObject.SetActive(true);
            _mambaTargetingEffect.transform.localPosition = _attackTarget.UIPositionOffset + new Vector2(0, 0.2f);

            _prevSequence?.Kill();
            _prevSequence = DOTween.Sequence();
            _prevSequence.Append(_targetEffectSpriteRenderer.DOFade(1f, 0.4f).From(0f));
            _prevSequence.Join(_mambaTargetingEffect.transform.DOScale(1f, 0.3f).From(5f));

            var loopSequence = DOTween.Sequence();
            loopSequence.Append(_mambaTargetingEffect.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 1, 0).SetLoops(2));
            loopSequence.AppendInterval(0.5f);
            loopSequence.SetLoops(int.MaxValue);
            _prevSequence.Append(loopSequence);

        }
        else if(_fireCheck || _fireTargetSearchRange < Vector2.Distance(owner.CenterPos, _attackTarget.Pos))
        {
            var newTarget = this.SearchTarget(owner, stage);
            if(newTarget == null)
            {
                _prevSequence?.Kill();
                _prevSequence = DOTween.Sequence();
                _prevSequence.Append(_targetEffectSpriteRenderer.DOFade(0f, 0.3f));
                _prevSequence.AppendCallback(() =>
                {
                    _mambaTargetingEffect.transform.SetParent(null);
                    _mambaTargetingEffect.gameObject.SetActive(false);

                    //사라지는 연출이끝나고_attackTarget을 null로 해줘야 
                    //연출이 안꼬인다.
                    _attackTarget = null;
                });
            }
            else if(_attackTarget != newTarget) //새로 타겟을 찾았는데 같은 타겟이면 또 연출할 필요가 없다
            {
                _attackTarget = newTarget;

                _prevSequence?.Kill();
                _prevSequence = DOTween.Sequence();
                _prevSequence.Append(_targetEffectSpriteRenderer.DOFade(0f, 0.3f));
                _prevSequence.AppendCallback(() =>
                {
                    _mambaTargetingEffect.transform.SetParent(_attackTarget.transform);
                    _mambaTargetingEffect.gameObject.SetActive(true);
                    _mambaTargetingEffect.transform.localPosition = _attackTarget.UIPositionOffset + new Vector2(0, 0.2f);
                });
                _prevSequence.Append(_targetEffectSpriteRenderer.DOFade(1f, 0.4f).From(0f));
                _prevSequence.Join(_mambaTargetingEffect.transform.DOScale(1f, 0.3f).From(5f));

                var loopSequence = DOTween.Sequence();
                loopSequence.Append(_mambaTargetingEffect.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 1, 0).SetLoops(2));
                loopSequence.AppendInterval(0.5f);
                loopSequence.SetLoops(int.MaxValue);
                _prevSequence.Append(loopSequence);
            }

            _fireCheck = false;

        }
    }


}
