#nullable enable
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Z.UIs.Commons.DebugInfoScreens
{
    /// <summary>화면 구석에 개발용 텍스트 줄을 쌓아 보여주는 오버레이.</summary>
    public class DebugInfoScreen : MonoBehaviour
    {
        public static readonly string PREFAB_PATH = "Commons/DebugInfoScreen/DebugInfoScreen.prefab";

        [SerializeField] private RectTransform _lineRoot = null!;
        [SerializeField] private DebugInfoText _lineTemplate = null!;

        private readonly List<DebugInfoText> _lines = new List<DebugInfoText>();

        public void Initialize()
        {
            _lineTemplate.gameObject.SetActive(false);
        }

        public DebugInfoText AddDebugInfoTextLine()
        {
            var line = Instantiate(_lineTemplate, _lineRoot);
            line.gameObject.SetActive(true);
            _lines.Add(line);
            return line;
        }

        public void Clear()
        {
            foreach (var line in _lines)
            {
                if (line != null)
                {
                    Destroy(line.gameObject);
                }
            }
            _lines.Clear();
        }
    }

    public class DebugInfoText : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text = null!;

        public void UpdateText(string text)
        {
            _text.text = text;
        }
    }
}
