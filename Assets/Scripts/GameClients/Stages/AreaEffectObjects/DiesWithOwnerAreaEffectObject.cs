using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.ResourcePools;

namespace Z.GameClients.Stages.AreaEffectObjects
{
    public class DiesWithOwnerAreaEffectObject : SmartAreaEffectObjectBase
    {
        public override bool IsAlive => _owner != null && !_owner.Action.IsDead;

        private Character _owner;


        public void AllocateSharedResources()
        {
            base.AllocateSharedResourcesForSmartBase(AreaEffectType.DiesWithOwnerAreaEffectObject);
        }

        public void Initialize(
            Character owner
            )
        {
            base.InitializeAreaObject(owner.Alliance);
            _owner = owner;

        }

        public override void UpdateLogic(Stage stage, float deltaTime)
        {
            base.UpdateLogic(stage, deltaTime);
        }

        public override void PuttingBackToPool()
        {
            base.PuttingBackToPool();
        }
    }
}
