using System;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages;
using Z.GameClients.Stages.AreaEffectObjects;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.PCs;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;
using Z.UnityHelpers;
using Random = UnityEngine.Random;

public class StickySlimeTranscendentObject : AreaEffectObjectBase
{
    public override bool IsAlive => _isAlive;
    private bool _isAlive;

    private PlayerCharacter _owner;
    private float _damage;
    private float _forwardMoveDuration;
    private float _objectRadius;
    private Vector2 _forwardDirection;
    private float _forwardMoveSpeed;
    private float _backwardMoveSpeed;

    private float _itemAcquireAdditionalRange;
    private float _dropChance;
    private float _goldChance;
    private long _dropGoldAmount;


    private SpriteAnimationHandler _forwardAnimation;
    private SpriteAnimationHandler _turnAnimation;
    private SpriteAnimationHandler _backwardJumpAnimation;
    private SpriteAnimationHandler _backwardLandingAnimation;

    private GameObject _backwardJumpBody;
    private GameObject _backwardLandingBody;
    private GameObject _backwardBodyShadow;
    private HashSet<Character> _hittedCharacters = new HashSet<Character>();
    private HashSet<Character> _currentHittedCharacters = new HashSet<Character>();

    private string _forwardBodyPath;
    private string _turnAnimationPath;
    private string _backwardJumpBodyPath;
    private string _backwardLandingBodyPath;
    private string _floorEffectPath;

    private enum SlimeState { ForwardMove, Turn, BackwardJump, BackLanding }
    private SlimeState _currentState;
    private float _stateChangeAt;


    public void AllocateSharedResources(AreaEffectType areaEffectType)
    {
        base.AllocateSharedResourcesForBase(areaEffectType);
        switch (areaEffectType)
        {
            case AreaEffectType.StickySlimeTranscendentObject_Default:
                {
                    _forwardBodyPath = "Stages/AreaEffects/StickySlime/Basic/Transcendent/waterIdle.prefab";
                    _turnAnimationPath = "Stages/AreaEffects/StickySlime/Basic/Transcendent/waterTurn.prefab";
                    _backwardJumpBodyPath = "Stages/AreaEffects/StickySlime/Basic/Transcendent/waterBackJump.prefab";
                    _backwardLandingBodyPath = "Stages/AreaEffects/StickySlime/Basic/Transcendent/waterBackLanding.prefab";
                    _floorEffectPath = "Stages/AreaEffects/StickySlime/Basic/Transcendent/mark.prefab";
                    
                }
                break;
            case AreaEffectType.StickySlimeTranscendentObject_Bongjun:
                {
                    _forwardBodyPath = "Stages/AreaEffects/StickySlime/Bongjun/Transcendent/waterIdle.prefab";
                    _turnAnimationPath = "Stages/AreaEffects/StickySlime/Bongjun/Transcendent/waterTurn.prefab";
                    _backwardJumpBodyPath = "Stages/AreaEffects/StickySlime/Bongjun/Transcendent/waterBackJump.prefab";
                    _backwardLandingBodyPath = "Stages/AreaEffects/StickySlime/Bongjun/Transcendent/waterBackLanding.prefab";
                    _floorEffectPath = "Stages/AreaEffects/StickySlime/Bongjun/Transcendent/mark.prefab";
                    
                }
                break;
            default:
                throw new NotSupportedException($"StickySlimeTranscendentObject을 사용하는 AreaEffectType이 아닙니다. 현재 AreaEffectType: [{areaEffectType}]");
        }

        _forwardAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(_forwardBodyPath);
        _forwardAnimation.transform.SetParent(this.transform);
        _forwardAnimation.transform.localPosition = Vector2.zero;
        _forwardAnimation.InitializeOnly();

        _turnAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(_turnAnimationPath);
        _turnAnimation.transform.SetParent(this.transform);
        _turnAnimation.transform.localPosition = Vector2.zero;
        _turnAnimation.InitializeOnly();

        _backwardJumpAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(_backwardJumpBodyPath);
        _backwardJumpAnimation.InitializeOnly();
        _backwardJumpBody = _backwardJumpAnimation.gameObject;
        _backwardJumpBody.transform.SetParent(this.transform);
        _backwardJumpBody.transform.localPosition = Vector2.zero;

        _backwardLandingAnimation = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(_backwardLandingBodyPath);
        _backwardLandingAnimation.InitializeOnly();
        _backwardLandingBody = _backwardLandingAnimation.gameObject;
        _backwardLandingBody.transform.SetParent(this.transform);
        _backwardLandingBody.transform.localPosition = Vector2.zero;

        _backwardBodyShadow = new GameObject("Shadow");
        _backwardBodyShadow.transform.SetParent(this.transform, worldPositionStays: false);
        var shadow = _backwardBodyShadow.AddComponent<SpriteRenderer>();
        shadow.sprite = ResourcePool.Instance.LoadResource<Sprite>("Stages/Characters/characterShadow.png");
        shadow.color = new Color(shadow.color.r, shadow.color.g, shadow.color.b, 0.9f);
        shadow.sortingLayerID = SortingLayer.NameToID("LowShadow");
        shadow.drawMode = SpriteDrawMode.Simple;
        shadow.transform.localPosition = new Vector3(0f, -0.5f, 0f);

    }

