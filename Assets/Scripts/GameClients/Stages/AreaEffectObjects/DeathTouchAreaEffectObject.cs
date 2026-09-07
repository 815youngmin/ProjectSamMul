using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    /// <summary>
    /// 데스터치 미사일. 전용 리소스 없이 미사일은 공용 공격 비주얼로, 낙하 지점과 폭발 범위는 AttackAreaFlashManager 로 표시한다.
    /// </summary>
    public class DeathTouchAreaEffectObject : AreaEffectObjectBase
    {
        //미사일이 공격하고 폭발 표시 시간이 지나면 제거
        public override bool IsAlive => (!_isAttacked || Time.time <= _isAttackedAt + EXPLOSION_DURATION);

        private const float MISSILE_DIAMETER = 0.5f;
        private const float EXPLOSION_DURATION = 0.5f;

        private GameObject _visual;
        private Character _owner;
        private float _damage;
        private float _knockBackPower;
        private float _areaEffectRadius;
        private float _movingTime;
        private bool _isAttacked;
        private float _isAttackedAt;

        private Vector2 _createPosition;
        private Vector2 _randomPosition;
        private Vector2 _destination;
        private float _moveTime;

        private bool _isTranscendent;
        private string _hitSoundPrefabPath;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.DeathTouch);

            _visual = PlayerAttackVisual.Attach(transform, MISSILE_DIAMETER);
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
            _moveTime = 0f;
            _isTranscendent = isTranscendent;

            _hitSoundPrefabPath = hitSoundPrefabPath;

            _visual.SetActive(true);
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            if (!_isAttacked)
            {
                this.MoveToDestinationAndRotateToMoveDirection(deltaTime);
                // 크로스헤어 대신 낙하 지점의 폭발 범위를 계속 표시한다.
                stage.AttackAreaFlashes.Show(new CircularTargetArea(_destination, _areaEffectRadius));

                var isArrived = CheckArrived();
                if (isArrived)
                {
                    this.AttackToTargetArea(stage);
                    _isAttacked = true;
                    _isAttackedAt = Time.time;
                    _visual.SetActive(false);
                }
            }
        }

        private void MoveToDestinationAndRotateToMoveDirection(float deltaTime)
        {
            _moveTime += deltaTime;
            float t = _moveTime / _movingTime;
            Vector2 currentPosition = this.CalculateBezierPoint(t, _createPosition, _randomPosition, _destination);

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

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
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
