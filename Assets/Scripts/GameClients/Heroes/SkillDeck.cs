using Shared.DataTables;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SamMul.GameClients.Stages.Characters.PCs.Skills;
using SamMul.Loggers;

namespace SamMul.GameClients.Heroes
{
    // 인 게임에서만 사용중...
    // 인게임에서 PlayerCharcater가 가지고 있다.
    public class SkillDeck
    {
        public string AvailableSkillDeck => string.Join(", ", _availableSkillDeck);
        public string BannedSkillDeck => string.Join(", ", _bannedSkillDeck);

        private readonly List<SkillId> _availableSkillDeck;
        private readonly List<SkillId> _bannedSkillDeck;

        public SkillDeck()
        {
            _availableSkillDeck = new List<SkillId>();
            _bannedSkillDeck = new List<SkillId>();
        }

        public void Initialize(HeroType heroType, IReadOnlyList<SkillId> userSkillDeck, int activeSkillCount, int passiveSkillCount)
        {
            // 액티브 스킬 정리.
            var activeSkills = userSkillDeck.Where(x => IsActiveSkill(x)).ToList();
            if (activeSkills.Count <= 0)
            {
                throw new LogicErrorException($"유저 스킬 덱에 액티브 스킬이 하나도 없습니다. 게임을 진행할 수 없습니다. {string.Join(", ", userSkillDeck)}");
            }

            // 캐릭터 기본 액티브 스킬 추가.
            var heroBasicActiveSkill = GetHeroBasicSkill(heroType);
            if (activeSkills.Contains(heroBasicActiveSkill))
            {
                Log.I.Error($"유저 스킬 덱에 [{heroType}]의 기본 액티브 스킬 {activeSkills}이 포함되어 있습니다. 캐릭터 전용 스킬이 포함되어 있으면 안 됩니다.");
                activeSkills.Remove(heroBasicActiveSkill);
            }
            activeSkills.Add(heroBasicActiveSkill);

            // 액티브 스킬의 개수가 요구된 개수보다 적은 경우 경고.
            if (activeSkills.Count < activeSkillCount)
            {
                Log.I.Error($"유저 스킬 덱의 액티브 스킬 개수는 {activeSkills.Count}개인데 스킬덱 초기화 인수로 전달된 액티브 스킬 개수는 {activeSkillCount}개입니다. {activeSkills.Count}개로 줄여 로직을 진행합니다.");
                activeSkillCount = activeSkills.Count;
            }

            // 패시브 스킬 정리.
            var passiveSkills = userSkillDeck.Where(x => IsPassiveSkill(x)).ToList();
            if (passiveSkills.Count <= 0)
            {
                throw new LogicErrorException($"유저 스킬 덱에 패시브 스킬이 하나도 없습니다. 게임을 진행할 수 없습니다. {string.Join(", ", userSkillDeck)}");
            }

            // 모든 액티브 스킬의 초월 조건 패시브 스킬을 포함하고 있는지 확인.
            foreach (var activeSkill in activeSkills)
            {
                var transcendConditionSkill = GetTranscendConditionSkill(activeSkill);
                if (!passiveSkills.Contains(transcendConditionSkill))
                {
                    Log.I.Error($"유저 스킬 덱에 액티브 스킬 {activeSkill}의 초월 조건 패시브 스킬 {transcendConditionSkill}이 포함되지 않았습니다. 강제로 추가합니다.");
                    passiveSkills.Add(transcendConditionSkill);
                }
            }

            // 패시브 스킬의 개수가 요구된 개수보다 적은 경우 경고.
            if (passiveSkills.Count < passiveSkillCount)
            {
                Log.I.Error($"유저 스킬 덱의 패시브 스킬 개수는 {passiveSkills.Count}개인데 스킬덱 초기화 인수로 전달된 패시브 스킬 개수는 {passiveSkillCount}개입니다. {passiveSkills.Count}개로 줄여 로직을 진행합니다.");
                passiveSkillCount = passiveSkills.Count;
            }

            // 액티브 스킬, 패시브 스킬의 개수로 입력된 값이 모두 0보다 작으면 모든 스킬을 포함한다.
            if (activeSkillCount < 0 && passiveSkillCount < 0)
            {
                _availableSkillDeck.Clear();
                _availableSkillDeck.AddRange(activeSkills);
                _availableSkillDeck.AddRange(passiveSkills);

                _bannedSkillDeck.Clear();
            }
            else
            {
                // 선택된 액티브 스킬과 패시브 스킬.
                var pickedActiveSkills = new List<SkillId>(activeSkillCount);
                var pickedPassiveSkills = new List<SkillId>(passiveSkillCount);

                // 캐릭터 기본 액티브 스킬을 먼저 추가한다.
                pickedActiveSkills.Add(heroBasicActiveSkill);
                activeSkills.Remove(heroBasicActiveSkill);

                // 나머지 액티브 스킬 추가.
                while (pickedActiveSkills.Count < activeSkillCount)
                {
                    // 만약 남은 액티브 스킬 개수가 0보다 적을 경우 중단. 개수를 조정했기 때문에 정상적인 흐름에서 이런 경우는 없다.
                    if (activeSkills.Count <= 0)
                    {
                        Log.I.Error($"액티브 스킬을 {activeSkillCount}개 채워야 하는데 남은 개수가 없어서 {pickedActiveSkills.Count}개밖에 채우지 못했습니다.");
                        break;
                    }

                    int index = Random.Range(0, activeSkills.Count);
                    pickedActiveSkills.Add(activeSkills[index]);
                    activeSkills.RemoveAt(index);
                }

                // 초월 조건 패시브 스킬을 먼저 추가한다.
                foreach (var activeSkill in pickedActiveSkills)
                {
                    var transcendConditionSkill = GetTranscendConditionSkill(activeSkill);

                    // 만약 이미 추가되어 있으면 다시 추가하지 않는다.
                    if (pickedPassiveSkills.Contains(transcendConditionSkill))
                    {
                        continue;
                    }

                    pickedPassiveSkills.Add(transcendConditionSkill);
                    passiveSkills.Remove(transcendConditionSkill);
                }

                // 나머지 패시브 스킬 추가.
                while (pickedPassiveSkills.Count < passiveSkillCount)
                {
                    // 만약 남은 패시브 스킬 개수가 0보다 적을 경우 중단. 개수를 조정했기 때문에 정상적인 흐름에서 이런 경우는 없다.
                    if (passiveSkills.Count <= 0)
                    {
                        Log.I.Error($"패시브 스킬을 {passiveSkillCount}개 채워야 하는데 남은 개수가 없어서 {pickedActiveSkills.Count}개밖에 채우지 못했습니다.");
                        break;
                    }

                    int index = Random.Range(0, passiveSkills.Count);
                    pickedPassiveSkills.Add(passiveSkills[index]);
                    passiveSkills.RemoveAt(index);
                }

                _availableSkillDeck.Clear();
                _availableSkillDeck.AddRange(pickedActiveSkills);
                _availableSkillDeck.AddRange(pickedPassiveSkills);

                _bannedSkillDeck.Clear();
                _bannedSkillDeck.AddRange(activeSkills);
                _bannedSkillDeck.AddRange(passiveSkills);
            }

            LogSkillDeck(heroType, _availableSkillDeck, _bannedSkillDeck);
        }

