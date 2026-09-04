#nullable enable
using Shared.DataTables;
using Shared.StaticDatas;
using UnityEngine;
using SamMul.GameClients;

namespace SamMul.UIs.Lobbies.MainLobbyPages
{
    /// <summary>
    /// 로비 화면. 첫 줄 챕터, 둘째 줄 캐릭터, 셋째 줄 아이템을 각각 하나씩 고르고 시작 버튼으로 스테이지에 들어간다.
    /// 각 줄의 기본 선택은 첫 항목이다.
    /// </summary>
    public class MainLobbyPage : MonoBehaviour
    {
        [SerializeField] private LobbyRadioGroup _chapterGroup = null!;
        [SerializeField] private LobbyRadioGroup _heroGroup = null!;
        [SerializeField] private LobbyRadioGroup _equipmentGroup = null!;
        [SerializeField] private MainChapterStartButton _mainChapterStartButton = null!;

        private ChapterStaticData _selectedChapter = null!;

        public void Initialize()
        {
            Debug.Assert(_chapterGroup);
            Debug.Assert(_heroGroup);
            Debug.Assert(_equipmentGroup);
            Debug.Assert(_mainChapterStartButton);

            var chapters = StaticDataRepository.Instance.Chapters.All;
            var heroes = StaticDataRepository.Instance.Heroes.All;
            var equipments = StaticDataRepository.Instance.Equipments.All;
            var userGameData = GameClient.CS.UserGameData;

            _chapterGroup.Initialize(chapters.Count, index => _selectedChapter = chapters[index]);
            _chapterGroup.Select(0);

            _heroGroup.Initialize(heroes.Count, index =>
            {
                userGameData.SelectedHeroType = heroes[index].HeroType;
                GameClient.CS.Save();
            });
            _heroGroup.Select(0);

            _equipmentGroup.Initialize(equipments.Count, index =>
            {
                userGameData.SelectedEquipmentId = equipments[index].Id;
                GameClient.CS.Save();
            });
            _equipmentGroup.Select(0);

            _mainChapterStartButton.Initialize(() => _selectedChapter);
        }
    }
}
