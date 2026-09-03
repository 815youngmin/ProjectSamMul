using Z.Animations.Placeholder;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.PCs;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class GumSupportFireAreaEffect : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _attackDelay + _attackDuration + _endDuration;

        private PlayerCharacter _owner;
        private float _damage;
        private float _attackRadius;

        private float _createdAt;
        private float _attackDelay;
        private float _endDuration;

        private float _attackAt;
        private float _attackDuration;


        private GameObject _bodyUp;
        private SkeletonAnimation _bodyUpSkeletonAnimation;
        private GameObject _bodyDown;
        private SkeletonAnimation _bodyDownSkeletonAnimation;

        private static readonly float AttackPeriod = 0.5f;
        private float _nextTickAt;
        private HashSet<Character> _hittedCharacters;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.GumSupportFireAreaEffect);
            _bodyUp = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/GumSupportFire/Backup_Attack_U.prefab");
            _bodyUp.transform.SetParent(this.transform);
            _bodyUp.transform.localPosition = new Vector3(0,-0.32f, 0f);
            _bodyUp.transform.localScale = Vector2.one;
            _bodyUpSkeletonAnimation = _bodyUp.GetComponent<SkeletonAnimation>();


            _bodyDown = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/GumSupportFire/Backup_Attack_D.prefab");
            _bodyDown.transform.SetParent(this.transform);
            _bodyDown.transform.localPosition = new Vector3(0, -0.32f, 0f);
            _bodyDown.transform.localScale = Vector2.one;
            _bodyDownSkeletonAnimation = _bodyDown.GetComponent<SkeletonAnimation>();

            _hittedCharacters = new HashSet<Character>();

        }

        public void Initialize(
            PlayerCharacter owner,
            Vector2 position,
            float damage,
            float attackDuration,
            float attackRadius
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _createdAt = now;
            _owner = owner;
            _damage = damage;
            this.transform.position = position;
            _attackDuration = attackDuration;
            _attackRadius = attackRadius;

            _bodyUp.transform.localScale = Vector3.one * attackRadius / 2f;
            _bodyDown.transform.localScale = Vector3.one * attackRadius / 2f;

            var startTrackEntry = _bodyUpSkeletonAnimation.AnimationState.SetAnimation(0, "start_U", false);
            _bodyUpSkeletonAnimation.AnimationState.AddAnimation(0, "ing_U", true, 0f);
            var endTrackEntry = _bodyUpSkeletonAnimation.AnimationState.AddAnimation(0, "end_U", false, _attackDuration);
            _bodyUpSkeletonAnimation.Update(0);
            _bodyUpSkeletonAnimation.gameObject.SetActive(true);

            _bodyDownSkeletonAnimation.AnimationState.SetAnimation(0, "start_D", false);
            _bodyDownSkeletonAnimation.AnimationState.AddAnimation(0, "ing_D", true, 0f);
            _bodyDownSkeletonAnimation.AnimationState.AddAnimation(0, "end_D", false, _attackDuration);
            _bodyDownSkeletonAnimation.Update(0);
            _bodyDownSkeletonAnimation.gameObject.SetActive(true);

            _attackDelay = startTrackEntry.Animation.Duration;
            _attackAt = now + _attackDelay;
            _endDuration = endTrackEntry.Animation.Duration;

            _hittedCharacters.Clear();
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if (now < _attackAt)
            {
                return;
            }

            if (_nextTickAt < now)
            {
                _nextTickAt = now + AttackPeriod;
                _hittedCharacters.Clear();
            }

            if(now <  _attackAt + _attackDuration)
            {
                var targetArea = new CircularTargetArea(this.transform.position, _attackRadius);
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Direction, Vector2.zero, knockBackPower: 0f, _hittedCharacters, _hittedCharacters, null);
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _bodyUpSkeletonAnimation.AnimationState.SetEmptyAnimations(0f);
            _bodyUpSkeletonAnimation.AnimationState.ClearTracks();
            _bodyUpSkeletonAnimation.Skeleton.SetToSetupPose();
            _bodyUpSkeletonAnimation.Update(0);

            _bodyDownSkeletonAnimation.AnimationState.SetEmptyAnimations(0f);
            _bodyDownSkeletonAnimation.AnimationState.ClearTracks();
            _bodyDownSkeletonAnimation.Skeleton.SetToSetupPose();
            _bodyDownSkeletonAnimation.Update(0);

            _bodyUpSkeletonAnimation.gameObject.SetActive(false);
            _bodyDownSkeletonAnimation.gameObject.SetActive(false);

            _hittedCharacters.Clear();
        }

    }

}