        private static SkillId GetHeroBasicSkill(HeroType heroType)
        {
            Debug.Assert(heroType != HeroType.Invalid);
            var heroBasicSkill = StaticDataRepository.Instance.Heroes.Get(heroType).BasicSkill;
            Debug.Assert(heroBasicSkill != SkillId.Invalid);
            Debug.Assert(IsActiveSkill(heroBasicSkill));
            return heroBasicSkill;
        }

        private static SkillId GetTranscendConditionSkill(SkillId skillId)
        {
            Debug.Assert(skillId != SkillId.Invalid);
            var transcendConditionSkill = StaticDataRepository.Instance.Skills.Get(new SkillKey(skillId, GameConstants.SKILL_MAX_LEVEL)).TranscendCondition;
            Debug.Assert(transcendConditionSkill != SkillId.Invalid);
            Debug.Assert(IsPassiveSkill(transcendConditionSkill));
            return transcendConditionSkill;
        }

        private static bool IsActiveSkill(SkillId skillId)
        {
            Debug.Assert(skillId != SkillId.Invalid);
            return StaticDataRepository.Instance.Skills.GetSkills(skillId)[0].skillType == SkillType.Active;
        }

        private static bool IsPassiveSkill(SkillId skillId)
        {
            Debug.Assert(skillId != SkillId.Invalid);
            return StaticDataRepository.Instance.Skills.GetSkills(skillId)[0].skillType == SkillType.Passive;
        }

