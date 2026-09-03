using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.PCs;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class RuneTrapTranscendentAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => _isAlive;

        private static readonly float DETECT_RANGE_RATE = 0.6f; // 폭발 범위의 비례해서 적을 탐색함.
        private static readonly float BOOM_DELAY = 0.5f;
        private static readonly string BOOM_SFX_PATH = "Sounds/SoundEffects/PCs/RuneTrapTranscendentBoom_SFX.prefab";

        private PlayerCharacter _owner;

        private float _damage;
        private float _boomRadius; // 범위 증가 효과 처리는 TrapBoomSkill에서 처리해 받습니다.
        private float _deadAt;
        private float _knockBackPower;

        private float _appearDuration;
        private float _serchStartTimeAt;
        private float _boomAt;
        private float _triggerAt;
        private bool _boomTrigger;
        private SpriteAnimationHandler _mineBodyAnimation;
        private SpriteAnimationHandler _boomAnimationHandler;
        private HashSet<Character> _hittedCharacters = new HashSet<Character>();

        private RuneTrapTimer _runeTrapTimer;
        List<Character> _charactersInArea = new List<Character>();
        private bool _isAlive;

        private string _hitSoundPrefabPath;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.RuneTrap);

            _mineBodyAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>("Stages/AreaEffects/TrapBomb/RuneTrap.prefab");
            _mineBodyAnimation.InitializeOnly();
            _mineBodyAnimation.transform.SetParent(this.transform);
            _mineBodyAnimation.gameObject.SetActive(false);

            _boomAnimationHandler = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>("Stages/AreaEffects/TrapBomb/RuneTrapBoom.prefab");
            _boomAnimationHandler.InitializeOnly();
            _boomAnimationHandler.transform.SetParent(this.transform);
            _boomAnimationHandler.gameObject.SetActive(false);

            _runeTrapTimer = ResourcePool.Instance.InstantiateFromResource<RuneTrapTimer>("Stages/AreaEffects/TrapBomb/RuneTrapTimer.prefab");
            _runeTrapTimer.transform.SetParent(this.transform);
            _runeTrapTimer.transform.localScale = Vector3.one;
            _runeTrapTimer.transform.localPosition = Vector3.zero;

            _isAlive = false;

        }

        public void Initialize(PlayerCharacter owner, Vector2 position, float damage, float radius, float lifeTime, float knockBackPower, string hitSoundPrefabPath)
        {
            float now = Time.time;

            transform.localPosition = position;
            _owner = owner;
            _damage = damage;
            _boomRadius = radius;
            _deadAt = lifeTime + now;
            _knockBackPower = knockBackPower;

            _appearDuration = 0.9f;
            _serchStartTimeAt = now + _appearDuration;
            _boomAnimationHandler.gameObject.SetActive(false);
            _isAlive = true;

            _mineBodyAnimation.gameObject.SetActive(true);
            _mineBodyAnimation.transform.localScale = Vector3.one * radius * DETECT_RANGE_RATE * 0.83333f;
            _mineBodyAnimation.InitializeAndPlay();

            _boomAnimationHandler.transform.localScale = Vector3.one * (radius * 0.4f);
            _boomAt = 0.0f;
            _boomTrigger = false;
            _runeTrapTimer.gameObject.SetActive(true);
            _runeTrapTimer.FillAmount(0);
            _runeTrapTimer.transform.localScale = Vector3.one * radius;

            _hitSoundPrefabPath = hitSoundPrefabPath;
            _hittedCharacters.Clear();
            _charactersInArea.Clear();
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if (now < _serchStartTimeAt)
            {
                return;
            }

            if (!_boomTrigger)
            {
                CircularTargetArea area = new CircularTargetArea(this.transform.position, _boomRadius * DETECT_RANGE_RATE);
                stage.FindAliveCharactersInArea(_owner.Alliance.ToEnemyAlliance(), area, _charactersInArea);
                if (_charactersInArea.Count > 0 || now > _deadAt)
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
                    _mineBodyAnimation.gameObject.SetActive(false);
                    _runeTrapTimer.gameObject.SetActive(false);
                    AreaAttack(stage);
                    _boomAt = float.MaxValue;

                    UnityGlobal.Sounds.PlayBySoundPrefab(BOOM_SFX_PATH, transform.position);
                }
                float fillAmount = (now - _triggerAt) / BOOM_DELAY;
                _runeTrapTimer.FillAmount(fillAmount);
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _owner = null;
            _boomAnimationHandler.gameObject.SetActive(false);
            _mineBodyAnimation.gameObject.SetActive(false);
        }

        private void AreaAttack(Stage stage)
        {
            CircularTargetArea attackCircularArea = new CircularTargetArea(this.transform.position, _boomRadius * 0.75f);
            CombatSystem.HitOnTargetArea(
                stage, attackCircularArea, _owner, _damage, 
                CombatSystem.KnockBackType.Pivot, attackCircularArea.Center, _knockBackPower,
                _hittedCharacters, _hittedCharacters,
                hitSoundPrefabPath: string.Empty);
            UnityGlobal.Sounds.PlayBySoundPrefab(_hitSoundPrefabPath, this.transform.position);

            SquareTargetArea attackRectArea;
            bool isLayOnSide = _owner.IsRuneTrapBombLayOnSide;
            if (isLayOnSide)
            {
                attackRectArea = new SquareTargetArea(this.transform.position, new Vector3(_boomRadius * 3.00f, _boomRadius * 0.75f), 0.0f);
                _boomAnimationHandler.transform.localRotation = Quaternion.AngleAxis(90.0f, Vector3.back);
            }
            else
            {
                attackRectArea = new SquareTargetArea(this.transform.position, new Vector3(_boomRadius * 0.75f, _boomRadius * 3.0f), 0.0f);
                _boomAnimationHandler.transform.localRotation = Quaternion.AngleAxis(0.0f, Vector3.back);
            }
            _owner.IsRuneTrapBombLayOnSide ^= true;

            CombatSystem.HitOnTargetArea(stage, attackRectArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, attackRectArea.Center, _knockBackPower, _hittedCharacters, _hittedCharacters, _hitSoundPrefabPath);


            _boomAnimationHandler.gameObject.SetActive(true);
            _boomAnimationHandler.InitializeAndPlay(null, () =>
            {
                _isAlive = false;
            });

        }
    }
}
