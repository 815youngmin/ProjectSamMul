using Shared.GameDataTypes;
using Z.Animations.Placeholder;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.ResourcePools;

namespace Z.GameClients.Stages.Characters.Actions
{
    public class DropShipSummonAction : SmartAction<SpriteMonsterAnimationController>
    {
        private Monster _owner;
        private Character _target;
        private float _summonDuration;

        private static readonly string SummonGroundEffectPath = "Stages/ETCEffects/edf_summon_eff.prefab";
        private static readonly string SummonWifiEffectPath = "Stages/SkillEffects/FX_EarthGuardian_Wifi.prefab";

        private static readonly string WifiReadyAnimationName = "SummonExecutionBegin";
        private static readonly string WifiSummonAnimationName = "SummonExecutionRepeat";
        private static readonly string WifiEndAnimationName = "SummonExecutionEnd";

        private SkeletonAnimation _summonGroundEffect;
        private SkeletonAnimation _summonWifiEffect;

        private float _attackPowerWeight;
        private float _hpWeight;
        private CharacterType _summonCharacaterType;

        public void Initialize(Monster owner, Character target, float summonDuration, SpriteMonsterAnimationController animationController)
        {
            _summonDuration = summonDuration;
            this.Initialize(owner, target, animationController);

            _attackPowerWeight = owner.StaticData.Param1;
            _hpWeight = owner.StaticData.Param2;
            _summonCharacaterType = owner.StaticData.SpawnMonsterType;

        }

        public override void Initialize(Monster owner, Character target, SpriteMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Summoned,
            _summonDuration,
            animationController);

            _owner = owner;
            _target = target;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            _summonWifiEffect = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(SummonWifiEffectPath);

            _summonWifiEffect.transform.SetParent(_owner.Body.transform);
            _summonWifiEffect.transform.localPosition = new Vector2(-0.6f, 0.11f);
            _summonWifiEffect.transform.localScale = Vector3.one * 0.5f;
            

            var wifiReadyDuration = _summonWifiEffect.skeleton.Data.FindAnimation(WifiReadyAnimationName).Duration;
            var wifiEndDuration = _summonWifiEffect.skeleton.Data.FindAnimation(WifiEndAnimationName).Duration;
            
            _summonWifiEffect.AnimationState.SetAnimation(0, WifiReadyAnimationName, false);
            _summonWifiEffect.AnimationState.AddAnimation(0, WifiSummonAnimationName, true, 0f);
            _summonWifiEffect.AnimationState.AddAnimation(0, WifiEndAnimationName, false, _summonDuration - wifiReadyDuration - wifiEndDuration);

            Vector3 summonPosition = _owner.Pos + Random.insideUnitCircle.normalized * Random.Range(_owner.ColliderRadius, 6);
            _summonGroundEffect = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(SummonGroundEffectPath);
            _summonGroundEffect.transform.position = summonPosition;
            _summonGroundEffect.transform.localScale = Vector3.one;

            var groundEffectAnimation = _summonGroundEffect.Skeleton.Data.FindAnimation("animation");
            var timeScale = groundEffectAnimation.Duration / _summonDuration;
            _summonGroundEffect.AnimationState.SetAnimation(0, groundEffectAnimation, loop: false).TimeScale = timeScale;

            _owner.StopMovement();
            AnimationController.PlayAttackForce(_summonDuration);

            float time = now + _summonDuration;
            base.AddOneOffSubAction(time, (Stage stage, float deltaTime, float now) =>
            {
                stage.CreateMonster(_owner.Alliance, _summonCharacaterType,
                MonsterInstanceInitialData.CreateForStageMonster(summonPosition,
                hpWeight: _hpWeight,
                attackPowerWeight: _attackPowerWeight,
                dropExp: 0,
                dropGolds: 0,
                dropItems: new List<DropItemType>()),
                isBoss: false,
                isElite: false);
            });
        }

        public override bool Cancel(Stage stage)
        {
            this.End(stage);
            return base.Cancel(stage);
        }

        public override ActionBase End(Stage stage)
        {
            if(_summonGroundEffect != null)
            {
                _summonGroundEffect.AnimationState.SetEmptyAnimation(0, 0f);
                ResourcePool.Instance.PutBackInstance(SummonGroundEffectPath, _summonGroundEffect.gameObject);
                _summonGroundEffect = null;
            }

            if(_summonWifiEffect != null)
            {
                _summonWifiEffect.AnimationState.SetEmptyAnimation(0, 0f);
                ResourcePool.Instance.PutBackInstance(SummonWifiEffectPath,_summonWifiEffect.gameObject);
                _summonWifiEffect = null;
            }

            return null;
        }
    }
}

