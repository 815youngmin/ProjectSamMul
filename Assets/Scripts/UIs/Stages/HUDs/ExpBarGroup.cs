using Shared.GameDataTypes;
using Shared.StaticDatas;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SamMul.ResourcePools;

namespace SamMul.UIs.Stages.HUDs
{
    public class ExpBarGroup : MonoBehaviour
    {
        //슬라이더 안쪽에 있는 텍스트는 레벨 텍스트 외에 존재하지 않는다.
        //그림자 연출등으로 인해 텍스트가 여러개 있을수 있으니 전부 저장해준다
        private TMP_Text[] _levelTexts;
        private Slider _expSlider;
        private bool _isInitialize = false;

        public void Initialize(HeroType heroType)
        {
            //데모 용으로 모든 영웅이 같은 경험치바를 사용하도록 한다.
            GameObject hudPrefab = ResourcePool.Instance.LoadResource<GameObject>("Stage/UIs/ExpBar/ExpBar.prefab");
            var sliderObject =  Instantiate(hudPrefab, this.transform);
            _expSlider = sliderObject.GetComponentInChildren<Slider>();

            _levelTexts = sliderObject.GetComponentsInChildren<TMP_Text>();
            _isInitialize = true;
        }

        public void UpdateLevelExp(int level, long currentExp, long expToNextLevel)
        {
            if(!_isInitialize)
            {
                return;
            }
            foreach (var levelText in _levelTexts)
            {
                levelText.text = level.ToString();
            }

            float expRate = (expToNextLevel > 0) ?
                ((float)currentExp / (float)expToNextLevel) : 1.0f;

            if (expRate > 1.0f)
            {
                expRate = 1.0f;
            }

            _expSlider.value = expRate;
        }
    }
}
