using Shared.GameDataTypes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SamMul.ResourcePools;

public class BanSkillGroup : MonoBehaviour
{
    [SerializeField] private List<BanSkillSlot> _banSkillSlots;
    [SerializeField] private RectTransform _slotParent;

    public void Initialize(SkillId[] banSkillIds)
    {
        for (int i = 0; i < _banSkillSlots.Count; i++)
        {
            ResourcePool.Instance.PutBackInstance(BanSkillSlot.PREFAB_PATH, _banSkillSlots[i].gameObject);
        }
        _banSkillSlots.Clear();

        if (banSkillIds != null)
        {
            foreach (var banSkillId in banSkillIds)
            {
                var banSkillSlot =  ResourcePool.Instance.InstantiateFromResource<BanSkillSlot>(BanSkillSlot.PREFAB_PATH);
                banSkillSlot.transform.SetParent(_slotParent);
                banSkillSlot.transform.localScale = Vector3.one;

                banSkillSlot.Initialize(banSkillId);
                _banSkillSlots.Add(banSkillSlot);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(_slotParent);
        }
    }
}