    public void Initialize(
        PlayerCharacter owner,
        float damage,
        float objectRadius,
        Vector2 startPosition,
        Vector2 forwardDirection,
        float forwardMoveSpeed,
        float forwardMoveDuration,
        float backwardMoveSpeed,
        float itemAcquireAdditionalRange,
        float dropChance,
        float goldChance,
        long dropGoldAmount
        )
    {
        base.InitializeAreaObject(owner.Alliance);
        float now = Time.time;
        _owner = owner;
        _damage = damage;
        _objectRadius = objectRadius;
        this.transform.position = startPosition;
        _forwardDirection = forwardDirection;
        _forwardMoveSpeed = forwardMoveSpeed;
        _forwardMoveDuration = forwardMoveDuration;
        _backwardMoveSpeed = backwardMoveSpeed;

        _itemAcquireAdditionalRange = itemAcquireAdditionalRange;
        _dropChance = dropChance;
        _goldChance = goldChance;
        _dropGoldAmount = dropGoldAmount;

        _hittedCharacters.Clear();

        _forwardAnimation.transform.localScale = objectRadius / 1f * Vector2.one;
        _turnAnimation.transform.localScale = objectRadius / 1f * Vector2.one;
        _backwardJumpBody.transform.localScale = objectRadius / 1.2f * Vector2.one;
        _backwardLandingBody.transform.localScale = objectRadius / 1.2f * Vector2.one;
        _backwardBodyShadow.transform.localScale = objectRadius / 1.2f * 1.5f * Vector2.one;

        _isAlive = true;

        _currentState = SlimeState.ForwardMove;
        _stateChangeAt = now;

        _forwardAnimation.gameObject.SetActive(true);
        _turnAnimation.gameObject.SetActive(false);
        _backwardJumpBody.gameObject.SetActive(false);
        _backwardLandingBody.gameObject.SetActive(false);
        _backwardBodyShadow.gameObject.SetActive(false);

        RotateToDirection(_forwardDirection);
    }

    public override void UpdateLogic(Stage stage, float deltaTime)
    {
        float now = Time.time;

        switch (_currentState)
        {
            case SlimeState.ForwardMove:
                {
                    //몸체 회전
                    RotateToDirection(_forwardDirection);
                    //전진 이동
                    MoveToForwardPosition(deltaTime);
                    //공격 처리
                    HitCheck(stage);

                    if (_stateChangeAt + _forwardMoveDuration < now)
                    {
                        _currentState = SlimeState.Turn;
                        _stateChangeAt = now;

                        _forwardAnimation.gameObject.SetActive(false);
                        _turnAnimation.gameObject.SetActive(true);
                        _turnAnimation.transform.localRotation = _forwardAnimation.transform.localRotation;
                        _turnAnimation.Play();
                        SortingLayerOrder();
                    }
                }
                break;
            case SlimeState.Turn:
                {
                    //공격 처리
                    HitCheck(stage);

                    if (_stateChangeAt + _turnAnimation.AnimationDuration < now)
                    {
                        _currentState = SlimeState.BackwardJump;
                        _stateChangeAt = now;

                        _turnAnimation.gameObject.SetActive(false);
                        _backwardJumpBody.gameObject.SetActive(true);
                        _backwardLandingBody.gameObject.SetActive(false);
                        _backwardJumpAnimation.Play();
                        _backwardBodyShadow.gameObject.SetActive(true);
                        _hittedCharacters.Clear();
                    }
                }
                break;
            case SlimeState.BackwardJump:
                {
                    Vector2 ownerDir = _owner.CenterPos - (Vector2)this.transform.position;
                    ownerDir.Normalize();

                    //되돌아 가는 이동 처리
                    MoveToOwnerPosition(deltaTime);

                    SortingLayerOrder();

                    //공격 처리
                    HitCheck(stage);

                    //생존여부 처리
                    AliveCheck();

                    //아이템 획득처리
                    AcquirableItemObject(stage);

                    if(_stateChangeAt + _backwardJumpAnimation.AnimationDuration <= now)
                    {
                        _currentState = SlimeState.BackLanding;
                        _stateChangeAt = now;
                        _backwardJumpBody.gameObject.SetActive(false);
                        _backwardLandingBody.gameObject.SetActive(true);
                        _backwardLandingAnimation.Play();
                        _backwardBodyShadow.gameObject.SetActive(false);

                        UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(_floorEffectPath, this.transform.position + new Vector3(0, -1, 0), this.transform.localScale, null);
                    }
                }
                break;
            case SlimeState.BackLanding:
                {
                    SortingLayerOrder();

                    //공격 처리
                    HitCheck(stage);

                    //생존여부 처리
                    AliveCheck();

                    //아이템 획득처리
                    AcquirableItemObject(stage);


                    if (_stateChangeAt + _backwardLandingAnimation.AnimationDuration <= now)
                    {
                        _currentState = SlimeState.BackwardJump;
                        _stateChangeAt = now;
                        _backwardJumpBody.gameObject.SetActive(true);
                        _backwardLandingBody.gameObject.SetActive(false);
                        _backwardJumpAnimation.Play();
                        _backwardBodyShadow.gameObject.SetActive(true);

                    }
                }
                break;
            default:
                break;
        }
    }

