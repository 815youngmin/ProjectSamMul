using DG.Tweening;
using Shared.GameDataTypes;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Animations;
using SamMul.GameClients.Stages.Characters.Monsters;
using SamMul.UnityHelpers;

namespace SamMul.GameClients.Stages.Characters.Actions
{
    public class MultipleHorizontalRangeAttackAndMonsterSummonAction : ActionBase
    {
        private readonly float ProjectileSpeed = 10.0f;
        private readonly float AliveDistance = 100.0f;

        private readonly static string SummonEffectPrefabPath = "Stages/ETCEffects/monsterWarning.prefab";
        private readonly static float SummonReadyDuration = 1.5f;

        private readonly Character _target = null;
        private readonly Monster _owner = null;

        private readonly float _hitTimeOnAttackAnimation;
        private float _hitFrameAt;
        private bool _isHitFired;
        private Vector2 _attackDirection;
        private string _projectilePrefabPath;
        private bool _isRemovableBySpinBladeObject;
        private bool _withIndicator;

        private int _projectileAmount;      //3
        private float _fireAngle;           //0.2f

        private float _summonFrameAt;
        private int _summonAmount;
        private CharacterType _summonCharacterType;
        private List<Vector2> _summonPositions;
        private float _attackPowerWeight;
        private float _hpWeight;



        private new MonsterAnimationController _animationController => (MonsterAnimationController)base._animationController;

        public MultipleHorizontalRangeAttackAndMonsterSummonAction(
            Character target,
            Monster owner,
            int projectileAmount,
            float fireAngle,
            bool isRemovableBySpinBladeObject,
            bool withIndicator,
            string projectileBodyPrefabPath,
            int summonAmount,
            float attackPowerWeight,
            float hpWeight,
            CharacterType summonCharacterType,
            MonsterAnimationController animationController)
            : this(owner, target.Pos - owner.CenterPos, projectileAmount, fireAngle, isRemovableBySpinBladeObject, withIndicator, projectileBodyPrefabPath, 
                  summonAmount, attackPowerWeight, hpWeight, summonCharacterType, animationController)
        {
            Debug.Assert(target != null);
            _target = target;
        }

        public MultipleHorizontalRangeAttackAndMonsterSummonAction(
            Monster owner,
            Vector2 attackDirection,
            int projectileAmount,
            float fireAngle,
            bool isRemovableBySpinBladeObject,
            bool withIndicator,
            string projectileBodyPrefabPath,
            int summonAmount,
            float attackPowerWeight,
            float hpWeight,
            CharacterType summonCharacterType,
            MonsterAnimationController animationController)
            : base(ActionType.Attack, animationController.AttackAnimationDuration, animationController)
        {
            _owner = owner;
            _hitTimeOnAttackAnimation = animationController.FindHitTimeOnAttackAnimation();
            _hitFrameAt = 0f;
            _isHitFired = false;
            _attackDirection = attackDirection;
            _projectilePrefabPath = projectileBodyPrefabPath;

            _projectileAmount = projectileAmount;
            _fireAngle = fireAngle;
            _isRemovableBySpinBladeObject = isRemovableBySpinBladeObject;
            _withIndicator = withIndicator;

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

            _animationController.SkipHitAnimation(true);
            _hitFrameAt = now + _hitTimeOnAttackAnimation;

            if (_target != null)
            {
                _attackDirection = _target.Pos - _owner.CenterPos;
            }

            if (_owner.MoveDir == Vector2.zero)
            {
                // 이동하지 않고 있을 경우, 공격 방향을 바라보도록 한다.
                _animationController.UpdateBodyDirectionByMoveDirection(_attackDirection);
            }
            else
            {
                _animationController.UpdateBodyDirectionByMoveDirection(_owner.MoveDir);
            }
            _animationController.PlayAttackForce(Duration);

            if (_withIndicator)
            {
                for (int i = -(_projectileAmount - 1); i <= _projectileAmount - 1; i += 2)
                {
                    Vector2 direction = Quaternion.Euler(0.0f, 0.0f, 0.5f * (float)i * _fireAngle / _projectileAmount) * _attackDirection;
                    float attackAngle = Mathf.Rad2Deg * Mathf.Atan2(direction.y, direction.x);

                    stage.CreateSquareAttackRangeIndicator(
                        center: _owner.CenterPos + 0.5f * AliveDistance * direction,
                        width: AliveDistance,
                        height: 1.0f,
                        angle: attackAngle,
                        duration: 1.0f);
                    stage.CreateDirectionalSquareRangeIndicator(
                        startPosition: _owner.CenterPos,
                        direction: direction,
                        thickness: 1.0f,
                        distance: AliveDistance,
                        duration: 1.0f);
                }
            }

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
            if (_isHitFired ||
                (now < _hitFrameAt))
            {
                return;
            }

            _isHitFired = true;

            float damage = _owner.RangeAttackPower;
            Vector2 firePosition = _owner.CenterPos;
            Vector2 direction = (_target.Pos - firePosition).normalized;

            for (int i = -(_projectileAmount - 1); i <= _projectileAmount - 1; i += 2)
            {
                Vector2 dir = Quaternion.Euler(0.0f, 0.0f, 0.5f * (float)i * _fireAngle / _projectileAmount) * direction;
                var projectile = stage.CreateProjectile(
                    _projectilePrefabPath,
                    _owner.Alliance,
                    _owner,
                    damage,
                    knockBackPower: 0.2f,
                    _owner.CenterPos,
                    dir,
                    ProjectileSpeed,
                    acceleration: 0.0f,
                    collidingRadius: 0.5f,
                    AliveDistance,
                    hitChances: 1,
                    splitCount: 0,
                    _isRemovableBySpinBladeObject,
                    hitSoundPrefabPath: string.Empty);
            }

        }

        public override ActionBase End(Stage stage)
        {
            _animationController.SkipHitAnimation(false);
            return null;
        }

        public override bool Cancel(Stage stage)
        {
            if (!base.Cancel(stage))
            {
                return false;
            }

            _animationController.SkipHitAnimation(false);
            _animationController.StopAttack();

            return true;
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

    }
}
