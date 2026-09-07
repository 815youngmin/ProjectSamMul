using UnityEngine;
using SamMul.ResourcePools;

namespace SamMul.GameClients.Stages.AreaEffectObjects
{
    /// <summary>
    /// 전용 공격 이펙트 리소스가 없는 데모에서 플레이어 스킬 오브젝트의 몸체로 쓰는 공용 비주얼.
    /// Stage/Common/PlayerAttackVisual.prefab 을 붙이고, 스프라이트 지름을 판정 지름에 맞춘다.
    /// </summary>
    public static class PlayerAttackVisual
    {
        public const string PREFAB_PATH = "Stage/Common/PlayerAttackVisual.prefab";

        /// <param name="diameter">부모 로컬 단위의 스프라이트 지름. 부모 스케일은 그대로 곱해진다.</param>
        public static GameObject Attach(Transform parent, float diameter)
        {
            var visual = ResourcePool.Instance.InstantiateFromResource(PREFAB_PATH);
            visual.transform.SetParent(parent, worldPositionStays: false);
            visual.transform.localPosition = Vector3.zero;
            SetDiameter(visual, diameter);
            return visual;
        }

        public static void SetDiameter(GameObject visual, float diameter)
        {
            var renderer = visual.GetComponentInChildren<SpriteRenderer>();
            float spriteDiameter = renderer != null && renderer.sprite != null ? renderer.sprite.bounds.size.x : 1f;
            visual.transform.localScale = Vector3.one * (diameter / Mathf.Max(spriteDiameter, 0.01f));
        }
    }
}
