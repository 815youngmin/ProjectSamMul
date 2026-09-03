using Shared.StaticDatas;
using UnityEngine;

namespace SamMul.GameClients.Stages.StageEvents
{
    public abstract class StageEventBase
    {
        public readonly int StageNumber;
        public readonly float BeginAt;
        public readonly float EndAt;
        public readonly int StageEventTickMaxNumber;
        private readonly float _targetCameraOrthographicSize;
        public float TargetCameraOrthographicSize => _targetCameraOrthographicSize;

        protected readonly StageEventStaticData _stageEventStaticData;

        public StageEventBase(StageEventStaticData stageEventStaticData)
        {
            Debug.Assert(stageEventStaticData.BeginAt <= stageEventStaticData.EndAt);

            _stageEventStaticData = stageEventStaticData;
            this.StageNumber = stageEventStaticData.StageNumber;
            this.BeginAt = stageEventStaticData.BeginAt;
            this.EndAt = stageEventStaticData.EndAt;

            this._targetCameraOrthographicSize = stageEventStaticData.CameraSize;

            if(stageEventStaticData.TickPeriod == 0)
            {
                this.StageEventTickMaxNumber = 0;
            }
            else
            {
                this.StageEventTickMaxNumber = (int)((stageEventStaticData.EndAt - stageEventStaticData.BeginAt) / stageEventStaticData.TickPeriod);
            }
        }

        public abstract void Update(Stage stage, int stageEventTickNumber);
        public virtual void Begin(Stage stage)
        {
            GameClient.CameraController.SetOrthographicSize(_targetCameraOrthographicSize);
        }
        public abstract void End(Stage stage);
    }
}
