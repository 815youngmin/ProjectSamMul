using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class GumSpecialMineAreaEffect : AreaEffectObjectBase
    {

        public override bool IsAlive => _isAlive;

        private PlayerCharacter _owner;
        private GameObject _body;
        private GameObject _explosionEffect;

        private SkeletonAnimation _bodySkeletonAnimation;
        private SkeletonAnimation _explosionAnimation;
        private Animation _bodyBoomAnimation;
        private float _hitTimeOnAnimation;

        private float _beginAt; 
        private float _autoBoomAt;
        private float _boomAt;
        private float _endAt;

        private bool _isTrigger;
        private bool _isAlive;

        private float _attackRadius; 
        private float _boomDamage; 
        private float _slowAttackDamage; 
        private float _slowAttackDuration; 
        private float _slowRate;
        List<Character> _charactersInArea = new List<Character>();

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.GumSpecialMine);
            _body = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/GumMine/GumSpecialMine.prefab");
            _bodySkeletonAnimation = _body.GetComponent<SkeletonAnimation>();
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one;

            _explosionEffect = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/GumMine/GumSpecialMineExplosion.prefab");
            _explosionAnimation = _explosionEffect.GetComponent<SkeletonAnimation>();
            _explosionEffect.transform.SetParent(this.transform);
            _explosionEffect.transform.localPosition = Vector2.zero;
            _explosionEffect.transform.localScale = Vector2.one;

            var hitFrameEventData = _bodySkeletonAnimation.Skeleton.Data.FindEvent("hit");
            _bodyBoomAnimation = _bodySkeletonAnimation.skeleton.Data.FindAnimation("boom");
            var hitEvent = CharacterAnimationController.FindEventInAnimationTimeline(_bodyBoomAnimation, hitFrameEventData);
            _hitTimeOnAnimation = hitEvent.Time;

        }

        public void Initialize(
            PlayerCharacter owner,
            Vector2 startPosition,
            float attackRadius,         //공격 범위
            float autoBoomWaitDuration, //자동 폭발 대기 시간
            float boomDamage,           //터질때 데미지
            float slowAttackDamage,     //터지면서 생성하는 장판 데미지
            float slowAttackDuration,   //터지면서 생성하는 장판 지속시간
            float slowRate              //터지면서 생성하는 장판 슬로우값
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _owner = owner;

            _attackRadius = attackRadius;
            _boomDamage = boomDamage;
            _slowAttackDamage = slowAttackDamage;
            _slowAttackDuration = slowAttackDuration;
            _slowRate = slowRate;

            _autoBoomAt = now + autoBoomWaitDuration;
            _boomAt = float.MaxValue;
            _endAt = float.MaxValue;
            this.transform.position = startPosition;
            _body.transform.localScale = Vector3.one * attackRadius / 2f;
            _explosionEffect.transform.localScale = Vector3.one * attackRadius / 2f;

            _isAlive = true;
            _isTrigger = false;
            _charactersInArea.Clear();

             var beginTrackEntry = _bodySkeletonAnimation.AnimationState.SetAnimation(0, "Begin", false);
            _bodySkeletonAnimation.AnimationState.AddAnimation(0, "Begin_idle", true, 0f);
            _body.GetComponent<MeshRenderer>().sortingOrder = (int)(this.transform.position.y * -100.0f);
            _bodySkeletonAnimation.Update(0);
            _bodySkeletonAnimation.gameObject.SetActive(true);
            _explosionAnimation.gameObject.SetActive(false);

            _beginAt = now + beginTrackEntry.Animation.Duration;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            //등장 애니메이션을 스킵하면 너무 어색해서 등장 애니메이션 시간동안은 트리거 체크를 하지 않는다.
            if (now <_beginAt)
            {
                return;
            }

            if(!_isTrigger)
            {
                CircularTargetArea area = new CircularTargetArea(this.transform.position, _attackRadius);
                stage.FindAliveCharactersInArea(_owner.Alliance.ToEnemyAlliance(), area, _charactersInArea);
                //몬스터가 밟았거나 자동 폭발 시간 도달하면 폭발 처리
                if (_charactersInArea.Count > 0 || _autoBoomAt <= now)
                {
                    _isTrigger = true;

                    _endAt = now + _bodyBoomAnimation.Duration;
                    _boomAt = now + _hitTimeOnAnimation;

                    _bodySkeletonAnimation.AnimationState.SetAnimation(0, "boom", false);
                    _explosionAnimation.gameObject.SetActive(true);
                    _explosionAnimation.AnimationState.SetAnimation(0, "boom", false);
                }

                return;
            }

            if(_boomAt <=now)
            {
                _boomAt = float.MaxValue;
                var targetArea = new CircularTargetArea(this.transform.position, _attackRadius);
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _boomDamage, CombatSystem.KnockBackType.Pivot, this.transform.position, knockBackPower: 0f, null, null, null);
            }

            if(_endAt <= now)
            {
                _isAlive = false;
                _endAt = float.MaxValue;

                //슬로우 장판 생성
                stage.CreateGumSpecialMineSlowAreaEffect(
                    _owner,
                    this.transform.position,
                    _slowAttackDuration,
                    _slowAttackDamage,
                    _attackRadius,
                    _slowRate
                    );
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();

            _bodySkeletonAnimation.AnimationState.SetEmptyAnimations(0f);
            _bodySkeletonAnimation.AnimationState.ClearTracks();
            _bodySkeletonAnimation.Skeleton.SetToSetupPose();
            _bodySkeletonAnimation.Update(0);

            _explosionAnimation.AnimationState.SetEmptyAnimations(0f);
            _explosionAnimation.AnimationState.ClearTracks();
            _explosionAnimation.Skeleton.SetToSetupPose();
            _explosionAnimation.Update(0);

            _bodySkeletonAnimation.gameObject.SetActive(false);
            _explosionAnimation.gameObject.SetActive(false);

            _charactersInArea.Clear();
        }
    }
}
