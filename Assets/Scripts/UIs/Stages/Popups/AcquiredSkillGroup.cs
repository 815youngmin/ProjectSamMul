using Shared.DataTables;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using System;
using System.Collections.Generic;
using UnityEngine;
using SamMul.GameClients;
using SamMul.GameClients.Stages.Characters.PCs;
using SamMul.GameClients.Stages.Characters.PCs.Skills;
using SamMul.UIs.Stages.Popups;

public class AcquiredSkillGroup : MonoBehaviour
{
    [SerializeField] private List<AcquiredSkillSlot> _activeSkills;
    [SerializeField] private List<AcquiredSkillSlot> _passiveSkills;

    public void Initialize(SkillKey[] acquiredSkills)
    {
        Debug.Assert(_activeSkills.Count == GameConstants.SKILLSLOT_NUMBER_OF_ACTIVE_SLOTS);
        Debug.Assert(_passiveSkills.Count == GameConstants.SKILLSLOT_NUMBER_OF_PASSIVE_SLOTS);

        int nextActiveSkillIndex = 0;
        int nextPassiveSkillIndex = 0;

        foreach (var acquiredSkill in acquiredSkills)
        {
            var skillStaticData = StaticDataRepository.Instance.Skills.Get(acquiredSkill);
            if (skillStaticData.skillType == SkillType.Active)
            {
                if (nextActiveSkillIndex < _activeSkills.Count)
                {
                    _activeSkills[nextActiveSkillIndex].Initialize(acquiredSkill);
                    nextActiveSkillIndex++;
                }
                else
                {
                    Debug.LogError("획득한 액티브 스킬이 너무 많아 표기할 수 없습니다. 확인이 필요합니다.");
                }
            }
            else if (skillStaticData.skillType == SkillType.Passive)
            {
                if (nextPassiveSkillIndex < _passiveSkills.Count)
                {
                    _passiveSkills[nextPassiveSkillIndex].Initialize(acquiredSkill);
                    nextPassiveSkillIndex++;
                }
                else
                {
                    Debug.LogError("획득한 패시브 스킬이 너무 많아 표기할 수 없습니다. 확인이 필요합니다.");
                }
            }
            else
            {
                throw new NotImplementedException($"[{skillStaticData.skillType}] 구현안됨");
            }
        }

        for (int i = nextActiveSkillIndex; i < _activeSkills.Count; i++)
        {
            _activeSkills[i].Initialize(new SkillKey(SkillId.Invalid, 0));
        }
        for (int i = nextPassiveSkillIndex; i < _passiveSkills.Count; i++)
        {
            _passiveSkills[i].Initialize(new SkillKey(SkillId.Invalid, 0));
        }
    }

    public void PlayAcquiredAnimationRelatedToSkillCandidates(SkillKey[] skillCandidates)
    {
        if (0 == skillCandidates.Length)
        {
            foreach (var slot in _activeSkills)
            {
                slot.StopScalingAnimation();
            }

            foreach (var slot in _passiveSkills)
            {
                slot.StopScalingAnimation();
            }
            return;
        }

        PlayerCharacter pc = GameClient.Stage.PC;
        HashSet<SkillId> skillIds = new HashSet<SkillId>();
        for (int i = 0; i < skillCandidates.Length; ++i)
        {
            var skillStaticData = StaticDataRepository.Instance.Skills.Get(skillCandidates[i]);
            if (0 != pc.GetSkillLevel(skillStaticData.Id))
            {
                continue;
            }

            foreach (var targetSkillid in skillStaticData.TranscendTargets)
            {
                skillIds.Add(targetSkillid);
            }

        }

        for (int i = 0; i < _activeSkills.Count; i++)
        {
            SkillId id = _activeSkills[i].SkillId;
            if (id == SkillId.Invalid)
            {
                break;
            }

            if (skillIds.TryGetValue(id, out _))
            {
                _activeSkills[i].PlayScalingAnimation();
            }
            else
            {
                _activeSkills[i].StopScalingAnimation();
            }

        }
        for (int i = 0; i < _passiveSkills.Count; i++)
        {
            SkillId id = _passiveSkills[i].SkillId;
            if (id == SkillId.Invalid)
            {
                break;
            }

            if (skillIds.TryGetValue(id, out _))
            {
                _passiveSkills[i].PlayScalingAnimation();
            }
            else
            {
                _passiveSkills[i].StopScalingAnimation();
            }
        }

    }




}
