using DG.Tweening;
using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;


namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class StageMeteorAreaEffectObject : AreaEffectObjectBase
    {
        private const string NORMAL_BOOM_EFFECT_PATH = "Stages/SkillEffects/fx_meteoExplosion.prefab";
        private const string NORMAL_SKELETON_ANIMATION_PATH = "Stages/AreaEffects/Meteor/prefab_MeteorAreaEffectObject.prefab";

        private static readonly Quaternion METEOR_DROP_ROTATION = Quaternion.Euler(0.0f, 0.0f, 45.0f);
        private static readonly Vector2 METEOR_DROP_DIRECTION = METEOR_DROP_ROTATION * Vector2.down;
        private static readonly float METEOR_DROP_SPEED = 30.0f;
        private static readonly float METEOR_DROP_TIME = 1.0f;

        public override bool IsAlive => Time.time <= _createAt + _indicatorDuration + METEOR_DROP_TIME + _normalBoomAnimation.Duration + 0.5f;

        private SkeletonAnimation _normalSkeletonAnimation;

        private Animation _normalDropAnimation;
        private Animation _normalBoomAnimation;

        AllianceType _allianceType;
        private float _attackDamage;
        private float _attackRadius;
        private float _knockbackPower;
        private float _burnDamage;
        private float _burnDuration;

        private CircularTargetArea _targetArea;
        private bool _hasBoomed;
        private float _createAt;
        private float _dropAt;
        private float _boomAt;
        private float _indicatorAt;
        private float _indicatorDuration;


        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.StageMeteorAreaEffectObject);

            _normalSkeletonAnimation = ResourcePool.Instance.InstantiateFromResource<SkeletonAnimation>(NORMAL_SKELETON_ANIMATION_PATH);
            _normalSkeletonAnimation.transform.SetParent(transform);
            _normalSkeletonAnimation.transform.localPosition = Vector3.zero;
            _normalSkeletonAnimation.transform.localScale = 0.8f * Vector3.one;
            _normalSkeletonAnimation.skeleton.FindSlot("size").Attachment = null; // 배경 제거.


            _normalDropAnimation = _normalSkeletonAnimation.skeleton.Data.FindAnimation("ing");
            _normalBoomAnimation = _normalSkeletonAnimation.skeleton.Data.FindAnimation("hit");

        }

        public void Initialize(
            Stage stage,
            AllianceType allianceType,
            float indicatorDuration,
            Vector2 dropPosition,
            float attackDamage,
            float attackRadius,
            float burnDamage,
            float burnDuration)
        {
            base.InitializeAreaObject(allianceType);

            _allianceType = allianceType;
            _indicatorDuration = indicatorDuration;
            _attackDamage = attackDamage;
            _attackRadius = attackRadius;
            _burnDamage = burnDamage;
            _burnDuration = burnDuration;

            _targetArea = new CircularTargetArea(dropPosition, attackRadius);
            _hasBoomed = false;

            _createAt = Time.time;
            _indicatorAt = _createAt;
            _dropAt = _indicatorAt + _indicatorDuration;
            _boomAt = _dropAt + METEOR_DROP_TIME;

            DOTween.Sequence()
                .AppendCallback(() =>
                {
                    _normalSkeletonAnimation.gameObject.SetActive(false);
                    stage.CreateCircularAttackRangeIndicator(_targetArea.Center, _targetArea.Radius, _indicatorDuration + METEOR_DROP_TIME);
                })
                .AppendInterval(_indicatorDuration)
                .AppendCallback(()=>
                {
                    _normalSkeletonAnimation.gameObject.SetActive(true);
                    _normalSkeletonAnimation.AnimationState.SetAnimation(0, _normalDropAnimation, loop: true);

                    transform.SetPositionAndRotation(dropPosition - METEOR_DROP_SPEED * METEOR_DROP_DIRECTION, METEOR_DROP_ROTATION);
                    transform.localScale = attackRadius * Vector3.one;
                })
                .Append(transform.DOMove(dropPosition, METEOR_DROP_TIME).SetEase(Ease.Linear))
                .AppendCallback(() =>
                {
                    this.Boom(stage);
                });

        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {

        }

        private void Boom(Stage stage)
        {

            transform.rotation = Quaternion.identity;
            _normalSkeletonAnimation.AnimationState.SetAnimation(0, _normalBoomAnimation, loop: false);
            stage.Particles.CreateParticle(NORMAL_BOOM_EFFECT_PATH, transform.position, 0.8f * _attackRadius * Vector3.one);
           
            List<Character> findedCharacters = new List<Character>();
            stage.FindAliveCharactersInArea(_allianceType.ToEnemyAlliance(), _targetArea, findedCharacters);

            foreach (Character character in findedCharacters)
            {
                character.Hitted(stage, null, _attackDamage, Vector2.zero, _targetArea.Center, null);
                character.StatusEffects.AddOrUpdateStatusEffect(stage, character, Characters.StatusEffects.StatusEffectType.Burn, _burnDuration, Time.time, _burnDamage);
            }
        }


        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();

            _normalSkeletonAnimation.gameObject.SetActive(false);
        }

    }

}
