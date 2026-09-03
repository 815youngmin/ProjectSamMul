using Z.Animations.Placeholder;
using Animation = Z.Animations.Placeholder.Animation;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.GameClients.Stages.ProjectileObjects;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class SpinBladeObject : AreaEffectObjectBase
    {
        public override bool IsAlive => true;

        private GameObject _bodyImage;
        private GameObject _normalSkillImage;
        private GameObject _transcendSkillImage;

        private SkeletonAnimation _skeletonAnimation;
        private Animation _idleAnimation;

        private Character _owner;

        private float _objectRadius;
        private float _movingRadius;
        private float _damage;
        private float _knockBackPower;
        private float _attackPeriod;

        private float _currentAngleDegree;
        private float _angleSpeedDegree;

        private HashSet<Character> _hittedCharactersInAttackPeriod;
        private float _lastClearedHittedCharactersAt;

        private readonly static string SPIN_BLADE_SPINE_RESOURCE_PATH = "Stages/AreaEffects/SpinBlade/guadian_SkeletonData.asset"; 
        private readonly static string SPIN_BLADE_TRANSCEND_PREFAB_RESOURCE_PATH = "Stages/AreaEffects/SpinBlade/SpinBlade_S_Object.prefab"; 

        private bool _isTranscend;
        private string _hitSoundPrefabPath;

        //_objectRadius: 오브젝트의 반지름 범위, 공격범위로도 사용, 초기화에서 주입
        //_movingRadius: 플레이어와의 거리, 초기화에서 주입
        //_baseDamage: 공격대미지, 초기화에서 주입
        //_attackPeriod: 몬스터별 공격 주기

        // duration동안 살아있고, 
        // attack period 마다 현재 위치에서 radius 범위에 있는 몬스터에게 damage만큼 타격을 입히고, (몬스터 개별, attack period 가 지난 뒤 충돌하면 다시 대미지)
        // owner 주변으로 원을 그리며 이동, 매 프레임 angleSpeed 만큼 이동

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.SpinBlade);
            _hittedCharactersInAttackPeriod = new HashSet<Character>();

            string bodyImagePrefabPath = "Stages/AreaEffects/SpinBlade/GuardianSupportObject.prefab";
            _bodyImage = ResourcePool.Instance.InstantiateFromResource(bodyImagePrefabPath);
            _bodyImage.transform.SetParent(this.gameObject.transform);

            _normalSkillImage = new GameObject("normalSkillImage");
            _normalSkillImage.transform.SetParent(_bodyImage.transform);
            _skeletonAnimation = SpineHelper.LoadSpine(_normalSkillImage, SPIN_BLADE_SPINE_RESOURCE_PATH);
            _normalSkillImage.GetComponent<MeshRenderer>().sortingLayerID = SortingLayer.NameToID("HighParticle");
            _idleAnimation = _skeletonAnimation.skeleton.Data.FindAnimation("skill");

            _transcendSkillImage = ResourcePool.Instance.InstantiateFromResource(SPIN_BLADE_TRANSCEND_PREFAB_RESOURCE_PATH);
            _transcendSkillImage.transform.SetParent(_bodyImage.transform);
            _transcendSkillImage.GetComponent<MeshRenderer>().sortingLayerID = SortingLayer.NameToID("HighParticle");
            _transcendSkillImage.SetActive(false);

            _isTranscend = false;

        }

        public void Initialize(
            AllianceType alliance,
            Character owner,
            float objectRadius,
            float movingRadius,
            float damage,
            float knockBackPower,
            float attackPeriod,
            float angleSpeedDegree,
            float startAngleDegree, //여러개 생성할때 생성 위치 맞추기 위해 초기화 과정에서 시작 Degree값을 입력받는다.
            string hitSoundPrefabPath)
        {
            base.InitializeAreaObject(alliance);
            _owner = owner;
            _objectRadius = objectRadius;
            _movingRadius = movingRadius;
            _damage = damage;
            _knockBackPower = knockBackPower;
            _attackPeriod = attackPeriod;

            _lastClearedHittedCharactersAt = 0f;

            _angleSpeedDegree = angleSpeedDegree;
            _currentAngleDegree = startAngleDegree;

            _hitSoundPrefabPath = hitSoundPrefabPath;

            this.MoveToCurrentPosition();
            Debug.Assert(null != _idleAnimation);
            _skeletonAnimation.AnimationState.SetAnimation(0, _idleAnimation, true);
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();

            _owner = null;
        }

        private void MoveToCurrentPosition()
        {
            var pivot = (Vector2)_owner.transform.position;

            float radian = _currentAngleDegree * Mathf.Deg2Rad;
            var offset = new Vector2(_movingRadius * Mathf.Cos(radian), _movingRadius * Mathf.Sin(radian));
            var nextPosition = pivot + offset;

            this.transform.position = nextPosition;
        }

        private void RemoveToCollidingProjectile(Stage stage)
        {
            List<ProjectileObject> projectileObjects = new List<ProjectileObject>();
            stage.FindAliveProjectilesInArea(_owner.Alliance.ToEnemyAlliance(), new CircularTargetArea(this.transform.position, _objectRadius), projectileObjects);

            foreach (var projectile in projectileObjects)
            {
                if (projectile.IsRemovableBySpinBladeObject)
                {
                   stage.ReserveToRemoveProjectile(projectile);
                }
            }
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float deltaDegree = deltaTime * _angleSpeedDegree;
            _currentAngleDegree += deltaDegree;
            if (_currentAngleDegree > 360f)
            {
                _currentAngleDegree -= 360f;
            }

            this.MoveToCurrentPosition();
            this.RemoveToCollidingProjectile(stage);

            if (_lastClearedHittedCharactersAt + _attackPeriod <= Time.time)
            {
                _hittedCharactersInAttackPeriod.Clear();
                _lastClearedHittedCharactersAt = Time.time;
            }

            var targetArea = new CircularTargetArea(this.transform.position, _objectRadius);
            CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, _owner.transform.position, _knockBackPower, _hittedCharactersInAttackPeriod, _hittedCharactersInAttackPeriod, _hitSoundPrefabPath);
        }

        public void ReApplyStats(float damage, float knockbackPower, float objectRadius, float movingRadius, float angleSpeedDegree)
        {
            _damage = damage;
            _knockBackPower = knockbackPower;
            _objectRadius = objectRadius;
            _movingRadius = movingRadius;
            _angleSpeedDegree = angleSpeedDegree;
        }

        public void SetAngleMoveSpeed(float speed) 
        {
            _angleSpeedDegree = speed;
        }

        public void SetTranscendFlag(bool isTranscend)
        {
            if(_isTranscend == isTranscend)
            {
                return;
            }

            _transcendSkillImage.SetActive(isTranscend);
            _normalSkillImage.SetActive(!isTranscend);

            GameObject toImageObject = (isTranscend ? _transcendSkillImage : _normalSkillImage);
            _skeletonAnimation = toImageObject.GetComponent<SkeletonAnimation>();
            _idleAnimation = _skeletonAnimation.skeleton.Data.FindAnimation("skill");
            _skeletonAnimation.AnimationState.SetAnimation(0, _idleAnimation, true);
        }
    }
}
