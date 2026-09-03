using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.GameClients.Stages.CombatSystems;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public class IceThreeLeapsBugPoisonAreaEffect : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _lifeEndTimeAt;
        private Monster _owner;         
        private float _droptime;        //독구슬 떨어지는 시간
        private float _lifeEndTimeAt;   //장판 끝나는 시간
        private float _arrivedTimeAt;   //독구슬 도착 시간, 장판 활성화 시간
        private float _radius;          //장판 범위
        private float _damage;          //장판 데미지
        private readonly float _tickDamagePeriod = 0.25f;   //장판 데미지 간격
        private float _tickDamageNextAt;                    //다음 공격 시간

        private Sequence _dropPoison;   //독구슬 연출 시퀀스
        private Sequence _fadeIn;       //독 장판 페이드인 연출
        private Sequence _fadeOut;      //독 장판 페이드아웃 연출
        private float _fadeOutSequenceDuration = 0.3f;  //페이드아웃 연출 시간
        private bool _isFadeOutSequencePlayed;

        private HashSet<Character> _hittedCharacters;
        private SpriteRenderer _poisonSpriteRenderer;
        private SpriteAnimationHandler _areaEffectAppearing;
        private SpriteAnimationHandler _areaEffectRepeating;
        private bool _isArrived;

        private readonly float radiusToScale = 0.4f; //radius 2.5f 기준 scale 1이어야됨

        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.IceThreeLeapsBugPoison);

            var poisonSprite = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/PoisonEffects/PoisonBall.prefab");
            _poisonSpriteRenderer = poisonSprite.GetComponent<SpriteRenderer>();
            _poisonSpriteRenderer.gameObject.SetActive(false);
            _poisonSpriteRenderer.transform.SetParent(this.transform);

            var appearingResourcePath = "Stages/AreaEffects/PoisonEffects/PoisonAreaEffectAppearing.prefab";
            _areaEffectAppearing = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(appearingResourcePath);
            _areaEffectAppearing.InitializeOnly();
            _areaEffectAppearing.gameObject.SetActive(false);
            _areaEffectAppearing.transform.localPosition = Vector3.zero;
            _areaEffectAppearing.transform.localScale = Vector3.one;
            _areaEffectAppearing.gameObject.transform.SetParent(this.transform);

            var repeatingResourcePath = "Stages/AreaEffects/PoisonEffects/PoisonAreaEffectRepeating.prefab";
            _areaEffectRepeating = ResourcePool.Instance.InstantiateFromResource<SpriteAnimationHandler>(repeatingResourcePath);
            _areaEffectRepeating.InitializeOnly();
            _areaEffectRepeating.gameObject.SetActive(false);
            _areaEffectRepeating.transform.localPosition = Vector3.zero;
            _areaEffectRepeating.transform.localScale = Vector3.one;
            _areaEffectRepeating.gameObject.transform.SetParent(this.transform);

            _hittedCharacters = new HashSet<Character>();
            this.AllocateFadeSequenceAnimation();
        }

        public void Initialize(Monster owner, Vector2 position,  float lifeTime, float dropTime, float damage, float radius)
        {
            base.InitializeAreaObject(owner.Alliance);

            float now = Time.time;
            _owner = owner;
            this.transform.position = position;
            _lifeEndTimeAt = lifeTime + now + dropTime;
            _arrivedTimeAt = dropTime + now;
            _droptime = dropTime;
            _radius = radius;
            _damage = damage;

            _poisonSpriteRenderer.gameObject.SetActive(true);
            _areaEffectAppearing.gameObject.SetActive(false);
            _areaEffectRepeating.gameObject.SetActive(false);

            Vector3 localScale = Vector3.one * radius * radiusToScale;

            _poisonSpriteRenderer.transform.localScale = localScale;
            _areaEffectAppearing.transform.localScale = localScale;
            _areaEffectRepeating.transform.localScale = localScale;


            _poisonSpriteRenderer.gameObject.transform.localPosition = new Vector3(0, 30f, 0);

            this.UpdateSortingOrder();
            _areaEffectAppearing.SpriteRenderer.color = Color.white;
            _areaEffectRepeating.SpriteRenderer.color = Color.white;

            this.AllocateDropPoisonSequenceAnimation();
            _dropPoison.Restart();
        }

        //독 구슬 연출 시간은 외부에서 받아와 처리하기 때문에 초기화 단계에서 할당해준다
        private void AllocateDropPoisonSequenceAnimation()
        {
            _dropPoison = DOTween.Sequence();

            _dropPoison.Append(_poisonSpriteRenderer.DOFade(0.0f, 0.0f));
            _dropPoison.Append(_poisonSpriteRenderer.DOFade(1.0f, _droptime));
            _dropPoison.Join(_poisonSpriteRenderer.gameObject.transform.DOLocalMove(Vector3.zero, _droptime).SetEase(Ease.InQuart));
            _dropPoison.SetRecyclable(true);
            _dropPoison.SetAutoKill(false);
            _dropPoison.Pause();
        }

        private void AllocateFadeSequenceAnimation()
        {
            _fadeIn = DOTween.Sequence();
            _fadeIn.Append(_areaEffectRepeating.SpriteRenderer.DOFade(0.0f, 0.0f));
            _fadeIn.Append(_areaEffectRepeating.SpriteRenderer.DOFade(1.0f, 0.5f));
            _fadeIn.SetRecyclable(true);
            _fadeIn.SetAutoKill(false);
            _fadeIn.Pause();

            _fadeOutSequenceDuration = 0.3f;
            _fadeOut = DOTween.Sequence();
            _fadeOut.Append(_areaEffectRepeating.SpriteRenderer.DOFade(0.0f, _fadeOutSequenceDuration));
            _fadeOut.SetRecyclable(true);
            _fadeOut.SetAutoKill(false);
            _fadeOut.Pause();
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _isArrived = false;
            _owner = null;
            _poisonSpriteRenderer.gameObject.SetActive(false);
            _areaEffectAppearing.gameObject.SetActive(false);
            _areaEffectRepeating.gameObject.SetActive(false);
            _hittedCharacters.Clear();

            _dropPoison.Pause();
            _fadeIn.Pause();
            _fadeOut.Pause();
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if(_arrivedTimeAt < now)
            { 
                if(!_isArrived)
                {
                    _isArrived = true;
                    this.ArrivedPoison(now);
                }

                this.UpdateAttacked(stage, now);
            }

            if (_lifeEndTimeAt - _fadeOutSequenceDuration < now && !_isFadeOutSequencePlayed)
            {
                _fadeOut.Restart();
                _isFadeOutSequencePlayed = true;
            }
        }

        //독구슬 도착시 불리는 인터페이스
        private void ArrivedPoison(float now)
        {
            _poisonSpriteRenderer.gameObject.SetActive(false);

            _areaEffectAppearing.gameObject.SetActive(true);
            _areaEffectRepeating.gameObject.SetActive(false);
            _areaEffectAppearing.InitializeAndPlay(hitEventHandler: null,
                () =>
                {
                    _areaEffectAppearing.gameObject.SetActive(false);
                    _areaEffectRepeating.gameObject.SetActive(true);
                    _areaEffectRepeating.Play();
                });


            this.UpdateSortingOrder();
            _areaEffectAppearing.SpriteRenderer.color = Color.white;
            _areaEffectRepeating.SpriteRenderer.color = Color.white;

            _tickDamageNextAt = now;
            _isFadeOutSequencePlayed = false;
            _fadeIn.Restart();
        }

        private void UpdateSortingOrder()
        {
            _areaEffectAppearing.SpriteRenderer.sortingOrder 
                = _areaEffectRepeating.SpriteRenderer.sortingOrder
                = (int)(transform.position.y * -100.0f) - ((int)transform.position.x % 80);
        }

        private void UpdateAttacked(Stage stage, float now)
        {
            if (_tickDamageNextAt <= now)
            {
                _hittedCharacters.Clear();
                _tickDamageNextAt = Time.time + _tickDamagePeriod;
            }

            Vector2 position = this.transform.position;
            float radiusSqrMagnitude = _radius * _radius;
            Vector2 ownerPosition = _owner.Pos;

            List<Character> characters = new List<Character>();
            var targetArea = new CircularTargetArea(this.transform.position, _radius);
            stage.FindAliveCharactersInArea(_owner.Alliance.ToEnemyAlliance(), targetArea, characters);
            foreach (var character in characters)
            {
                if (_hittedCharacters.Contains(character))
                {
                    continue;
                }

                float sqrMagnitude = (character.Pos - position).sqrMagnitude;
                if (radiusSqrMagnitude < sqrMagnitude)
                {
                    continue;
                }

                Vector2 direction = character.Pos - ownerPosition;
                direction.Normalize();
                character.Hitted(stage, _owner, _damage, Vector2.zero, character.Pos, hitSoundPrefabPath: string.Empty);
                _hittedCharacters.Add(character);
            }
        }

    }
}