        private static void LogSkillDeck(HeroType heroType, IReadOnlyList<SkillId> availableSkillDeck, IReadOnlyList<SkillId> bannedSkillDeck)
        {
#if UNITY_EDITOR
            Log.I.Debug(
                $"{heroType} Available Skill List\n" +
                "Active\n" +
                string.Join(",", availableSkillDeck.Where(x => IsActiveSkill(x))) + "\n" +
                "Passive\n" +
                string.Join(",", availableSkillDeck.Where(x => IsPassiveSkill(x))) + "\n");

            Log.I.Debug(
                $"{heroType} Banned Skill List\n" +
                "Active\n" +
                string.Join(",", bannedSkillDeck.Where(x => IsActiveSkill(x))) + "\n" +
                "Passive\n" +
                string.Join(",", bannedSkillDeck.Where(x => IsPassiveSkill(x))) + "\n");
#endif
        }

        public bool ContainsSkill(SkillId skillId)
        {
            return _availableSkillDeck.Contains(skillId);
        }

        private readonly List<SkillId> v_skillCandidates = new List<SkillId>();
        private readonly List<SkillKey> v_selectedSkills = new List<SkillKey>();
        public SkillKey[] Select3SkillsToLearn(SkillSet skillSet, int activeSkillCount, int passiveSkillCount)
        {
            Debug.Assert(activeSkillCount + passiveSkillCount <= 3, "activeSkillCount, passiveSkillCount 합이 3이상 입니다. 해당 인터페이스는 3개의 스킬을 전달하는 인터페이스입니다.");

            v_selectedSkills.Clear();
            v_skillCandidates.Clear();

            foreach (var skillId in _availableSkillDeck)
            {
                if (skillId == SkillId.Invalid)
                {
                    continue;
                }

                // 스킬을 더 배울 공간이 없으면, 새로운 스킬은 배울 수 없다.
                if (skillSet.IsSlotFull && !skillSet.HasSkill(skillId))
                {
                    continue;
                }

                if (skillSet.IsAcquirableSkill(skillId))
                {
                    if (!v_skillCandidates.Contains(skillId))
                    {
                        v_skillCandidates.Add(skillId);
                    }
                }
            }

            //액티브 스킬 패시브 스킬 분류
            List<int> activeSkillIndexs = new List<int>();
            List<int> passiveSkillIndexs = new List<int>();

            for (int i = 0; i < v_skillCandidates.Count; i++)
            {
                var data = StaticDataRepository.Instance.Skills.GetSkills(v_skillCandidates[i]);
                if (data[0].skillType == SkillType.Active)
                {
                    activeSkillIndexs.Add(i);
                }
                else if (data[0].skillType == SkillType.Passive)
                {
                    passiveSkillIndexs.Add(i);
                }
            }

            //외부에서 요청하는 타입 개수만큼 넣어줌

            for (int i = 0; i < activeSkillCount; i++)
            {
                if (activeSkillIndexs.Count <= 0)
                {
                    break;
                }
                int index = Random.Range(0, activeSkillIndexs.Count);
                int activeSkillIndex = activeSkillIndexs[index];
                var selectedSkill = v_skillCandidates[activeSkillIndex];
                int currentSkillLevel = skillSet.GetSkillLevel(selectedSkill);
                v_selectedSkills.Add(new SkillKey(selectedSkill, currentSkillLevel + 1));

                activeSkillIndexs.RemoveAt(index);
            }

            for (int i = 0; i < passiveSkillCount; i++)
            {
                if (passiveSkillIndexs.Count <= 0)
                {
                    break;
                }

                int index = Random.Range(0, passiveSkillIndexs.Count);
                int passiveSkillIndex = passiveSkillIndexs[index];
                var selectedSkill = v_skillCandidates[passiveSkillIndex];
                int currentSkillLevel = skillSet.GetSkillLevel(selectedSkill);
                v_selectedSkills.Add(new SkillKey(selectedSkill, currentSkillLevel + 1));

                passiveSkillIndexs.RemoveAt(index);
            }


            //이미 선택된 스킬 목록은 제거
            for (int i = 0; i < v_selectedSkills.Count; i++)
            {
                v_skillCandidates.Remove(v_selectedSkills[i].Id);
            }

            int remainingCount = 3 - v_selectedSkills.Count;

            for (int i = 0; i < remainingCount; ++i)
            {
                if (v_skillCandidates.Count <= 0)
                {
                    break;
                }

                int randomIndex = Random.Range(0, v_skillCandidates.Count);
                var selectedSkill = v_skillCandidates[randomIndex];
                int currentSkillLevel = skillSet.GetSkillLevel(selectedSkill);
                v_selectedSkills.Add(new SkillKey(selectedSkill, currentSkillLevel + 1));
                v_skillCandidates.RemoveAt(randomIndex);
            }

            return v_selectedSkills.ToArray();
        }

