#nullable enable
using Shared.GameDataTypes;
using Shared.StaticDatas;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.ResourcePools;
using SamMul.UnityHelpers;

namespace SamMul.UIs.Lobbies
{
    public class LobbyUserInfoGroup : MonoBehaviour
    {
        [SerializeField] private Image _profileImage;
        [SerializeField] private Image _expInImage;
        [SerializeField] private TextMeshProUGUI _level;
        [SerializeField] private RectTransform _statUpStartTransform;
        [SerializeField] private Image _elementIcon;

        public RectTransform statUpStartRectTransform { get { return _statUpStartTransform; } }

        public void Initialize(HeroType selectedHero, int accountLevel, long accountExp)
        {
            Debug.Assert(_profileImage);
            Debug.Assert(_level);

            this.UpdateProfileImage(selectedHero);
            this.UpdateAccountLevel(accountLevel, accountExp);
        }

        public void UpdateProfileImage(HeroType selectedHero)
        {
            var profileImagePath = StaticDataRepository.Instance.CharacterImagePaths.Get(selectedHero).DatachipIconPath;
            _profileImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(profileImagePath);

            var heroData = StaticDataRepository.Instance.Heroes.Get(selectedHero);
            _elementIcon.sprite = ResourcePool.Instance.LoadResource<Sprite>(heroData.ElementType.IconPath());
        }


        public void UpdateAccountLevel(int accountLevel, long currentExp)
        {
            var expForLevelUp = StaticDataRepository.Instance.AccountLevels.Get(accountLevel).ExpForLevelUp;

            _level.text = $"Lv {accountLevel}";

            float expRate = (float)currentExp / (float)expForLevelUp;
            _expInImage.fillAmount = Mathf.Clamp(expRate, 0.0f, 1.0f);
        }
    }
}
