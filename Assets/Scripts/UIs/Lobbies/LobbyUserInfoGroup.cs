#nullable enable
using Shared.GameDataTypes;
using Shared.StaticDatas;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.UIs.Lobbies
{
    public class LobbyUserInfoGroup : MonoBehaviour
    {
        [SerializeField] private Image _profileImage;
        [SerializeField] private TextMeshProUGUI _nickname;
        [SerializeField] private Image _expInImage;
        [SerializeField] private TextMeshProUGUI _level;
        [SerializeField] private RectTransform _statUpStartTransform;
        [SerializeField] private Image _elementIcon;

        public RectTransform statUpStartRectTransform { get { return _statUpStartTransform; } }

        public void Initialize(HeroType selectedHero, string nickname, int accountLevel, long accountExp)
        {
            Debug.Assert(_profileImage);
            Debug.Assert(_nickname);
            Debug.Assert(_level);

            this.UpdateProfileImage(selectedHero);
            this.UpdateNickname(nickname);
            this.UpdateAccountLevel(accountLevel, accountExp);
        }

        public void UpdateProfileImage(HeroType selectedHero)
        {
            var profileImagePath = StaticDataRepository.Instance.CharacterImagePaths.Get(selectedHero).DatachipIconPath;
            _profileImage.sprite = ResourcePool.Instance.LoadResource<Sprite>(profileImagePath);

            var heroData = StaticDataRepository.Instance.Heroes.Get(selectedHero);
            _elementIcon.sprite = ResourcePool.Instance.LoadResource<Sprite>(heroData.ElementType.IconPath());
        }

        public void UpdateNickname(string userName)
        {
            var tokens = userName.Split('#');
            if (tokens.Length <= 0)
            {
                _nickname.text = userName;
            }

            // #4124 형식의 숫자 자릿수 # 문자를 포함해서
            int numberPartLength = tokens.Last().Length + 1;
            _nickname.text = userName.Substring(0, userName.Length - numberPartLength);
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
