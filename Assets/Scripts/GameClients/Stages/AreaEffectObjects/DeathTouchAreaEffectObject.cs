using SamMul.Animations.Placeholder;
using Animation = SamMul.Animations.Placeholder.Animation;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class DeathTouchAreaEffectObject : AreaEffectObjectBase
    {
        //미사일이 공격하고 트레일이 사라지면 제거
        public override bool IsAlive => (!_isAttacked || Time.time <= _isAttackedAt + _explosionDuration);

        private GameObject _crosshairImage;
        private Character _owner;
        private float _damage;
        private float _knockBackPower;
        private float _areaEffectRadius;
        private float _movingTime;
        private bool _isAttacked;
        private float _isAttackedAt;
        private float _explosionDuration;

        private Vector2 _createPosition;
        private Vector2 _randomPosition;
        private Vector2 _destination;
        private float _moveTime;

        private bool _isTranscendent;
        private string _hitSoundPrefabPath;

        private readonly string NormalBoomEffectPath = "Stages/AreaEffects/DeathTouch/FX_DeathTouch_Boom_N.prefab";
        private readonly string TranscendentBoomEffectPath = "Stages/AreaEffects/DeathTouch/FX_DeathTouch_Boom_S.prefab";

        private readonly string NormalTrailEffectPath = "Stages/SkillEffects/DeathTouchProjectile.prefab";
        private readonly string TranscendentTrailEffectPath = "Stages/SkillEffects/DeathTouchProjectile_transcendence.prefab";

        private GameObject _transcendExplosionBody;
        private GameObject _transcendTrail;
        private TrailRenderer _transcendTrailRenderer;
        private SkeletonAnimation _transcendExplosionSkeletonAnimation;
        private Animation _transcendExplosionAnimation;

        private GameObject _normalExplosionBody;
        private GameObject _normalTrail;
        private TrailRenderer _normalTrailRenderer;
        private SkeletonAnimation _normalExplosionSkeletonAnimation;
        private Animation _normalExplosionAnimation;

        //미사일드론 미사일에서 유일하게 재사용하는건 크로스헤어다
        //AllocateSharedResources 에선 크로스헤어 관련된 리소스만 할당하고 그외 미사일과 관련된 리소스는
        //Initialize단계에서 할당한다.
        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.DeathTouch);
            
            string crosshairImageSpinePath = "Stages/SkillEffects/Aiming_SkeletonData.asset";
            _crosshairImage = new GameObject("MissileDroneCrosshairImage");
            _crosshairImage.transform.localScale = new Vector3(1, 1, 1f);
            var crosshairSkeletonAnimation = SpineHelper.LoadSpine(_crosshairImage, crosshairImageSpinePath, sortingLayerName: "LowParticle");
            crosshairSkeletonAnimation.AnimationState.SetAnimation(0, "aiming", true);

            _transcendExplosionBody = ResourcePool.Instance.InstantiateFromResource(TranscendentBoomEffectPath);
            _transcendExplosionBody.transform.SetParent(this.transform);
            _transcendExplosionBody.transform.localPosition = Vector2.zero;
            _transcendExplosionBody.transform.localScale = Vector2.one;
            _transcendExplosionSkeletonAnimation = _transcendExplosionBody.GetComponentInChildren<SkeletonAnimation>();
            _transcendExplosionAnimation = _transcendExplosionSkeletonAnimation.skeleton.Data.FindAnimation("Begin");

            _transcendTrail = ResourcePool.Instance.InstantiateFromResource(TranscendentTrailEffectPath);
            _transcendTrail.transform.SetParent(this.transform);
            _transcendTrail.transform.localPosition = Vector2.zero;
            _transcendTrail.transform.localScale = Vector2.one;
            _transcendTrailRenderer = _transcendTrail.GetComponentInChildren<TrailRenderer>();
            _transcendExplosionSkeletonAnimation.GetComponent<MeshRenderer>().sortingOrder = _transcendTrailRenderer.sortingOrder + 1;

            _normalExplosionBody = ResourcePool.Instance.InstantiateFromResource(NormalBoomEffectPath);
            _normalExplosionBody.transform.SetParent(this.transform);
            _normalExplosionBody.transform.localPosition = Vector2.zero;
            _normalExplosionBody.transform.localScale = Vector2.one;
            _normalExplosionSkeletonAnimation = _normalExplosionBody.GetComponentInChildren<SkeletonAnimation>();
            _normalExplosionAnimation = _normalExplosionSkeletonAnimation.skeleton.Data.FindAnimation("Begin");

            _normalTrail = ResourcePool.Instance.InstantiateFromResource(NormalTrailEffectPath);
            _normalTrail.transform.SetParent(this.transform);
            _normalTrail.transform.localPosition = Vector2.zero;
            _normalTrail.transform.localScale = Vector2.one;
            _normalTrailRenderer = _normalTrail.GetComponentInChildren<TrailRenderer>();
            _normalExplosionSkeletonAnimation.GetComponent<MeshRenderer>().sortingOrder = _normalTrailRenderer.sortingOrder + 1;

        }

        public void Initialize(
                  AllianceType alliance,
                  Character owner,
                  float areaEffectRadius,
                  Vector2 createPosition,
                  Vector2 destination,
                  float movingTime,
                  float damage,
                  float knockBackPower,
                  bool isTranscendent, 
                  string hitSoundPrefabPath)
        {
            base.InitializeAreaObject(alliance);
            _owner = owner;
            _areaEffectRadius = areaEffectRadius;
            _movingTime = movingTime;
            _damage = damage;
            _knockBackPower = knockBackPower;
            _isAttacked = false;

            _createPosition = createPosition; 
            _destination = destination;
            _randomPosition = this.GetMovingPaths(_createPosition, destination);
            this.transform.position = _createPosition;
            _crosshairImage.SetActive(true);
            _crosshairImage.transform.position = destination;
            _moveTime = 0f;
            _isTranscendent = isTranscendent;

            _hitSoundPrefabPath = hitSoundPrefabPath;

            _normalExplosionBody.SetActive(false);
            _transcendExplosionBody.SetActive(false);

            if (isTranscendent)
            {
                _normalTrail.SetActive(false);
                _transcendTrail.SetActive(true);
                _transcendTrailRenderer.Clear();
                _explosionDuration = _transcendExplosionAnimation.Duration;
            }
            else
            {
                _normalTrail.SetActive(true);
                _transcendTrail.SetActive(false);
                _normalTrailRenderer.Clear();
                _explosionDuration = _normalExplosionAnimation.Duration;
            }
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            if (!_isAttacked)
            {
                this.MoveToDestinationAndRotateToMoveDirection(deltaTime);
                var isArrived = CheckArrived();
                if (isArrived)
                {
                    this.AttackToTargetArea(stage);
                    _isAttacked = true;
                    _isAttackedAt = Time.time;
                    _crosshairImage.SetActive(false);
                    this.CreateBoomEffect(stage);
                }
            }
        }
        private void CreateBoomEffect(Stage stage)
        {
            //실제 공격범위에 맞춰 사이즈 조절 
            if (_isTranscendent)
            {
                _normalExplosionBody.SetActive(false);
                _transcendExplosionBody.SetActive(true);
                _transcendExplosionSkeletonAnimation.AnimationState.SetAnimation(0, _transcendExplosionAnimation, false);
                _transcendExplosionBody.transform.right = Random.insideUnitCircle;
                _transcendExplosionBody.transform.localScale = Vector3.one * _areaEffectRadius / 0.6f;
            }
            else
            {
                _normalExplosionBody.SetActive(true);
                _transcendExplosionBody.SetActive(false);
                _normalExplosionSkeletonAnimation.AnimationState.SetAnimation(0, _normalExplosionAnimation, false);
                _normalExplosionBody.transform.right = Random.insideUnitCircle;
                _normalExplosionBody.transform.localScale = Vector3.one * _areaEffectRadius / 0.8f;
            }
        }

        private void MoveToDestinationAndRotateToMoveDirection(float deltaTime)
        {
            _moveTime += deltaTime;
            float t = _moveTime / _movingTime;
            Vector2 currentPosition = this.CalculateBezierPoint(t, _createPosition, _randomPosition, _destination);
            Vector2 prevPosition = this.transform.position;

            Vector2 direction = currentPosition - prevPosition;
            direction.Normalize();

            this.transform.position = currentPosition;
        }

        private bool CheckArrived()
        {
            return _moveTime / _movingTime >=  1f;
        }

        private void AttackToTargetArea(Stage stage)
        {
            var targetArea = new CircularTargetArea(this.transform.position, _areaEffectRadius);
            CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, targetArea.Center, _knockBackPower, null, null, _hitSoundPrefabPath);
        }

        //미사일 움직임에 랜덤성을 주기위한 중간지점 경로 제작 코드
        private Vector2 GetMovingPaths(Vector2 createPosition, Vector2 destination)
        {
            float distance = Vector2.Distance(createPosition, destination);
            Vector2 center = (createPosition + destination) * 0.5f;
            Vector2 randomPosition = center + Random.insideUnitCircle * distance;

            return randomPosition;
        }

        // 다음 재사용시 어느 어떤 색상의 미사일을 호출할지 알수없다.
        // 사용했던 미사일, 폭발 리소스는 ResourcePool에 반환한다.
        // 크로스헤어는 다시 재사용하기때문에 반환하지 않는다.
        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();

            _normalExplosionSkeletonAnimation.AnimationState.ClearTracks();
            _normalExplosionSkeletonAnimation.skeleton.SetToSetupPose();
            _normalExplosionSkeletonAnimation.Update(0);

            _transcendExplosionSkeletonAnimation.AnimationState.ClearTracks();
            _transcendExplosionSkeletonAnimation.skeleton.SetToSetupPose();
            _transcendExplosionSkeletonAnimation.Update(0);
        }

        private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            if(t > 1)
            {
                t = 1f;
            }
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            Vector3 p = uu * p0;
            p += 2 * u * t * p1;
            p += tt * p2;

            return p;
        }

    }
}
