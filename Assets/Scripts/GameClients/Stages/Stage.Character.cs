#nullable enable
using Shared.DataTables;
using Shared.GameDataTypes;
using Shared.GameLogics;
using Shared.StaticDatas;
using Shared.UserDatas;
using System;
using System.Collections.Generic;
using UnityEngine;
using Z.GameClients.Stages.Characters;
using Z.GameClients.Stages.Characters.Monsters;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs;
using Z.GameClients.Stages.Characters.Monsters.MonsterAIs.AIStrategies;
using Z.GameClients.Stages.Characters.PCs;
using Z.GameClients.Stages.CombatSystems;
using Z.Loggers;
using Z.Scenes;
using Z.UnityHelpers;

namespace Z.GameClients.Stages
{

    public partial class Stage
    {
        // Partial 클래스의 멤버는 생성자가 정의된 기본 코드파일에 정의해주세요. 
        // 이 클래스의 경우, Stage.cs가 멤버를 정의할 기본 코드 파일입니다.

        public Monster CreateMonster(AllianceType alliance, CharacterType monsterType, MonsterInstanceInitialData initialData, bool isBoss, bool isElite, MonsterAIBlackboardBase? aiBlackboard)
        {
            var poolingKey = Character.MakePoolKey(monsterType);
            var newMonster = _characterPool.TakeOneMonsterFromPool(poolingKey);
            var monsterStaticData = _staticDatas.Monsters.Get(monsterType);

            newMonster.gameObject.SetActive(true);
            newMonster.InitializeMonster(alliance, monsterStaticData, initialData, stage: this, isBoss: isBoss, isElite, aiBlackboard);
            newMonster.transform.position = initialData.spawnPoint;

            _monstersCreatedOnThisFrame.Add(newMonster);

            newMonster.OnEnterredIntoStage(this);
            return newMonster;
        }

        public Monster CreateMonster(AllianceType alliance, CharacterType monsterType, MonsterInstanceInitialData initialData, bool isBoss, bool isElite)
            => CreateMonster(alliance, monsterType, initialData, isBoss, isElite, aiBlackboard: null);

        /// <summary>
        /// 몬스터를 미리 생성해서 풀링해둔다.
        /// </summary>
        public void PreCreateMonster(CharacterType monsterType, int count)
        {
            // 미리 생성해서 캐싱하는 몬스터는, Initialize해서는 안 된다.
            // 1) Initialize하면서 AI가 초기화되고, 보스 AI에 버그가 생긴다.
            // 2) 실제로 캐싱해둘 내용은 AllocateShared의 리소스들이고, Initialize로직에서 생성한것들은 매번 새로 생성하기 때문에, 불필요한 초기화이기도 한다.
            var poolingKey = Character.MakePoolKey(monsterType);
            _characterPool.Reserve(poolingKey, count);
        }

        //몬스터 소환시 사용하는 인터페이스 입니다.
        //Apper 액션으로 전환하지 않습니다. (소환 초기화 단계에서 SummonAction을 넣어주기 때문에)
        public ISummonedMonsterCommandSender SummonMonster(AllianceType allianceType, CharacterType monsterType, MonsterInstanceInitialData initialData, float summonTime)
        {
            return this.SummonMonster(allianceType, monsterType, initialData, summonTime, false);
        }

        public ISummonedMonsterCommandSender SummonMonster(AllianceType allianceType, CharacterType monsterType, MonsterInstanceInitialData initialData, float summonTime, bool isBoss)
        {
            var poolingKey = Character.MakePoolKey(monsterType);
            var newMonster = _characterPool.TakeOneMonsterFromPool(poolingKey);
            var monsterStaticData = _staticDatas.Monsters.Get(monsterType);

            newMonster.gameObject.SetActive(true);
            ISummonedMonsterCommandSender commandSender = newMonster.InitializeSummonMonster(allianceType, monsterStaticData, initialData, stage: this, summonTime, isBoss);
            newMonster.transform.position = initialData.spawnPoint;

            _monstersCreatedOnThisFrame.Add(newMonster);

            return commandSender;
        }

        private void CreatePlayerCharacter(
            IReadOnlyList<SkillId> userSkillDeck,
            HeroStaticData heroStaticData,
            HeroData heroData,
            IEnumerable<EquipmentData> equippedEquipments,
            EvolutionData evolutionData)
        {
            var userPC = _characterPool.TakeOnePlayerCharacterFromPool(heroData.HeroType);

            // PC는 무조건 첫번째 요소여야 한다.
            _allianceCharacters[(int)AllianceType.Players].Insert(0, userPC);
            _playerCharacters.Insert(0, userPC);

            float elementBonusRate = AvatarLogic.CalculateElementBonusRate(heroStaticData.ElementType, this.StageElementType);

            userPC.InitializePlayerCharacter(userSkillDeck, heroStaticData, heroData, equippedEquipments, evolutionData, elementBonusRate, this);
            userPC.gameObject.SetActive(false);
        }

