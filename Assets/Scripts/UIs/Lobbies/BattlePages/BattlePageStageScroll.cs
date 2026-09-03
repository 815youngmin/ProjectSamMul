using DG.Tweening;
using Shared.Localizers;
using Shared.StaticDatas;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Z.GameClients;
using Z.ResourcePools;
using Z.UnityHelpers;

namespace Z.UIs.Lobbies.BattlePages
{
    public class BattlePageStageScroll : MonoBehaviour, IEndDragHandler, IDragHandler, IBeginDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        public Transform LeftSide => _leftSide;
        public Transform RightSide => _rightSide;

        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;
        [SerializeField] private GridLayoutGroup _gridLayoutGroup;
        [SerializeField] private Transform _leftSide;
        [SerializeField] private Image _leftSideImage;
        [SerializeField] private Transform _rightSide;
        [SerializeField] private Image _rightSideImage;


        private int _clearedHighestChapterNumber;
        private long _highestStageTimeInSeconds;
        private List<BattlePageStageScrollIcon> _stageScrollStageIcons = new List<BattlePageStageScrollIcon>();
        private int _currentSelectedChapterIndex;
        private int? _prevSelectedChapterIndex = null;
        private bool _isDrag;
        private bool _isOptimizing;
        private Sequence _positionOptimizeSequence;

        private Sequence _touchRightSequence;
        private Sequence _touchLeftSequence;

        private float _IconBetweenWidth;

        private Action<PointerEventData> _onBeginDragAction;
        private Action<PointerEventData> _onDragAction;
        private Action<PointerEventData> _onEndDragAction;

        public string StageNameText { get; private set; }
        public string StageRecordText { get; private set; }
        public string ChapterNumberText { get; private set; }

        public string StageElementPath { get; private set; }

        private int _chapterCount;

