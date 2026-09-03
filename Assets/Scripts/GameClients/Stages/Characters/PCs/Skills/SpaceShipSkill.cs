using Shared.StaticDatas;
using Z.Animations.Placeholder;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters.Stats;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
	public class SpaceShipSkill : SkillBase
	{
		private static readonly string SpaceShipShadowPath = "Stages/SkillEffects/SpaceShip/SpaceShipShadow.prefab";
        private static readonly string SpaceShipBeginAnimation = "shadow_in";
        private static readonly string SpaceShipRepeatAnimation = "shadow_ing";
        private static readonly string SpaceShipEndAnimation = "shadow_out";
        private SkeletonAnimation _spaceShipShadow;

        private static readonly float SpaceShipShadowBasicScale = 4f;
        private static readonly float BasicIceAttackRadius = 1.8f;
        private static readonly float BasicTranscendentScale = 1.5f;

        //dataTable Parameter
        private readonly float _iceAttackPowerRate;
        private readonly float _transcendentIcePowerRate;
        private readonly int _iceDropCount;
        private readonly float _iceDropDuration;
        private readonly float _iceDropRadius;
        private readonly float _targetSearchRadius;

        //const Parameter
        private static readonly float SlowAreaEffectLifeTime = 1f;
        private static readonly float SlowDuration = 2f;
        private static readonly float StunDuration = 2f;
        private static readonly float SlowRatio = 0.4f;

        private IReadOnlyCharacterStatCalculators _characterStats;
        private readonly List<Character> _enemies;

        public override float Cooltime => base.Cooltime / _characterStats.SkillAttackSpeedValue;

        private Vector2 _spaceShipPosition;
        private float _attackAt;

        public SpaceShipSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats) : base(staticData)
        {
            _characterStats = characterStats;
            _enemies = new List<Character>();

            _iceAttackPowerRate = staticData.Parameter1;
            _iceDropRadius = staticData.Parameter2; //5f
            _iceDropCount = (int)staticData.Parameter3;  //30f
            _iceDropDuration = staticData.Parameter4;
            _targetSearchRadius = staticData.Parameter5;
            _transcendentIcePowerRate = staticData.Parameter6;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            _enemies.Clear();

            Vector2 position = owner.Pos +  Random.insideUnitCircle.normalized * Random.Range(0, _targetSearchRadius);
            _spaceShipPosition = position;
            _spaceShipShadow = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(SpaceShipShadowPath);
            _spaceShipShadow.transform.position = position;
            _spaceShipShadow.transform.localScale = Vector3.one * SpaceShipShadowBasicScale;

            float beginAnimationDuration = _spaceShipShadow.skeleton.Data.FindAnimation(SpaceShipBeginAnimation).Duration;
            float endAnimationDuration = _spaceShipShadow.skeleton.Data.FindAnimation(SpaceShipEndAnimation).Duration;
            float shadowRepeatDuration = (base.Duration - beginAnimationDuration - endAnimationDuration);    //등장, 대기, 퇴장 모든 애니메이션 합이 지속시간과 동일해야된다.

            _spaceShipShadow.AnimationState.SetAnimation(0, SpaceShipBeginAnimation, false);
            _spaceShipShadow.AnimationState.AddAnimation(0, SpaceShipRepeatAnimation, true, 0f);
            _spaceShipShadow.AnimationState.AddAnimation(0, SpaceShipEndAnimation, false, shadowRepeatDuration);

			_attackAt = now + beginAnimationDuration;

			UnityGlobal.Sounds.PlayBySoundPrefab("Sounds/SoundEffects/PCs/SpaceShipAppear_SFX.prefab", position);
		}

		public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            if(_spaceShipShadow != null)
            {
                ResourcePool.Instance.PutBackInstance(SpaceShipShadowPath, _spaceShipShadow.gameObject);
                _spaceShipShadow = null;
            }
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if(_attackAt <= now)
            {
                int finalIceDropCount = (int)(_iceDropCount * owner.Stats.DurationIncreaseRate.Value);  //지속시간이 늘어난만큼 추가 얼음 개수를 계산한다.
                float finalDropDuration = _iceDropDuration * owner.Stats.DurationIncreaseRate.Value;
                float attackRadius = owner.Stats.AttackRangeDistanceRatio.Value * BasicIceAttackRadius;
                float dropRadius = _iceDropRadius * owner.Stats.AttackRangeDistanceRatio.Value;
                float attackDamage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, _iceAttackPowerRate);

                stage.CreateSpaceShipIceGroup(owner, finalIceDropCount, finalDropDuration, _spaceShipPosition, dropRadius, attackRadius, SlowAreaEffectLifeTime, SlowDuration, SlowRatio, attackDamage);
                _attackAt = float.MaxValue;

                if (IsTranscendent)
                {
                    float transcendentAttackDamage = CombatSystem.CalculateSkillAttackDamage(owner.Stats, _transcendentIcePowerRate);
                    float transcendentAttackObjectSscale = owner.Stats.AttackRangeDistanceRatio.Value * BasicTranscendentScale;
                    float attackDelay = finalDropDuration + SlowAreaEffectLifeTime; 
                    stage.CreateSpaceShipTranscendentIceObject(owner, _spaceShipPosition, attackDelay, StunDuration, transcendentAttackDamage, transcendentAttackObjectSscale);
                }
            }
        }
    }
}