        // 유니티 GC를 괴롭히지 않도록 임시 컨테이너를 재활용한다. v 는 volatile의 v
        private readonly List<Character> v_charactersToRemove = new List<Character>();
        private void UpdateCharacters(float now, float deltaTime)
        {
#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("Stage.UpdateCharacters"))
#endif
            {
#if USE_SCOPED_PROFILER
                using (new ScopedProfiler("Stage.UpdateCharacters.HandleNewMonsters"))
#endif
                {
                    // 이전프레임에 생성되었던 몬스터를 _aliveMonsters에 옮겨 담는다.
                    foreach (var character in _monstersCreatedOnThisFrame)
                    {
                        _allianceCharacters[(int)character.Alliance].Add(character);
                    }
                    _monstersCreatedOnThisFrame.Clear();
                }

#if USE_SCOPED_PROFILER
                using (new ScopedProfiler("Stage.UpdateCharacters.UpdatePlayersAlliance"))
#endif
                {
#if USE_SCOPED_PROFILER
                    using (new ScopedProfiler("Players.UpdateLogic"))
#endif
                    {
                        foreach (var character in _allianceCharacters[(int)AllianceType.Players])
                        {
                            character.UpdateLogic(this);
                        }
                    }
                }

#if USE_SCOPED_PROFILER
                using (new ScopedProfiler("Stage.UpdateCharacters.UpdateMonstersAlliance"))
#endif
                {
                    foreach (var character in _allianceCharacters[(int)AllianceType.Monsters])
                    {
                        character.UpdateLogic(this);
                        CheckAndRePosition(character);
                    }
                }

#if USE_SCOPED_PROFILER
                using (new ScopedProfiler("Stage.UpdateCharacters.RemoveCharacters"))
#endif
                {
                    // NOTE: 첫번째 요소는 PC인데, PC Dead에 관한 처리는 씬 변경할때 처리를 한다.
                    for (int i = 1; i < _allianceCharacters[(int)AllianceType.Players].Count; ++i)
                    {
                        Character character = _allianceCharacters[(int)AllianceType.Players][i];

                        if (!this.WillRemoveCharacter(character, now))
                        {
                            continue;
                        }

                        v_charactersToRemove.Add(character);
                    }

                    foreach (var character in _allianceCharacters[(int)AllianceType.Monsters])
                    {
                        if (!this.WillRemoveCharacter(character, now))
                        {
                            continue;
                        }

                        v_charactersToRemove.Add(character);
                    }

                    foreach (var character in v_charactersToRemove)
                    {
                        _allianceCharacters[(int)character.Alliance].Remove(character);
                        character.gameObject.SetActive(false);
                        _characterPool.PutBackCharacter(character);
                    }

                    v_charactersToRemove.Clear();
                }
            }
        }

        /// <summary>
        /// 몬스터를 제거할지 여부를 확인합니다.
        /// </summary>
        /// <param name="character">
        /// 제거 여부를 확인할 몬스터입니다.
        /// </param>
        /// <param name="now">
        /// 현재 시각입니다.
        /// </param>
        /// <returns>
        /// 제거 조건을 만족하면 true, 만족하지 않으면 false를 반환합니다.
        /// </returns>
        private bool WillRemoveCharacter(Character character, float now)
        {
            if (!character.Action.IsDead)
            {
                return false;
            }

            if (character is PlayerCharacter pc &&
                pc.IsFakeDead)
            {
                return false;
            }

            if (now < character.WillBeRemovedFromStageAt)
            {
                return false;
            }

            return true;
        }

        //스테이지 존재하는 모든 몬스터 Dead 호출
        public void KillAllMonsterAllianceCharacters()
        {
            //NOTE: //이번 프레임에 생성될 몬스터들을 미리 소환 리스트에 집어넣고 Dead처리를 진행한다.
            //이 작업을 진행하지 않으면 몬스터들이 제거되고 이번프레임에 소환 대기중이던 몬스터가 소환되어
            //사용자가 원하던 그림과 많이 다른 결과를 얻게 된다.

            foreach (var character in _monstersCreatedOnThisFrame)
            {
                _allianceCharacters[(int)character.Alliance].Add(character);
            }
            _monstersCreatedOnThisFrame.Clear();

            foreach (var monster in _allianceCharacters[(int)AllianceType.Monsters])
            {
                monster.ForceKillSelf(stage: this);
            }
        }

