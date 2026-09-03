#nullable enable
using UnityEngine;

namespace SamMul.UnityHelpers
{
    public static class Vector2Extension
    {
        /// <summary><paramref name="baseDirection"/>에 직교하는 왼쪽 또는 오른쪽 방향 벡터를 돌려준다.</summary>
        public static Vector2 GetOrthogonalVector(this Vector2 baseDirection, bool left)
        {
            return left
                ? new Vector2(-baseDirection.y, baseDirection.x)
                : new Vector2(baseDirection.y, -baseDirection.x);
        }
    }
}
