using Shared.StaticDatas;
using UnityEngine;

namespace Z.GameClients
{
    public static class StageStaticDataExtension
    {
        public static Rect GetWalkableArea(this StageStaticData staticData)
        {
            return new Rect(-0.5f * staticData.Width, -0.5f * staticData.Height, staticData.Width, staticData.Height);
        }
    }

}
