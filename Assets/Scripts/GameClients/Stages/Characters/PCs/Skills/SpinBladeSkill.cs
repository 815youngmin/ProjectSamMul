#nullable enable
using Shared.StaticDatas;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.AreaEffectObjects;
using SamMul.GameClients.Stages.Characters.Stats;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.UnityHelpers;
using SamMul.UnityHelpers.Sounds;

namespace SamMul.GameClients.Stages.Characters.PCs.Skills
{
    public class SpinBladeSkill : SkillBase
    {
        private List<SpinBladeObject> _spinBladeObjects;

        private float _attackPowerRate;
        private int _totalAmount;   //한번에 생성되는 가디언 서포트 개수
        private float _knockBackPower;
        private float _attackPeriod;    //공격 간격
        private readonly IReadOnlyCharacterStatCalculators _characterStats;
        public override float Duration => base.Duration *  _characterStats.DurationIncreaseRateValue;
        public override float Cooltime => base.Cooltime / _characterStats.SkillAttackSpeedValue;

        private readonly float _fadeDuration;
        private float _spinBladeObjectScale;
        private float _angleSpeedDegree;

        public SpinBladeSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats) : base(staticData)
        {
            _spinBladeObjects = new List<SpinBladeObject>();

            _attackPowerRate = staticData.Parameter1;
            _totalAmount = (int)staticData.Parameter2;
            _knockBackPower = staticData.Parameter3;
            _attackPeriod = staticData.Parameter4;

            _characterStats = characterStats;

            _fadeDuration = 0.5f;
            _angleSpeedDegree = 200.0f;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            float playerAttackRangeDistanceRatio = ((PlayerCharacter)owner).Stats.AttackRangeDistanceRatio.Value;

            float objectRadius = 0.7f * playerAttackRangeDistanceRatio;
            float movingRadius = 4.3f * playerAttackRangeDistanceRatio;
            Vector3 spinBladeObjectScale = Vector3.one * playerAttackRangeDistanceRatio;
            _spinBladeObjectScale = playerAttackRangeDistanceRatio;
            float baseAngleSpeedDegree = 200.0f;
            _angleSpeedDegree = baseAngleSpeedDegree * _characterStats.ProjectileMoveSpeedIncreaseRateValue;
            float damage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, _attackPowerRate);
            float knockbackPower = CombatSystem.CalculateSkillAttackKnockBackPower(owner.Stats, _knockBackPower);

            for (int i = 0; i < _totalAmount; i++)
            {
                //현재 생성되는 spinBladeObject의 현재 위치(Degree 값)
                float startAngleDegree = (float)i / _totalAmount * 360f;

                var spinBladeObject = stage.CreateSpinBladeObject(
                    owner.Alliance,
                    owner,
                    objectRadius,
                    movingRadius,
                    damage,
                    knockbackPower,
                    attackPeriod: _attackPeriod,
                    _angleSpeedDegree,
                    startAngleDegree,
                    StaticData.SkillHitSFXPath
                );
                spinBladeObject.SetTranscendFlag(this.IsTranscendent);
                _spinBladeObjects.Add(spinBladeObject);
                spinBladeObject.transform.localScale = spinBladeObjectScale;
            }

            TryFade(now);

            UnityGlobal.Sounds.PlayBySoundPrefab(StaticData.SkillSFXPath, owner.Pos);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            
            if (_spinBladeObjects.Count > 0)
            {
                for (int i = 0; i < _spinBladeObjects.Count; i++)
                {
                    stage.ReserveToRemoveAreaEffect(_spinBladeObjects[i]);
                }
                _spinBladeObjects.Clear();
            }
            
        }

        public override void ReApplyStat(PlayerCharacterStatCalculators ownerStats)
        {
            float playerAttackRangeDistanceRatio = ownerStats.AttackRangeDistanceRatio.Value;
            float objectRadius = 0.7f * playerAttackRangeDistanceRatio;
            float movingRadius = 4.3f * playerAttackRangeDistanceRatio;
            Vector3 spinBladeObjectScale = Vector3.one * playerAttackRangeDistanceRatio;
            float baseAngleSpeedDegree = 200.0f;
            _angleSpeedDegree = baseAngleSpeedDegree * _characterStats.ProjectileMoveSpeedIncreaseRateValue;

            float damage = CombatSystem.CalculateSkillAttackDamage(ownerStats, _attackPowerRate);
            float knockbackPower = CombatSystem.CalculateSkillAttackKnockBackPower(ownerStats, _knockBackPower);

            for (int i = 0; i < _spinBladeObjects.Count; i++)
            {
                _spinBladeObjects[i].ReApplyStats(damage, knockbackPower, objectRadius, movingRadius, _angleSpeedDegree);
                _spinBladeObjects[i].transform.localScale = spinBladeObjectScale;
            }
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            TryFade(now);
        }

        private void FadeInout(float time, bool isFadeIn)
        {
            float scale = isFadeIn ? Mathf.Lerp(0.0f, 1.0f, time) : Mathf.Lerp(1.0f, 0.0f, time);

            Vector3 resultScale = new Vector3(scale, scale, 1.0f) * _spinBladeObjectScale;
            resultScale.z = 1.0f;

            float resultSpeed = _angleSpeedDegree * ((0.5f * scale) + 0.5f);

            for (int i = 0; i < _spinBladeObjects.Count; i++)
            {
                _spinBladeObjects[i].transform.localScale = resultScale;
                _spinBladeObjects[i].SetAngleMoveSpeed(resultSpeed);
            }
        }

        private void TryFade(float time)
        {
            if (time < ActivatedAt + _fadeDuration)
            {
                float fadeTime = (time - ActivatedAt) / _fadeDuration;
                FadeInout(fadeTime, true);
            }
            else if (DeactivatingAt - _fadeDuration < time)
            {
                float fadeTime = (time - (DeactivatingAt - _fadeDuration)) / _fadeDuration;
                FadeInout(fadeTime, false);
            }
        }
    }
}