using Shared.StaticDatas;
using UnityEngine;
using Z.GameClients.Stages.Characters.Stats;
using Z.GameClients.Stages.CombatSystems;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    public class GluttonSkill : SkillBase
    {
        private float _attackPowerRate;
        private int _totalAmount;   //한번에 최대 던지는 칼날드론 개수
        private float _leftAmount;  //이번 활성화에 던져야 되는 남아있는 칼날드론 개수
        private float _gluttonObjectCreatePeriod;
        private float _lastGluttonObjectCreateAt;
        private float _playerAttackRangeDistanceRatio;
        private float _knockBackPower;
        private float _attackRangeRatio;

        private readonly IReadOnlyCharacterStatCalculators _characterStats;

        public override float Duration => base.Duration / _characterStats.SkillAttackSpeedValue;
        public override float Cooltime => base.Cooltime / _characterStats.SkillAttackSpeedValue;

        private static readonly float TargetSearchRadius = 15;

        public GluttonSkill(SkillStaticData staticData, IReadOnlyCharacterStatCalculators characterStats) : base(staticData)
        {
            _attackPowerRate = staticData.Parameter1;
            _totalAmount = (int)staticData.Parameter2;
            _knockBackPower = staticData.Parameter3;
            _attackRangeRatio = staticData.Parameter4;

            _characterStats = characterStats;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            _leftAmount = _totalAmount;
            _playerAttackRangeDistanceRatio = ((PlayerCharacter)owner).Stats.AttackRangeDistanceRatio.Value;
            _gluttonObjectCreatePeriod = Duration / _totalAmount;

        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);

            //혹시나 남아있는 칼날 드론 오브젝트를 생성해준다
            for(int i = 0; i < _leftAmount; i++)
            {
                Vector2 targetDir = this.GetClosestTargetDirection(owner, stage);
                this.CreateGluttonObjectAndInitialize(owner, stage, targetDir);
            }
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if(_leftAmount <=0)
            {
                return;
            }
            if(now <  _lastGluttonObjectCreateAt + _gluttonObjectCreatePeriod )
            {
                return;
            }
            _lastGluttonObjectCreateAt = now;

            if(IsTranscendent)
            {
                _leftAmount = 0;
            }
            else
            {
                _leftAmount--;
            }

            Vector2 targetDir = this.GetClosestTargetDirection(owner, stage);
            this.CreateGluttonObjectAndInitialize(owner, stage, targetDir);
        }

        private void CreateGluttonObjectAndInitialize(PlayerCharacter owner, Stage stage, Vector2 targetDir)
        {
            PlaySkillSoundEffect(owner.Pos);
            float damage = CombatSystem.CalculateSkillAttackDamage(owner.Stats,_attackPowerRate);
            float objectRadius = 1.1f * _playerAttackRangeDistanceRatio * _attackRangeRatio;
            float movingSpeed = 20f * _characterStats.ProjectileMoveSpeedIncreaseRateValue;
            float oppositeDirectionAcceleration = 30f * _characterStats.ProjectileMoveSpeedIncreaseRateValue;
            Vector3 gluttonObjectScale = Vector3.one * _playerAttackRangeDistanceRatio * _attackRangeRatio;
            float knockbackPower = CombatSystem.CalculateSkillAttackKnockBackPower(owner.Stats, _knockBackPower);

            if(IsTranscendent)
            {
                for(int i = 0; i < _totalAmount; i++)
                {
                    var gluttonObject = stage.CreateGluttonObject(
                            owner.Alliance,
                            owner,
                            objectRadius,
                            Quaternion.Euler(0,0, 360f /_totalAmount * i) * Vector2.up,
                            movingSpeed,
                            oppositeDirectionAcceleration,
                            damage,
                            knockbackPower,
                            lifeTime: 5f,
                            this.IsTranscendent,
                            StaticData.SkillHitSFXPath);
                    gluttonObject.transform.localScale = gluttonObjectScale;
                }
            }
            else
            {
                var gluttonObject = stage.CreateGluttonObject(
                                            owner.Alliance,
                                            owner,
                                            objectRadius,
                                            targetDir,
                                            movingSpeed,
                                            oppositeDirectionAcceleration,
                                            damage,
                                            knockbackPower,
                                            lifeTime: 5f,
                                            this.IsTranscendent,
                                            StaticData.SkillHitSFXPath);
                gluttonObject.transform.localScale = gluttonObjectScale;
            }

        }

        // 몬스터 → 아이템 박스 → 랜덤 순으로 방향 반환
        private Vector2 GetClosestTargetDirection(PlayerCharacter owner, Stage stage)
        {
            Vector2 direction;
            var monster =  stage.FindClosestCharacter(
                owner.Alliance.ToEnemyAlliance(),
                owner.CenterPos,
                limitDistance: TargetSearchRadius,
                condition: character => !character.Action.IsDead && !character.IsImmuneToHit);

            if(monster != null)
            {
                direction =  monster.CenterPos - owner.CenterPos;
            }
            else
            {
                var item = owner. FindClosestBreakableItemObjectExceptFence(stage, TargetSearchRadius);
                if (item == null)
                {
                    direction = owner.CenterPos + Random.insideUnitCircle;
                }
                else
                {
                    direction = (Vector2)item.transform.position - owner.CenterPos;
                }

            }

            direction.Normalize();
            return direction;
        }
    }
}
