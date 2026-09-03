using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Z.UIs.Stages.HUDs
{
    public class BossHPBarGroup : MonoBehaviour
    {
        [SerializeField] private TMP_Text _bossNameText;
        [SerializeField] private Slider _hpSlider;

        private void Awake()
        {
            Debug.Assert(_bossNameText != null);
            Debug.Assert(_hpSlider != null);
            _bossNameText.gameObject.SetActive(false); // TODO. 사용안함 추후 제게 해야합니다.
        }

        public void UpdateBossHPBar(float currentHP, float maxHP)
        {
            float hpRate = (maxHP > 0) ? (currentHP / maxHP) : 1.0f;
            if(hpRate > 1.0f)
            {
                hpRate = 1.0f;
            }
            _hpSlider.value = hpRate;
        }
        public void UpdateBossName(string bossName)
        {
            _bossNameText.text = bossName;
        }
    }
}
