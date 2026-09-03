using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class AresSpearAreaEffectObject : AreaEffectObjectBase
    {
        enum State
        {
            None,
            MoveTargetPosition,   //타겟 방향으로 날아가기
            Split,              //분열 됨
            MoveWaitingPosition,     //대기 위치로 이동
            Wait,               //대기 중
            MoveStartPosition     //시작 위치로 되돌아가기
        }

        private State _currentState;
        private State _nextState;
        private GameObject _body;
        private List<GameObject> _spears;
        private Character _owner;
        private float _damage;

        private string _spearPath = "Stages/Projectiles/Ares_spear.prefab";
        public override bool IsAlive => Time.time < _backArrivedAt;

        private float _targetArrivedAt;
        private float _splitPositionArrivedAt;
        private float _waitAt;
        private float _backArrivedAt;

        private Vector2 _startPosition;
        private Vector2 _targetPosition;
        private Vector2 _backPosition;
        private float _splitDistnace;

        private bool _isHit;

        private List<float> _spearSpeeds;
        private List<Vector2> _spearDirections;
        private HashSet<Character> _hittedCharacters;

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.AresSpear);

            _body = new GameObject("aresSpears");
            _body.transform.SetParent(transform);
            _body.transform.localPosition = Vector3.zero;
            _body.transform.localScale = Vector3.one;

            _spears = new List<GameObject>();
            _spearSpeeds = new List<float>();
            _spearDirections = new List<Vector2>();
            _hittedCharacters = new HashSet<Character>();

            for (int i =0; i < 4; i++)
            {
                GameObject spear = ResourcePool.Instance.InstantiateFromResource(_spearPath);
                spear.transform.SetParent(_body.transform);
                spear.transform.localPosition = Vector3.zero;
                spear.transform.localScale = Vector3.one;
                _spears.Add(spear);
                _spearSpeeds.Add(0);
                _spearDirections.Add(Vector2.zero);
            }
        }

        public void Initialize(
            Character owner,
            Stage stage,
            Vector2 startPosition,
            Vector2 targetPosition,
            Vector2 backPosition,
            float splitDistance,
            float targetArrivedAt,
            float splitPositionArrivedAt,
            float waitAt,
            float backArrivedAt,
            float damage)
        {
            base.InitializeAreaObject(owner.Alliance);

            _owner = owner;
            _damage = damage;

            _spears[0].gameObject.SetActive(true);
            _spears[1].gameObject.SetActive(false);
            _spears[2].gameObject.SetActive(false);
            _spears[3].gameObject.SetActive(false);

            _startPosition = startPosition;
            _targetPosition = targetPosition;
            _backPosition = backPosition;
            _splitDistnace = splitDistance;
            _targetArrivedAt = targetArrivedAt;
            _splitPositionArrivedAt = splitPositionArrivedAt;
            _waitAt = waitAt;
            _backArrivedAt = backArrivedAt;

            float now = Time.time;
            float distance = Vector2.Distance(_targetPosition, startPosition);

            _spears[0].transform.position = _startPosition;
            _spears[0].transform.localScale = Vector3.one;
            _spearDirections[0] = (_targetPosition - startPosition).normalized;
            _spearSpeeds[0] = distance / (_targetArrivedAt - now);

            _nextState = State.MoveTargetPosition;
            _currentState = State.None;
            _isHit = false;
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            _currentState = _nextState;

            if(_currentState == State.MoveTargetPosition)
            {
                if (now > _targetArrivedAt)
                {
                    _nextState = State.Split;
                    //최종 목적지까지 이동 후 분열
                }
                else if (_isHit)
                {
                    _nextState = State.Split;
                    //플레이어와 충돌 분열
                }
                else
                {
                    //목적지로 전진
                    Vector2 deltaMovement = _spearDirections[0] * _spearSpeeds[0] * deltaTime;
                    _spears[0].transform.Translate(deltaMovement.x, deltaMovement.y, 0f,Space.World);
                    _spears[0].transform.right = _spearDirections[0];
                }
            }
            else if(_currentState == State.Split)
            {
                //분열
                for(int i = 0; i < _spears.Count; i++)
                {
                    _spears[i].transform.position = _spears[0].transform.position;
                    _spears[i].gameObject.SetActive(true);
                    _spears[i].transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    _spearDirections[i] = Quaternion.Euler(0, 0, 90 * i) * _spearDirections[0];
                    _spearSpeeds[i] = _splitDistnace / (_splitPositionArrivedAt - now);
                }

                //분열 이펙트 생성
                UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation("Stages/SkillEffects/hit_effect.prefab", _spears[0].transform.position, Vector2.one, null);
                _nextState = State.MoveWaitingPosition;
            }
            else if(_currentState == State.MoveWaitingPosition)
            {
                if(now > _splitPositionArrivedAt)
                {
                    //분열 끝
                    _nextState = State.Wait;
                }
                else
                {
                    for(int i =0; i < _spears.Count; i++)
                    {
                        Vector2 deltaMovement = _spearDirections[i] * _spearSpeeds[i] * deltaTime;
                        _spears[i].transform.Translate(deltaMovement.x, deltaMovement.y, 0f, Space.World);
                        _spears[i].transform.Rotate(0, 0, 1000f *deltaTime); 
                    }
                    //분열 위치로 이동 
                }
            }
            else if(_currentState == State.Wait)
            {
                if (now > _waitAt)
                {
                    //대기 끝
                    _nextState = State.MoveStartPosition;

                    //돌아올때 창에 다시 맞을 수 있도록 초기화 해줘야됨
                    _hittedCharacters.Clear();

                    for (int i = 0; i < _spears.Count; i++)
                    {
                        float distance = Vector2.Distance(_spears[i].transform.position, _backPosition);
                        _spearDirections[i] = (_backPosition - (Vector2)_spears[i].transform.position).normalized;
                        _spearSpeeds[i] = distance / (_backArrivedAt - now);
                    }
                }
                else
                {
                    //대기
                    //창 방향 전환 연출같은거 넣어줘도 좋을듯
                    for (int i = 0; i < _spears.Count; i++)
                    {
                        _spears[i].transform.Rotate(0, 0, 1000f * deltaTime);
                    }
                }
            }
            else if(_currentState == State.MoveStartPosition)
            {
                if(now > _backArrivedAt)
                {
                    //종료
                    for (int i = 0; i < _spears.Count; i++)
                    {
                        _spears[i].gameObject.SetActive(false);
                    }
                }
                else
                {
                    //이동
                    for (int i = 0; i < _spears.Count; i++)
                    {
                        Vector2 deltaMovement = _spearDirections[i] * _spearSpeeds[i] * deltaTime;
                        _spears[i].transform.Translate(deltaMovement.x, deltaMovement.y, 0f, Space.World);
                        _spears[i].transform.right = _spearDirections[i];
                    }
                }
            }

            this.HitCheckToSpears(stage, _owner);
        }

        private void HitCheckToSpears(Stage stage, Character owner)
        {
            for(int i = 0; i < _spears.Count; i++)
            {
                if (_spears[i].activeSelf)
                {
                    float angle = (Mathf.Atan2(_spears[i].transform.right.y, _spears[i].transform.right.x) * Mathf.Rad2Deg);
                    SquareTargetArea spearTargetArea = new SquareTargetArea(_spears[i].transform.position, new Vector2(6f * _spears[i].transform.localScale.x, 0.5f * _spears[i].transform.localScale.y), angle);
                    CombatSystem.HitOnTargetArea(stage, spearTargetArea, owner, _damage, CombatSystem.KnockBackType.Pivot, Vector2.zero, 0f, _hittedCharacters, _hittedCharacters, hitSoundPrefabPath: string.Empty);
                }
            }

            if(_hittedCharacters.Count != 0)
            {
                _isHit = true;
            }
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();

            for(int i =0; i < _spears.Count; i++)
            {
                _spears[i].gameObject.SetActive(false);
                _spears[i].transform.localPosition = Vector3.zero;
                _spears[i].transform.localScale = Vector3.one;
                _spears[i].transform.localRotation = Quaternion.identity;
                _spearSpeeds[i] = 0;
                _spearDirections[i] = Vector2.zero;
            }

            _hittedCharacters.Clear();
            _isHit = false;
        }
    }
}
