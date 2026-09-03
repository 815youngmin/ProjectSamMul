using DG.Tweening;
using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class SusanooBeadObject : SmartAreaEffectObjectBase
    {
        private readonly string BeadPath = "Stages/Projectiles/Susanoo_w.prefab";
        private readonly string LightningPath = "Stages/AreaEffects/ZeusLightning/Zeus_Lightning.prefab";
        private readonly float BeadRadius = 0.55f;

        public override bool IsAlive => !_isHit && Time.time <= _createdAt + _lifeTime;

        private Stage _stage;
        private Monster _owner;
        private float _damage; 
        private float _attackRadius;

        private float _createdAt;
        private float _lifeTime;
        private bool _isHit;
        private float _lightningAt;


        private GameObject _beadBody;
        private GameObject _lightningBody;
        
        private SpriteAnimationHandler _lightningAnimationHandler;
        private Sequence _beadScaleSequence;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForSmartBase(AreaEffectType.SusanooBeadObject);
            
            _beadBody = ResourcePool.Instance.InstantiateFromResource(BeadPath);
            _beadBody.transform.SetParent(this.transform);
            _beadBody.transform.localPosition = Vector2.zero;
            _beadBody.transform.localScale = Vector2.one;
          
            _lightningBody = ResourcePool.Instance.InstantiateFromResource(LightningPath);
            _lightningBody.transform.SetParent(this.transform);
            _lightningBody.transform.localPosition = Vector2.zero;
            _lightningBody.transform.localScale = Vector2.one;
            _lightningAnimationHandler = _lightningBody.GetComponent<SpriteAnimationHandler>();
            _lightningAnimationHandler.InitializeOnly();
        }

        public void Initialize(
            Stage stage,
            Monster owner,
            Vector2 startPosition,
            Vector2 endPosition,
            float waitTime,
            float attackRadius,
            float damage
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _stage = stage;
            _owner = owner;
            _createdAt = now;
            _attackRadius = attackRadius;
            _damage = damage;

            _lifeTime = waitTime + _lightningAnimationHandler.AnimationDuration;
            _lightningAt = now + waitTime;

            Vector2 beadObjectScale = Vector3.one * attackRadius / BeadRadius * 0.35f;
            _beadScaleSequence = DOTween.Sequence();
            _beadScaleSequence.Append(_beadBody.transform.DOScale(beadObjectScale * 1.2f, 0.1f).From(beadObjectScale));
            _beadScaleSequence.Append(_beadBody.transform.DOScale(beadObjectScale, 0.1f));
            _beadScaleSequence.AppendInterval(0.1f);
            _beadScaleSequence.Append(_beadBody.transform.DOScale(beadObjectScale * 1.2f, 0.1f));
            _beadScaleSequence.Append(_beadBody.transform.DOScale(beadObjectScale, 0.1f));
            _beadScaleSequence.AppendInterval(0.1f);
            _beadScaleSequence.SetLoops(-1);
            _beadScaleSequence.Restart();
            _beadBody.SetActive(true);

            stage.CreateCircularAttackRangeIndicator(endPosition, attackRadius, waitTime);

            _lightningBody.transform.localScale = Vector3.one * (attackRadius / 1.5f);
            _lightningBody.SetActive(false);

            this.transform.position = startPosition;
            DOTween.Sequence(this.transform.DOJump(endPosition, jumpPower: 5f, 1, 0.3f).SetEase(Ease.Linear));


        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            base.UpdateLogic(stage, deltaTime);

            float now = Time.time;
            if(_lightningAt <= now)
            {
                _beadBody.SetActive(false);
                _lightningBody.SetActive(true);
                _lightningAnimationHandler.InitializeAndPlay(Hit);
                _lightningAt = float.MaxValue;
            }
        }
        private void Hit()
        {
            CircularTargetArea targetArea = new CircularTargetArea(this.transform.position, _attackRadius);
            CombatSystem.HitOnTargetArea(
                _stage, targetArea, _owner, _damage,
                CombatSystem.KnockBackType.Pivot, Vector2.zero, 0f, null, null,
                hitSoundPrefabPath: string.Empty);
        }

        public override void PuttingBackToPool()
        {
            _beadBody.SetActive(false);
            _beadBody.transform.localScale = Vector3.one;

            _lightningBody.SetActive(false);
            _lightningBody.transform.localScale = Vector3.one;

            _beadScaleSequence.Pause();
            DOTween.Kill(_beadScaleSequence);
            _beadScaleSequence = null;

            base.PuttingBackToPool();
        }

    }

}
