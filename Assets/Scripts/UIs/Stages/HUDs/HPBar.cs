using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace SamMul.UIs.Stages.HUDs
{
    // Character GameObject의 자식으로 붙는다. UIRoot 의 자식이 아님
    public class HPBar : MonoBehaviour
    {
        [SerializeField] private Slider _slider;
        // 채워지는 게이지 이미지. 게이지 색상변경을 위함
        [SerializeField] private Image _fill;

        public static readonly Color32 PC_COLOR = new Color32(11, 197, 42, 255);
        public static readonly Color32 AIPC_COLOR = new Color32(11, 83, 255, 255);
        public static readonly Color32 MONSTER_COLOR = new Color32(255, 52, 52, 255);
        public static readonly Color32 HP_BUFFER_COLOR = new Color32(71, 79, 255, 255);

        public static readonly Color32[] INVINCIBLE_EFFECT_COLOR =
        {
            new Color32( 58, 255,  95, 255),
            new Color32( 71, 189, 255, 255),
            new Color32(112,  76, 255, 255),
            new Color32(184,  91, 255, 255),
            new Color32(255,  66,  66, 255),
            new Color32(255, 153,  33, 255),
            new Color32(167, 229,  92, 255)
        };

        private Canvas _canvas;

        public void AllocateSharedResources(bool isPC)
        {
            Canvas canvas = GetComponent<Canvas>();
            canvas.sortingLayerID = SortingLayer.NameToID("Object");
            canvas.sortingOrder = isPC ? short.MaxValue : short.MaxValue - 1;
            _canvas = isPC ? null : canvas;
        }

        public void Initialize(Transform ownerTransform, Vector3 offsetFromOwnerPosition, Vector3 hpBarLocalScale, Color fillColor)
        {
            Debug.Assert(_slider != null, "프리팹에서 _slider를 연결해주세요.");
            Debug.Assert(_fill != null, "프리팹에서 _fill 연결해주세요.");

            this.transform.SetParent(ownerTransform);
            this.transform.localPosition = offsetFromOwnerPosition;
            this.transform.localScale = hpBarLocalScale;

            _fill.color = fillColor;
        }

        public void UpdateLogic(float currentHP, float maxHP, Vector2 position)
        {
            this.UpdateSlider(currentHP, maxHP);
            this.UpdateSorting(position);
        }

        private void UpdateSorting(Vector2 pos)
        {
            if (null == _canvas)
            {
                return;
            }

            _canvas.sortingOrder = (int)(pos.y * -100.0f);
        }

        private void UpdateSlider(float currentHP, float maxHP)
        {
            if (maxHP <= 0f)
            {
                // 오버플로 방지
                _slider.value = 1f;
                return;
            }

            _slider.value = currentHP / maxHP;
        }

        /// <summary>
        /// 무적 효과를 재생합니다.
        /// </summary>
        public void PlayInvincibleEffect()
        {
            var sequence = DOTween.Sequence(this);
            foreach (var color in INVINCIBLE_EFFECT_COLOR)
            {
                sequence.Append(_fill.DOColor(color, 0.1f).SetEase(Ease.Linear));
            }
            sequence.SetLoops(-1).OnKill(() => _fill.DOColor(PC_COLOR, 0.1f));
        }

        /// <summary>
        /// 무적 효과를 중지합니다.
        /// </summary>
        public void StopInvincibleEffect()
        {
            DOTween.Kill(this);
        }
    }
}
