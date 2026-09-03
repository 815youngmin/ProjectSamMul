using Shared.StaticDatas;
using Z.GameClients.Stages.Characters.Stats;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    public class CreatureSavingsSkill : SkillBase
    {
        private IReadOnlyCustomParameters _customParameterReader;
        private ICustomParameterChanger _customParameterChanger;

        private int _maxCreatureKillCount;
        private long _prevStageKillCount;
        private int _stackKillAmount;
        private int _maxStackValue;

        private int _currentStack;
        private int _currentKillCount;

        private StatModifier _dodgeRateIncreaser;    //회피율
        private StatModifier _moveSpeedIncresear;    //이동속도
        private StatModifier _attackSpeedIncreaser; //공격속도
        private StatModifier _skillSpeedIncreaser;  //공격속도
        private StatModifier _attackRangeDistanceIncreaser; //공격범위

        private float _dodgeRateIncreaserValue;     //flat
        private float _moveSpeedIncreaserValue;     //percentAdd
        private float _attackSpeedIncreaserValue;   //flat
        private float _skillSpeedIncreaserValue;    //flat
        private float _attackRangeDistanceIncreaserValue;//PercentAdd

        public CreatureSavingsSkill(SkillStaticData staticData, IReadOnlyCustomParameters customParameterReader, ICustomParameterChanger customParameterChanger) : base(staticData)
        {
            _customParameterReader = customParameterReader;
            _customParameterChanger = customParameterChanger;

            _dodgeRateIncreaserValue = staticData.Parameter1 * 0.01f;
            _moveSpeedIncreaserValue = staticData.Parameter2 * 0.01f;
            _attackSpeedIncreaserValue = staticData.Parameter3 * 0.01f;
            _skillSpeedIncreaserValue = staticData.Parameter3 * 0.01f;
            _attackRangeDistanceIncreaserValue = staticData.Parameter4 * 0.01f;
            _stackKillAmount = (int)staticData.Parameter5;
            _maxStackValue = (int)staticData.Parameter6;
        }

        public override void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Activate(owner, stage, now);
            _prevStageKillCount = stage.EliminatedMonsters;
            _maxCreatureKillCount = _maxStackValue * _stackKillAmount;

            this.UpdateStack();
            this.RemoveStackStat(owner);
            this.AddStackStat(owner);
            owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
        }

        public override void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            base.Deactivate(owner, stage, now);
            this.RemoveStackStat(owner);
            owner.ReApplyStatsToAcquiredSkills(stage, owner.Stats);
        }

        public override void Update(PlayerCharacter owner, Stage stage, float now)
        {
            if(_currentStack >= _maxStackValue)
            {
                //스택이 최대치면 업데이트 할 필요 없다.
                return;
            }

            //따로 킬 카운트를 체크하는 이벤트는 없어서 stage.EliminatedMonsters 변수를 활용해서 킬 카운트를 계산한다.
            if (_prevStageKillCount != stage.EliminatedMonsters)
            {
                var killCount = stage.EliminatedMonsters - _prevStageKillCount;
                var currentKillCount = _customParameterReader.GetParameterValue(CustomParameterType.CreatureSavingsKillCount);

                if (currentKillCount + killCount >= _maxCreatureKillCount)
                {
                    _customParameterChanger.ChangeParameterValue(CustomParameterType.CreatureSavingsKillCount, _maxCreatureKillCount);
                }
                else
                {
                    _customParameterChanger.ChangeParameterValue(CustomParameterType.CreatureSavingsKillCount, currentKillCount + killCount);
                }
                _prevStageKillCount = stage.EliminatedMonsters;
            }


            int prevStack = _currentStack;
            this.UpdateStack();

            //이전 스택과 현재 스택이 다르면 스탯 변경 
            if (prevStack < _currentStack)
            {
                this.RemoveStackStat(owner);
                this.AddStackStat(owner);
            }

        }

        private void UpdateStack()
        {
            _currentKillCount = (int)_customParameterReader.GetParameterValue(CustomParameterType.CreatureSavingsKillCount);
            _currentStack = _currentKillCount / _stackKillAmount;
            if (_currentStack > _maxStackValue)
            {
                _currentStack = _maxStackValue;
            }
        }

        private void RemoveStackStat(PlayerCharacter owner)
        {
            if (_dodgeRateIncreaser != null)
            {
                owner.Stats.DodgeRate.RemoveModifier(_dodgeRateIncreaser);
            }
            if (_moveSpeedIncresear != null)
            {
                owner.Stats.MoveSpeed.RemoveModifier(_moveSpeedIncresear);
            }
            if (_attackSpeedIncreaser != null)
            {
                owner.Stats.CharacterAttackSpeed.RemoveModifier(_attackSpeedIncreaser);
            }
            if (_skillSpeedIncreaser != null)
            {
                owner.Stats.SkillAttackSpeed.RemoveModifier(_skillSpeedIncreaser);
            }
            if (_attackRangeDistanceIncreaser != null)
            {
                owner.Stats.AttackRangeDistanceRatio.RemoveModifier(_attackRangeDistanceIncreaser);
            }
        }

        private void AddStackStat(PlayerCharacter owner)
        {
           
            _dodgeRateIncreaser = new StatModifier(_dodgeRateIncreaserValue * _currentStack, StatModType.Flat);
            _moveSpeedIncresear = new StatModifier(_moveSpeedIncreaserValue * _currentStack, StatModType.PercentAdd);
            _attackSpeedIncreaser = new StatModifier(_attackSpeedIncreaserValue * _currentStack, StatModType.Flat);
            _skillSpeedIncreaser = new StatModifier(_skillSpeedIncreaserValue * _currentStack, StatModType.Flat);
            _attackRangeDistanceIncreaser = new StatModifier(_attackRangeDistanceIncreaserValue * _currentStack, StatModType.PercentAdd);

            owner.Stats.DodgeRate.AddModifier(_dodgeRateIncreaser);
            owner.Stats.MoveSpeed.AddModifier(_moveSpeedIncresear);
            owner.Stats.CharacterAttackSpeed.AddModifier(_attackSpeedIncreaser);
            owner.Stats.SkillAttackSpeed.AddModifier(_skillSpeedIncreaser);
            owner.Stats.AttackRangeDistanceRatio.AddModifier(_attackRangeDistanceIncreaser);
        }
    }
}