        /// <summary>
        /// 보스가 소환될 때 스테이지에 남아 있는 일반 몬스터들을 모두 제거합니다.
        /// </summary>
        public void RemoveAllMonstersOnBossSpawn()
        {
            foreach (var character in _monstersCreatedOnThisFrame)
            {
                _allianceCharacters[(int)character.Alliance].Add(character);
            }
            _monstersCreatedOnThisFrame.Clear();

            foreach (var monster in _allianceCharacters[(int)AllianceType.Monsters])
            {
                monster.DisappearFromStage(stage: this);
            }
        }

        public void CheckAndRePosition(Character character)
        {
#if USE_SCOPED_PROFILER
            using (new ScopedProfiler("Stage.Characters.CheckAndReposition"))
#endif
            {
                Monster? monster = character as Monster;
                Debug.Assert(monster != null);
                if (monster!.IsBoss)
                {
                    return;
                }

                float distance = GameClient.CameraController.OrthographicSize * 1.77f;
                Vector3 pcPosition = this.PC.transform.position;
                Vector3 direction = character.transform.position - pcPosition;
                if (distance * distance < direction.sqrMagnitude)
                {
                    Rect walkableArea = StaticData.GetWalkableArea();
                    Vector2 newPosition;
                    if (StaticData.SpawnLogicType == SpawnLogicType.FromVerticalBoundary)
                    {
                        bool isTop = UnityEngine.Random.Range(0, 2) == 0;
                        newPosition = new Vector2(UnityEngine.Random.Range(walkableArea.xMin, walkableArea.xMax), pcPosition.y);
                        newPosition.y += isTop ? distance : distance * -1f;
                    }
                    else
                    {
                        Vector3 newDir = Quaternion.AngleAxis(UnityEngine.Random.Range(0.0f, 360.0f), Vector3.back) * Vector3.up;
                        newPosition = pcPosition + (newDir * distance);
                    }
                    if (!walkableArea.Contains(newPosition))
                    {
                        newPosition.x = Mathf.Max(walkableArea.xMin, newPosition.x);
                        newPosition.x = Mathf.Min(walkableArea.xMax, newPosition.x);
                        newPosition.y = Mathf.Max(walkableArea.yMin, newPosition.y);
                        newPosition.y = Mathf.Min(walkableArea.yMax, newPosition.y);
                    }
                    character.transform.localPosition = newPosition;
                    monster.MonsterAIEvent.OnRePosition(this);
                }
            }
        }

        private static bool AllowAllCharacters(Character character) => true;

        private List<Character> v_closestTargetCharacters = new List<Character>();

        public Character? FindClosestCharacter(AllianceType allianceType, Vector2 position, float limitDistance, Func<Character, bool> condition)
        {
            // REMARK : 
            // 범위가 일정 크기를 넘어선 경우, (존 크기 기준 인접존 이상)
            // 1) 인접존 범위 내에서 검색하고, 여기에 가까운 캐릭터가 있으면 바로 리턴해주고
            // 2) 인섭존 내부에 없으면 확장해서 추가 검색한다
            //
            // 성능 최적화가 필요한 경우는 몬스터가 많은 경우이고, 이 때엔 인접존 내에서 탐색을 끝내 전체범위를 모두 탐색하지 않도록 최적화한다.
            // 몬스터가 많이 없는 경우에는, 두번 탐색하더라도 문제 없다. 개별 호출 내의 탐색비용도 저렴하고, 탐색이 호출 횟수도 적기 때문이다.
            //
            // 단순히 코드 라인에서 호출횟수를 줄이는 것이 최적화가 아니다.
            // 우리는 n 제곱의 성능부하를 다루고 있음에 유의할 것.
            v_closestTargetCharacters.Clear();

            // 탐색범위 반지름이 존 한 두개 크기를 넘어서면, 존 두개 너비 내에서 선탐색한다.
            float FAST_SERACHING_DISTANCE = GameConstants.ZONE_WIDTH * 1.5f;
            if (FAST_SERACHING_DISTANCE < limitDistance)
            {
                this.FindCharactersInArea(allianceType, new CircularTargetArea(position, FAST_SERACHING_DISTANCE), condition, v_closestTargetCharacters);
                if (v_closestTargetCharacters.Count <= 0)
                {
                    this.FindCharactersInArea(allianceType, new CircularTargetArea(position, limitDistance), condition, v_closestTargetCharacters);
                }
            }
            else
            {
                this.FindCharactersInArea(allianceType, new CircularTargetArea(position, limitDistance), condition, v_closestTargetCharacters);
            }

            return FindClosestCharacterFromPosition(position, candidates: v_closestTargetCharacters, condition: AllowAllCharacters);
        }