    public override void PuttingBackToPool()
    {
        base.PuttingBackToPool();
        _hittedCharacters.Clear();
    }

    private void MoveToForwardPosition(float deltaTime)
    {
        Vector2 currentPosition = this.transform.position;
        Vector2 nextPosition = currentPosition + _forwardDirection * _forwardMoveSpeed * deltaTime;
        this.transform.position = nextPosition;
    }

    private void MoveToOwnerPosition(float deltaTime)
    {
        Vector2 currentPosition = this.transform.position;
        Vector2 ownerDir = (_owner.CenterPos - currentPosition).normalized;
        Vector2 nextPosition;

        if (Vector2.Distance(_owner.CenterPos, currentPosition) < _backwardMoveSpeed * deltaTime)
        {
            nextPosition = _owner.CenterPos;
        }
        else
        {
            nextPosition = currentPosition + ownerDir * _backwardMoveSpeed * deltaTime;
        }

        this.transform.position = nextPosition;
    }

    private void RotateToDirection(Vector2 dir)
    {
        float angle = Mathf.Rad2Deg * Mathf.Atan2(dir.y, dir.x);
        _forwardAnimation.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private void AliveCheck()
    {
        if (Vector2.Distance(_owner.CenterPos, this.transform.position) < _objectRadius)
        {
            //오브젝트 충돌 범위내에 owner가 있으면 오브젝트를 죽인다.
            _isAlive = false;
        }
    }

    private void AcquirableItemObject(Stage stage)
    {
        float acquisitionDistance = (_objectRadius + _itemAcquireAdditionalRange);
        foreach (var acquirableItem in stage.TakeAcquirableItemObjectsInDistance(this.transform.position, acquisitionDistance))
        {
            acquirableItem.OnAcquiredWithSlime(_owner, stage);
            // NOTE: OnAcquired 호출즉시 경험치가 오르지는 않는다. 획득 애니메이션 재생 다 끝나야 들어온다. 
        }
    }

    private void DropCheckGoldOrGem(Stage stage, float dropChance, float goldChance, long dropGoldAmount, Vector2 createPos)
    {
        float dropRandom = Random.Range(minInclusive: 0f, maxInclusive: 0.9999999f);
        if (dropChance < dropRandom)
        {
            return;
        }

        float goldRandom = Random.Range(minInclusive: 0f, maxInclusive: 0.9999999f);
        if (goldChance < goldRandom)
        {
            long actualGemAmount = stage.TakeMonsterDropGems(1);
            if (actualGemAmount > 0)
            {
                stage.CreateGemObject(actualGemAmount, createPos);
            }
        }
        else
        {
            long actualGoldAmount = stage.TakeItemBoxDropGolds(dropGoldAmount);
            if (actualGoldAmount > 0)
            {
                stage.CreateGoldObject(actualGoldAmount, createPos);
            }
        }
    }

    private void HitCheck(Stage stage)
    {
        var targetArea = new CircularTargetArea(this.transform.position, _objectRadius);
        CombatSystem.HitOnTargetArea(stage, targetArea, _owner, _damage, CombatSystem.KnockBackType.Pivot, targetArea.Center, knockBackPower: 0.1f, _currentHittedCharacters, _hittedCharacters, null);

        foreach (var hittedCharacters in _currentHittedCharacters)
        {
            if (hittedCharacters.Action.IsDead)
            {
                DropCheckGoldOrGem(stage, _dropChance, _goldChance, _dropGoldAmount, this.transform.position);
            }
            else
            {
                _hittedCharacters.Add(hittedCharacters);
            }
        }
        _currentHittedCharacters.Clear();
    }

    private void SortingLayerOrder()
    {
        int bodyLayerOrder = (int)(transform.position.y * -100.0f);

        if (_turnAnimation != null)
        {
            _turnAnimation.SpriteRenderer.sortingOrder = bodyLayerOrder;
        }
        else
        {
            Debug.LogWarning("_turnAnimation 이 비어있습니다. 로직이 변경되었으면 해당 코드 수정이 필요합니다.");
        }

        if (_backwardJumpAnimation != null)
        {
            _backwardJumpAnimation.SpriteRenderer.sortingOrder = bodyLayerOrder;
        }
        else
        {
            Debug.LogWarning("_backwardJumpAnimation 이 비어있습니다. 로직이 변경되었으면 해당 코드 수정이 필요합니다.");
        }

        if (_backwardLandingAnimation != null)
        {
            _backwardLandingAnimation.SpriteRenderer.sortingOrder = bodyLayerOrder;
        }
        else
        {
            Debug.LogWarning("_backwardLandingAnimation 이 비어있습니다. 로직이 변경되었으면 해당 코드 수정이 필요합니다.");
        }

    }
}
