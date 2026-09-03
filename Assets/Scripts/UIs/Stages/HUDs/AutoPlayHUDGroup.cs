using DG.Tweening;
using Shared.GameDataTypes;
using Shared.Localizers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutoPlayHUDGroup : MonoBehaviour
{
    [SerializeField] private AcquiredSkillGroup _acquiredSkillGroup;
    [SerializeField] private TextMeshProUGUI _dpsLabelText;
    [SerializeField] private TextMeshProUGUI _dpsText;
    [SerializeField] private TextMeshProUGUI _maxDpsLabelText;
    [SerializeField] private TextMeshProUGUI _maxDpsText;

    [SerializeField] private RectTransform _dpsRectTransform;
    [SerializeField] private RectTransform _maxDpsRectTransform;
    [SerializeField] private RectTransform _activeSkillRectTransform;
    [SerializeField] private RectTransform _passiveSkillRectTransform;

    [SerializeField] private Image _descriptionBackground;
    [SerializeField] private TextMeshProUGUI _descriptionText;

    private bool _isAutoPlayActivated;

    private Sequence _exitSequence;
    private Sequence _enterSequence;

    private Sequence _descriptionFadeSequence;
    private Sequence _descriptionFadeOutSequence;

    private Vector3 _enterActivePosition;
    private Vector3 _enterPassivePosition;
    private Vector3 _enterDpsPosition;
    private Vector3 _enterMaxDpsPosition;

    private Vector3 _exitActivePosition;
    private Vector3 _exitPassivePosition;
    private Vector3 _exitDpsPosition;
    private Vector3 _exitMaxDpsPosition;

    private readonly float _enterSequenceDuration = 0.5f;
    private readonly float _exitSequenceDuration = 0.5f;

    private readonly float _fadeInWaitingDuration = 0f;
    private readonly float _fadeInDuration = 1f;
    private readonly float _fadeOutWaitingDuration = 15f;
    private readonly float _fadeOutDuration = 1f;

    public void Initialize(bool isAutoPlayActivated, SkillKey[] acquiredSkills)
    {
        _isAutoPlayActivated = isAutoPlayActivated;

        if (_isAutoPlayActivated)
        {
            _acquiredSkillGroup.Initialize(acquiredSkills);
            _dpsLabelText.text = Localizer.Instance.GetText("UI_AUTOPLAY_DPS");
            _dpsText.text = "0";
            _maxDpsLabelText.text = Localizer.Instance.GetText("UI_AUTOPLAY_MAXDPS");
            _maxDpsText.text = "0";
            gameObject.SetActive(true);

            _enterActivePosition = _activeSkillRectTransform.localPosition;
            _enterPassivePosition = _passiveSkillRectTransform.localPosition;
            _enterDpsPosition = _dpsRectTransform.localPosition;
            _enterMaxDpsPosition = _maxDpsRectTransform.localPosition;

            _exitActivePosition = _enterActivePosition - new Vector3(_activeSkillRectTransform.rect.width * 1.5f, 0);
            _exitPassivePosition = _enterPassivePosition + new Vector3(_passiveSkillRectTransform.rect.width * 1.5f, 0);
            _exitDpsPosition = _enterDpsPosition - new Vector3(_dpsRectTransform.rect.width * 1.5f, 0);
            _exitMaxDpsPosition = _enterMaxDpsPosition - new Vector3(_maxDpsRectTransform.rect.width * 1.5f, 0);

            _activeSkillRectTransform.localPosition = _exitActivePosition;
            _passiveSkillRectTransform.localPosition = _exitPassivePosition;
            _dpsRectTransform.localPosition = _exitDpsPosition;
            _maxDpsRectTransform.localPosition = _exitMaxDpsPosition;

            //UI 퇴장 시퀀스
            //오토 플레이를 끄는 시점에 호출된다.
            _exitSequence = DOTween.Sequence();
            _exitSequence.Append(_activeSkillRectTransform.DOLocalMove(_exitActivePosition, _exitSequenceDuration).From(_enterActivePosition));
            _exitSequence.Join(_passiveSkillRectTransform.DOLocalMove(_exitPassivePosition, _exitSequenceDuration).From(_enterPassivePosition));
            _exitSequence.Join(_dpsRectTransform.DOLocalMove(_exitDpsPosition, _exitSequenceDuration).From(_enterDpsPosition));
            _exitSequence.Join(_maxDpsRectTransform.DOLocalMove(_exitMaxDpsPosition, _exitSequenceDuration).From(_enterMaxDpsPosition));
            _exitSequence.SetRecyclable(true);
            _exitSequence.SetAutoKill(false);
            _exitSequence.Pause();

            //UI 입장 시퀀스
            //오토 플레이를 켜는 시점에 호출된다.
            _enterSequence = DOTween.Sequence();
            _enterSequence.Append(_activeSkillRectTransform.DOLocalMove(_enterActivePosition, _enterSequenceDuration).From(_exitActivePosition));
            _enterSequence.Join(_passiveSkillRectTransform.DOLocalMove(_enterPassivePosition, _enterSequenceDuration).From(_exitPassivePosition));
            _enterSequence.Join(_dpsRectTransform.DOLocalMove(_enterDpsPosition, _enterSequenceDuration).From(_exitDpsPosition));
            _enterSequence.Join(_maxDpsRectTransform.DOLocalMove(_enterMaxDpsPosition, _enterSequenceDuration).From(_exitMaxDpsPosition));
            _enterSequence.SetRecyclable(true);
            _enterSequence.SetAutoKill(false);
            _enterSequence.Play();


            _descriptionBackground.color = new Color(1f, 1f, 1f, 0f);
            _descriptionText.color = new Color(1f, 1f, 1f, 0f);
            _descriptionText.text = Localizer.Instance.GetText("UI_AUTOPLAY_DESCRIPTION");

            _descriptionFadeSequence = DOTween.Sequence();
            _descriptionFadeSequence.AppendInterval(_fadeInWaitingDuration);
            _descriptionFadeSequence.Append(_descriptionBackground.DOFade(1f, _fadeInDuration));
            _descriptionFadeSequence.Join(_descriptionText.DOFade(1f, _fadeInDuration));
            _descriptionFadeSequence.AppendInterval(_fadeOutWaitingDuration);
            _descriptionFadeSequence.Append(_descriptionBackground.DOFade(0f, _fadeOutDuration));
            _descriptionFadeSequence.Join(_descriptionText.DOFade(0f, _fadeOutDuration));
            _descriptionFadeSequence.Play();

            //중간에 오토 플레이 끄는 경우 일찍 fadeOut 하는 연출
            _descriptionFadeOutSequence = DOTween.Sequence();
            _descriptionFadeOutSequence.Append(_descriptionBackground.DOFade(0f, _fadeOutDuration));
            _descriptionFadeOutSequence.Join(_descriptionText.DOFade(0f, _fadeOutDuration));
            _descriptionFadeOutSequence.SetRecyclable(true);
            _descriptionFadeOutSequence.SetAutoKill(false);
            _descriptionFadeOutSequence.Pause();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void UpdateAcquiredSkillGroup(SkillKey[] acquiredSkills)
    {
        if(!_isAutoPlayActivated)
        {
            return;
        }
        _acquiredSkillGroup.Initialize(acquiredSkills);
    }

    public void UpdateDPSText(float dps)
    {
        if (!_isAutoPlayActivated)
        {
            return;
        }
        _dpsText.text = dps.ToString("N1");
    }

    public void UpdateMaxDps(float maxDps)
    {
        if (!_isAutoPlayActivated)
        {
            return;
        }
        _maxDpsText.text = maxDps.ToString("N1");
    }

    public void ShowAutoPlayUI()
    {
        if (!_isAutoPlayActivated)
        {
            return;
        }
        _enterSequence.Restart();
    }
    public void HideAutoPlayUI()
    {
        if (!_isAutoPlayActivated)
        {
            return;
        }
        _exitSequence.Restart();

        if(_descriptionFadeOutSequence != null && 
            _descriptionFadeSequence.IsPlaying())
        {
            _descriptionFadeSequence.Pause();
            _descriptionFadeOutSequence.Restart();
        }

    }
}