        public SkillKey[] SelectSkillsForTutorialChapter(SkillSet skillSet, int avatarLevel, HeroStaticData characterStaticData)
        {
            v_selectedSkills.Clear();
            v_skillCandidates.Clear();

            var acquiredActiveSkills = skillSet.GetAcquiredSkillKeys()
                .Select(x => StaticDataRepository.Instance.Skills.Get(x))
                .Where(x => x.skillType == SkillType.Active)
                .ToArray();
            var acquiredPassiveSkills = skillSet.GetAcquiredSkillKeys()
                .Select(x => StaticDataRepository.Instance.Skills.Get(x))
                .Where(x => x.skillType == SkillType.Passive)
                .ToArray();

            SkillId transcendableSkillId = acquiredActiveSkills
                .Where(x => skillSet.IsAbleToTranscend(x.Id))
                .FirstOrDefault()?.Id ?? SkillId.Invalid;

            SkillStaticData highestLevelActiveSkill = acquiredActiveSkills.OrderByDescending(x => x.Level)
                .Where(x => x.Level <= GameConstants.SKILL_MAX_LEVEL)
                .FirstOrDefault();


            SkillId TakeOneFromCandidates(List<SkillId> candidates)
            {
                int selectedIndex = Random.Range(minInclusive: 0, maxExclusive: candidates.Count());
                var selectedSkillId = candidates[selectedIndex];
                candidates.RemoveAt(selectedIndex);
                return selectedSkillId;
            }

            if (acquiredActiveSkills.Count() <= 1)
            {
                // 새로운 액티브스킬 3개 (방어필드, 데스터치 반드시 포함, 나머지 1개는 랜덤)
                var candidates = new List<SkillId>
                {
                    SkillId.DefensiveField,
                    SkillId.SpinBlade, // DurationUp
                    //SkillId.Glutton, // AcquisitionDistanceUp
                    SkillId.AmbushMonster, // AttackSpeedUp
                    SkillId.DeathTouch, // GoldAmountUp
                    SkillId.ShockBomb, // RangeUp (텐티스윕도 같은것 필요로 함)
                };

                v_skillCandidates.Add(TakeOneFromCandidates(candidates));
                v_skillCandidates.Add(SkillId.Glutton);
                v_skillCandidates.Add(TakeOneFromCandidates(candidates));
            }
            else if (acquiredActiveSkills.Count() <= 2)
            {
                var candidates = new List<SkillId>
                {
                    characterStaticData.BasicSkill,
                    SkillId.DefensiveField,
                    SkillId.SpinBlade, // DurationUp
                    SkillId.Glutton, // AcquisitionDistanceUp
                    SkillId.AmbushMonster, // AttackSpeedUp
                    SkillId.DeathTouch, // GoldAmountUp
                    SkillId.ShockBomb, // RangeUp (텐티스윕도 같은것 필요로 함)
                };

                // - 궁극진화 가능한 경우 궁극진화 액티브스킬 반드시 포함
                // - 선택한 액티브스킬2개, 새로운 액티브스킬 1개(방어필드 보유 안했다면 방어필드 포함)   
                if (transcendableSkillId != SkillId.Invalid)
                {
                    v_skillCandidates.Add(transcendableSkillId);
                    candidates = candidates.Where(x => x != transcendableSkillId).ToList();
                }

                Debug.Assert(acquiredActiveSkills.Count() == 2);
                foreach (var acquiredActiveSkill in acquiredActiveSkills)
                {
                    if (acquiredActiveSkill.Id == transcendableSkillId)
                    {
                        continue;
                    }

                    if ((acquiredActiveSkill.Level >= GameConstants.SKILL_MAX_LEVEL) &&
                        !skillSet.IsAbleToTranscend(acquiredActiveSkill.Id))
                    {
                        continue;
                    }

                    if (!v_skillCandidates.Contains(acquiredActiveSkill.Id))
                    {
                        v_skillCandidates.Add(acquiredActiveSkill.Id);
                    }
                }

                while (v_skillCandidates.Count() < 3)
                {
                    var skillId = TakeOneFromCandidates(candidates);
                    if (skillSet.HasSkill(skillId) &&
                        skillSet.GetSkillLevel(skillId) >= GameConstants.SKILL_MAX_LEVEL)
                    {
                        continue;
                    }

                    if (!v_skillCandidates.Contains(skillId))
                    {
                        v_skillCandidates.Add(skillId);
                    }
                }
            }
            else
            {
                // 액티브스킬 3개 이상인경우
                // - 궁극진화 가능한 경우 궁극진화 액티브스킬 반드시 포함
                // - 선택한 액티브 스킬에 매칭되는 패시브스킬 1개(이미 찍은 패시브 스킬은 제외)
                // - 선택한 액티브 스킬 중 레벨이 가장 높은것에 매칭되는 패시브 스킬 1개를 우선해서 선정
                // - 후보지로 패시브스킬 1개가 있다면 : 선택한 액티브 스킬중 2개
                // - 후보지로 패시브스킬 0개라면: 선택한 액티브 스킬중 3개

                // 궁극진화 가능한 경우 궁극진화 액티브스킬 반드시 포함
                if (transcendableSkillId != SkillId.Invalid)
                {
                    v_skillCandidates.Add(transcendableSkillId);
                }

                // 최고 레벨의 엑티브스킬에 매칭되는 패시브스킬을 보유하고 있지 않다면 그것을 추가
                if (highestLevelActiveSkill.Level <= GameConstants.SKILL_MAX_LEVEL)
                {
                    var maxLevelSkill = StaticDataRepository.Instance.Skills.Get(new SkillKey(highestLevelActiveSkill.Id, GameConstants.SKILL_MAX_LEVEL));
                    Debug.Assert(maxLevelSkill != null);
                    var transcendRequirement = maxLevelSkill.TranscendCondition;
                    Debug.Assert(transcendRequirement != SkillId.Invalid);
                    if (!v_skillCandidates.Contains(transcendRequirement) &&
                        !skillSet.HasSkill(transcendRequirement))
                    {
                        v_skillCandidates.Add(transcendRequirement);
                    }
                }

                // - 선택한 액티브 스킬 중 레벨이 가장 높은것에 매칭되는 패시브 스킬 1개를 우선해서 선정 (이미 찍은 스킬은 제외)

                // 액티브스킬중 가장 레벨높은거 하나 먼저 보여주고
                if (highestLevelActiveSkill != null &&
                    highestLevelActiveSkill.Level < GameConstants.SKILL_MAX_LEVEL &&
                    !v_skillCandidates.Contains(highestLevelActiveSkill.Id))
                {
                    v_skillCandidates.Add(highestLevelActiveSkill.Id);
                }

                // 남은 건 선택한 액티브스킬로 채운다. 3번 시도해보고 안되면 후보지에서 선택해서 마져 채운다.
                int retryCount = 0;
                while (v_skillCandidates.Count() < 3)
                {
                    retryCount++;
                    if (retryCount > 12)
                    {
                        break;
                    }
                    var nextIndex = Random.Range(minInclusive: 0, maxExclusive: acquiredActiveSkills.Length);
                    var nextSkill = acquiredActiveSkills[nextIndex];

                    if (v_skillCandidates.Contains(nextSkill.Id))
                    {
                        continue;
                    }

                    if (nextSkill.Level < GameConstants.SKILL_MAX_LEVEL ||
                        skillSet.IsAbleToTranscend(nextSkill.Id))
                    {
                        v_skillCandidates.Add(nextSkill.Id);
                    }
                }

                if (v_skillCandidates.Count < 3)
                {
                    var candidates = new List<SkillId>
                    {
                        SkillId.DefensiveField,
                        SkillId.SpinBlade, // DurationUp
                        SkillId.Glutton, // AcquisitionDistanceUp
                        SkillId.AmbushMonster, // AttackSpeedUp
                        SkillId.DeathTouch, // GoldAmountUp
                        SkillId.ShockBomb, // RangeUp (텐티스윕도 같은것 필요로 함)
                    };

                    while (v_skillCandidates.Count() < 3)
                    {
                        var skillId = TakeOneFromCandidates(candidates);
                        if (!v_skillCandidates.Contains(skillId))
                        {
                            v_skillCandidates.Add(skillId);
                        }
                    }
                }
            }

            Debug.Assert(v_skillCandidates.Count() == 3, "로직을 잘못짠게 아니면 3개 나와야 함");
            foreach (var skillId in v_skillCandidates)
            {
                int currentSkillLevel = skillSet.GetSkillLevel(skillId);
                if (currentSkillLevel < GameConstants.SKILL_MAX_LEVEL ||
                    skillSet.IsAbleToTranscend(skillId))
                {
                    v_selectedSkills.Add(new SkillKey(skillId, currentSkillLevel + 1));
                }
                else
                {
                    Debug.LogWarning($"스킬 후보지가 이미 만렙찍은 스킬임. [{skillId}] [{currentSkillLevel}]");
                }
            }

            return v_selectedSkills.ToArray();
        }

