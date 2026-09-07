using Shared.GameDataTypes;
using Shared.StaticDatas;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.PCs;

namespace SamMul.GameClients.Stages.StageEvents
{
    public interface IStageTimerControllable
    {
        public bool StartStageTimer(float startRunningTime);
        public void PauseStageTimer(StageEventBase pauser);
        public void ResumeStageTimer(StageEventBase pauser);
    }

    public class StageEventController : IStageTimerControllable
    {
        private readonly IReadOnlyList<StageEventExecutionInfo> _stageEventExecutionInfos;

        // 스테이지 이벤트를 제어하는 기준 시간.
        // 게임 진행에 따라 멈추었다가 재개되기도 한다. (보스등장하면 멈춤)
        private float _stageRunningTime;
        private readonly float _maxStageTime;
        private bool _isStageTimerEnabled;
        private int _prevExecutionIndex;
        private StageEventBase _stageEventControllerPauser;

        public float StageRunningTime => _stageRunningTime;
        public float MaxStageTime => _maxStageTime;

        public bool IsStageClear => StageRunningTime >= _maxStageTime + 1;

        // 스테이지의 로직 구현을 위해
        // 보스 스폰 이벤트에 대한 레퍼런스만 따로 모아둔다. 원본은 _stageEventExecutionInfos에 존재한다.
        private IReadOnlyList<StageEventExecutionInfo> _bossSpawnEventReferencesForGameLogic;

        public StageEventController(IReadOnlyList<StageEventStaticData> stageEventStaticDatas, StageStaticData stageStaticData)
        {
            var stageEventExecutionInfos = new List<StageEventExecutionInfo>();
            var bossSpawnEventReferencesForGameLogic = new List<StageEventExecutionInfo>();

            _prevExecutionIndex = 0;

            for (int i = 0; i < stageEventStaticDatas.Count; i++)
            {
                // 스테이지 이벤트 데이터 생성
                // 엘리트 몬스터 소환 이벤트는 일반 몬스터 소환 이벤트와 다른점이 없다. 
                // 데이터 테이블 단계에서 구분하기 편하게 하기 위해서 다음과 같이 분리되었다.
                var stageEventStaticData = stageEventStaticDatas[i];
                StageEventBase stageEventData = null;
                switch (stageEventStaticData.EventType)
                {
                    case StageEventType.MonsterSpawn:
                        stageEventData = new MonsterSpawnStageEvent(stageEventStaticData);
                        break;
                    case StageEventType.EliteSpawn:
                        stageEventData = new EliteSpawnStageEvent(stageEventStaticData);
                        break;
                    case StageEventType.MonsterMassSpawn:
                        stageEventData = new MonsterMassSpawnStageEvent(stageEventStaticData);
                        break;
                    case StageEventType.AutoRespawnMonster:
                        stageEventData = new AutoRespawnMonsterStageEvent(stageEventStaticData);
                        break;
                    case StageEventType.BossSpawn:
                        stageEventData = new BossSpawnStageEvent(stageTimerController: this, stageEventStaticData, stageStaticData);
                        break;
                    case StageEventType.RushWarning:
                        stageEventData = new RushWarningStageEvent(stageEventStaticData);
                        break;
                    case StageEventType.BossWarning:
                        stageEventData = new BossWarningStageEvent(stageEventStaticData);
                        break;
                    case StageEventType.StageEnterInit:
                        stageEventData = new StageEnterInitEvent(stageEventStaticData);
                        break;
                    case StageEventType.MonsterSpawnToTop:
                        stageEventData = new MonsterSpawnToTopStageEvent(stageEventStaticData);
                        break;
                    case StageEventType.MonsterSpawnToBottom:
                        stageEventData = new MonsterSpawnToBottomStageEvent(stageEventStaticData);
                        break;
                    case StageEventType.MeteorSpawn:
                        stageEventData = new MeteorSpawnStageEvent(stageEventStaticData);
                        break;
                    case StageEventType.LightningSpawn:
                        stageEventData = new LightningSpawnStageEvent(stageEventStaticData);
                        break;
                    case StageEventType.SandAreaEffectSpawn:
                        stageEventData = new SandAreaEffectSpawnStageEvent(stageEventStaticData);
                        break;
                    case StageEventType.IceAreaEffectSpawn:
                        stageEventData = new IceAreaEffectSpawnStageEvent(stageEventStaticData);
                        break;
                    case StageEventType.StageEffectMeteorWarning:
                        stageEventData = new StageEffectWarningStageEvent(stageEventStaticData, WarningPopup.WarningType.StageEffectMeteor);
                        break;
                    case StageEventType.StageEffectIceAreaWarning:
                        stageEventData = new StageEffectWarningStageEvent(stageEventStaticData, WarningPopup.WarningType.StageEffectIceArea);
                        break;
                    case StageEventType.StageEffectSandAreaWarning:
                        stageEventData = new StageEffectWarningStageEvent(stageEventStaticData, WarningPopup.WarningType.StageEffectSandArea);
                        break;
                    case StageEventType.StageEffectLightningWarning:
                        stageEventData = new StageEffectWarningStageEvent(stageEventStaticData, WarningPopup.WarningType.StageEffectLightning);
                        break;
                    default:
                        Debug.LogWarning("stageEventType이 잘못되었습니다.");
                        break;
                }

                // 스테이지 이벤트 호출 정보 생성
                if (stageEventData != null)
                {
                    {
                        var beginEventExecution = new StageEventExecutionInfo(
                                stageEventData, priorityIndex: i, time: stageEventData.BeginAt,
                                callType: StageEventExecutionInfo.StageEventCallType.Begin,
                                stageEventTickNumber: 0);

                        stageEventExecutionInfos.Add(beginEventExecution);
                        if (stageEventStaticData.EventType == StageEventType.BossSpawn)
                        {
                            bossSpawnEventReferencesForGameLogic.Add(beginEventExecution);
                        }
                    }

                    for (int j = 0; j < stageEventData.StageEventTickMaxNumber; j++)
                    {
                        var updateEvent = new StageEventExecutionInfo(
                                stageEventData, priorityIndex: i, time: stageEventData.BeginAt + j * stageEventStaticData.TickPeriod,
                                callType: StageEventExecutionInfo.StageEventCallType.Update,
                                stageEventTickNumber: j);
                        stageEventExecutionInfos.Add(updateEvent);
                    }

                    if (stageEventStaticData.EventType != StageEventType.BossSpawn)
                    {
                        var endEvent = new StageEventExecutionInfo(
                                stageEventData, priorityIndex: i, time: stageEventData.EndAt,
                                callType: StageEventExecutionInfo.StageEventCallType.End, stageEventTickNumber: 0);
                        stageEventExecutionInfos.Add(endEvent);
                    }
                }
            }
            //스테이지 이벤트 호출 리스트 정렬
            stageEventExecutionInfos.Sort();

            _stageEventExecutionInfos = stageEventExecutionInfos;
            _bossSpawnEventReferencesForGameLogic = bossSpawnEventReferencesForGameLogic;

            _maxStageTime = _stageEventExecutionInfos[_stageEventExecutionInfos.Count - 1].ExecutingAt;
        }

