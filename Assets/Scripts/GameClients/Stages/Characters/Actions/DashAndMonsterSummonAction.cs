using DG.Tweening;
using Shared.GameDataTypes;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.Characters.Actions
{
    public class DashAndMonsterSummonAction : ActionBase
    {
        private SpriteMonsterAnimationController AnimationController => (SpriteMonsterAnimationController)base._animationController;

        private readonly static string SummonEffectPrefabPath = "Stages/ETCEffects/monsterWarning.prefab";
        private readonly static float SummonReadyDuration = 1.5f;

        private readonly Monster _owner;
        private readonly Character _target;
        private readonly float _dashSpeed;
        private readonly float _dashPreDelay;
        private readonly float _dashDuration;

        private float _startDashAt;
        private float _endDashAt;
        private Vector2 _dashDirection;
        private bool _isDashing;

        private float _summonFrameAt;
        private int _summonAmount;
        private CharacterType _summonCharacterType;
        private List<Vector2> _summonPositions;
        private float _attackPowerWeight;
        private float _hpWeight;

        public DashAndMonsterSummonAction(
            Monster owner,
            Character target,
            SpriteMonsterAnimationController animationController,
            float dashSpeed,
            float dashPreDaly,
            float dashDuration,
            float dashPostDelay,
            int summonAmount,
            float attackPowerWeight,
            float hpWeight,
            CharacterType summonCharacterType)
            : base(ActionType.Skill, dashPreDaly + dashDuration + dashPostDelay, animationController)
        {
            _owner = owner;
            _target = target;
            _dashSpeed = dashSpeed;
            _dashPreDelay = dashPreDaly;
            _dashDuration = dashDuration;

            _summonFrameAt = 0f;
            _summonAmount = summonAmount;
            _summonCharacterType = summonCharacterType;
            _summonPositions = new List<Vector2>();
            _attackPowerWeight = attackPowerWeight;
            _hpWeight = hpWeight;

        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            _startDashAt = now + _dashPreDelay;
            _endDashAt = _startDashAt + _dashDuration;
            _dashDirection = (_target.Pos - _owner.Pos).normalized;
            _isDashing = false;

            AnimationController.UpdateBodyDirectionByMoveDirection(_dashDirection);
            AnimationController.PlayIdleAttackActionInfinitely();

            float dashDistance = _dashSpeed * _dashDuration;
            float dashAngle = Mathf.Rad2Deg * Mathf.Atan2(_dashDirection.y, _dashDirection.x);
            stage.AreaIndicators.CreateSquareAttackRangeIndicator(
                _owner.Pos + 0.5f * dashDistance * _dashDirection,
                dashDistance,
                2.0f * _owner.CollisionAttackRadius,
                dashAngle,
                _dashPreDelay);
            stage.AreaIndicators.CreateDirectionalIndicator(_owner.Pos, _dashDirection, dashDistance, 2.0f * _owner.CollisionAttackRadius, _dashPreDelay);

            float spawnPositionEffectScale = 1 / 2.395f * 1.5f;
            for (int i = 0; i < _summonAmount; i++)
            {
                _summonPositions.Add(_owner.Pos + Random.insideUnitCircle * 3f);
                //애니메이션이 1초짜리입니다.
                //나중에 end이벤트에 소환을 넣던지 애니메이션 속도를 조절하던지 해야될듯
                UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(SummonEffectPrefabPath, _summonPositions[i], Vector2.one * spawnPositionEffectScale, null);
            }

            _summonFrameAt = now + SummonReadyDuration;
            float atPosition = _summonFrameAt - now;
            DOTween.Sequence().InsertCallback(atPosition, () =>
            {
                this.Summon(stage);
            });
        }

        public override void Update(Stage stage, float deltaTime, float now)
        {
            if (_startDashAt < now && now < _endDashAt)
            {
                if (!_isDashing)
                {
                    AnimationController.PlayAttackForce(AnimationController.AttackAnimationDuration);
                    _isDashing = true;
                }

                Vector2 deltaMovement = deltaTime * _dashSpeed * _dashDirection;
                _owner.transform.Translate(deltaMovement);
            }
            else if (_endDashAt <= now)
            {
                if (_isDashing)
                {
                    _owner.StopMovement();
                    _isDashing = false;
                }
            }
        }

        private void Summon(Stage stage)
        {
            for (int i = 0; i < _summonPositions.Count; i++)
            {
                stage.CreateMonster(_owner.Alliance, _summonCharacterType,
                MonsterInstanceInitialData.CreateForStageMonster(_summonPositions[i],
                hpWeight: _hpWeight,
                attackPowerWeight: _attackPowerWeight,
                dropExp: 0,
                dropGolds: 0,
                dropItems: new List<DropItemType>()),
                isBoss: false,
                isElite: false);
            }
        }
        public override ActionBase End(Stage stage)
        {
            return null;
        }
    }
}
