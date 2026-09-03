using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.Characters.Actions.Boss.Alibaba
{
    public class SchaibarMultiAreaEffectAction : SmartAction<SpineMonsterAnimationController>
    {
        private static readonly string AttackEffectPath = "Stages/AreaEffects/Egypt_BossAnubisMask_eff2.prefab";

        private static readonly string ReadyAnimationName = "PeriodicExecutionBegin";
        private static readonly string AttackAnimationName = "PeriodicExecutionRepeat";
        private static readonly string EndAnimationName = "PeriodicExecutionEnd";


        private static readonly float MELEE_DAMAGE_COEFFICIENT = 1.0f;
        private static readonly float MeleeAttackRadius = 4f;

        private static readonly float AREAEFFECT_DAMAGE_COEFFICIENT = 1.0f;
        private static readonly int AttackCount = 3;
        private static readonly int AreaEffectAmount = 6;
        private static readonly float AreaEffectRadius = 2;
        private static readonly float AreaEffectCreateDistance = 15f;
        private static readonly float IndicatorDuration = 1f;
        private static readonly float AreaEffectAttackPeriod = 0.25f;
        private static readonly float AreaEffectLifeTime = 5f;


        private Monster _owner;
        private Character _target;

        private Animation _readyAnimation;
        private Animation _attackAnimation;
        private Animation _endAnimation;
        private Vector2 _attackPosition;

        private SkeletonAnimation _effect;

        public override void Initialize(Monster owner, Character target, SpineMonsterAnimationController animationController)
        {
            base.InitializeBase(ActionType.Skill,
                animationController.FindAnimation(ReadyAnimationName).Duration +
                animationController.FindAnimation(AttackAnimationName).Duration * AttackCount +
                animationController.FindAnimation(EndAnimationName).Duration,
                animationController);

            _owner = owner;
            _target = target;

            _readyAnimation = AnimationController.FindAnimation(ReadyAnimationName);
            _attackAnimation = AnimationController.FindAnimation(AttackAnimationName);
            _endAnimation = AnimationController.FindAnimation(EndAnimationName);
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);
            AnimationController.SetAnimation(BodyAnimationTrack.WholeBody, _readyAnimation, false);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _attackAnimation, true, 0f);
            AnimationController.ContinueAnimation(BodyAnimationTrack.WholeBody, _endAnimation, false, _attackAnimation.Duration * AttackCount);

            _effect = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(AttackEffectPath);
            _effect.transform.localScale = Vector3.one;
            _effect.transform.localPosition = Vector3.zero;
            _effect.gameObject.SetActive(false);


            base.AddOneOffSubAction(now, (Stage stage, float deltaTime, float now) =>
            {
                bool isMovingToLeft = _owner.MoveDir.x <= 0;
                if (isMovingToLeft)
                {
                    _attackPosition = _owner.CenterPos + new Vector2(-5f, -3f);
                }
                else
                {
                    _attackPosition = _owner.CenterPos + new Vector2(5f, -3f);
                }
                stage.CreateCircularAttackRangeIndicator(_attackPosition, MeleeAttackRadius, _readyAnimation.Duration +  AnimationController.FindHitTime(_attackAnimation));

            });

            float attackAt = now;
            for (int i = 0; i < AttackCount; i++)
            {
                attackAt = now + _readyAnimation.Duration + AnimationController.FindHitTime(_attackAnimation) + _attackAnimation.Duration * i;
                base.AddOneOffSubAction(attackAt, (Stage stage, float deltaTime, float now) =>
                {
                    _effect.gameObject.SetActive(true);
                    _effect.AnimationState.SetAnimation(0, "attacK2_effu", false);
                    _effect.transform.position = _attackPosition;

                    CircularTargetArea attackArea = new CircularTargetArea(_attackPosition, MeleeAttackRadius);
                    CombatSystem.HitOnTargetArea(
                        stage, attackArea, _owner, _owner.SpecialAttackPower * MELEE_DAMAGE_COEFFICIENT,
                        CombatSystem.KnockBackType.Pivot, Vector2.zero,
                        knockBackPower: 0, null, null, hitSoundPrefabPath: string.Empty);


                    this.CreateAreaEffect(stage, _attackPosition);
                });
            }
        }

        private void CreateAreaEffect(Stage stage, Vector2 centerPos)
        {
            for (int i = 0; i < AreaEffectAmount; i++)
            {
                Vector2 pos = centerPos + Random.insideUnitCircle.normalized * Random.Range(0f, AreaEffectCreateDistance);
                stage.CreateSandAreaEffectObject(_owner, delay: 0f, IndicatorDuration, _owner.SpecialAttackPower * AREAEFFECT_DAMAGE_COEFFICIENT,
                    AreaEffectAttackPeriod, AreaEffectLifeTime, AreaEffectRadius, pos);
            }
        }

        public override ActionBase End(Stage stage)
        {
            if (_effect != null)
            {
                ResourcePool.Instance.PutBackInstance(AttackEffectPath, _effect.gameObject);
                _effect = null;
            }
            return null;
        }
        public override bool Cancel(Stage stage)
        {
            this.End(stage);
            return base.Cancel(stage);
        }
    }
}

