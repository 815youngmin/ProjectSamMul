#nullable enable
using UnityEngine;

namespace SamMul.UIs.Commons
{
    /// <summary>등급/레벨을 별 아이콘 개수로 보여주는 표시기. 별 오브젝트를 차례로 켜고 마지막 별을 깜빡인다.</summary>
    public class GradeStarDisplayer : MonoBehaviour
    {
        [SerializeField] private GameObject[] _stars = null!;
        [SerializeField] private float _blinkPeriod = 0.6f;

        private int _activeStars;
        private bool _blinking;
        private float _blinkTime;

        public void InitializeForSkillSelector(int level)
        {
            _activeStars = Mathf.Clamp(level, 0, _stars.Length);
            for (int i = 0; i < _stars.Length; ++i)
            {
                _stars[i].SetActive(i < _activeStars);
            }
            _blinking = false;
        }

        public void PlayLastStarBlinkAnimation()
        {
            _blinking = _activeStars > 0;
            _blinkTime = 0f;
        }

        public void StopAnimation()
        {
            _blinking = false;
            if (_activeStars > 0)
            {
                _stars[_activeStars - 1].SetActive(true);
            }
        }

        private void Update()
        {
            if (!_blinking)
            {
                return;
            }
            _blinkTime += Time.unscaledDeltaTime;
            bool on = Mathf.Repeat(_blinkTime, _blinkPeriod) < _blinkPeriod * 0.5f;
            _stars[_activeStars - 1].SetActive(on);
        }
    }
}
