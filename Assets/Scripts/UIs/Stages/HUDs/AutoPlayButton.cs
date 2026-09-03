using DG.Tweening;
using Shared.GameDataTypes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Z.GameClients;
using Z.Scenes;
using Z.UnityHelpers;

public class AutoPlayButton : MonoBehaviour
{
    [SerializeField] private ZButton _button;
    [SerializeField] private TextMeshProUGUI _iconText;

    [SerializeField] private Image _rotateCircle;
    [SerializeField] private Image _backgroundCircle;

    private bool _isAutoPlayActivated;
    private Sequence _rotateCircleSequence;

    private readonly Color _aiEnableColor = new Color(0.992156f, 0.768627f, 0.062745f);
    private readonly Color _aiDisableColor = Color.white;

    public void Initialize(bool isAutoPlayActivated)
    {
        if(GameClient.CS.UserGameData != null)
        {
            _isAutoPlayActivated = isAutoPlayActivated;

            if (_isAutoPlayActivated)
            {
                //오토 활성화
                this.EnableButton();
            }
            else
            {
                //오토 비활성화
                this.DisableButton();
            }
        }
        else
        {
            //userGameData가 없다는건 오류거나 테스트 플레이중이다.
            //경고 로그 띄워주고 비활성화 처리
            Debug.LogWarning("GameClient.CS.UserGameData가 비어있습니다. 확인이 필요합니다.");
            this.DisableButton();
        }


    }

    private void ToggleAutoPlay()
    { 
        //활성화 상태에서 눌림 비활성화로 변경해야됨
        if(_isAutoPlayActivated)
        {
            this.DisableAutoPlay();
        }
        //비활성화 상태에서 눌림 활성화로 변경해야됨
        else
        {
            this.EnableAutoPlay();
        }
    }

    private void EnableAutoPlay()
    {
        _isAutoPlayActivated = true;
        _iconText.color = _aiEnableColor;
        _rotateCircle.gameObject.SetActive(true);
        _backgroundCircle.gameObject.SetActive(true);
        _rotateCircleSequence.Restart();

        GameClient.PCController.SwitchAutoPlay(_isAutoPlayActivated);
        var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
        stageSceneUI.ShowAutoPlayUI();
    }

    private void DisableAutoPlay()
    {
        _isAutoPlayActivated = false;
        _iconText.color = _aiDisableColor;
        _rotateCircle.gameObject.SetActive(false);
        _backgroundCircle.gameObject.SetActive(false);
        _rotateCircleSequence.Pause();

        GameClient.PCController.SwitchAutoPlay(_isAutoPlayActivated);
        var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
        stageSceneUI.HideAutoPlayUI();
    }


    private void EnableButton()
    {
        _rotateCircleSequence = DOTween.Sequence();
        _rotateCircleSequence.Append(_rotateCircle.transform.DOLocalRotate(new Vector3(0, 0, -360f), 1f, RotateMode.FastBeyond360).SetEase(Ease.Linear));
        _rotateCircleSequence.OnComplete(() =>
        {
            _rotateCircle.transform.transform.localRotation = Quaternion.identity;
        });
        _rotateCircleSequence.SetRecyclable(true);
        _rotateCircleSequence.SetAutoKill(false);
        _rotateCircleSequence.SetLoops(-1);
        _rotateCircleSequence.Pause();

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(ToggleAutoPlay);
        _button.SetInteractable(true);

        this.gameObject.SetActive(true);

        //AI 활성화로 시작해야된다. 
        if(_isAutoPlayActivated)
        {
            this.EnableAutoPlay();
        }
        //AI 비활성화로 시작해야된다.
        else
        {
            this.DisableAutoPlay();
        }
    }

    private void DisableButton()
    {
        this.gameObject.SetActive(false);
    }

}
