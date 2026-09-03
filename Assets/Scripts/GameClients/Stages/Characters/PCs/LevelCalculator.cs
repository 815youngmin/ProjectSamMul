using Shared.StaticDatas;
using System.Diagnostics;
using Unity.Mathematics;

namespace Z.GameClients.Stages.Characters.PCs
{
    public class LevelCalculator
    {
        public int Level { get; private set; }

        // 이번 레벨에서 획득한 경험치임. (레벨1부터 얻은 경험치 총량이 아님)
        private long _exp;
        public long CurrentExp => _exp;

        //올라가야되는 예약된 경험치량
        //한번에 많은량의 경험치를 전달하면 이쁘지 않아 매 프레임 적당량을 전달한다.
        //전달된 경험치량 만큼 줄어든다.
        private long _reservedExp;

        // 다음 레벨업을 위해 필요한 경험치 양
        private long _expToNextLevel;
        public long ExpToNextLevel => _expToNextLevel;

        public LevelCalculator()
        {
            Level = 1;
            _exp = 0;
            _expToNextLevel = StaticDataRepository.Instance.ExpTable.GetExpForLevelUp(1);
        }

        public void ReserveToGainExp(long increment)
        {
            _reservedExp += increment;
        }

        /// <summary>
        /// 예약된 경험치를 확인하고 매 프레임 적당량 경험치에게 전달해준다.
        /// </summary>
        public void CalculateAndConveyExp()
        {
            if(_reservedExp <= 0)
            {
                return;
            }

            long maximumExp = _expToNextLevel / 20;
            long minimumExp = 5;

            if(_reservedExp < minimumExp)
            {
                _exp += _reservedExp;
                _reservedExp = 0;
            }
            else
            {
                long conveyExp = _reservedExp / 10;
                if(maximumExp < conveyExp)
                {
                    _exp += maximumExp;
                    _reservedExp -= maximumExp;
                }
                else
                {
                    _exp += conveyExp;
                    _reservedExp -= conveyExp;
                }
            }
        }

        /// <summary>
        /// 경험치를 확인하고, 레벨업 가능하면 레벨업 시켜준다.
        /// 단 하나의 레벨업 처리만 한다. 레벨업 가능한지 반복해서 확인해야 한다.
        /// </summary>
        public bool CheckAndIncreaseOneLevel()
        {
            if (_exp < _expToNextLevel)
            {
                return false;
            }

            _exp -= _expToNextLevel;
            Level += 1;
            _expToNextLevel = StaticDataRepository.Instance.ExpTable.GetExpForLevelUp(Level);

            return true;
        }
    }

}
