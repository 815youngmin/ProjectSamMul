using Shared.GameDataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Z.GameClients.Stages.Characters.StatusEffects
{
    public class StatusEffectSet
    {
        private Dictionary<string, StatusEffect> _activeEffects;
        private List<string> _effectsToDeleteKeys;

        public StatusEffectSet()
        {
            _activeEffects = new Dictionary<string, StatusEffect>();
            _effectsToDeleteKeys = new List<string>();
        }


        public bool TryAddStatusEffect(Stage stage, Character owner, StatusEffectType type, float duration, float now, float effectParameter1, float effectParameter2 = 0f)
        {
            string statusEffectKey = type.ToString();
            return this.TryAddStatusEffect(stage, owner, type, statusEffectKey, duration, now, effectParameter1, effectParameter2);
        }
        /// <summary>
        /// 같은 StatusEffect가 있으면 아무 처리 없이 False를 반환합니다.
        /// </summary>
        /// <returns>상태 효과 추가 여부를 반환합니다</returns>
        public bool TryAddStatusEffect(Stage stage, Character owner, StatusEffectType type, string statusEffectKey, float duration, float now, float effectParameter1, float effectParameter2 = 0f)
        {
            if (owner.Action.IsDead)
            {
                return false;
            }

            if (this.MonsterCCImmuneCheck(type, owner, stage))
            {
                return false;
            }

            if (_activeEffects.ContainsKey(statusEffectKey))
            {
                return false;
            }
            var statusEffect = this.CreateStatusEffect(type, duration, effectParameter1, effectParameter2);
            statusEffect.Begin(stage, owner, now);
            _activeEffects.Add(statusEffectKey, statusEffect);

            return true;
        }
       
        public void AddOrUpdateStatusEffect(Stage stage, Character owner, StatusEffectType type, float duration, float now, float effectParameter1, float effectParameter2 = 0f)
        {
            string statusEffectKey = type.ToString();
            this.AddOrUpdateStatusEffect(stage, owner, type, statusEffectKey, duration, now, effectParameter1, effectParameter2);
        }
        /// <summary>
        /// 같은 StatusEffect가 있으면 갱신 시킵니다.
        /// </summary>
        public void AddOrUpdateStatusEffect(Stage stage, Character owner, StatusEffectType type, string statusEffectKey, float duration, float now, float effectParameter1, float effectParameter2 = 0f)
        {
            if (owner.Action.IsDead)
            {
                return;
            }

            if (this.MonsterCCImmuneCheck(type, owner, stage))
            {
                return;
            }

            if (_activeEffects.ContainsKey(statusEffectKey))
            {
                var previousEffect = _activeEffects[statusEffectKey];
                previousEffect.End(stage, owner, now);
                _activeEffects.Remove(statusEffectKey);
            }
            var statusEffect = this.CreateStatusEffect(type, duration, effectParameter1, effectParameter2);
            statusEffect.Begin(stage, owner, now);
            _activeEffects.Add(statusEffectKey, statusEffect);
        }

        public void AddOrUpdateStatusEffect(Stage stage, Character owner, StatusEffectType type, float updateThresholdTime,  float duration, float now, float effectParameter1, float effectParameter2 = 0f)
        {
            string statusEffectKey = type.ToString();
            this.AddOrUpdateStatusEffect(stage, owner, type, statusEffectKey, updateThresholdTime, duration, now, effectParameter1, effectParameter2);
        }

        /// <summary>
        /// 같은 StatusEffect가 있으면 적용된 시간이 updateThresholdTime 이상 되었으면 갱신 시킵니다.
        /// </summary>
        public void AddOrUpdateStatusEffect(Stage stage, Character owner, StatusEffectType type, string statusEffectKey, float updateThresholdTime, float duration, float now, float effectParameter1, float effectParameter2 = 0f)
        {
            if (owner.Action.IsDead)
            {
                return;
            }

            if (this.MonsterCCImmuneCheck(type, owner, stage))
            {
                return;
            }

            if (_activeEffects.ContainsKey(statusEffectKey))
            { 
                if (now - _activeEffects[statusEffectKey].StartedAt < updateThresholdTime)
                {
                    return;
                }
                var previousEffect = _activeEffects[statusEffectKey];
                previousEffect.End(stage, owner, now);
                _activeEffects.Remove(statusEffectKey);

            }
            var statusEffect = this.CreateStatusEffect(type, duration, effectParameter1, effectParameter2);
            statusEffect.Begin(stage, owner, now);
            _activeEffects.Add(statusEffectKey, statusEffect);
        }

        private StatusEffect CreateStatusEffect(StatusEffectType type, float duration, float effectParameter1, float effectParameter2)
        {
            switch (type)
            {
                case StatusEffectType.Burn:
                    {
                        return new BurnStatusEffect(duration, dotDamage: effectParameter1);
                    }
                case StatusEffectType.Freeze:
                    {
                        return new FreezeStatusEffect(duration, minusMoveSpeedPercentValue: effectParameter1, beginDamage: effectParameter2);
                    }
                case StatusEffectType.SlowMove:
                    {
                        // effectParameter1 : 0~1 사이의 값을 사용한다.
                        // Percent Multiplier로, 0.2의 값을 넣으면 이동속도가 20%로 줄어든다. (이동속도의 80%만큼을 차감, 근데 곱셈으로)
                        return new SlowMove(effectParameter1, duration);
                    }
                case StatusEffectType.IncreaseAttackSpeed:
                    {
                        return new IncreaseAttackSpeedStatusEffect(effectParameter1, duration);
                    }
                case StatusEffectType.IncreaseAttackDamage:
                    {
                        return new IncreaseAttackDamageStatusEffect(effectParameter1, duration);
                    }
                case StatusEffectType.IncreaseReceivedDamage:
                    {
                        return new IncreaseReceivedDamageStatusEffect(effectParameter1, duration);
                    }
                case StatusEffectType.SuperArmor:
                    {
                        return new SuperArmorStatusEffect(duration);
                    }
                case StatusEffectType.Stun:
                    {
                        return new StunStatusEffect(duration);
                    }
                case StatusEffectType.VisionRestriction:
                    {
                        return new VisionRestrictionStatusEffect(effectParameter1, duration);
                    }
                case StatusEffectType.PushedBack:
                    {
                        return new PushedBackStatusEffect(duration, effectParameter1, effectParameter2);
                    }
                case StatusEffectType.Invisible:
                    {
                        return new InvisibleStatusEffect(duration);
                    }
                case StatusEffectType.SpreadBurn:
                    {
                        return new SpreadBurnStatusEffect(duration, dotDamage: effectParameter1, spreadAmount: (int)effectParameter2);
                    }
                case StatusEffectType.ElectricShocked:
                default:
                    {
                        Debug.LogError($"{type}구현 안 됨. 구현해주세염.");
                        return null;
                    }
            }
        }

        public void Update(Stage stage, Character owner, float now)
        {
            foreach (var activeEffectData in _activeEffects)
            {
                activeEffectData.Value.Update(stage, owner, now);
                if (activeEffectData.Value.EndAt <= now)
                {
                    _effectsToDeleteKeys.Add(activeEffectData.Key);
                }

                //burn 같은 틱뎀으로 죽을 경우 dead 처리가 되면서 activeEffects를 비워버리는 처리가 존재한다.
                //StatusEffect.Clear 처리가 되서 나온 경우 진행을 멈추고 넘어간다.
                if (_activeEffects.Count == 0 || owner.Action.IsDead)
                {
                    break;
                }
            }

            foreach (var effectToDeleteKey in _effectsToDeleteKeys)
            {
                if(_activeEffects.ContainsKey(effectToDeleteKey))
                {
                    _activeEffects[effectToDeleteKey].End(stage, owner, now);
                    _activeEffects.Remove(effectToDeleteKey);
                }
            }
            _effectsToDeleteKeys.Clear();
        }

        public void Clear(Character owner, Stage stage)
        {
            foreach(var effect in _activeEffects.Values)
            {
                effect.Cancel(owner, stage);
            }
            _activeEffects.Clear();
            _effectsToDeleteKeys.Clear();
        }

        public void OwnerDead(Character owner, Stage stage)
        {
            foreach (var effect in _activeEffects.Values)
            {
                effect.OwnerDead(owner, stage);
            }
        }


        private bool MonsterCCImmuneCheck(StatusEffectType type, Character owner, Stage stage)
        {
            if (!owner.CharacterType.IsHeroType() && stage.IsMonsterCCImmune)
            {
                switch (type)
                {
                    case StatusEffectType.Burn:
                    case StatusEffectType.Freeze:
                    case StatusEffectType.SlowMove:
                    case StatusEffectType.Stun:
                    case StatusEffectType.PushedBack:
                        //CC면역인경우 처리해주면 안되는경우
                        return true; ;
                    case StatusEffectType.ElectricShocked:
                    case StatusEffectType.IncreaseAttackSpeed:
                    case StatusEffectType.IncreaseAttackDamage:
                    case StatusEffectType.IncreaseReceivedDamage:
                    case StatusEffectType.SuperArmor:
                    case StatusEffectType.VisionRestriction:
                        //CC면역과는 상관 없는 상태효과들
                        break;
                    default:
                        Debug.LogError($"{type}CC면역 처리 없음. 구현해주세염.");
                        break;
                }
            }
            return false;
        }
    }

}
