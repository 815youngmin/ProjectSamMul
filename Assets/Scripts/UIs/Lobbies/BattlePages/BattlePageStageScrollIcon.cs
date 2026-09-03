#nullable enable
using DG.Tweening;
using Shared.StaticDatas;
using Z.Animations.Placeholder;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Z.GameClients;
using Z.ResourcePools;

namespace Z.UIs.Lobbies.BattlePages
{
    public class BattlePageStageScrollIcon : MonoBehaviour
    {
        private static readonly Vector3 SELECTED_SCALE = new Vector3(0.75f, 0.75f, 0.75f); // 사이즈는 아트팀에서 전달해주셔서 해당 0.75 사이즈에 맞춰 작업
        private static readonly Vector3 UNSELECTED_SCALE = new Vector3(0.5f, 0.5f, 0.5f);
        private static readonly Color SELECTED_COLOR = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        private static readonly Color UNSELECTED_COLOR = new Color(0.2f, 0.2f, 0.2f, 1.0f);
        private static readonly float DURATION = 0.2f;

        [SerializeField] private GameObject _chapterIcon;
        [SerializeField] private Image _chapterIconFallbackImage;
        [SerializeField] private SkeletonGraphic _chapterIconAnimation;

        private readonly AsyncResourceLoader _asyncResourceLoader = new AsyncResourceLoader();

        public int ChapterNumber => _chapter.ChapterNumber;
        private ChapterStaticData _chapter = null!;

        private Sequence _selectSequence = null!;
        private Sequence _unSelectSequence = null!;

        public void Initialize(int chapterNumber, bool show)
        {
            Debug.Assert(0 <= chapterNumber && chapterNumber <= GameClient.CS.ServiceFinalChapterNumber, $"{chapterNumber} 잘못된 챕터 번호입니다. 확인이 필요합니다.");
            _chapter = StaticDataRepository.Instance.Chapters.FindChapter(chapterNumber)!;

            _selectSequence = DOTween.Sequence(this)
                .Append(_chapterIcon.transform.DOScale(SELECTED_SCALE, DURATION))
                .Join(_chapterIconFallbackImage.DOColor(SELECTED_COLOR, DURATION))
                .Join(_chapterIconAnimation.DOColor(SELECTED_COLOR, DURATION))
                .SetAutoKill(false)
                .Pause();

            _unSelectSequence = DOTween.Sequence(this)
                .Append(_chapterIcon.transform.DOScale(UNSELECTED_SCALE, DURATION))
                .Join(_chapterIconFallbackImage.DOColor(UNSELECTED_COLOR, DURATION))
                .Join(_chapterIconAnimation.DOColor(UNSELECTED_COLOR, DURATION))
                .SetAutoKill(false)
                .Pause();

            if (show)
            {
                this.ShowChapterIcon(true);
                _chapterIcon.transform.localScale = SELECTED_SCALE;
                _chapterIconFallbackImage.color = SELECTED_COLOR;
                _chapterIconAnimation.color = SELECTED_COLOR;
            }
            else
            {
                this.ShowChapterIcon(false);
                _chapterIcon.transform.localScale = UNSELECTED_SCALE;
                _chapterIconFallbackImage.color = UNSELECTED_COLOR;
                _chapterIconAnimation.color = UNSELECTED_COLOR;
            }
        }

        public void PlaySelectSequence()
        {
            _unSelectSequence.Pause();
            _selectSequence.Restart();
        }

        public void PlayUnselectSequence()
        {
            _selectSequence.Pause();
            _unSelectSequence.Restart();
        }

        public void ShowChapterIcon(bool show)
        {
            _chapterIcon.SetActive(show);

            if (show)
            {
                _asyncResourceLoader.LoadAndApplyResource<SkeletonDataAsset>(
                    resourceAddress: _chapter.ChapterIconSpinePath,
                    applyFallback: () =>
                    {
                        _chapterIconFallbackImage.gameObject.SetActive(true);
                        _chapterIconAnimation.gameObject.SetActive(false);
                    },
                    applyActual: (resource) =>
                    {
                        _chapterIconFallbackImage.gameObject.SetActive(false);
                        _chapterIconAnimation.gameObject.SetActive(true);

                        _chapterIconAnimation.skeletonDataAsset = resource;
                        _chapterIconAnimation.Initialize(true);
                        var animation = _chapterIconAnimation.SkeletonData.Animations.FirstOrDefault();
                        if (animation != null)
                        {
                            _chapterIconAnimation.AnimationState.SetAnimation(0, animation, loop: true);
                        }
                    },
                    applyActualCondition: () => this != null);
            }
        }
    }
}