        /// <summary>
        /// 스킬상자에서 얻을 수 있는 스킬 목록을 계산해준다.
        /// </summary>
        public SkillKey[] GetCandidateSkillsToLearnBySkillBox(SkillSet skillSet)
        {
            List<SkillKey> acquirableSkills = new List<SkillKey>();

            var acquiredSkills = skillSet.GetAcquiredSkillIds();
            for (int i = 0; i < acquiredSkills.Length; i++)
            {
                var acquiredSkillId = acquiredSkills[i];
                int skillLevel = skillSet.GetSkillLevel(acquiredSkills[i]);

                if (skillLevel >= GameConstants.SKILL_TRANSCENDENT_LEVEL)
                {
                    continue;
                }

                if (skillLevel == GameConstants.SKILL_MAX_LEVEL)
                {
                    if (skillSet.IsAbleToTranscend(acquiredSkills[i]))
                    {
                        acquirableSkills.Add(new SkillKey(acquiredSkillId, skillLevel + 1));
                    }
                    continue;
                }

                acquirableSkills.Add(new SkillKey(acquiredSkillId, skillLevel + 1));
            }
            return acquirableSkills.ToArray();
        }

        /// <summary>
        /// 스킬박스 획득시 습득할 스킬들을 선택해서 전달해주는 함수
        /// selectSkillCount보다 현재 배울수 있는 스킬의 개수가 더 작으면 배울 수 있는 스킬만큼만 전달해준다
        /// </summary>
        /// <param name="selectSkillCount">스킬박스 획득 후 습득해야되는 스킬의 개수</param>
        /// <returns>스킬박스 획득 후 습득해야되는 스킬들</returns>
        /// <seealso cref="SkillDeck.Select3SkillsToLearn(SkillSet)"/>
        /// <remarks>스킬 습득방식은 SelectSkillsToLearnBySkillBox, Select3SkillsToLearn 두가지 방식이 있다. </remarks>
        public SkillKey[] SelectSkillsToLearnBySkillBox(SkillSet skillSet, int selectSkillCount)
        {
            // 획득한 스킬들 중 획득 가능한 만큼 리스트에 저장 (예: 1렙이면 4개 5렙이면 0개, 5렙에 초월 가능이면 1개)
            List<SkillId> acquirableSkillIds = new List<SkillId>();
            List<SkillId> canTranscendSkills = new List<SkillId>();
            Dictionary<SkillId, int> checkReturnSkills = new Dictionary<SkillId, int>();
            List<SkillKey> retSkillIds = new List<SkillKey>();

            var acquiredSkills = skillSet.GetAcquiredSkillIds();
            for (int i = 0; i < acquiredSkills.Length; i++)
            {
                int skillLevel = skillSet.GetSkillLevel(acquiredSkills[i]);

                if (skillLevel >= GameConstants.SKILL_TRANSCENDENT_LEVEL)
                {
                    continue;
                }

                if (skillLevel == GameConstants.SKILL_MAX_LEVEL && skillSet.IsAbleToTranscend(acquiredSkills[i]))
                {
                    //초월조건 달성한 스킬 (5렙, 패시브 스킬 배움)
                    //초월조건 달성한 스킬 리스트는 따로 넣어준다.
                    canTranscendSkills.Add(acquiredSkills[i]);
                }
                else
                {
                    acquirableSkillIds.Add(acquiredSkills[i]);
                }
            }


            for (int i = 0; i < selectSkillCount; i++)
            {
                if (canTranscendSkills.Count == 0 && acquirableSkillIds.Count == 0)
                {
                    break;
                }

                if (canTranscendSkills.Count > 0)
                {
                    //초월 가능한 스킬들이 있으면 먼저 넣어준다.
                    int randomIndex = Random.Range(0, canTranscendSkills.Count);
                    retSkillIds.Add(new SkillKey(canTranscendSkills[randomIndex], GameConstants.SKILL_TRANSCENDENT_LEVEL));
                    canTranscendSkills.RemoveAt(randomIndex);
                }
                else
                {
                    //초월 가능한 스킬들이 없으면 획득 가능한 스킬들중 하나를 넣어준다..
                    int randomIndex = Random.Range(0, acquirableSkillIds.Count);
                    SkillId skillId = acquirableSkillIds[randomIndex];

                    if (!checkReturnSkills.ContainsKey(skillId))
                    {
                        checkReturnSkills.Add(skillId, 0);
                    }

                    int skillLevel = skillSet.GetSkillLevel(skillId);
                    // NOTE: 스킬박스 획득 시점에 스킬초월한 가능한 상태가 아니면 여러개 획득으로 조건이 만족한다해도 초월스킬을 배울수 없다.
                    int skillMaxLevel = GameConstants.SKILL_MAX_LEVEL;

                    if (skillMaxLevel - skillLevel <= checkReturnSkills[skillId])
                    {
                        --i;
                        acquirableSkillIds.RemoveAt(randomIndex);
                        continue;
                    }

                    ++checkReturnSkills[skillId];
                    retSkillIds.Add(new SkillKey(skillId, checkReturnSkills[skillId] + skillLevel));
                }
            }
            return retSkillIds.ToArray();
        }

