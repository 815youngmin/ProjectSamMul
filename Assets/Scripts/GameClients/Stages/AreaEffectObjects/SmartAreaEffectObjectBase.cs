using System;
using UnityEngine;
using SamMul.GameClients.Stages.Characters;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    public abstract class SmartAreaEffectObjectBase : AreaEffectObjectBase
    {

        public struct AreaEffectRecurringActionParameter
        {
            public Stage stage;
            public AreaEffectObjectBase areaEffectObject;

            public AreaEffectRecurringActionParameter(Stage stage, AreaEffectObjectBase areaEffectObject)
            {
                this.stage = stage;
                this.areaEffectObject = areaEffectObject;
            }
        }

        private RecurringActionManager<AreaEffectRecurringActionParameter> _recurringActionManager;

        protected void AllocateSharedResourcesForSmartBase(AreaEffectType type)
        {
            base.AllocateSharedResourcesForBase(type);
            _recurringActionManager = new RecurringActionManager<AreaEffectRecurringActionParameter>();
        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            float now = Time.time;
            _recurringActionManager.Update(now, new AreaEffectRecurringActionParameter(stage, this));
        }

        public float AddOneOffAction(float beginAt, Action<AreaEffectRecurringActionParameter> recurringAction)
            => _recurringActionManager.AddAction(beginAt, 0.0f, 0.0f, recurringAction);

        public float AddDurationalAction(float beginAt, float duration, Action<AreaEffectRecurringActionParameter> recurringAction)
            => _recurringActionManager.AddAction(beginAt, duration, 0.0f, recurringAction);

        public float AddIntervalDurationalAction(float beginAt, float duration, float interval, Action<AreaEffectRecurringActionParameter> recurringAction)
            => _recurringActionManager.AddAction(beginAt, duration, interval, recurringAction);

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
            _recurringActionManager.ClearAction();
        }
    }
}
