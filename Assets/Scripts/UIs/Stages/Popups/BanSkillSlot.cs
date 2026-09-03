using Shared.GameDataTypes;
using Shared.StaticDatas;
using UnityEngine;
using UnityEngine.UI;
using SamMul.ResourcePools;

public class BanSkillSlot : MonoBehaviour
{
    public static readonly string PREFAB_PATH = "Stages/UIs/Popups/SkillSelectorPopup/BanSkillSlot.prefab";

    [SerializeField] private Image _skillIcon;
    public void Initialize(SkillId skillId)
    {
        var skillKey = new SkillKey(skillId, 1);
        SkillStaticData skillStaticData = StaticDataRepository.Instance.Skills.Get(skillKey);

        _skillIcon.gameObject.SetActive(true);
        _skillIcon.sprite = ResourcePool.Instance.LoadResource<Sprite>(skillStaticData.IconResourcePath);
    }
}
