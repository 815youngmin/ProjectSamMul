using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class RuneTrapBombAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => _isAlive;

        private static readonly float DETECT_RANGE_RATE = 0.6f; // 폭발 범위의 비례해서 적을 탐색함.
        private static readonly float BOOM_DELAY = 0.5f;
        private static readonly string BOOM_SFX_PATH = "Sounds/SoundEffects/PCs/RuneTrapNormalBoom_SFX.prefab";

        private PlayerCharacter _owner;
        private GameObject _mineBody;

        private SkeletonAnimation _mineSkeletonAnimation;
        private Animation _appear;
        private Animation _bomb;
        private Animation _idle;

        private float _damage;
        private float _boomRadius; // 범위 증가 효과 처리는 TrapBoomSkill에서 처리해 받습니다.
        private float _knockBackPower;

        private float _deadAt;
        private float _appearDuration;
        private float _serchStartTimeAt;
        private float _boomAt;
        private float _triggerAt;
        private bool _boomTrigger;
        private SpriteAnimationHandler _boomAnimationHandler;

        private RuneTrapTimer _runeTrapTimer;
        List<Character> _charactersInArea = new List<Character>();
        private bool _isAlive;
        private string _hitSoundPrefabPath;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.TrapBomb);

            _mineBody = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/TrapBomb/TrapBomb.prefab");
            _mineBody.transform.SetParent(this.transform);

            _mineSkeletonAnimation = _mineBody.GetComponent<SkeletonAnimation>();
            _appear = _mineSkeletonAnimation.Skeleton.Data.FindAnimation("mine_appear");
            _bomb = _mineSkeletonAnimation.Skeleton.Data.FindAnimation("mine_bomb");
            _idle = _mineSkeletonAnimation.Skeleton.Data.FindAnimation("mine_idle");

            _boomAnimationHandler = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>("Stages/AreaEffects/TrapBomb/mine_bomb.prefab");
            _boomAnimationHandler.InitializeOnly();
            _boomAnimationHandler.transform.SetParent(this.transform);
            _boomAnimationHandler.gameObject.SetActive(false);

            _runeTrapTimer = ResourcePool.Instance.InstantiateFromResource<RuneTrapTimer>("Stages/AreaEffects/TrapBomb/RuneTrapTimer.prefab");
            _runeTrapTimer.transform.SetParent(this.transform);
            _runeTrapTimer.transform.localScale = Vector3.one;
            _runeTrapTimer.transform.localPosition = Vector3.zero;
            _isAlive = false;

        }

        public void Initialize(PlayerCharacter owner, float damage, float radius, float lifeTime, float knockBackPower, string hitSoundPrefabPath)
        {
            float now = Time.time;

            _owner = owner;
            _damage = damage;
            _boomRadius = radius;
            _deadAt = lifeTime + now;
            _knockBackPower = knockBackPower;
            _hitSoundPrefabPath = hitSoundPrefabPath;

            _appearDuration = 0.9f;
            _serchStartTimeAt = now + _appearDuration;
            _boomAnimationHandler.gameObject.SetActive(false);
            _isAlive = true;

            _mineSkeletonAnimation.AnimationState.SetAnimation(0, _appear, false).TimeScale = _appear.Duration / _appearDuration;
            _mineSkeletonAnimation.AnimationState.AddAnimation(0, _idle, true, 0.0f);
            _mineSkeletonAnimation.Update(0);//_apper 애니메이션 첫 프레임으로 업데이트 해줘서 자연스럽게 설치되도록 진행한다.
            _mineBody.SetActive(true);

            _boomAnimationHandler.transform.localScale = Vector3.one * (radius * 0.33f);
            _mineBody.transform.localScale = Vector3.one * radius * DETECT_RANGE_RATE * 0.8333f;
            _boomAt = 0.0f;
            _boomTrigger = false;
            _runeTrapTimer.gameObject.SetActive(true);
            _runeTrapTimer.FillAmount(0);
            _runeTrapTimer.transform.localScale = Vector3.one * radius;
            _charactersInArea.Clear();
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if (now < _serchStartTimeAt)
            {
                return;
            }

            if(!_boomTrigger)
            {
                CircularTargetArea area = new CircularTargetArea(this.transform.position, _boomRadius * DETECT_RANGE_RATE);
                stage.FindAliveCharactersInArea(_owner.Alliance.ToEnemyAlliance(), area, _charactersInArea);
                if (_charactersInArea.Count > 0 || now >_deadAt)
                {
                    _boomTrigger = true;
                    _boomAt = now + BOOM_DELAY;
                    _triggerAt = now;
                }
            }
            else
            {
                if (now > _boomAt)
                {
                    _boomAt = float.MaxValue;
                    TrackEntry trackEntry = _mineSkeletonAnimation.AnimationState.SetAnimation(0, _bomb, false);
                    trackEntry.Complete += (TrackEntry trackEntry) =>
                    {
                        AreaAttack(stage);
                    };

                    UnityGlobal.Sounds.PlayBySoundPrefab(BOOM_SFX_PATH, transform.position);
                }

                _runeTrapTimer.FillAmount((now - _triggerAt) / BOOM_DELAY);
            }

        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _owner = null;
            _boomAnimationHandler.gameObject.SetActive(false);
            _mineBody.SetActive(false);
        }

        private void AreaAttack(Stage stage)
        {
            _mineBody.SetActive(false);
            _runeTrapTimer.gameObject.SetActive(false);

            _boomAnimationHandler.gameObject.SetActive(true);
            _boomAnimationHandler.InitializeAndPlay(null, () =>
            {
                _isAlive = false;
            });

            CircularTargetArea attackArea = new CircularTargetArea(this.transform.position, _boomRadius);
            // Hit 처리중에 Throw처리가 되면 오브젝트가 꺼지지 않기 때문에 제일 마지막에 처리한다.
            CombatSystem.HitOnTargetArea(stage, attackArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, attackArea.Center, _knockBackPower, null, null, _hitSoundPrefabPath);
        }
    }
}