        public bool StartStageTimer(float startRunningTime)
        {
            _isStageTimerEnabled = true;
            _stageRunningTime = startRunningTime;

            bool isInitEventSkipped = this.ForceMoveExecutionIndex(startRunningTime);
            return isInitEventSkipped;
        }

        public void PauseStageTimer(StageEventBase pauser)
        {
            if (_stageEventControllerPauser != null)
            {
                Debug.LogError("StageEventController가 이미 멈춰있습니다. PauseStageTimer 처리가 중복요청되면 안 됩니다. 상황파악 후 수정해주세요.");
            }

            _isStageTimerEnabled = false;
            _stageEventControllerPauser = pauser;
        }

        public void ResumeStageTimer(StageEventBase pauser)
        {
            if (_stageEventControllerPauser == null)
            {
                Debug.LogError("StageEventController가 비어있습니다. ResumeStageTimer 처리가 중복요청되면 안 됩니다. 상황 파악 후 수정해주세요.");
            }
            _isStageTimerEnabled = true;
            _stageEventControllerPauser = null;
        }

        public void Update(Stage stage)
        {
#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("UpdateStageEvents"))
#endif
            {
                if (_isStageTimerEnabled)
                {
                    _stageRunningTime += Time.deltaTime;
                    int currentExecutionIndex = _prevExecutionIndex;

                    while (currentExecutionIndex < _stageEventExecutionInfos.Count &&
                        _stageEventExecutionInfos[currentExecutionIndex].ExecutingAt < _stageRunningTime)
                    {
                        var stageEventExecutionInfo = _stageEventExecutionInfos[currentExecutionIndex];
                        switch (stageEventExecutionInfo.CallType)
                        {
                            case StageEventExecutionInfo.StageEventCallType.Begin:
                                stageEventExecutionInfo.StageEvent.Begin(stage);
                                break;
                            case StageEventExecutionInfo.StageEventCallType.Update:
                                stageEventExecutionInfo.StageEvent.Update(stage, stageEventExecutionInfo.StageEventTickNumber);
                                break;
                            case StageEventExecutionInfo.StageEventCallType.End:
                                stageEventExecutionInfo.StageEvent.End(stage);
                                break;
                        }
                        currentExecutionIndex++;

                        if (!_isStageTimerEnabled)
                        {
                            break;
                        }
                    }
                    _prevExecutionIndex = currentExecutionIndex;
                }
                else
                {
                    if (_stageEventControllerPauser != null)
                    {
                        //stageEventTicknumber는 일시정지 시 의미없는 값 이므로 -1 전달
                        _stageEventControllerPauser.Update(stage, stageEventTickNumber: -1);
                    }
                }
            }
        }

