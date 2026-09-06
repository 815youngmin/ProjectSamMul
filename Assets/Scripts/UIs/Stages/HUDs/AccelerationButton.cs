using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.GameClients;
using SamMul.Loggers;
using SamMul.Scenes;
using SamMul.UnityHelpers;

public class AccelerationButton : MonoBehaviour
{
    public enum AccelerationType
    {
        x1_0,
        x1_5,
        x2_0,
    }

    [SerializeField] private ZButton _button;
    [SerializeField] TextMeshProUGUI _iconText;

    private bool _isButtonEnable;
    private readonly Color _accelerationColor = new Color(0.992156f, 0.768627f, 0.062745f);
    private AccelerationType _currentAccelerationType;


    public void Initialize(int? chapterNumber)
    {
        var userGameData = GameClient.CS.UserGameData;
        if (userGameData == null)
        {
            // userGameData가 없다는건 오류거나 테스트 플레이중이다.
            // 일단 활성화 시켜주고 경고 로그 하나 띄워주자.
            // 가속 버튼 활성화.
            Log.I.Error("GameClient.CS.UserGameData가 비어있습니다. 확인이 필요합니다.");
            this.EnableButton();
            this.ChangeStageAccleration();
            return;
        }

        bool activateAcceleration = chapterNumber != null && userGameData.ClearedHighestChapter >= chapterNumber;

        if (activateAcceleration)
        {
            this.EnableButton();
            this.ChangeStageAccleration();
        }
        else
        {
            this.DisableButton();
            UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>().StageTimer.DisableAccelerationText();
        }
    }

    private void ChangeStageAccleration()
    {
        switch (_currentAccelerationType)
        {
            case AccelerationType.x1_0:
                {
                    _currentAccelerationType = AccelerationType.x1_5;
                    UnityGlobal.Scenes.GetCurrentScene<BaseScene>().TimeScaleController.ChangeTimeScale(1.5f);
                    _iconText.text = "x1.5";
                    _iconText.color = _accelerationColor;
                    UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>().StageTimer.EnableAccelerationText("x1.5");

                }
                break;
            case AccelerationType.x1_5:
                {
                    _currentAccelerationType = AccelerationType.x2_0;
                    UnityGlobal.Scenes.GetCurrentScene<BaseScene>().TimeScaleController.ChangeTimeScale(2.0f);
                    _iconText.text = "x2.0";
                    _iconText.color = _accelerationColor;
                    UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>().StageTimer.EnableAccelerationText("x2.0");

                }
                break;
            case AccelerationType.x2_0:
                {
                    _currentAccelerationType = AccelerationType.x1_0;
                    UnityGlobal.Scenes.GetCurrentScene<BaseScene>().TimeScaleController.ChangeTimeScale(1.0f);
                    _iconText.text = "x1.0";
                    _iconText.color = Color.white;
                    UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>().StageTimer.DisableAccelerationText();

                }
                break;
        }
    }

    private void EnableButton()
    {

        //가속 버튼 활성화
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() =>
        {
            ChangeStageAccleration();
        });
        _currentAccelerationType = AccelerationType.x1_0;
        _iconText.text = "x1.0";
        _iconText.color = Color.white;
        _isButtonEnable = true;
        this.gameObject.SetActive(true);
    }

    private void DisableButton()
    {
        //가속 버튼 비활성화
        _isButtonEnable = false;
        this.gameObject.SetActive(false);

    }


    public void PauseAccelerationButton()
    {
        if (!_isButtonEnable)
        {
            return;
        }

        UnityGlobal.Scenes.GetCurrentScene<BaseScene>().TimeScaleController.ChangeTimeScale(1.0f);
        _button.SetInteractable(false);

        _button.ButtonImage.color = Color.white;
        _iconText.color = new Color(1f, 1f, 1f, 0.4f);

    }
    public void ResumeAccelerationButton()
    {
        if (!_isButtonEnable)
        {
            return;
        }

        switch (_currentAccelerationType)
        {
            case AccelerationType.x1_0:
                {
                    _iconText.color = Color.white;
                    UnityGlobal.Scenes.GetCurrentScene<BaseScene>().TimeScaleController.ChangeTimeScale(1.0f);
                }
                break;
            case AccelerationType.x1_5:
                {
                    _iconText.color = _accelerationColor;
                    UnityGlobal.Scenes.GetCurrentScene<BaseScene>().TimeScaleController.ChangeTimeScale(1.5f);
                }
                break;
            case AccelerationType.x2_0:
                {
                    _iconText.color = _accelerationColor;
                    UnityGlobal.Scenes.GetCurrentScene<BaseScene>().TimeScaleController.ChangeTimeScale(2.0f);
                }
                break;
        }
        _button.SetInteractable(true);
    }
}
