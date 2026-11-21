#nullable enable
using System;
using Shared.Localizers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.Localizations;
using SamMul.UnityHelpers;

namespace SamMul.UIs.Lobbies.Popups
{
    /// <summary>
    /// 로비 옵션 팝업. 언어(한국어 / 영어), 음소거, 배경음·효과음 볼륨을 바꾼다. 값은 바꾸는 즉시 적용·저장된다.
    /// </summary>
    public class OptionPopup : BasePopup
    {
        // _languageGroup 의 자식 버튼 순서와 같아야 한다.
        private static readonly LanguageType[] LANGUAGES = { LanguageType.Korean, LanguageType.English };

        [SerializeField] private ZButton _closeButton = null!;

        [Header("언어")]
        [SerializeField] private LobbyRadioGroup _languageGroup = null!;

        [Header("사운드")]
        [SerializeField] private ZButton _muteButton = null!;
        [SerializeField] private TextMeshProUGUI _muteText = null!;
        [SerializeField] private Slider _bgmSlider = null!;
        [SerializeField] private Slider _effectSlider = null!;

        private Action? _onLanguageChanged;

        /// <param name="onLanguageChanged">언어가 바뀐 뒤 호출된다. 팝업 밖에서 코드로 넣은 텍스트를 갱신하는 데 쓴다.</param>
        public void Initialize(Action<bool> closeRequester, Action onLanguageChanged)
        {
            base.InitializeBase(closeRequester, isCloseOnOutsideClick: true);
            _onLanguageChanged = onLanguageChanged;

            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(() => this.Close(skipAnimation: false));

            _languageGroup.Initialize(LANGUAGES.Length, index => this.SetLanguage(LANGUAGES[index]));
            _languageGroup.Select(Mathf.Max(0, Array.IndexOf(LANGUAGES, Localizer.Instance.CurrentLanguage.Type)));

            this.UpdateMuteText();
            _muteButton.onClick.RemoveAllListeners();
            _muteButton.onClick.AddListener(() =>
            {
                UnityGlobal.Sounds.SetMuted(!UnityGlobal.Sounds.IsMuted);
                this.UpdateMuteText();
            });

            InitializeVolumeSlider(_bgmSlider, UnityGlobal.Sounds.GetBGMVolume(), UnityGlobal.Sounds.SetBGMVolume);
            InitializeVolumeSlider(_effectSlider, UnityGlobal.Sounds.GetEffectVolume(), UnityGlobal.Sounds.SetEffectVolume);
        }

        private static void InitializeVolumeSlider(Slider slider, float volume, Action<float> setVolume)
        {
            slider.onValueChanged.RemoveAllListeners();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = volume;
            slider.onValueChanged.AddListener(value => setVolume(value));
        }

        private void SetLanguage(LanguageType language)
        {
            if (Localizer.Instance.CurrentLanguage.Type == language)
            {
                return;
            }

            Localizer.Instance.SetLanguage(language);
            PlayerPrefs.SetString(LocalizedText.LANGUAGE_KEY, language.ToString());

            foreach (var localizedText in FindObjectsOfType<LocalizedText>(includeInactive: true))
            {
                localizedText.UpdateText();
            }
            this.UpdateMuteText();
            _onLanguageChanged?.Invoke();
        }

        private void UpdateMuteText()
        {
            _muteText.text = Localizer.Instance.GetText(UnityGlobal.Sounds.IsMuted ? "UI_SOUND_OFF" : "UI_SOUND_ON");
        }
    }
}
