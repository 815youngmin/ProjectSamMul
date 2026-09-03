#nullable enable
using System;
using TMPro;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.Monsters;

namespace SamMul.UIs.Stages.HUDs
{
    /// <summary>
    /// 몬스터 아래에 이름을 표시하는 HUD 입니다.
    /// </summary>
    public class MonsterNameDisplayer : MonoBehaviour
    {
        public static readonly string PREFAB_PATH = "Stages/UIs/HUDs/MonsterNameDisplayer/MonsterNameDisplayer.prefab";

        public enum Color { Red, Blue }

        private static readonly Color32 RED = new Color32(255, 50, 50, 255);
        private static readonly Color32 BLUE = new Color32(0, 150, 255, 255);

        [SerializeField] private TextMeshPro _arrowText = null!;
        [SerializeField] private TextMeshPro _monsterNameText = null!;

        public void Initialize(Monster owner, Color color)
        {
            Color32 textColor = color switch
            {
                Color.Red => RED,
                Color.Blue => BLUE,
                _ => throw new NotImplementedException($"{color} 색상이 정의되지 않았습니다."),
            };

            _monsterNameText.text = owner.StaticData.MonsterName;
            _monsterNameText.color = textColor;
            _arrowText.color = textColor;

            this.transform.SetParent(owner.transform);
            this.transform.localPosition = owner.ColliderRadius * 0.5f * Vector3.down;
            this.transform.localScale = 0.15f * Vector3.one;
        }
    }
}
