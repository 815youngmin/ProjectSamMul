using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.GameClients.Stages.ItemObjects;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class GymBallObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private GameObject _bodyImage;
        private GameObject _normalSkillImage;
        private GameObject _transcendSkillImage;

        private Character _owner;
        private Vector2 _movingDirection;
        private float _movingSpeed;
        private float _damage;
        private float _knockBackPower;
        private float _lifeTime;
        private float _createdAt;
        private int _spliteCount;
        private bool _isTranscend;

        private HashSet<Collider2D> _hittedColliders;
        private float _hittedClearAt;
        private string _hitSoundPrefabPath;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.GymBall);

            string normalImagePrefabPath = "Stages/AreaEffects/StarlightDartObject.prefab";
            string transcendImagePrefabPath = "Stages/AreaEffects/StarlightDartObject_S.prefab";

            _bodyImage = new GameObject("GymballBodyImage");
            _bodyImage.transform.SetParent(this.gameObject.transform);

            _normalSkillImage = ResourcePool.Instance.InstantiateFromResource(normalImagePrefabPath);
            _normalSkillImage.transform.SetParent(_bodyImage.transform);
            _normalSkillImage.transform.localPosition = Vector3.zero;
            _normalSkillImage.transform.localScale = Vector3.one;


            _transcendSkillImage = ResourcePool.Instance.InstantiateFromResource(transcendImagePrefabPath);
            _transcendSkillImage.transform.SetParent(_bodyImage.transform);
            _transcendSkillImage.transform.localPosition = Vector3.zero;
            _transcendSkillImage.transform.localScale = Vector3.one;
            _hittedColliders = new HashSet<Collider2D>();
        }

        public void Initialize(
            AllianceType alliance,
            Character owner,
            Vector2 movingDirection,
            float movingSpeed,
            float damage,
            float knockBackPower,
            float lifeTime,
            int spliteCount,
            bool isTranscend,
            string hitSoundPrefabPath)
        {
            base.InitializeAreaObject(alliance);
            _owner = owner;
            _movingDirection = movingDirection.normalized;
            _movingSpeed = movingSpeed;
            _damage = damage;
            _knockBackPower = knockBackPower;
            _lifeTime = lifeTime;
            _createdAt = Time.time;
            this.transform.position = _owner.CenterPos;
            _spliteCount = spliteCount;
            _isTranscend = isTranscend;
            _hitSoundPrefabPath = hitSoundPrefabPath;

            if (isTranscend)
            {
                _transcendSkillImage.SetActive(true);
                _normalSkillImage.SetActive(false);
            }
            else
            {
                _transcendSkillImage.SetActive(false);
                _normalSkillImage.SetActive(true);
            }

        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            if(_hittedClearAt <= now)
            {
                _hittedColliders.Clear();
                _hittedClearAt = now + 0.1f;
            }

            Vector2 currentPosition = this.transform.position;
            float moveDistance = _movingSpeed * deltaTime;
            Vector2 nextPosition = currentPosition + _movingDirection * moveDistance;
            int collisionMask = Physics2D.GetLayerCollisionMask(LayerMask.NameToLayer("Player"));
            RaycastHit2D[] hits = Physics2D.LinecastAll(currentPosition, nextPosition, collisionMask);
            if (hits.Length != 0)
            {
                RaycastHit2D hit = new RaycastHit2D();
                float minDistanceSqr = float.MaxValue;
                foreach (RaycastHit2D target in hits)
                {
                    if(_hittedColliders.Contains(target.collider))
                    {
                        continue;
                    }

                    Character character = target.collider.GetComponent<Character>();
                    if(character != null)
                    {
                        if (character.Alliance == _owner.Alliance)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        BreakableItemObject breakableItemObject = target.collider.GetComponent<BreakableItemObject>();
                        if (breakableItemObject == null)
                        {
                            continue;
                        }
                    }

                    Vector2 objectPosition = target.collider.transform.position;
                    float distanceSqr = (target.point - objectPosition).sqrMagnitude;
                    if (distanceSqr < minDistanceSqr)
                    {
                        hit = target;
                        minDistanceSqr = distanceSqr;
                    }
                }

                if (hit)
                {
                    if(0 != _spliteCount)
                    {
                        this.CreatSpliteGymBall(stage);
                        return;
                    }

                    Vector2 hitPoint = hit.point;
                    Vector2 targetPosition = hit.collider.gameObject.transform.position;
                    Vector2 dir = hitPoint - targetPosition;
                    _movingDirection = Vector2.Reflect(_movingDirection.normalized, dir.normalized);

                    float remainingDistacne = Mathf.Sqrt(minDistanceSqr);
                    this.transform.position = hitPoint + _movingDirection.normalized * remainingDistacne;
                    this.RotateBodyImageToMoveDirection();

                    _hittedColliders.Add(hit.collider);
                    Character character = hit.collider.GetComponent<Character>();
                    if (character != null)
                    {
                        character.Hitted(
                            stage, _owner, _damage, dir.normalized * -1.0f * _knockBackPower, hitPoint, _hitSoundPrefabPath);
                    }
                    else
                    {
                        BreakableItemObject breakableItemObject = hit.collider.GetComponent<BreakableItemObject>();
                        if (breakableItemObject != null)
                        {
                            breakableItemObject.OnBroken(_owner, _damage, stage);
                        }
                    }
                    return;
                }
            }

            this.transform.position = nextPosition;
            this.ScreenReflect();
            this.RotateBodyImageToMoveDirection();
        }

        private void ScreenReflect()
        {
            Camera camera = Camera.main;

            Matrix4x4 VPMatrix = camera.projectionMatrix * camera.worldToCameraMatrix;
            Vector3 wvpPosition = VPMatrix.MultiplyPoint(transform.position);
            Vector3 dir = _movingDirection;
            if (wvpPosition.x < -1)
            {
                float distance = -1 - wvpPosition.x;
                wvpPosition.x += 2.0f * distance;
                dir.x = Mathf.Abs(dir.x);
            }
            else if (wvpPosition.x > 1)
            {
                float distance = 1 - wvpPosition.x;
                wvpPosition.x += 2.0f * distance;
                dir.x = Mathf.Abs(dir.x) * -1.0f;
            }

            if (wvpPosition.y < -1)
            {
                float distance = -1 - wvpPosition.y;
                wvpPosition.y += 2.0f * distance;
                dir.y = Mathf.Abs(dir.y);
            }
            else if (wvpPosition.y > 1)
            {
                float distance = 1 - wvpPosition.y;
                wvpPosition.y += 2.0f * distance;
                dir.y = Mathf.Abs(dir.y) * -1.0f;
            }

            transform.position = VPMatrix.inverse.MultiplyPoint(wvpPosition);
            _movingDirection = dir;
        }

        private void CreatSpliteGymBall(Stage stage)
        {
            for (int i = 0; i < _spliteCount; ++i)
            {
                Vector2 targetDirection = Random.insideUnitCircle;

                float remainingDuration = (_createdAt + _lifeTime - Time.time);
                var ballObject = stage.CreateGymBallObject(
                    _owner.Alliance,
                    _owner,
                    targetDirection.normalized,
                    _movingSpeed,
                    _damage,
                    _knockBackPower,
                    remainingDuration,
                    0,
                    false,
                    _hitSoundPrefabPath);
                ballObject.transform.transform.position = this.transform.position;
                ballObject.transform.localScale = this.transform.localScale;
            }
            _lifeTime = 0.0f;

        }

        private void RotateBodyImageToMoveDirection()
        {
            Vector2 direction = _movingDirection * _movingSpeed;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _bodyImage.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
}
