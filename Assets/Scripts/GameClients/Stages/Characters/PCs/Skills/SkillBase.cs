using Shared.DataTables;
using Shared.GameDataTypes;
using Shared.StaticDatas;
using System;
using UnityEngine;
using Z.GameClients.Stages.Characters.Stats;
using Z.UnityHelpers;

namespace Z.GameClients.Stages.Characters.PCs.Skills
{
    public abstract class SkillBase
    {
        private readonly SkillStaticData _staticData;
        public SkillStaticData StaticData => _staticData;

        public SkillId Id => _staticData.Id;
        public int Level => _staticData.Level;
        public SkillId TranscendCondition => _staticData.TranscendCondition;
        public SkillType SkillType => _staticData.skillType;
        public bool IsHeroBasicSkill => _staticData.IsHeroBasicSkill;
        public HeroType? HeroBasicSkillOwner => _staticData.HeroBasicSkillOwner;
        public HeroType? DesignatedHero => _staticData.DesignatedHero;
        public bool HasTranscendCondition => _staticData.HasTranscendCondition;
        // 초월된 스킬인지 여부
        public bool IsTranscendent => this.Level == GameConstants.SKILL_TRANSCENDENT_LEVEL;

        public virtual float Duration => _staticData.Duration;
        public virtual float Cooltime => _staticData.Cooltime;

        public bool IsActivated { get; private set; }
        public float ActivatedAt { get; private set; }
        public float ActivatingAt => DeactivatedAt + Cooltime;
        public float DeactivatedAt { get; private set; }
        public float DeactivatingAt => ActivatedAt + Duration;

        public SkillBase(SkillStaticData staticData)
        {
            _staticData = staticData;
            IsActivated = false;
            ActivatedAt = 0;
            DeactivatedAt = 0;
        }

        /// <summary>
        /// 스킬을 활성화하고 캐릭터에게 효과를 적용한다. 
        /// 지속시간(<see cref="Duration"/>동안 지속된다.
        /// </summary>       
        public virtual void Activate(PlayerCharacter owner, Stage stage, float now)
        {
            ActivatedAt = now;
            IsActivated = true;
        }

        /// <summary>
        /// 활성화된 스킬을 진행시킨다.
        /// 활성화된 상태에서 지속시간(<see cref="Duration"/>)동안 매 프레임 호출된다.
        /// </summary>
        public abstract void Update(PlayerCharacter owner, Stage stage, float now);

        /// <summary>
        /// 스킬을 비활성화하고 캐릭터로부터 효과를 해제한다.
        /// 이 함수가 리턴하고 쿨타임(<see cref="Cooltime"/>만큼 지나면 다시 활성화된다.
        /// </summary>
        public virtual void Deactivate(PlayerCharacter owner, Stage stage, float now)
        {
            if (!IsActivated)
            {
                Debug.LogWarning($"{Id} 스킬이 중복해서 {nameof(Deactivate)}요청되었습니다. 왜죠? 코드 확인해서 수정해주세요.");
                return;
            }
            DeactivatedAt = now;
            IsActivated = false;
        }

        /// <remarks>
        /// 이 인터페이스는 위험합니다. 새로 스킬을 구현할 때 이 함수를 오버라이드하지 마세요
        /// </remarks>
        [Obsolete("이 인터페이스는 위험합니다. 새로 스킬을 구현할 때 이 함수를 오버라이드하지 마세요.")]
        public virtual void ReApplyStat(PlayerCharacterStatCalculators ownerStats)
        {
        }

        public virtual void AttackedEnemy(Stage stage, PlayerCharacter owner, Character enemy, float damage)
        {
        }

        public virtual void OnPlayerCharacterDead(PlayerCharacter owner, Stage stage, float now)
        {
            if (IsActivated)
            {
                this.Deactivate(owner, stage, now);
            }
        }

        public virtual void OnPlayerCharacterResurrected(PlayerCharacter owner, Stage stage, float now)
        {
            this.Activate(owner, stage, now);
        }

        protected void PlaySkillSoundEffect(Vector3 pos)
        {
            if (string.IsNullOrEmpty(StaticData.SkillSFXPath))
            {
                return;
            }
            UnityGlobal.Sounds.PlayBySoundPrefab(StaticData.SkillSFXPath, pos);
        }

        protected void PlaySpawnedObjectSoundEffect(Vector3 pos)
        {
            if (string.IsNullOrEmpty(StaticData.SpawnedObjectSFXPath))
            {
                return;
            }
            UnityGlobal.Sounds.PlayBySoundPrefab(StaticData.SpawnedObjectSFXPath, pos);
        }

        protected void PlaySkillHitSoundEffect(Vector3 pos)
        {
            if (string.IsNullOrEmpty(StaticData.SkillHitSFXPath))
            {
                return;
            }
            UnityGlobal.Sounds.PlayBySoundPrefab(StaticData.SkillHitSFXPath, pos);
        }
    }
}