        public void InitializeStageEnterEvent(Stage stage)
        {
            // NOTE: StaticData에서 StageEnterInitEvent는 BeginAt을 float.MinValue로 잡아 첫 요소로 들어올수 있게 했습니다.
            // 아니라면 수정 필요.
            StageEnterInitEvent stageEnterInitEvent = (StageEnterInitEvent)_stageEventExecutionInfos.First().StageEvent;
            stageEnterInitEvent.StageEnterInitialize(stage);
        }

        /// <summary>
        /// <paramref name="targetRunningTime"/>에 지정된 스테이지 시각으로 스테이지 이벤트 실행점을 이동합니다.
        /// </summary>
        /// <returns>StageEnterInitEvent가 생략되었는지 여부를 리턴합니다. true면 생략된 것임.
        /// 생략된 경우, 별도로 플레이어 입장 처리 <see cref="Stages.Characters.PCs.PlayerCharacter.OnEnterredIntoStage"/> 해줘야 초기화가 올바로 동작됨.</returns>
        public bool ForceMoveExecutionIndex(float targetRunningTime)
        {
            bool isStageEnterInitEventSkipped;

            if (targetRunningTime < 0.1f)
            {
                _prevExecutionIndex = 0;
                isStageEnterInitEventSkipped = false;
                return isStageEnterInitEventSkipped;
            }

            // 이동한 시점으로부터 10초 전까지의 이벤트들은 모두 다시 실행시켜준다.
            targetRunningTime -= 10f;

            // 이동하게되면 InitEvent호출은 생략하고 다음것부터 시작합니다.
            int index = 2;
            isStageEnterInitEventSkipped = true;

            while (_stageEventExecutionInfos[index].ExecutingAt < targetRunningTime)
            {
                index++;
            }
            _prevExecutionIndex = index;

            return isStageEnterInitEventSkipped;
        }

        /// <summary>
        /// 테스트용. 스테이지 시각을 <paramref name="runningTime"/> 으로 옮기고 그 시점부터 이벤트를 다시 실행한다.
        /// 타이머가 멈춰 있으면(보스전 진행 중 등) 이동하지 않고 false 를 돌려준다.
        /// </summary>
        public bool TryJumpToRunningTime(float runningTime)
        {
            if (!_isStageTimerEnabled)
            {
                return false;
            }

            _stageRunningTime = runningTime;
            this.ForceMoveExecutionIndex(runningTime);
            return true;
        }

        public void GetBossSpawnTimes(in List<float> bossSpawnStartTimes)
        {
            foreach (var bossSpawnEventExecution in _bossSpawnEventReferencesForGameLogic)
            {
                bossSpawnStartTimes.Add(bossSpawnEventExecution.ExecutingAt);
            }
        }

        public float GetLastBossSpawnTime()
        {
            if (!_bossSpawnEventReferencesForGameLogic.Any())
            {
                return -1f;
            }

            return _bossSpawnEventReferencesForGameLogic.Last().ExecutingAt;
        }

        public int GetTotalBossAmount()
        {
            if (!_bossSpawnEventReferencesForGameLogic.Any())
            {
                return 0;
            }
            return _bossSpawnEventReferencesForGameLogic.Count();
        }
    }
}