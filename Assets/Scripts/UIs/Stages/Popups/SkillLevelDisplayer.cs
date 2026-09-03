using Shared.DataTables;
using UnityEngine;
using UnityEngine.UI;
using SamMul.ResourcePools;

namespace SamMul.UIs.Stages.Popups
{
    public class SkillLevelDisplayer : MonoBehaviour, ILayoutGroup
    {
        private static readonly string TRANSCENDENT_STAR_PATH = "Commons/Icon/starS_Fill.png";
        private static readonly string NORMAL_STAR_PATH = "Commons/Icon/star_fill.png";

        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private GridLayoutGroup _gridLayoutGroup;

        [SerializeField] private GameObject[] _starSlots;
        [SerializeField] private Image[] _stars;

        /// <summary>
        /// 스킬 등급 표시기를 초기화합니다.
        /// </summary>
        /// <param name="level">
        /// 스킬 레벨.
        /// </param>
        /// <param name="displayEmptyStars">
        /// 빈 별을 보여줄 것인가?
        /// </param>
        public void Initialize(int level, bool displayEmptyStars)
        {
            Debug.Assert(_starSlots.Length == 6);

            if (level == GameConstants.SKILL_TRANSCENDENT_LEVEL)
            {
                for (int i = 0; i < _starSlots.Length; ++i)
                {
                    _starSlots[i].SetActive(true);
                    _stars[i].sprite = ResourcePool.Instance.LoadResource<Sprite>(TRANSCENDENT_STAR_PATH);
                    _stars[i].gameObject.SetActive(true);
                }
            }
            else
            {
                for (int i = 0; i < level; ++i)
                {
                    _starSlots[i].SetActive(true);
                    _stars[i].sprite = ResourcePool.Instance.LoadResource<Sprite>(NORMAL_STAR_PATH);
                    _stars[i].gameObject.SetActive(true);
                }
                for (int i = level; i < _starSlots.Length - 1; ++i)
                {
                    if (displayEmptyStars)
                    {
                        _starSlots[i].SetActive(true);
                        _stars[i].gameObject.SetActive(false);
                    }
                    else
                    {
                        _starSlots[i].SetActive(false);
                    }
                }
                _starSlots[_starSlots.Length - 1].SetActive(false);
            }
        }

        /// <summary>
        /// 레이아웃이 재빌드될 때 별의 크기를 조절합니다.
        /// </summary>
        void ILayoutController.SetLayoutHorizontal()
        {
            float cellSize = _rectTransform.rect.width / (float)_starSlots.Length;
            _gridLayoutGroup.cellSize = cellSize * Vector2.one;
        }

        /// <summary>
        /// 인터페이스를 명시적으로 구현한 메소드입니다. 아무 동작도 하지 않습니다.
        /// </summary>
        void ILayoutController.SetLayoutVertical()
        {

        }
    }
}