        public void Initialize(int clearedHighestChapterNumber, long highestStageTimeInSeconds, Action updateBattlePage, Action<PointerEventData> onBeginDragAction, Action<PointerEventData> onDragAction, Action<PointerEventData> onEndDragAction)
        {
            Debug.Assert(_scrollRect);
            Debug.Assert(_content);
            Debug.Assert(_gridLayoutGroup);
            Debug.Assert(_leftSideImage);
            Debug.Assert(_rightSideImage);

            _highestStageTimeInSeconds = highestStageTimeInSeconds;
            _clearedHighestChapterNumber = clearedHighestChapterNumber;

            //0챕터가 클리어되지 않았으면 0챕터 아이콘도 노출시켜준다.
            int startChapter = GameClient.CS.UserGameData.ClearedHighestChapter >= 0 ? 1 : 0;
            _chapterCount = GameClient.CS.ServiceFinalChapterNumber - startChapter + 1;
            this.RemoveAllExampleIcons();

            //필요한 챕터 아이콘 생성
            for (int i = startChapter; i <= GameClient.CS.ServiceFinalChapterNumber; i++)
            {
                var stageIcon = ResourcePool.Instance.InstantiateFromResource<BattlePageStageScrollIcon>("Lobbys/UIs/BattlePages/BattlePageStageScrollIcon.prefab");
                stageIcon.transform.SetParent(_content.transform);
                stageIcon.transform.localScale = Vector3.one;
                stageIcon.transform.localPosition = Vector3.zero;
                stageIcon.Initialize(chapterNumber: i, show: false);
                _stageScrollStageIcons.Add(stageIcon);
            }

            float canvasWidth = this.GetComponent<RectTransform>().rect.width;
            float cellSizeX = _gridLayoutGroup.cellSize.x;

            int paddingLeft = (int)((canvasWidth * 0.5f) - (cellSizeX * 0.5f));
            int paddingRight = (int)((canvasWidth * 0.5f) - (cellSizeX * 0.5f));

            //아이콘 사이 공백 가로 사이즈 계산
            //아이콘 사이 공백 개수는 아이콘 개수 -1
            _IconBetweenWidth = 1f / (_chapterCount - 1);

            _gridLayoutGroup.padding = new RectOffset(paddingLeft, paddingRight, 0, 0);
            _gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayoutGroup.constraintCount = _chapterCount;

            _scrollRect.onValueChanged.RemoveAllListeners();
            _scrollRect.onValueChanged.AddListener((Vector2 position) =>
            {
                if (_scrollRect.velocity != Vector2.zero && (_scrollRect.normalizedPosition.x <= 0 || 1 <= _scrollRect.normalizedPosition.x))
                {
                    // velocity가 매우 큰 값일 경우, 스크롤 가능한 영역 끝에 도달해도 velocity가 남아 서서히 감소하는데 감소되는 상황에서는 onValueChanged는 호출되지 않는다.
                    // 그러므로 스크롤 영역 끝으로 이동시 Veclocity를 0으로 세팅한다.
                    _scrollRect.velocity = Vector2.zero;
                }

                _currentSelectedChapterIndex = this.GetBatteStageIconIndexFromScrollPosition();
                updateBattlePage();
                this.UpdateStageInfoText();
                this.UpdateStageIcon();
                this.OptimizePosition();
                _prevSelectedChapterIndex = _currentSelectedChapterIndex;
            });

            _currentSelectedChapterIndex = this.GetBattleStageIconIndexFromChapterNumber(Math.Clamp(_clearedHighestChapterNumber + 1, 0, GameClient.CS.ServiceFinalChapterNumber));
            this.UpdateStageInfoText();
            this.UpdateStageIcon();
            _prevSelectedChapterIndex = _currentSelectedChapterIndex;

            //외부 BattlePage에 있는 UI중 BattlePageScroll 상태에 따라 애니메이션이 변경되는 UI가 있다.
            //해당 UI 애니메이션을 처리하기 위한 Action
            _onBeginDragAction = onBeginDragAction;
            _onDragAction = onDragAction;
            _onEndDragAction = onEndDragAction;

            _touchRightSequence = DOTween.Sequence();
            _touchRightSequence.AppendCallback(() =>
            {
                PointerEventData rightDragData = new PointerEventData(null);
                rightDragData.delta = new Vector2(-1f, 0f);

                _onBeginDragAction(rightDragData);
                _onDragAction(rightDragData);
            }).AppendInterval(0.2f);
            _touchRightSequence.AppendCallback(() =>
            {
                PointerEventData rightDragData = new PointerEventData(null);
                rightDragData.delta = new Vector2(-1f, 0f);
                _onEndDragAction(rightDragData);
            });
            _touchRightSequence.OnRewind(() =>
            {
                PointerEventData rightDragData = new PointerEventData(null);
                rightDragData.delta = new Vector2(-1f, 0f);
                _onEndDragAction(rightDragData);
            });
            _touchRightSequence.SetAutoKill(false);
            _touchRightSequence.SetRecyclable(true);
            _touchRightSequence.Pause();

            _touchLeftSequence = DOTween.Sequence();
            _touchLeftSequence.AppendCallback(() =>
            {
                PointerEventData leftDragData = new PointerEventData(null);
                leftDragData.delta = new Vector2(1f, 0f);

                _onBeginDragAction(leftDragData);
                _onDragAction(leftDragData);
            }).AppendInterval(0.2f);
            _touchLeftSequence.AppendCallback(() =>
            {
                PointerEventData leftDragData = new PointerEventData(null);
                leftDragData.delta = new Vector2(1f, 0f);
                _onEndDragAction(leftDragData);
            });
            _touchLeftSequence.OnRewind(() =>
            {
                PointerEventData leftDragData = new PointerEventData(null);
                leftDragData.delta = new Vector2(1f, 0f);
                _onEndDragAction(leftDragData);
            });
            _touchLeftSequence.SetAutoKill(false);
            _touchLeftSequence.SetRecyclable(true);
            _touchLeftSequence.Pause();

            var sideSequence = DOTween.Sequence();
            sideSequence.Append(_rightSideImage.DOFade(0.2f, 1f).From(0.35f));
            sideSequence.Join(_rightSideImage.transform.DOScale(1f, 1f).From(0.9f));
            sideSequence.Join(_leftSideImage.DOFade(0.2f, 1f).From(0.35f));
            sideSequence.Join(_leftSideImage.transform.DOScale(1f, 1f).From(0.9f));
            sideSequence.Append(_rightSideImage.DOFade(0.35f, 1f).From(0.2f));
            sideSequence.Join(_rightSideImage.transform.DOScale(0.9f, 1f).From(1f));
            sideSequence.Join(_leftSideImage.DOFade(0.35f, 1f).From(0.2f));
            sideSequence.Join(_leftSideImage.transform.DOScale(0.9f, 1f).From(1f));
            sideSequence.SetLoops(-1);

        }

