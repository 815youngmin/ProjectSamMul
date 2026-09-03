using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.GameClients.Stages.ItemObjects;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class TornadoAreaEffectObject : AreaEffectObjectBase
    {
        private const string BODY_BOTTOM_PATH = "Stages/AreaEffects/Tornadoes/TornadoUnder.prefab";
        private const string BODY_TOP_PATH = "Stages/AreaEffects/Tornadoes/TornadoUp.prefab";

        private const string NORMAL_CREATION_SFX = "Sounds/SoundEffects/PCs/TornadoNormalCreation_SFX.prefab";
        private const string TRANSCENDENT_CREATION_SFX = "Sounds/SoundEffects/PCs/TornadoTranscendentCreation_SFX.prefab";
        private const string TRANSCENDENT_LIGHTNING_SFX = "Sounds/SoundEffects/PCs/TornadoTranscendentLightning_SFX.prefab";

        private const string NORMAL_BOTTOM_START_ANIMATION_NAME = "Stom_N_start";
        private const string NORMAL_BOTTOM_REPEAT_ANIMATION_NAME = "Stom_N";
        private const string NORMAL_BOTTOM_END_ANIMATION_NAME = "Stom_N_end";

        private const string TRANSCENDENT_BOTTOM_START_ANIMATION_NAME = "Stom_S_start";
        private const string TRANSCENDENT_BOTTOM_REPEAT_ANIMATION_NAME = "Stom_S";
        private const string TRANSCENDENT_BOTTOM_END_ANIMATION_NAME = "Stom_S_end";

        private const string NORMAL_TOP_START_ANIMATION_NAME = "Stom_N_start";
        private const string NORMAL_TOP_REPEAT_ANIMATION_NAME = "Stom_N";
        private const string NORMAL_TOP_END_ANIMATION_NAME = "Stom_N_end";

        private const string TRANSCENDENT_TOP_START_ANIMATION_NAME = "Stom_S_start";
        private const string TRANSCENDENT_TOP_REPEAT_ANIMATION_NAME = "Stom_S";
        private const string TRANSCENDENT_TOP_END_ANIMATION_NAME = "Stom_S_end";
        private const string TRANSCENDENT_TOP_LIGHTNING_ANIMATION_NAME = "Stom_S_lightning";

        public override bool IsAlive => _isBottomAlive || _isTopAlive;

        private SkeletonAnimation _bodyBottomSkeletonAnimation;
        private SkeletonAnimation _bodyTopSkeletonAnimation;

        private Animation _normalBottomStartAnimation;
        private Animation _normalBottomRepeatAnimation;
        private Animation _normalBottomEndAnimation;

        private Animation _transcendentBottomStartAnimation;
        private Animation _transcendentBottomRepeatAnimation;
        private Animation _transcendentBottomEndAnimation;

        private Animation _normalTopStartAnimation;
        private Animation _normalTopRepeatAnimation;
        private Animation _normalTopEndAnimation;

        private Animation _transcendentTopStartAnimation;
        private Animation _transcendentTopRepeatAnimation;
        private Animation _transcendentTopEndAnimation;
        private Animation _transcendentTopLightningAnimation;

        private Character _owner;
        private float _attackDamagePerTick;
        private float _attackRadius;
        private float _attackDuration;
        private float _attackTickPeriod;
        private float _transcendentAttackDamage;
        private bool _isTranscendent;

        private List<Character> _enemies;
        private float _disappearsAt;
        private float _nextAttackTickAt;
        private bool _isBottomAlive;
        private bool _isTopAlive;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.TornadoObject);

            var bodyBottom = ResourcePool.Instance.InstantiateFromResource(BODY_BOTTOM_PATH);
            bodyBottom.transform.SetParent(transform);
            bodyBottom.transform.localPosition = Vector3.zero;
            bodyBottom.transform.localScale = 0.3f * Vector3.one;
            _bodyBottomSkeletonAnimation = bodyBottom.GetComponent<SkeletonAnimation>();

            var bodyTop = ResourcePool.Instance.InstantiateFromResource(BODY_TOP_PATH);
            bodyTop.transform.SetParent(transform);
            bodyTop.transform.localPosition = Vector3.zero;
            bodyTop.transform.localScale = 0.3f * Vector3.one;
            _bodyTopSkeletonAnimation = bodyTop.GetComponent<SkeletonAnimation>();

            _normalBottomStartAnimation = _bodyBottomSkeletonAnimation.Skeleton.Data.FindAnimation(NORMAL_BOTTOM_START_ANIMATION_NAME);
            _normalBottomRepeatAnimation = _bodyBottomSkeletonAnimation.Skeleton.Data.FindAnimation(NORMAL_BOTTOM_REPEAT_ANIMATION_NAME);
            _normalBottomEndAnimation = _bodyBottomSkeletonAnimation.Skeleton.Data.FindAnimation(NORMAL_BOTTOM_END_ANIMATION_NAME);

            _transcendentBottomStartAnimation = _bodyBottomSkeletonAnimation.Skeleton.Data.FindAnimation(TRANSCENDENT_BOTTOM_START_ANIMATION_NAME);
            _transcendentBottomRepeatAnimation = _bodyBottomSkeletonAnimation.Skeleton.Data.FindAnimation(TRANSCENDENT_BOTTOM_REPEAT_ANIMATION_NAME);
            _transcendentBottomEndAnimation = _bodyBottomSkeletonAnimation.Skeleton.Data.FindAnimation(TRANSCENDENT_BOTTOM_END_ANIMATION_NAME);

            _normalTopStartAnimation = _bodyTopSkeletonAnimation.Skeleton.Data.FindAnimation(NORMAL_TOP_START_ANIMATION_NAME);
            _normalTopRepeatAnimation = _bodyTopSkeletonAnimation.Skeleton.Data.FindAnimation(NORMAL_TOP_REPEAT_ANIMATION_NAME);
            _normalTopEndAnimation = _bodyTopSkeletonAnimation.Skeleton.Data.FindAnimation(NORMAL_TOP_END_ANIMATION_NAME);

            _transcendentTopStartAnimation = _bodyTopSkeletonAnimation.Skeleton.Data.FindAnimation(TRANSCENDENT_TOP_START_ANIMATION_NAME);
            _transcendentTopRepeatAnimation = _bodyTopSkeletonAnimation.Skeleton.Data.FindAnimation(TRANSCENDENT_TOP_REPEAT_ANIMATION_NAME);
            _transcendentTopEndAnimation = _bodyTopSkeletonAnimation.Skeleton.Data.FindAnimation(TRANSCENDENT_TOP_END_ANIMATION_NAME);
            _transcendentTopLightningAnimation = _bodyTopSkeletonAnimation.Skeleton.Data.FindAnimation(TRANSCENDENT_TOP_LIGHTNING_ANIMATION_NAME);

        }

        public void Initialize(
            Character owner,
            Vector2 position,
            float attackDamagePerTick,
            float attackRadius,
            float attackDuration,
            float attackTickPeriod,
            float transcendentAttackDamage,
            bool isTranscendent)
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _attackDamagePerTick = attackDamagePerTick;
            _attackRadius = attackRadius;
            _attackDuration = attackDuration;
            _attackTickPeriod = attackTickPeriod;
            _transcendentAttackDamage = transcendentAttackDamage;
            _isTranscendent = isTranscendent;

            transform.position = position;
            transform.localScale = _attackRadius * Vector3.one;

            float timeScale = (_isTranscendent ? _transcendentBottomRepeatAnimation : _normalBottomRepeatAnimation).Duration / _attackTickPeriod;
            _bodyBottomSkeletonAnimation.timeScale = timeScale;
            _bodyTopSkeletonAnimation.timeScale = timeScale;

            _enemies = new List<Character>();
            _disappearsAt = Time.time + _attackDuration;
            _nextAttackTickAt = Time.time + (_isTranscendent ? _transcendentBottomStartAnimation : _normalBottomStartAnimation).Duration / timeScale;
            _isBottomAlive = true;
            _isTopAlive = true;

            if (_isTranscendent)
            {
                _bodyBottomSkeletonAnimation.AnimationState.SetAnimation(0, _transcendentBottomStartAnimation, loop: false);
                _bodyBottomSkeletonAnimation.AnimationState.AddAnimation(0, _transcendentBottomRepeatAnimation, loop: true, delay: 0.0f);

                _bodyTopSkeletonAnimation.AnimationState.SetAnimation(0, _transcendentTopStartAnimation, loop: false);
                _bodyTopSkeletonAnimation.AnimationState.AddAnimation(0, _transcendentTopRepeatAnimation, loop: true, delay: 0.0f);
            }
            else
            {
                _bodyBottomSkeletonAnimation.AnimationState.SetAnimation(0, _normalBottomStartAnimation, loop: false);
                _bodyBottomSkeletonAnimation.AnimationState.AddAnimation(0, _normalBottomRepeatAnimation, loop: true, delay: 0.0f);

                _bodyTopSkeletonAnimation.AnimationState.SetAnimation(0, _normalTopStartAnimation, loop: false);
                _bodyTopSkeletonAnimation.AnimationState.AddAnimation(0, _normalTopRepeatAnimation, loop: true, delay: 0.0f);
            }

            UnityGlobal.Sounds.PlayBySoundPrefab(_isTranscendent ? TRANSCENDENT_CREATION_SFX : NORMAL_CREATION_SFX, transform.position);

        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if (_nextAttackTickAt < now)
            {
                _nextAttackTickAt += _attackTickPeriod;
                _enemies.Clear();
                var targetArea = new CircularTargetArea(transform.position, _attackRadius);
                stage.FindAliveCharactersInArea(_owner.Alliance.ToEnemyAlliance(), targetArea, _enemies);
                foreach (var enemy in _enemies)
                {
                    enemy.Hitted(stage, _owner, _attackDamagePerTick, (Vector2)transform.position - enemy.Pos, enemy.Pos, null);
                }

                IReadOnlyList<BreakableItemObject> breakableItemObjects = stage.BreakableItemObjects;
                foreach (var breakableItemObject in breakableItemObjects)
                {
                    if (targetArea.Contains(breakableItemObject.transform.position, BreakableItemObject.ITEM_COLLIDER_RADIUS))
                    {
                        breakableItemObject.OnBroken(_owner, _attackDamagePerTick, stage);
                    }
                }
            }

            if (_disappearsAt < now)
            {
                _nextAttackTickAt = float.MaxValue;
                _disappearsAt = float.MaxValue;

                if (_isTranscendent)
                {
                    var bottom = _bodyBottomSkeletonAnimation.AnimationState.SetAnimation(0, _transcendentBottomEndAnimation, loop: false);
                    bottom.Complete += (TrackEntry trackEntry) => { _isBottomAlive = false; };

                    _bodyTopSkeletonAnimation.AnimationState.SetAnimation(0, _transcendentTopEndAnimation, loop: false);
                    var top = _bodyTopSkeletonAnimation.AnimationState.AddAnimation(0, _transcendentTopLightningAnimation, loop: false, delay: 0.0f);
                    top.Start += (TrackEntry trackEntry) =>
                    {
                        _bodyTopSkeletonAnimation.timeScale = 1.0f;

                        _enemies.Clear();
                        stage.FindAliveCharactersInArea(_owner.Alliance.ToEnemyAlliance(), new CircularTargetArea(transform.position, _attackRadius), _enemies);
                        foreach (var enemy in _enemies)
                        {
                            enemy.Hitted(stage, _owner, _transcendentAttackDamage, Vector2.zero, enemy.Pos, null);
                        }

                        UnityGlobal.Sounds.PlayBySoundPrefab(TRANSCENDENT_LIGHTNING_SFX, transform.position);
                    };
                    top.Complete += (TrackEntry trackEntry) => { _isTopAlive = false; };
                }
                else
                {
                    var bottom = _bodyBottomSkeletonAnimation.AnimationState.SetAnimation(0, _normalBottomEndAnimation, loop: false);
                    bottom.Complete += (TrackEntry trackEntry) => { _isBottomAlive = false; };

                    var top = _bodyTopSkeletonAnimation.AnimationState.SetAnimation(0, _normalTopEndAnimation, loop: false);
                    top.Complete += (TrackEntry trackEntry) => { _isTopAlive = false; };
                }
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _bodyBottomSkeletonAnimation.AnimationState.SetEmptyAnimation(0, 0.0f);
            _bodyTopSkeletonAnimation.AnimationState.SetEmptyAnimation(0, 0.0f);
        }
    }
}
