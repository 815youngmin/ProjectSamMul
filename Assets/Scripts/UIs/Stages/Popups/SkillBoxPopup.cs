#nullable enable
using System;
using System.Text;
using Shared.GameDataTypes;
using Shared.Localizers;
using Shared.StaticDatas;
using TMPro;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.PCs;

namespace SamMul.UIs.Stages.Popups
{
    /// <summary>
    /// 스킬 상자를 열었을 때 습득한 스킬 목록을 보여주는 팝업 (최소 구현).
    /// 습득 자체는 팝업이 열리기 전에 <see cref="PlayerCharacter"/>가 처리하고, 이 팝업은 결과만 보여준다.
    /// </summary>
    public class SkillBoxPopup : BasePopup
    {
        public static readonly string PREFAB_PATH = "Stages/UIs/Popups/SkillBoxPopup/SkillBoxPopup.prefab";

        [SerializeField] private TextMeshProUGUI _titleText = null!;
        [SerializeField] private TextMeshProUGUI _skillListText = null!;
        [SerializeField] private ZButton _okButton = null!;

        public void Initialize(PlayerCharacter owner, SkillKey[] acquiredSkills, SkillKey[] candidateSkills, Action<bool> closeRequester)
        {
            base.InitializeBase(closeRequester);

            _titleText.text = Localizer.Instance.GetText("UI_SKILLBOX_POPUP_TITLE");

            var builder = new StringBuilder();
            foreach (var skillKey in acquiredSkills)
            {
                var skill = StaticDataRepository.Instance.Skills.Get(skillKey);
                builder.AppendLine($"{skill.Name}  Lv.{skillKey.Level}");
            }
            _skillListText.text = builder.ToString();

            _okButton.onClick.RemoveAllListeners();
            _okButton.onClick.AddListener(() => this.Close(skipAnimation: false));
        }
    }
}
