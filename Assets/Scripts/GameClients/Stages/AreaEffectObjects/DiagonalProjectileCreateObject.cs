using System.Collections.Generic;
using System.IO.Pipes;
using UnityEngine;
using Z.GameClients.Stages.Characters;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class DiagonalProjectileCreateObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _fireDuration;

        private int _fireCount;         //총 발사 개수
        private float _fireDuration;    //발사 시간
        private float _horizontalWidth; //발사 범위 
        private int _fireLineCount;     //발사 범위를 몇개로 쪼개서 발사할지 

        //Projectile Parameter
        private string _bodyProjectileResourcePath;
        private float _baseDamage; 
        private float _knockBackPower;
        private float _speed;
        private float _acceleration;
        private float _collidingRadius;
        private float _aliveDistance;
        private int _hitChances;
        private int _splitCount;
        private string _hitSoundPrefabPath;
        private bool _isRemovableBySpinBladeObject;

        private Character _owner;
        private Vector2 _centerPos;
        private float _createdAt;
        private List<float> _attackAts;
        private List<float> _offsets;
        private List<Vector2> _firePositions;

        private int _currentAttackCount;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.DiagonalProjectileCreateObject);
        }

        public void Initialize(
            Character owner,
            Stage stage,
            int fireCount,
            float fireDuration,
            float horizontalWidth,          
            int fireLineCount,
            string bodyProjectileResourcePath,
            float baseDamage,
            float knockBackPower,
            float speed,
            float acceleration,
            float collidingRadius,
            float aliveDistance,
            int hitChances,
            int splitCount,
            bool isRemovableBySpinBladeObject,
            string hitSoundPrefabPath
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _createdAt = now;

            _owner = owner;
            _fireCount = fireCount;
            _fireDuration = fireDuration;
            _bodyProjectileResourcePath = bodyProjectileResourcePath;
            _horizontalWidth = horizontalWidth;
            _fireLineCount = fireLineCount;
            _baseDamage = baseDamage;
            _knockBackPower = knockBackPower;
            _speed = speed;
            _acceleration = acceleration;
            _collidingRadius = collidingRadius;
            _aliveDistance = aliveDistance;
            _hitChances = hitChances;
            _splitCount = splitCount;
            _hitSoundPrefabPath = hitSoundPrefabPath;
            _isRemovableBySpinBladeObject = isRemovableBySpinBladeObject;
            _currentAttackCount = 0;

            _attackAts = new List<float>();
            _offsets = new List<float>();
            for (int i = -(_fireLineCount - 1); i <= _fireLineCount - 1; i += 2)
            {
                float offset = (float)i * _horizontalWidth / _fireLineCount;
                _offsets.Add(offset);
            }
            _firePositions = new List<Vector2>();
            _centerPos = _owner.CenterPos;

            this.GetProjectileFireData(stage, now);
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            for (int i = _currentAttackCount; i < _attackAts.Count; i++)
            {
                if (_attackAts[i] <= now)
                {
                    _currentAttackCount++;
                    stage.CreateProjectile(
                        _bodyProjectileResourcePath,
                        _owner.Alliance,
                        _owner,
                        _baseDamage,
                        _knockBackPower,
                        _firePositions[i],
                        new Vector2(1f, 1f).normalized,
                        _speed,
                        _acceleration,
                        _collidingRadius,
                        _aliveDistance,
                        _hitChances,
                        _splitCount,
                        _isRemovableBySpinBladeObject,
                        _hitSoundPrefabPath
                        );
                }
                else
                {
                    return;
                }
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

        //투사체 발사 데이터 생성 인터페이스
        //생성 위치, 발사 시간 
        private void GetProjectileFireData(Stage stage, float now)
        {
            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();

            Vector2 createStandardDir = new Vector2(1f, -1f).normalized;//총알 생성 기준 방향
            Vector2 leftBottomDir = new Vector2(-1, -1f).normalized;

            Vector2 rectLeftTop = new Vector2(rect.xMin, rect.yMax);
            Vector2 rectLeftBottom = new Vector2(rect.xMin, rect.yMin);
            Vector2 rectRightBottom = new Vector2(rect.xMax, rect.yMin);

            float attackPeriod = _fireDuration / _fireCount;
            for (int i = 0; i < _fireCount; i++)
            {
                Vector2 createPosition = _centerPos + createStandardDir * _offsets[Random.Range(0, _offsets.Count - 1)]; //owner 기준으로 랜덤 수평 범위 좌표 지정
                if (this.FindIntersection(rectLeftTop, rectLeftBottom, createPosition, leftBottomDir, out var intersection1))
                {
                    //사각형 세로 변 에 교차점 있음 총알 발사
                    _firePositions.Add(intersection1);
                    _attackAts.Add(now + attackPeriod * i);
                }
                else if (this.FindIntersection(rectLeftBottom, rectRightBottom, createPosition, leftBottomDir, out var intersection2))
                {
                    //사각형 가로변에 교차점 있음 총알 발사
                    _firePositions.Add(intersection2);
                    _attackAts.Add(now + attackPeriod * i);
                }
                else
                {
                    //교차 위치가 없음 
                }
            }
        }


        /// <summary>
        /// 선위에 겹치는 위치를 찾기위한 인터페이스
        /// </summary>
        /// <param name="p1">사각형의 꼭지점1</param>
        /// <param name="p2">사각형의 꼭지점2</param>
        /// <param name="b">점 위치</param>
        /// <param name="dir">점의 방향</param>
        /// <param name="intersection">결과값</param>
        /// <returns>반환되면 true 없으면 false</returns>
        private bool FindIntersection(Vector2 p1, Vector2 p2, Vector2 b, Vector2 dir, out Vector2 intersection)
        {
            intersection = Vector2.zero;

            //사각형의 변 벡터
            Vector2 aDir = p2 - p1;

            // 행렬식 계산
            float denominator = aDir.x * dir.y - aDir.y * dir.x;

            // 평행 여부 확인
            if (Mathf.Abs(denominator) < Mathf.Epsilon)
                return false;

            // 파라미터 t와 u 계산
            float t = ((b.x - p1.x) * dir.y - (b.y - p1.y) * dir.x) / denominator;
            float u = ((b.x - p1.x) * aDir.y - (b.y - p1.y) * aDir.x) / denominator;

            // u >= 0일 때만 교차로 인정 (점 B에서 진행 방향으로만)
            if (u < 0)
                return false;

            //사각형의 변 길이를 넘어가서 교차되면 않된다
            if (t < 0 || t > 1)
                return false;

            // 교차점 계산
            intersection = p1 + t * aDir;
            return true;
        }

    }

}