        private void RemoveAllExampleIcons()
        {
            if (_stageScrollStageIcons.Any())
            {
                // 이미 다 지우고 실제 쓰는것들만남았다. 요청을 무시한다.
                return;
            }

            foreach (var icon in _content.GetComponentsInChildren<BattlePageStageScrollIcon>())
            {
                icon.gameObject.transform.SetParent(null);
                GameObject.Destroy(icon);
            }
            _stageScrollStageIcons.Clear();
        }


        public void SetSelectedChapterNumber(int chpaterNumber)
        {
            int chapterIconIndex = this.GetBattleStageIconIndexFromChapterNumber(chpaterNumber);
            Vector2 normalizedPosition = _scrollRect.normalizedPosition;
            normalizedPosition.x = this.ConvertIndexToNormalizedPositionX(chapterIconIndex);
            _scrollRect.normalizedPosition = normalizedPosition;
        }

        /// <summary>
        /// </summary>
        /// <param name="index">챕터 번호가 아닌 인덱스를 넣어주세요.(챕터2 => 인덱스 1)</param>
        /// <returns></returns>
        private float ConvertIndexToNormalizedPositionX(int index)
        {
            float x = (float)(index) / (_chapterCount - 1);
            x = Mathf.Clamp(x, 0.0f, 1.0f);
            return x;
        }

        //현재 선택된 스테이지 텍스트 정보를 업데이트 해준다.
        private void UpdateStageInfoText()
        {
            int currentSelectedChapterNumber = GetBattleStageIconChapterNumber(_currentSelectedChapterIndex);

            if (currentSelectedChapterNumber <= _clearedHighestChapterNumber)
            {
                StageRecordText = Localizer.Instance.GetText("UI_CLEAR");
            }
            else if (currentSelectedChapterNumber == (_clearedHighestChapterNumber + 1))
            {
                //이후 최장 생존시간 정보를 받아와 넣어준다.
                int minute = (int)_highestStageTimeInSeconds / 60;
                int second = (int)_highestStageTimeInSeconds % 60;
                StageRecordText = string.Format(Localizer.Instance.GetText("UI_BEST_RECORD"), minute, second);
            }
            else
            {
                StageRecordText = Localizer.Instance.GetText("UI_LOCKED");
            }

            //이후 스테이지 이름 정보를 받아와 넣어준다.
            var chapter = StaticDataRepository.Instance.Chapters.FindChapter(currentSelectedChapterNumber);
            if (chapter == null)
            {
                throw new LogicErrorException($"존재하지 않는 챕터에 접근하려 함. 요청된 챕터[{currentSelectedChapterNumber}]");
            }
            ChapterNumberText = Localizer.Instance.GetText("UI_CHAPTER") + " " + chapter.ChapterNumber.ToString();
            StageNameText = chapter.ChapterName;
            StageElementPath = chapter.ElementType.IconPath();
        }

        //선택 아이콘 좌, 우 에 있는 아이콘 까지만(화면 내의 아이콘) 스파인 알파효과를 적용한다.
        // 그 외엔 알파 효과를 꺼둔다.
        private void UpdateStageIcon()
        {
            if (_prevSelectedChapterIndex == _currentSelectedChapterIndex)
            {
                return;
            }

            _stageScrollStageIcons[_currentSelectedChapterIndex].PlaySelectSequence();
            if (_prevSelectedChapterIndex != null)
            {
                _stageScrollStageIcons[_prevSelectedChapterIndex.Value].PlayUnselectSequence();
            }

            for (int i = 0; i < _stageScrollStageIcons.Count; ++i)
            {
                _stageScrollStageIcons[i].ShowChapterIcon(_currentSelectedChapterIndex - 2 <= i && i <= _currentSelectedChapterIndex + 2);
            }
        }

        private void OptimizePosition()
        {
            if (_scrollRect.velocity.magnitude < 100 && !_isDrag && !_isOptimizing)
            {
                this.SetSelectedChapterNumberAndPlayMoveSequence(_currentSelectedChapterIndex);
            }
        }

