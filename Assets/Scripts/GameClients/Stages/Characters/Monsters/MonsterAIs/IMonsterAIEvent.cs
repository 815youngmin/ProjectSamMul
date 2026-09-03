using UnityEngine;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies;

namespace Z.GameClients.Stages.Characters.Monsters.MonsterAIs
{
    /// <summary>
    /// 몬스터에서 AI관련 이벤트가 발생하면, AIController에 이벤트를 전달해주기 위한 인터페이스
    /// </summary>
    public interface IMonsterAIEvent
    {
        void OnEnterredIntoStage(Stage stage);
        // NOTE: 누가때렸는지 정보를 파라미터로 받아와야하는데, 파라미터로 받을 방법이 요원하다.
        // 컴뱃시스템을 크게 수정해야함. 일단 파라미터 생략한다.
        void OnHitted(Stage stage, Character attacker);
        void OnDead(Stage stage);
        void OnDisappearing(Stage stage);
        void Update(Stage stage);
        void OnRePosition(Stage stage);
    }


    public interface ISummonedMonsterCommandSender
    {
        void SendMoveCommand(Stage stage, Vector2 targetPos);

        void SendIdleCommand(Stage stage, Vector2 direction);

        void SendChainCommand(Character chainTarget);

        void SendForceKillSelfCommand(Stage stage);

        void SendUpdateDrawOrderCommand();

        void SendColliderEnable(bool enable);

        //해당 기능을 사용하면 ISummonedMonsterCommandSender 기능을 사용할 수 없습니다.
        void SendChangeMonsterAIStrategyCommand(Stage stage, MonsterAIStrategyBase monsterAIStrategy);
    }

}
