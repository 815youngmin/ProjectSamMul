using DG.Tweening;
using Shared.GameDataTypes;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters.Animations;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.CombatSystems;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.Characters.Actions
{
    public class MultipleReflectionRangeAttackAndMonsterSummonAction : ActionBase
    {
        private readonly static string SummonEffectPrefabPath = "Stages/ETCEffects/monsterWarning.prefab";
        private readonly static float SummonReadyDuration = 1.5f;
        private readonly static float SummonDelay = 0.2f;

        private static readonly float ProjectileKnobackPower = 0.1f;

        private Monster _owner;
        private Character _target;

        private string _projectilePrefabPath;
        private int _projectileAmount;
        private float _projectileSpeed;
        private float _projectileLifeTime;
        private float _projectileRadius;
        private float _projectileRotateSpeed;

        private float _summonFrameAt;
        private int _summonAmount;
        private CharacterType _summonCharacterType;
        private List<Vector2> _summonPositions;
        private bool _isSummoned;
        private float _attackPowerWeight;
        private float _hpWeight;


        public MultipleReflectionRangeAttackAndMonsterSummonAction(
            Monster owner,
            Character target,
            int projectileAmount,
            float projectileSpeed,
            float projectileLifeTime,
            float projectileRadius,
            float projectileRotateSpeed,
            string projectileBodyPrefabPath,
            int summonAmount,
            float attackPowerWeight,
            float hpWeight,
            CharacterType summonCharacterType,
            MonsterAnimationController animationController
         ) : base(ActionType.Attack,
                 duration: SummonReadyDuration + SummonDelay,
                 animationController)
        {
            _target = target;
            _owner = owner;

            _projectileAmount = projectileAmount;
            _projectileSpeed = projectileSpeed;
            _projectileLifeTime = projectileLifeTime;
            _projectileRadius = projectileRadius;
            _projectileRotateSpeed = projectileRotateSpeed;
            _projectilePrefabPath = projectileBodyPrefabPath;

            _summonFrameAt = 0f;
            _summonAmount = summonAmount;
            _summonCharacterType = summonCharacterType;
            _summonPositions = new List<Vector2>();
            _isSummoned = false;
            _attackPowerWeight = attackPowerWeight;
            _hpWeight = hpWeight;
        }

        public override void Begin(Stage stage, float now)
        {
            base.Begin(stage, now);

            _animationController.SkipHitAnimation(true);
            if (_owner.MoveDir == Vector2.zero)
            {
                // 이동하지 않고 있을 경우, 공격 방향을 바라보도록 한다.
                _animationController.UpdateBodyDirectionByMoveDirection(_target.Pos - _owner.Pos);
            }
            else
            {
                _animationController.UpdateBodyDirectionByMoveDirection(_owner.MoveDir);
            }
            _animationController.PlayIdleAttackActionInfinitely();

            float spawnPositionEffectScale = 1 / 2.395f * 1.5f;
            for (int i = 0; i < _summonAmount; i++)
            {
                _summonPositions.Add(_owner.Pos + Random.insideUnitCircle * 3f);
                //애니메이션이 1초짜리입니다.
                //나중에 end이벤트에 소환을 넣던지 애니메이션 속도를 조절하던지 해야될듯
                UnityGlobal.SpriteAnimations.CreateAndPlaySpriteAnimation(SummonEffectPrefabPath, _summonPositions[i], Vector2.one * spawnPositionEffectScale, null);
            }

            float damage = _owner.RangeAttackPower;
            var rect = stage.FenceRect != null ? stage.FenceRect.Value : stage.StaticData.GetWalkableArea();
            for (int i = 0; i < _projectileAmount; i++)
            {
                Vector2 attackDirection = Quaternion.Euler(0, 0, 360 / _projectileAmount * i) * Vector2.up;

                stage.CreateReflectionAreaEffectObject(
                    _owner,
                    _projectileRadius,
                    _owner.CenterPos,
                    attackDirection,
                    _projectileSpeed,
                    _projectileRotateSpeed,
                    damage,
                    ProjectileKnobackPower,
                    _projectileLifeTime,
                    rect,
                    _projectilePrefabPath
                    );
            }

            _summonFrameAt = now + SummonReadyDuration;
        }

        public override void Update(Stage stage, float deltaTime, float now)
        {
            if(_summonFrameAt <= now)
            {
                this.Summon(stage);
                _isSummoned = true;
                _summonFrameAt = float.MaxValue;
            }
        }

        public override bool Cancel(Stage stage)
        {
            if (_isSummoned == false)
            {
                float now = Time.time;
                float atPosition = _summonFrameAt - now;
                DOTween.Sequence().InsertCallback(atPosition, () =>
                {
                    this.Summon(stage);
                });
                _isSummoned = true;
            }
            return base.Cancel(stage);
        }

        public override ActionBase End(Stage stage)
        {
            if(_isSummoned == false)
            {
                float now = Time.time;
                float atPosition = _summonFrameAt - now;
                DOTween.Sequence().InsertCallback(atPosition, () =>
                {
                    this.Summon(stage);
                });
                _isSummoned = true;
            }
            return null;
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

