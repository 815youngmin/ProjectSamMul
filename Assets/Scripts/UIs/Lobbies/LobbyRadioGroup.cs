#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SamMul.UIs.Lobbies
{
    /// <summary>
    /// 로비의 한 줄(챕터 / 캐릭터 / 아이템). 자식 ZButton 들을 라디오 버튼처럼 다뤄 항상 하나만 선택된다.
    /// 버튼은 프리팹의 자식 순서대로 항목 인덱스에 대응한다.
    /// </summary>
    public class LobbyRadioGroup : MonoBehaviour
    {
        private static readonly Vector3 SELECTED_SCALE = Vector3.one;
        private static readonly Vector3 UNSELECTED_SCALE = Vector3.one * 0.8f;
        private static readonly Color SELECTED_COLOR = Color.white;
        private static readonly Color UNSELECTED_COLOR = new Color(0.65f, 0.65f, 0.65f, 1f);

        private ZButton[] _buttons = Array.Empty<ZButton>();
        private Action<int>? _onSelected;

        public int SelectedIndex { get; private set; } = -1;

        /// <param name="itemCount">데이터 항목 수. 프리팹의 버튼 수와 같아야 한다.</param>
        /// <param name="onSelected">선택이 바뀔 때 호출된다.</param>
        public void Initialize(int itemCount, Action<int> onSelected)
        {
            var found = new List<ZButton>(this.GetComponentsInChildren<ZButton>(includeInactive: true));
            found.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));
            _buttons = found.ToArray();
            if (_buttons.Length != itemCount)
            {
                throw new InvalidOperationException($"{this.name}: 버튼 {_buttons.Length}개, 데이터 {itemCount}개. 프리팹의 버튼 수를 데이터에 맞춰주세요.");
            }

            _onSelected = onSelected;
            for (int i = 0; i < _buttons.Length; ++i)
            {
                int index = i;
                _buttons[i].onClick.RemoveAllListeners();
                _buttons[i].onClick.AddListener(() => this.Select(index));
            }
        }

        public void Select(int index)
        {
            if (index == SelectedIndex)
            {
                return;
            }
            SelectedIndex = index;
            for (int i = 0; i < _buttons.Length; ++i)
            {
                bool selected = i == index;
                _buttons[i].SetButtonScale(selected ? SELECTED_SCALE : UNSELECTED_SCALE);
                _buttons[i].SetColor(selected ? SELECTED_COLOR : UNSELECTED_COLOR);
            }
            _onSelected?.Invoke(index);
        }
    }
}
