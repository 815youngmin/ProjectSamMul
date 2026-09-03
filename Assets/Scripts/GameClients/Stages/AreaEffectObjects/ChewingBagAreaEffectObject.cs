using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.CombatSystems;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class ChewingBagAreaEffectObject : AreaEffectObjectBase
    {
        public override bool IsAlive => Time.time <= _createdAt + _lifeTime;

        private Character _owner;
        private float _damage;
        private float _attackRadius;
        private float _moveSpeedChangeRatio;

        private float _tickDamageAt;
        private readonly float _attackTickInterval = 0.25f;
        private HashSet<Character> _hittedCharacters;
        private List<Character> _targetCharacters;

        private float _createdAt;
        private float _lifeTime;

        private GameObject _body;

        private Sequence _fadeIn;
        private Sequence _fadeOut;

        private float _fadeOutAt;

        private readonly string _slowStatusEffectKey = "ChewingBagAreaEffectObject";
        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForBase(AreaEffectType.ChewingBag);

            _body = ResourcePool.Instance.InstantiateFromResource("Stages/AreaEffects/ChewingBagAreaEffectObject.prefab");
            _body.transform.SetParent(this.transform);
            _body.transform.localPosition = Vector2.zero;
            _body.transform.localScale = Vector2.one * 0.8f;

            _hittedCharacters = new HashSet<Character>();
            _targetCharacters = new List<Character>();
            this.AllocateSequenceAnimation();
        }

        private void AllocateSequenceAnimation()
        {
            _fadeIn = DOTween.Sequence();
            _fadeIn.Append(_body.GetComponent<SpriteRenderer>().DOFade(0.0f, 0.0f));
            _fadeIn.Append(_body.GetComponent<SpriteRenderer>().DOFade(1.0f, 0.5f));
            _fadeIn.SetRecyclable(true);
            _fadeIn.SetAutoKill(false);
            _fadeIn.Pause();

            _fadeOut = DOTween.Sequence();
            _fadeOut.Append(_body.GetComponent<SpriteRenderer>().DOFade(0.0f, 0.3f));
            _fadeOut.SetRecyclable(true);
            _fadeOut.SetAutoKill(false);
            _fadeOut.Pause();

        }

        public void Initialize(
            Character owner,
            float damage,
            float moveSpeedChangeRatio,
            float duration,
            bool isLeft)
        {
            base.InitializeAreaObject(owner.Alliance);
            float now = Time.time;
            _owner = owner;
            _damage = damage;
            _moveSpeedChangeRatio = moveSpeedChangeRatio;

            _lifeTime = duration;
            _createdAt = now;
            _tickDamageAt = now;

            _body.GetComponent<SpriteRenderer>().color = this.GetRandomGumColor();

            _fadeOutAt = now + _lifeTime - 0.3f;
            _fadeIn.Restart();
            if (isLeft)
            {
                Vector3 leftDir = Quaternion.Euler(0, 0, 90f) * owner.MoveDir;
                this.transform.position = owner.transform.position + leftDir * Random.Range(0.1f, 0.4f);
            }
            else
            {
                Vector3 rightDir = Quaternion.Euler(0, 0, -90f) * owner.MoveDir;
                this.transform.position = owner.transform.position + rightDir * Random.Range(0.1f, 0.4f);
            }
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;

            if (_fadeOutAt <= now)
            {
                _fadeOutAt = float.MaxValue;
                _fadeOut.Restart();
            }

            if (now < _tickDamageAt)
            {
                return;
            }

            _targetCharacters.Clear();
            var targetArea = new SquareTargetArea(this.transform.position, new Vector2(1, 4), 0f);
            stage.FindAliveCharactersInArea(_owner.Alliance.ToEnemyAlliance(), targetArea, _targetCharacters);
            foreach (var character in _targetCharacters)
            {
                if (_hittedCharacters.Contains(character))
                {
                    continue;
                }
                character.Hitted(stage, _owner, _damage, Vector2.zero, character.Pos, null);

                if (!character.IsBoss)
                {
                    character.StatusEffects.AddOrUpdateStatusEffect(stage, character, Characters.StatusEffects.StatusEffectType.SlowMove, _slowStatusEffectKey, _attackTickInterval, now, _moveSpeedChangeRatio);
                }
                _hittedCharacters.Add(character);
            }
            _tickDamageAt = Time.time + _attackTickInterval;
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();

            _hittedCharacters.Clear();
            _targetCharacters.Clear();
            _fadeIn.Pause();
            _fadeOut.Pause();
        }

        private Color GetRandomGumColor()
        {
            int rnd = Random.Range(0, 4);
            if (rnd == 0)
            {
                return new Color(1, 0.65490f, 0.882352f);
            }
            else if (rnd == 1)
            {
                return new Color(0.97254f, 1f, 0.580392f);
            }
            else if (rnd == 2)
            {
                return new Color(0.42745f, 0.937254f, 1f);
            }
            else if (rnd == 3)
            {
                return new Color(1f, 0.678431f, 0.486274f);
            }
            else
            {
                return Color.white;
            }
        }

    }
}
