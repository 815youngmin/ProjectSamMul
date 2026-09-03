using Shared.Localizers;
using Shared.StaticDatas;
using TMPro;
using UnityEngine;

namespace Z.UIs.Stages.HUDs
{
    public class StageTimer : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _timerText;
        [SerializeField] TextMeshProUGUI _accelerationText;
        [SerializeField] TextMeshProUGUI _progressLabel;
        [SerializeField] TextMeshProUGUI _progressText;

        private int _lastPercentage;
        private bool _isDisplayingBossName;

        public void Initialize()
        {
            _progressLabel.text = Localizer.Instance.GetText("UI_INGAME_PROGRESS");
            _lastPercentage = 0;
            _isDisplayingBossName = false;
        }

        public void UpdateTimer(float stageTime, float maxStageTime)
        {
            if (_isDisplayingBossName)
            {
                return;
            }

            int minutes = (int)stageTime / 60;
            int seconds = (int)stageTime % 60;

            string timeString = string.Format("{0,2:00}:{1,2:00}", minutes, seconds);
            _timerText.text = timeString;

            int progressPercentage = maxStageTime <= 0f ? 0 : (int)(stageTime * 100f / maxStageTime);
            if (progressPercentage < 30)
            {
                _progressLabel.enabled = false;
                _progressText.enabled = false;
            }
            else
            {
                _progressLabel.enabled = true;
                _progressText.enabled = true;

                if (_lastPercentage != progressPercentage)
                {
                    _lastPercentage = progressPercentage;
                    _progressText.text = string.Format("{0}%", progressPercentage);
                }
            }
        }

        public void EnableAccelerationText(string accelerationText)
        {
            _accelerationText.gameObject.SetActive(true);
            _accelerationText.text = accelerationText;
        }

        public void DisableAccelerationText()
        {
            _accelerationText.gameObject.SetActive(false);
        }

        public void DisplayBossName(MonsterStaticData bossStaticData)
        {
            _timerText.text = bossStaticData.MonsterName;
            _progressLabel.enabled = false;
            _progressText.enabled = false;
            _accelerationText.enabled = false;
            _isDisplayingBossName = true;
        }

        public void HideBossName()
        {
            _progressLabel.enabled = true;
            _progressText.enabled = true;
            _accelerationText.enabled = true;
            _isDisplayingBossName = false;
        }
    }
}
