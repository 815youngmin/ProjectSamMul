using DG.Tweening;
using Z.Animations.Placeholder;
using System;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.PCs;
using Z.GameClients.Stages.CombatSystems;
using Z.GameClients.Stages.ProjectileObjects;
using Z.ResourcePools;
using Z.UnityHelpers;

using Random = UnityEngine.Random;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class BubbleGumAreaEffectObject : AreaEffectObjectBase
    {
        public static readonly string SLOW_STATUS_EFFECT_KEY = "BubbleGumAreaEffectObject";
        private List<string> _gumPoppingProjectilePaths;

        private static readonly string NORMAL_CREATION_SFX_PATH = "Sounds/SoundEffects/PCs/BubbleGumNormalCreation_SFX.prefab";
        private static readonly string TRANSCENDENT_CREATION_SFX_PATH = "Sounds/SoundEffects/PCs/BubbleGumTranscendentCreation_SFX.prefab";
        private static readonly string BOOM_SFX_PATH = "Sounds/SoundEffects/PCs/BubbleGumBoom_SFX.prefab";

        public override bool IsAlive => Time.time <= _aliveUntil;

        private PlayerCharacter _owner;
        private float _dotDamage;
        private float _dotDamagePeriod;
        private float _explosionDamage;
        private float _subProjectileDamage;
        private float _areaRadius;
        private float _moveSpeedChangeRatio;
        private float _moveSpeedChangeRatioForElite;
        private float _moveSpeedChangeDuration;

        private float _nextDotDamageAt;
        private float _explosionAt;
        private float _lifetime; // 이 장판의 지속시간. 지속시간이 만료되면 터지면서 끝난다.
        private float _aliveUntil; // 이 장판이 제거될 시간.
        private float _createAnimationDuration;
        private float _endAnimationDuration;

        private int _miniGumAmount; //CleavageGum 등급효과 기능

        private HashSet<Character> _hittedCharacters;

        private GameObject _bubbleGumBody;
        private SkeletonAnimation _bubbleGumSkeletonAnimation;
        private GameObject _poppingGumBody;
        private SkeletonAnimation _poppingGumSkeletonAnimation;

        private GameObject _currentBody;

        private bool _isTranscendent;
        private AreaEffectType _areaEffectType;

        public void AllocateSharedResources(AreaEffectType areaEffectType)
        {
            Debug.Assert(areaEffectType == AreaEffectType.BubbleGum_Default || areaEffectType == AreaEffectType.BubbleGum_Spy);
            base.AllocateSharedResourcesForBase(areaEffectType);
            _areaEffectType = areaEffectType;

            var bodyPrefabPath = areaEffectType switch
            {
                AreaEffectType.BubbleGum_Default => "Stages/AreaEffects/BubbleGum/BubbleGum.prefab",
                AreaEffectType.BubbleGum_Spy => "Stages/AreaEffects/BubbleGum/BubbleGum_Spy.prefab",
                _ => throw new NotSupportedException($"AreaEffectType {areaEffectType}이 BubbleGum이 아닙니다."),
            };


            _bubbleGumBody = ResourcePool.Instance.InstantiateFromResource(bodyPrefabPath);   
            _bubbleGumSkeletonAnimation = _bubbleGumBody.GetComponent<SkeletonAnimation>();    
            _bubbleGumBody.transform.SetParent(this.transform);
            _bubbleGumBody.transform.localPosition = Vector3.zero;
            _bubbleGumBody.transform.localScale = new Vector3(0.145f, 0.145f, 0.145f);

            var poppingPrefabPath = areaEffectType switch
            {
                AreaEffectType.BubbleGum_Default => "Stages/AreaEffects/BubbleGum/gum_s.prefab",
                AreaEffectType.BubbleGum_Spy => "Stages/AreaEffects/BubbleGum/BubbleGum_Spy_S.prefab",
                _ => throw new NotSupportedException($"AreaEffectType {areaEffectType}이 BubbleGum이 아닙니다."),
            };

            _gumPoppingProjectilePaths = new List<string>();
            switch (areaEffectType)
            {
                case AreaEffectType.BubbleGum_Default:
                    {
                        _gumPoppingProjectilePaths.Add("Stages/Projectiles/Gum_Projectile/gumball01.prefab");
                        _gumPoppingProjectilePaths.Add("Stages/Projectiles/Gum_Projectile/gumball02.prefab");
                        _gumPoppingProjectilePaths.Add("Stages/Projectiles/Gum_Projectile/gumball03.prefab");
                        _gumPoppingProjectilePaths.Add("Stages/Projectiles/Gum_Projectile/gumball04.prefab");
                        _gumPoppingProjectilePaths.Add("Stages/Projectiles/Gum_Projectile/gumball05.prefab");
                        _gumPoppingProjectilePaths.Add("Stages/Projectiles/Gum_Projectile/gumball06.prefab");
                        break;
                    }
                case AreaEffectType.BubbleGum_Spy:
                    {
                        _gumPoppingProjectilePaths.Add("Stages/Projectiles/Gum_Projectile/gumball_Spy.prefab");
                        break;
                    }
                default:
                    {
                        throw new NotSupportedException($"AreaEffectType {areaEffectType}이 BubbleGum이 아닙니다.");
                    } 
            }
            _poppingGumBody = ResourcePool.Instance.InstantiateFromResource(poppingPrefabPath);   
            _poppingGumSkeletonAnimation = _poppingGumBody.GetComponent<SkeletonAnimation>();
            _poppingGumBody.transform.SetParent(this.transform);
            _poppingGumBody.transform.localPosition = Vector2.zero;
            _poppingGumBody.transform.localScale = new Vector3(0.145f, 0.145f, 0.145f);
            _hittedCharacters = new HashSet<Character>();

        }

        /// <param name="subProjectileDamage">초월 폭파시 날아가는 작은 추가 프로젝타일의 대미지</param>
        /// <param name="lifetime">이 오브젝트(껌 장판)의 지속시간</param>
        public void Initialize(
            PlayerCharacter owner,
            Vector2 spawnPosition,
            float dotDamage,
            float dotDamagePeriod,
            float explosionDamage,
            float subProjectileDamage,
            float areaRadius,
            float lifetime,
            float moveSpeedChangeRatio,
            float moveSpeedChangeDuration,
            int miniGumAmount,
            bool isTranscendent
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _owner = owner;
            _dotDamage = dotDamage;
            _dotDamagePeriod = dotDamagePeriod;
            _explosionDamage = explosionDamage;
            _subProjectileDamage = subProjectileDamage;
            _areaRadius = areaRadius;
            _moveSpeedChangeRatio = moveSpeedChangeRatio;
            if (_moveSpeedChangeRatio < 0f)
            {
                _moveSpeedChangeRatio = 0.1f;
            }
            else if (_moveSpeedChangeRatio > 1f)
            {
                _moveSpeedChangeRatio = 1f;
            }
            _moveSpeedChangeRatioForElite = 1f - ((1f-moveSpeedChangeRatio) * 0.5f);
            _moveSpeedChangeDuration = moveSpeedChangeDuration;
            _nextDotDamageAt = now;
            _lifetime = lifetime;
            _explosionAt = now + lifetime;
            _miniGumAmount = miniGumAmount;

            _isTranscendent = isTranscendent;

            if (isTranscendent)
            {
                //초월 리소스
                _createAnimationDuration = _poppingGumSkeletonAnimation.Skeleton.Data.FindAnimation("start").Duration;
                _endAnimationDuration = _poppingGumSkeletonAnimation.Skeleton.Data.FindAnimation("boom").Duration;

                _poppingGumSkeletonAnimation.AnimationState.SetAnimation(0, "start", false);
                _poppingGumSkeletonAnimation.AnimationState.AddAnimation(0, "ing", true, 0f);
                _poppingGumSkeletonAnimation.AnimationState.AddAnimation(0, "boom", false, _lifetime - _createAnimationDuration);
                _poppingGumSkeletonAnimation.Update(0);

                _poppingGumBody.SetActive(true);
                _bubbleGumBody.SetActive(false);

                _currentBody = _poppingGumBody;
            }
            else
            {
                //일반 리소스
                _createAnimationDuration = _bubbleGumSkeletonAnimation.Skeleton.Data.FindAnimation("create").Duration;
                _endAnimationDuration = _bubbleGumSkeletonAnimation.Skeleton.Data.FindAnimation("end").Duration;

                _bubbleGumSkeletonAnimation.AnimationState.SetAnimation(0, "create", false);
                _bubbleGumSkeletonAnimation.AnimationState.AddAnimation(0, "idle", true, 0f);
                _bubbleGumSkeletonAnimation.AnimationState.AddAnimation(0, "end", false, _lifetime - _createAnimationDuration);
                _bubbleGumSkeletonAnimation.Update(0);

                _bubbleGumBody.SetActive(true);
                _poppingGumBody.SetActive(false);

                _currentBody = _bubbleGumBody;
            }

            _aliveUntil = _explosionAt + _endAnimationDuration;

            this.transform.position = spawnPosition;
            this.transform.localScale = 2.0f * _areaRadius * Vector3.one;

            UnityGlobal.Sounds.PlayBySoundPrefab(_isTranscendent ? TRANSCENDENT_CREATION_SFX_PATH : NORMAL_CREATION_SFX_PATH, transform.position);
        }

        private static readonly HashSet<Character> v_TempCharacterSet = new HashSet<Character>();
        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            
            if (_nextDotDamageAt <= now)
            {
                _nextDotDamageAt = now + _dotDamagePeriod;
                
                CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, _areaRadius);
                v_TempCharacterSet.Clear();
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _dotDamage, CombatSystem.KnockBackType.Pivot, this.transform.position, knockBackPower: 0f, v_TempCharacterSet, null, null);
                foreach(var hittedCharacter in v_TempCharacterSet)
                {
                    if (!hittedCharacter.IsBoss)
                    {
                        float moveSpeedChangeRatio = hittedCharacter.IsElite? _moveSpeedChangeRatioForElite : _moveSpeedChangeRatio;
                        hittedCharacter.StatusEffects.AddOrUpdateStatusEffect(
                            stage,
                            hittedCharacter,
                            Characters.StatusEffects.StatusEffectType.SlowMove, 
                            SLOW_STATUS_EFFECT_KEY, 
                            _moveSpeedChangeDuration, 
                            now, 
                            moveSpeedChangeRatio);
                    }
                }
            }

            if (_explosionAt <= now)
            {
                _explosionAt = float.MaxValue;
                CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, _areaRadius);
                CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _explosionDamage, CombatSystem.KnockBackType.Pivot, this.transform.position, knockBackPower: 0f, _hittedCharacters, _hittedCharacters, null);

                foreach (var hittedCharacter in _hittedCharacters)
                {
                    if (!hittedCharacter.IsBoss)
                    {
                        float moveSpeedChangeRatio = hittedCharacter.IsElite? _moveSpeedChangeRatioForElite : _moveSpeedChangeRatio;
                        hittedCharacter.StatusEffects.AddOrUpdateStatusEffect(stage, hittedCharacter, Characters.StatusEffects.StatusEffectType.SlowMove, SLOW_STATUS_EFFECT_KEY, _moveSpeedChangeDuration, now, moveSpeedChangeRatio);
                    }
                }

                UnityGlobal.Sounds.PlayBySoundPrefab(BOOM_SFX_PATH, transform.position);

                //초월 오브젝트면 투사체를 생성해준다.
                if (_isTranscendent)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        var projectile = stage.CreateProjectile(
                            _gumPoppingProjectilePaths[Random.Range(0, _gumPoppingProjectilePaths.Count)],
                            _owner.Alliance,
                            _owner,
                            _subProjectileDamage,
                            knockBackPower: 0f,
                            this.transform.position,
                            Quaternion.Euler(0, 0, 60 * i) * Vector2.up,
                            speed: 10f,
                            acceleration: 1f,
                            collidingRadius: 1f,
                            aliveDistance: 50f,
                            hitChances: 99999,
                            splitCount: 0,
                            isRemovableBySpinBladeObject: false,
                            hitCharacterHandler: OnSubProjectileHitOnTarget,
                            hitItemHandler: null,
                            onFinishedHandler: null,
                            string.Empty
                            );
                        projectile.transform.DOShakeScale(50f / 10f, 0.5f);
                    }
                }
                //초월시에는 생성해주지 않는다
                //등급효과로 인해 미니껌이 있으면 생성해준다.
                else if (_miniGumAmount > 0)
                {
                    //CleavageGum 등급효과
                    float randomAngle = Random.Range(0, 360f);
                    for (int i = 0; i < _miniGumAmount; i++)
                    {
                        Vector3 dir = Quaternion.Euler(0, 0, randomAngle + 360f / (float)_miniGumAmount * i) * Vector2.up;
                        Vector2 spawnPos = this.transform.position + dir * _areaRadius;

                        stage.CreateBubbleGumAreaEffectObject(
                            _owner,
                            spawnPos,
                            dotDamage: _dotDamage * 0.5f,
                            dotDamagePeriod: _dotDamagePeriod,
                            explosionDamage: _explosionDamage * 0.5f,
                            subProjectileDamage: _subProjectileDamage * 0.5f,
                            areaRadius: _areaRadius * 0.5f,
                            lifetime: _lifetime * 0.5f,
                            _moveSpeedChangeRatio,
                            _moveSpeedChangeDuration,
                            miniGumAmount: 0,
                            isTranscendent: false);
                    }
                }
            }

        }

        public override void PuttingBackToPool()
        {
            _hittedCharacters.Clear();
            base.PuttingBackToPool();

            if(_isTranscendent)
            {
                _poppingGumSkeletonAnimation.AnimationState.SetEmptyAnimations(0f);
                _poppingGumSkeletonAnimation.AnimationState.ClearTracks();
                _poppingGumSkeletonAnimation.Skeleton.SetToSetupPose();
                _poppingGumSkeletonAnimation.Update(0);
            }
            else
            {
                _bubbleGumSkeletonAnimation.AnimationState.SetEmptyAnimations(0f);
                _bubbleGumSkeletonAnimation.AnimationState.ClearTracks();
                _bubbleGumSkeletonAnimation.Skeleton.SetToSetupPose();
                _bubbleGumSkeletonAnimation.Update(0);
            }
        }

        private bool OnSubProjectileHitOnTarget(Stage stage, Character target, Vector2 hitPosition, ProjectileObject attackerProjectile)
        {
            if (target == null)
            {
                return false;
            }

            bool isHitted = attackerProjectile.TryHitCharacter(stage, target, hitPosition, attackerProjectile);
            if (isHitted)
            {
                if (!target.IsBoss)
                {
                    float moveSpeedChangeRatio = target.IsElite? _moveSpeedChangeRatioForElite : _moveSpeedChangeRatio;
                    target.StatusEffects.AddOrUpdateStatusEffect(stage, target, Characters.StatusEffects.StatusEffectType.SlowMove, SLOW_STATUS_EFFECT_KEY, _moveSpeedChangeDuration, Time.time, moveSpeedChangeRatio);
                }
            }

            return isHitted;
        }
    }

}