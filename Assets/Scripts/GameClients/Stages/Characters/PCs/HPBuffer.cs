using Shared.GameLogics;
using UnityEngine;
using Z.GameClients.Stages.Characters.Stats;
using Z.ResourcePools;
using Z.UIs.Stages.HUDs;

namespace Z.GameClients.Stages.Characters.PCs
{
    public class HPBuffer
    {
        private static readonly string HP_BUFFER_PREFAB_PATH = "Stages/UIs/HUDs/PlayerHPBar/HPBuffer.prefab";

        public readonly StatCalculator MaxHP;

        private readonly PlayerCharacter _owner;
        private readonly HPBar _hpBar;

        public bool IsActive => _currentHP > 0.0f;
        private float _currentHP;

        /// <summary>
        /// 체력 버퍼를 생성합니다.
        /// </summary>
        /// <param name="owner">
        /// 체력 버퍼로 보호받는 플레이어 캐릭터.
        /// </param>
        /// <param name="maxHpPercentage">
        /// 체력 버퍼의 최대 체력으로 설정할 <paramref name="owner"/>의 기본 최대 체력의 비율.
        /// </param>
        public HPBuffer(PlayerCharacter owner, float maxHpPercentage)
        {
            _owner = owner;
            _hpBar = ResourcePool.Instance.InstantiateFromResource<HPBar>(HP_BUFFER_PREFAB_PATH);
            _hpBar.AllocateSharedResources(isPC: true);
            _hpBar.Initialize(_owner.transform, new Vector2(0.0f, -0.18f), Vector3.one, HPBar.HP_BUFFER_COLOR);

            // 기본 최대 체력은 아바타 페이지에서 보여지는 체력으로 설정.
            float basicMaxHP = AvatarLogic.CalculateMaxHp(_owner.HeroData, _owner.EquippedEquipments, _owner.EvolutionData);
            MaxHP = new StatCalculator(basicMaxHP);
            MaxHP.AddModifier(new StatModifier(maxHpPercentage - 1.0f, StatModType.PercentAdd));
            _currentHP = MaxHP.Value;
            _hpBar.gameObject.SetActive(_currentHP > 0.0f);
        }

        /// <summary>
        /// 매 프레임마다 체력 버퍼를 업데이트합니다.
        /// </summary>
        public void UpdateLogic()
        {
            if (!IsActive)
            {
                return;
            }

            _hpBar.UpdateLogic(_currentHP, MaxHP.Value, _owner.Pos);
        }

        /// <summary>
        /// 체력 버퍼의 체력바의 활성화/비활성화를 전환합니다.
        /// </summary>
        /// <param name="activate">
        /// true이면 체력바를 활성화하고, false이면 비활성화합니다.
        /// </param>
        private void ToggleHPBar(bool activate)
        {
            if (_hpBar.gameObject.activeSelf == activate)
            {
                return;
            }

            _hpBar.gameObject.SetActive(activate);
        }

        /// <summary>
        /// 체력 버퍼의 체력을 충전합니다. 현재 체력은 최대 체력을 넘을 수 없습니다.
        /// </summary>
        /// <param name="increment">
        /// 체력을 회복할 양입니다.
        /// </param>
        public void ChargeHP(float increment)
        {
            if (increment <= 0)
            {
                return;
            }

            _currentHP += increment;
            if (_currentHP > MaxHP.Value)
            {
                _currentHP = MaxHP.Value;
            }

            if (_currentHP > 0.0f)
            {
                this.ToggleHPBar(true);
            }
        }

        /// <summary>
        /// 체력 버퍼가 <see cref="_owner"/> 대신 피해를 받아 완충합니다.
        /// </summary>
        /// <param name="damage">
        /// 체력 버퍼가 받을 피해입니다.
        /// </param>
        /// <returns>
        /// 체력 버퍼가 미처 완충하지 못하고 남은 피해입니다. 남은 피해는 <see cref="_owner"/>가 받게 됩니다.
        /// </returns>
        public float TakeDamage(float damage)
        {
            if (damage <= 0.0f)
            {
                return 0.0f;
            }

            if (damage >= _currentHP)
            {
                float leftDamage = damage - _currentHP;
                _currentHP = 0.0f;
                this.ToggleHPBar(false);
                return leftDamage;
            }
            else
            {
                _currentHP -= damage;
                return 0.0f;
            }
        }

        /// <summary>
        /// 체력 버퍼를 파괴합니다.
        /// </summary>
        /// <remarks>
        /// 이 함수가 호출된 후 더 이상 체력 버퍼 객체에 접근할 수 없습니다.
        /// </remarks>
        public void Destroy()
        {
            _currentHP = 0.0f;
            ResourcePool.Instance.PutBackInstance(HP_BUFFER_PREFAB_PATH, _hpBar.gameObject);
        }
    }
}
