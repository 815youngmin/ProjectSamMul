using Shared.DataTables;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using System;
using UnityEngine;
using UnityEngine.UI;
using SamMul.ResourcePools;
using TMPro;

namespace SamMul.UIs.Stages.Popups
{
    public class PauseMenuSkillIcon : MonoBehaviour
    {
        [SerializeField] private Image _skillIcon;
        [SerializeField] private Image _background;
        [SerializeField] private TextMeshProUGUI _skillLevelText;

        private static readonly string ACTIVE_SKILL_BACKGROUND = "Stage/UIs/PauseMenuPopup/ActiveSkillBackground.png";
        private static readonly string PASSIVE_SKILL_BACKGROUND = "Stage/UIs/PauseMenuPopup/PassiveSkillBackground.png";
        private static readonly string TRANSCENDENT_SKILL_BACKGROUND = "Stage/UIs/PauseMenuPopup/TranscendentSkillBackground.png";


        public void Initialize(SkillStaticData skillStaticData)
        {
            if (skillStaticData == null || skillStaticData.Id == SkillId.Invalid)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            _skillIcon.sprite = ResourcePool.Instance.LoadResource<Sprite>(skillStaticData.IconResourcePath);
            _skillLevelText.text = skillStaticData.Level.ToString();
            switch (skillStaticData.skillType)
            {
                case SkillType.Active:
                    {
                        _background.sprite = ResourcePool.Instance.LoadResource<Sprite>(GameConstants.SKILL_TRANSCENDENT_LEVEL == skillStaticData.Level ?
                            TRANSCENDENT_SKILL_BACKGROUND : ACTIVE_SKILL_BACKGROUND);
                    }
                    break;
                case SkillType.Passive:
                    {
                        _background.sprite = ResourcePool.Instance.LoadResource<Sprite>(PASSIVE_SKILL_BACKGROUND);
                    }
                    break;
                default:
                    {
                        throw new NotImplementedException("비정상적인 Type입니다. 확인해주세요.");
                    }
            }
        }
    }
}
