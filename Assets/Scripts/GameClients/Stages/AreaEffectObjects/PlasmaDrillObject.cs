using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class PlasmaDrillObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private GameObject _normalSkill;
        private GameObject _transcendSkill;
        private SpriteAnimationHandler _normalSkillImage;
        private SpriteAnimationHandler _transcendSkillImage;
        private SpriteAnimationHandler _targetSkillImage;

        private TrailRenderer _normalTrailRenderer;
        private TrailRenderer _transcendTrailRenderer;

        private Character _owner;
        private float _objectRadius;
        private Vector2 _movingDirection;
        private float _movingSpeed;
        private float _damage;
        private float _knockBackPower;
        private float _attackPeriod;
        private float _lifeTime;
        private float _createdAt;

        private HashSet<Character> _hittedCharactersInAttackPeriod;
        private float _lastClearedHittedCharactersAt;

        private bool _isTranscend;

        private string _hitSoundPrefabPath;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.PlasmaDrill);
            _hittedCharactersInAttackPeriod = new HashSet<Character>();

            string normalPrefabPath = "Stages/AreaEffects/PlasmaDrill/Plasma_Drill.prefab";
            string transcendPrefabPath = "Stages/AreaEffects/PlasmaDrill/Plasma_Drill_S.prefab";

            _normalSkill = ResourcePool.Instance.InstantiateFromResource(normalPrefabPath);
            _normalSkillImage = _normalSkill.GetComponentInChildren<SpriteAnimationHandler>();
            _normalSkillImage.InitializeOnly();
            _normalTrailRenderer = _normalSkill.GetComponentInChildren<TrailRenderer>();
            _normalSkill.transform.SetParent(transform);
            _normalSkill.transform.localPosition = Vector3.zero;
            _normalSkill.transform.localScale = Vector3.one;

            _transcendSkill = ResourcePool.Instance.InstantiateFromResource(transcendPrefabPath); ;
            _transcendSkillImage = _transcendSkill.GetComponentInChildren<SpriteAnimationHandler>();
            _transcendSkillImage.InitializeOnly();
            _transcendTrailRenderer = _transcendSkill.GetComponentInChildren<TrailRenderer>();
            _transcendSkill.transform.SetParent(transform);
            _transcendSkill.transform.localPosition = Vector3.zero;
            _transcendSkill.transform.localScale = Vector3.one;

        }

        public void Initialize(
            AllianceType alliance,
            Character owner,
            float objectRadius,
            Vector2 movingDirection,
            float movingSpeed,
            float damage,
            float knockBackPower,
            float attackPeriod,
            float lifeTime,
            bool isTranscend,
            string hitSoundPrefabPath)
        {
            base.InitializeAreaObject(alliance);
            _owner = owner;
            _objectRadius = objectRadius;
            _movingDirection = movingDirection.normalized;
            _movingSpeed = movingSpeed;
            _damage = damage;
            _knockBackPower = knockBackPower;
            _attackPeriod = attackPeriod;
            _lifeTime = lifeTime;
            _createdAt = Time.time;
            this.transform.position = _owner.CenterPos;
            _isTranscend = isTranscend;

            if (isTranscend)
            {
                _transcendSkill.SetActive(true);
                _normalSkill.SetActive(false);
                _targetSkillImage = _transcendSkillImage;
            }
            else
            {
                _transcendSkill.SetActive(false);
                _normalSkill.SetActive(true);
                _targetSkillImage = _normalSkillImage;
            }

            _normalTrailRenderer.Clear();
            _transcendTrailRenderer.Clear();
            _targetSkillImage.InitializeAndPlay();
            _hitSoundPrefabPath = hitSoundPrefabPath;
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            Vector2 nextPosition = this.MoveToCurrentPosition(deltaTime);
            if (_isTranscend)
            {
                this.RotateMovingDirectionToTargetDirection(stage, nextPosition);
            }
            else
            {
                this.ReflectionToCameraSize(nextPosition);
            }
            this.RotateBodyImageToMoveDirection();

            if (_lastClearedHittedCharactersAt + _attackPeriod <= Time.time)
            {
                _hittedCharactersInAttackPeriod.Clear();
                _lastClearedHittedCharactersAt = Time.time;
            }

            var targetArea = new CircularTargetArea(this.transform.position, _objectRadius);
            CombatSystem.HitOnTargetArea(
                stage, targetArea, _owner, _damage,
                CombatSystem.KnockBackType.Pivot, targetArea.Center, _knockBackPower,
                _hittedCharactersInAttackPeriod, _hittedCharactersInAttackPeriod, _hitSoundPrefabPath);
        }

        private Vector2 MoveToCurrentPosition(float deltaTime)
        {
            Vector2 currentPosition = this.transform.position;
            Vector2 nextPosition = currentPosition + _movingDirection * _movingSpeed * deltaTime;
            return nextPosition;
        }

        private void ReflectionToCameraSize(Vector2 nextMovePosition)
        {
            var worldRect = GameClient.CameraController.GetWorldRectInCamera(0.0f, 0.0f, 0.0f, 500.0f);
            Vector2 movePosition = nextMovePosition;
            Vector2 reflectedVector = Vector2.zero;

            if (movePosition.x < worldRect.xMin)
            {
                //왼쪽벽 반사 
                reflectedVector.x = 1.0f;
                //벽 안쪽으로 이동
                movePosition.x = worldRect.xMin;
                //반사각 계산
                if (Vector2.Dot(Vector2.right, _movingDirection) < 0)
                {
                    _movingDirection = Vector2.Reflect(_movingDirection, Vector2.right);
                }
                else
                {
                    _movingDirection = Vector2.Lerp(_owner.MoveDir, _movingDirection, 0.5f);
                }
            }
            else if (movePosition.x > worldRect.xMax)
            {
                //오른쪽벽 반사
                reflectedVector.x = -1.0f;
                //벽 안쪽으로 이동
                movePosition.x = worldRect.xMax;
                //반사각 계산
                if (Vector2.Dot(Vector2.left, _movingDirection) < 0)
                {
                    _movingDirection = Vector2.Reflect(_movingDirection, Vector2.left);
                }
                else
                {
                    _movingDirection = Vector2.Lerp(_owner.MoveDir, _movingDirection, 0.5f);
                }

            }
            else if (movePosition.y < worldRect.yMin)
            {
                //아래 벽 반사
                reflectedVector.y = 1.0f;
                //벽 안쪽으로 이동
                movePosition.y = worldRect.yMin;
                //반사각 계산
                if (Vector2.Dot(Vector2.up, _movingDirection) < 0)
                {
                    _movingDirection = Vector2.Reflect(_movingDirection, Vector2.up);
                }
                else
                {
                    _movingDirection = Vector2.Lerp(_owner.MoveDir, _movingDirection, 0.2f);
                }
            }
            else if (movePosition.y > worldRect.yMax)
            {
                //위쪽 벽 반사
                reflectedVector.y = -1.0f;
                //벽 안쪽으로 이동
                movePosition.y = worldRect.yMax;
                //반사각 계산
                if (Vector2.Dot(Vector2.down, _movingDirection) < 0)
                {
                    _movingDirection = Vector2.Reflect(_movingDirection, Vector2.down);
                }
                else
                {
                    _movingDirection = Vector2.Lerp(_owner.MoveDir, _movingDirection, 0.5f);
                }
            }
            _movingDirection.Normalize();
            this.transform.position = movePosition;
        }

        private void RotateMovingDirectionToTargetDirection(Stage stage, Vector2 nextMovePosition)
        {
            var worldRect = GameClient.CameraController.GetWorldRectInCamera(0.0f, 0.0f, 0f, 500.0f);
            Vector2 movePosition = nextMovePosition;

            Character target = null;
            Vector2 targetDirection = Vector2.right;
            List<Character> enemies = new List<Character>();
            stage.FindAliveCharactersInArea(_owner.Alliance.ToEnemyAlliance(), new SquareTargetArea(worldRect.center, 0.95f * worldRect.size, 0f), enemies);
            if (enemies.Count != 0)
            {
                target = enemies[Random.Range(0, enemies.Count)];
                Vector2 targetVector = target.transform.position;
                targetDirection = targetVector - nextMovePosition;
                targetDirection.Normalize();
            }
            else
            {
                this.ReflectionToCameraSize(nextMovePosition);
                return;
            }

            Vector2 reflectedVector = Vector2.zero;
            if (movePosition.x < worldRect.xMin)
            {
                reflectedVector.x = 1.0f;
                //왼쪽벽 반사
                movePosition.x = worldRect.xMin;
                if (Vector2.Dot(Vector2.right, _movingDirection) < 0)
                {
                    _movingDirection = targetDirection;
                }
                else
                {
                    _movingDirection = Vector2.Lerp(_owner.MoveDir, _movingDirection, 0.5f);
                }
            }
            else if (movePosition.x > worldRect.xMax)
            {
                reflectedVector.x = -1.0f;
                //오른쪽벽 반사
                movePosition.x = worldRect.xMax;
                if (Vector2.Dot(Vector2.left, _movingDirection) < 0)
                {
                    _movingDirection = targetDirection;
                }
                else
                {
                    _movingDirection = Vector2.Lerp(_owner.MoveDir, _movingDirection, 0.5f);
                }
            }
            else if (movePosition.y < worldRect.yMin)
            {
                reflectedVector.y = 1.0f;
                //아래 벽 반사
                movePosition.y = worldRect.yMin;
                if (Vector2.Dot(Vector2.up, _movingDirection) < 0)
                {
                    _movingDirection = targetDirection;
                }
                else
                {
                    _movingDirection = Vector2.Lerp(_owner.MoveDir, _movingDirection, 0.5f);
                }
            }
            else if (movePosition.y > worldRect.yMax)
            {
                reflectedVector.y = -1.0f;
                //위쪽 벽 반사
                movePosition.y = worldRect.yMax;
                if (Vector2.Dot(Vector2.down, _movingDirection) < 0)
                {
                    _movingDirection = targetDirection;
                }
                else
                {
                    _movingDirection = Vector2.Lerp(_owner.MoveDir, _movingDirection, 0.5f);
                }
            }

            _movingDirection.Normalize();
            this.transform.position = movePosition;
        }

        private void RotateBodyImageToMoveDirection()
        {
            Vector2 direction = _movingDirection * _movingSpeed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.right = Quaternion.AngleAxis(-90.0f, Vector3.forward) * direction;
        }
    }
}