        public Character? FindClosestCharacter(AllianceType allianceType, float limitDistance, Vector2 position)
        {
            return this.FindClosestCharacter(allianceType, position, limitDistance, condition: AllowAllCharacters);
        }

        public static Character? FindClosestCharacterFromPosition(Vector2 position, IReadOnlyList<Character> candidates, Func<Character, bool> condition)
        {
            if (candidates == null || candidates.Count <= 0)
            {
                return null;
            }

            Character? closestTarget = null;
            float closestDistance = float.MaxValue;

            int count = candidates.Count;
            if (count == 1)
            {
                return candidates[0];
            }

            for (int i = 0; i < count; i++)
            {
                Character other = candidates[i];

                if (!condition.Invoke(other))
                {
                    continue;
                }

                float distance = Vector2.SqrMagnitude(other.Pos - position);
                if (closestDistance > distance)
                {
                    closestDistance = distance;
                    closestTarget = other;
                }
            }

            return closestTarget;
        }

        public void FindAliveCharactersInArea(AllianceType alliance, CircularTargetArea area, in List<Character> result)
        {
            this.FindCharactersInArea(
                alliance,
                area,
                condition: (Character other) => !other.Action.IsDead,
                in result
            );
        }

        public void FindAliveCharactersInArea(AllianceType alliance, CircularSectorTargetArea area, in List<Character> result)
        {
            this.FindCharactersInArea(
                alliance,
                area,
                condition: (Character other) => !other.Action.IsDead,
                in result
                );
        }

        public void FindAliveCharactersInArea(AllianceType alliance, SquareTargetArea area, in List<Character> result)
        {
            this.FindCharactersInArea(
                alliance,
                area,
                condition: (Character other) => !other.Action.IsDead,
                in result
            );
        }

        /// <summary>
        /// 1차 탐색범위에 대상이 있다면 해당 목록을 <paramref name="result"/>로 돌려준다.
        /// 1차 탐색범위에서 대상을 찾지 못한 경우, 2차 탐색 범위로 넓혀 대상을 탐색하고 해당 목록을 <paramref name="result"/>로 돌려준다.
        /// </summary>
        /// <param name="searchingAreaRadius">1차 탐색 범위</param>
        /// <param name="fallbackSearchingAreaRadius">1차 탐색 범위에 대상이 없을 경우 추가 탐색할 2차 탐색 범위</param>
        public void FindAliveCharactersInArea(
            AllianceType alliance,
            Vector2 searchingAreaCenterPosition,
            float searchingAreaRadius,
            float fallbackSearchingAreaRadius,
            in List<Character> result)
        {
            if (searchingAreaRadius >= fallbackSearchingAreaRadius)
            {
                Log.I.Warn($"{nameof(FindAliveCharactersInArea)}의 2차 탐색범위는 1차탐색범위보다 넓어야 합니다. searchingAreaRadius[{searchingAreaRadius}] fallbackSearchingAreaRadius[{fallbackSearchingAreaRadius}]");
            }

            this.FindCharactersInArea(
                alliance,
                new CircularTargetArea(searchingAreaCenterPosition, searchingAreaRadius),
                condition: (Character other) => !other.Action.IsDead,
                in result);

            if (result.Count <= 0)
            {
                this.FindCharactersInArea(
                    alliance,
                    new CircularTargetArea(searchingAreaCenterPosition, fallbackSearchingAreaRadius),
                    condition: (Character other) => !other.Action.IsDead,
                    in result);
            }
        }

        /// <param name="condition">이 조건을 만족하는 캐릭터들만 결과에 포함합니다.</param>
        private List<Character> v_findTargetCharacters = new List<Character>();
        public void FindCharactersInArea(AllianceType alliance, CircularTargetArea area, Func<Character, bool> condition, in List<Character> result)
        {
            if (AllianceType.Players == alliance)
            {
                var characters = _allianceCharacters[(int)AllianceType.Players];
                int count = characters.Count;

                for (int i = 0; i < count; ++i)
                {
                    var character = characters[i];
                    if (IsInArea(character, area)
                        && condition(character))
                    {
                        result.Add(character);
                    }
                }

                return;
            }

            v_findTargetCharacters.Clear();
            _zoneManager.TryGetCharacters(area.GetMinMaxRect(), in v_findTargetCharacters);

            int targetCount = v_findTargetCharacters.Count;
            for (int i = 0; i < targetCount; ++i)
            {
                var character = v_findTargetCharacters[i];

                if (character.Alliance != alliance)
                {
                    continue;
                }

                if (IsInArea(character, area) &&
                   condition(character))
                {
                    result.Add(character);
                }
            }
        }