        public void SetSelectedChapterNumberAndPlayMoveSequence(int selectedChapterNumber)
        {
            float targetNormalPositionX = this.ConvertIndexToNormalizedPositionX(selectedChapterNumber);

            if (_positionOptimizeSequence != null)
            {
                _positionOptimizeSequence.Kill();
                _positionOptimizeSequence = null;
            }
            _isOptimizing = true;

            _scrollRect.velocity = Vector3.zero;
            _positionOptimizeSequence = DOTween.Sequence(this);
            _positionOptimizeSequence.Append(_scrollRect.DONormalizedPos(new Vector2(targetNormalPositionX, 0f), 0.2f, false));
            _positionOptimizeSequence.OnComplete(() =>
            {
                _isOptimizing = false;
            });
            _positionOptimizeSequence.Play();
        }

        //현재 선택된 스테이지 인덱스 값은 전달한다.
        //_scrollRect의 normalizedPosition.x의 값은 0~1 이다
        public int GetBatteStageIconIndexFromScrollPosition()
        {
            float xPosition = _scrollRect.normalizedPosition.x + _IconBetweenWidth * 0.5f;

            if (xPosition >= 1)
            {
                return _chapterCount - 1;
            }
            else
            {
                int selectedChapterIndex = (int)(xPosition / _IconBetweenWidth);
                return selectedChapterIndex;
            }
        }

        //챕터 아이콘의 챕터 번호를 반환한다.
        //상황에 따라 아이콘 인덱스 번호와 챕터 번호가 다른 경우가 존재한다.
        private int GetBattleStageIconChapterNumber(int iconIndex)
        {
            if (iconIndex < _stageScrollStageIcons.Count &&
               iconIndex >= 0)
            {
                return _stageScrollStageIcons[iconIndex].ChapterNumber;
            }
            throw new ArgumentOutOfRangeException($"잘못된 iconIndex{iconIndex}값 입니다. {0}~{_stageScrollStageIcons.Count - 1}의 값만 입력이 가능합니다.");
        }

        public int GetCurrentSelectedChapterNumber()
        {
            int currentChapterNumber = this.GetBattleStageIconChapterNumber(_currentSelectedChapterIndex);
            return currentChapterNumber;
        }

        //챕터의 아이콘 인덱스 번호를 반환한다.
        public int GetBattleStageIconIndexFromChapterNumber(int chapterNumber)
        {
            for (int i = 0; i < _stageScrollStageIcons.Count; i++)
            {
                if (_stageScrollStageIcons[i].ChapterNumber == chapterNumber)
                {
                    return i;
                }
            }
            throw new ArgumentOutOfRangeException($"챕터 넘버에 따른 아이콘 정보가 존재하지 않습니다. 확인이 필요합니다. chapterNumber:{chapterNumber}");
        }

        public void OnDrag(PointerEventData data)
        {
            _isDrag = true;
            _onDragAction.Invoke(data);

            if (_positionOptimizeSequence != null)
            {
                _positionOptimizeSequence.Kill();
                _positionOptimizeSequence = null;
                _isOptimizing = false;
            }
        }

        public void OnEndDrag(PointerEventData data)
        {
            _isDrag = false;
            _onEndDragAction.Invoke(data);
            _scrollRect.onValueChanged.Invoke(Vector2.zero);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _onBeginDragAction.Invoke(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.dragging)
            {
                return;
            }

            if (eventData.pressPosition.y < Screen.height * 0.25f ||
               eventData.pressPosition.y > Screen.height * 0.75f)
            {
                //터치 영역 높이 아님
                return;
            }
            if (eventData.pressPosition.x < Screen.width * 0.25f)
            {
                //왼쪽 터치 체크
                this.SetSelectedChapterNumberAndPlayMoveSequence(_currentSelectedChapterIndex - 1);
                _touchLeftSequence.Restart();


            }
            else if (eventData.pressPosition.x > Screen.width * 0.75f)
            {
                //오른쪽 터치 체크
                this.SetSelectedChapterNumberAndPlayMoveSequence(_currentSelectedChapterIndex + 1);
                _touchRightSequence.Restart();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            //아무것도 하지 않는다.
            //해당 인터페이스가 없으면 OnPointerUp 이벤트에 들어오지 않음
        }
    }
}