        public SkillKey[] SelectSkillsToLearnByTutorialSkillBox(SkillSet skillSet, int selectSkillCount, SkillId ownerBasicSkillId)
        {
            var selectedSkills = new List<SkillKey>();

            // 1. 궁극진화 가능한 스킬 가장 먼저 
            // 2. 내가 들고 있는 액티브 스킬중 레벨 제일 높은 것에 몰빵
            // 3. 나머지 내가 들고있는것중 하나로 

            var acquiredActiveSkills = skillSet.GetAcquiredSkillKeys()
                .Select(x => StaticDataRepository.Instance.Skills.Get(x))
                .Where(x => x.skillType == SkillType.Active)
                .ToArray();
            var acquiredPassiveSkills = skillSet.GetAcquiredSkillKeys()
                .Select(x => StaticDataRepository.Instance.Skills.Get(x))
                .Where(x => x.skillType == SkillType.Passive)
                .ToArray();

            SkillId transcendableSkillId = acquiredActiveSkills
                .Where(x => skillSet.IsAbleToTranscend(x.Id))
                .FirstOrDefault()?.Id ?? SkillId.Invalid;

            if (transcendableSkillId != SkillId.Invalid)
            {
                selectedSkills.Add(new SkillKey(transcendableSkillId, GameConstants.SKILL_TRANSCENDENT_LEVEL));
            }

            foreach (var acquiredSkill in acquiredActiveSkills.OrderByDescending(x => x.Level))
            {
                if (selectedSkills.Count() >= 5)
                {
                    break;
                }

                for (int level = acquiredSkill.Level + 1; level <= GameConstants.SKILL_MAX_LEVEL; ++level)
                {
                    if (selectedSkills.Count() >= 5)
                    {
                        break;
                    }

                    var skillKey = new SkillKey(acquiredSkill.Id, level);
                    if (!selectedSkills.Contains(skillKey))
                    {
                        selectedSkills.Add(skillKey);
                    }
                }
            }

            foreach (var acquiredSkill in acquiredPassiveSkills.OrderByDescending(x => x.Level))
            {
                if (selectedSkills.Count() >= 5)
                {
                    break;
                }

                for (int level = acquiredSkill.Level + 1; level <= GameConstants.SKILL_MAX_LEVEL; ++level)
                {
                    if (selectedSkills.Count() >= 5)
                    {
                        break;
                    }

                    var skillKey = new SkillKey(acquiredSkill.Id, level);
                    if (!selectedSkills.Contains(skillKey))
                    {
                        selectedSkills.Add(skillKey);
                    }
                }
            }
            return selectedSkills.ToArray();
        }

        public SkillId[] GetBanSkillArray()
        {
            return _bannedSkillDeck.ToArray();
        }
    }
}