        public void FindCharactersInArea(AllianceType alliance, CircularSectorTargetArea area, Func<Character, bool> condition, in List<Character> result)
        {
            List<Character> characters = new List<Character>();
            _zoneManager.TryGetCharacters(area.GetMinMaxRect(), in characters);

            foreach (var character in _allianceCharacters[(int)alliance])
            {
                if (area.Contains(character.Pos) &&
                    condition(character))
                {
                    result.Add(character);
                }
            }
        }

        public void FindCharactersInArea(AllianceType alliance, SquareTargetArea area, Func<Character, bool> condition, in List<Character> result)
        {
            if (AllianceType.Players == alliance)
            {
                foreach (var character in _allianceCharacters[(int)AllianceType.Players])
                {
                    if (IsInArea(character, area)
                        && condition(character))
                    {
                        result.Add(character);
                    }
                }
                return;
            }

            v_findTargetCharacters.Clear();
            _zoneManager.TryGetCharacters(area.GetMinMaxRect(), in v_findTargetCharacters);

            foreach (var character in v_findTargetCharacters)
            {
                if (character.Alliance != alliance)
                {
                    continue;
                }

                if (IsInArea(character, area) &&
                    condition(character))
                {
                    result.Add(character);
                }
            }
        }

        public static bool IsInArea(Character character, CircularTargetArea area)
        {
            float squaredSearchingRadius = (area.Radius + character.ColliderRadius);
            squaredSearchingRadius *= squaredSearchingRadius;

            if ((character.Pos - area.Center).sqrMagnitude <= squaredSearchingRadius)
            {
                return true;
            }

            return false;
        }

        public void FindCharactersInArea(AllianceType alliance, SquareTargetArea area, in List<Character> result)
        {
            v_findTargetCharacters.Clear();
            _zoneManager.TryGetCharacters(area.GetMinMaxRect(), in v_findTargetCharacters);

            foreach (var character in v_findTargetCharacters)
            {
                if (character.Alliance != alliance)
                {
                    continue;
                }

                if (IsInArea(character, area))
                {
                    result.Add(character);
                }
            }
        }

        public static bool IsInArea(Character character, SquareTargetArea area)
        {
            if (area.Contains(character.Pos, character.ColliderRadius))
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public void FindCharactersInStage(AllianceType alliance, Func<Character, bool> condition, in List<Character> result)
        {
            foreach (var character in _allianceCharacters[(int)alliance])
            {
                if (condition(character))
                {
                    result.Add(character);
                }
            }
        }

        public void IncreaseMonsterKillCount(bool isBoss, bool isElite)
        {
            EliminatedBosses += isBoss ? 1 : 0;
            EliminatedElites += isElite ? 1 : 0;
            EliminatedMonsters += 1;
            var stageSceneUI = UnityGlobal.Scenes.GetCurrentSceneUI<StageSceneUIRoot>();
            Debug.Assert(stageSceneUI != null);
            stageSceneUI!.UpdateKillCount(EliminatedMonsters);
        }

        /// <summary>
        /// 캐릭터를 강제로 현재 사각형 내부에 가둡니다.
        /// 울타리가 있는 경우 울타리 내부, 없는 경우는 맵 내부가 현재 사각형이 됩니다.
        /// </summary>
        /// <param name="character">
        /// 사각형 내부에 가둘 캐릭터.
        /// </param>
        /// <remarks>
        /// 성능상의 문제로 이 함수를 매 프레임 모든 캐릭터를 대상으로 호출하는 것은 매우 강력하게 권장되지 않습니다.
        /// 필요한 경우에만 제한적으로 호출하세요.
        /// </remarks>
        public void ForceClampCharacterToCurrentRect(Character character)
        {
            // 변수 캐싱.
            var rect = FenceRect ?? StaticData.GetWalkableArea();
            var position = character.Pos;
            var colliderRadius = character.ColliderRadius;

            // 새로운 좌표 계산.
            position.x = Mathf.Clamp(position.x, rect.xMin + colliderRadius, rect.xMax - colliderRadius);
            position.y = Mathf.Clamp(position.y, rect.yMin + colliderRadius, rect.yMax - colliderRadius);

            // 새로운 좌표가 원래 좌표와 다르면 위치 재설정.
            if (position != character.Pos)
            {
                character.transform.position = position;
            }
        }
    }
}
