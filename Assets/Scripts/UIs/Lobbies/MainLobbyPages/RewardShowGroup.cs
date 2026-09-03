using Shared.DataTables;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using Z.Animations.Placeholder;
using UnityEngine;
using Z.UIs.Commons.Rewards;

public class RewardShowGroup : MonoBehaviour
{
    // 데모에서 제거된 정복 보상 버튼. 프리팹에 남아 있으므로 숨겨둔다.
    [SerializeField] private SkeletonGraphic _conquestRewardSpine;
    [SerializeField] private ZButton _conquestRewardButton;
    [SerializeField] private GameObject _specialReward;
    [SerializeField] private RewardItemCard _rewardItem;

    public void Initialize()
    {
        Debug.Assert(_conquestRewardSpine != null);
        Debug.Assert(_specialReward != null);

        _conquestRewardSpine.gameObject.SetActive(false);
        _conquestRewardButton.gameObject.SetActive(false);
        _specialReward.gameObject.SetActive(false);
    }

    public void ShowSpecialReward(ChapterStaticData chapterStaticData)
    {
        _specialReward.gameObject.SetActive(true);

        if (chapterStaticData.FirstClearRewardEquipmentId != EquipmentId.Invalid)
        {
            var equipmentData = StaticDataRepository.Instance.Equipments.Get(chapterStaticData.FirstClearRewardEquipmentId);
            _rewardItem.InitializeForEquipment(equipmentData.Id, equipmentData.Rarity.InitialGrade(), amount: 1);
        }
        else if (chapterStaticData.FirstClearRewardHeroType != HeroType.Invalid)
        {
            var characterData = StaticDataRepository.Instance.Heroes.Get(chapterStaticData.FirstClearRewardHeroType);
            _rewardItem.InitializeForCharacter(characterData.HeroType, characterData.Rarity.InitialGrade(), amount: 1);
        }
        else
        {
            Debug.LogError("스페셜 보상 정보가 없는데 활성화 하려 합니다. 코드 확인이 필요합니다.");
        }
    }

    public void HideSpecialReward()
    {
        _specialReward.gameObject.SetActive(false);
    }
}
