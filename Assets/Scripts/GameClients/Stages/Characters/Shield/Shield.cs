using System;
using UnityEngine;

namespace Z.GameClients.Stages.Characters.Shields
{
    public abstract class Shield
    {
        protected Character _owner;
        protected readonly GameObject _shieldToggleObject;

        public bool IsActive => _shieldAmount > 0;
        private int _shieldAmount;

        public Shield(Character owner)
        {
            _shieldToggleObject = new GameObject("Shield");
            _shieldToggleObject.transform.SetParent(owner.transform, false);
            _shieldToggleObject.transform.localPosition = Vector3.zero;
            _shieldToggleObject.transform.localScale = Vector3.one;
            _shieldToggleObject.transform.localRotation = Quaternion.identity;

            _shieldAmount = 0;
            _shieldToggleObject.SetActive(false);

            _owner = owner;
        }

        public abstract void UpdateLogic();

        //방어막 효과가 활성화 될때 호출합니다.
        protected virtual void ActivateShiled()
        {
            _shieldToggleObject.SetActive(true);
        }

        //방어막 효과가 비활성화 될때 호출합니다.
        protected virtual void DeactivateShiled()
        {
            _shieldToggleObject.SetActive(false);
        }

        /// <summary>
        /// 방어막 효과가 감소될때 호출합니다.
        /// </summary>
        /// <param name="shieldAmount">남아있는 방어막 개수</param>
        protected virtual void DecreaseShield(int shieldAmount) { }

        /// <summary>
        /// 방어막 효과 활성화/비활성화를 전환합니다.
        /// </summary>
        /// <param name="activate">
        /// true이면 방어막을 활성화하고, false이면 비활성화합니다.
        /// </param>
        private void ToggleShieldEffect(bool activate)
        {
            if (_shieldToggleObject.activeSelf == activate)
            {
                return;
            }

            if(activate == true)
            {
                ActivateShiled();
            }
            else
            {
                DeactivateShiled();
            }
        }

        /// <summary>
        /// 방어막 개수를 늘립니다.
        /// </summary>
        /// <param name="amount">
        /// 늘릴 방어막의 개수입니다.
        /// </param>
        public void IncreaseShieldAmount(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            if (_shieldAmount <= 0)
            {
                this.ToggleShieldEffect(true);
            }

            _shieldAmount += amount;
        }

        /// <summary>
        /// 방어막 개수를 줄입니다.
        /// </summary>
        /// <param name="amount">
        /// 줄일 방어막의 개수입니다.
        /// </param>
        /// <remarks>
        /// 피격 당 방어막이 1개씩 줄어들기 때문에, 보통 이 함수에 전달하는 <paramref name="amount"/>의 값은 1입니다.
        /// </remarks>
        public void DecreaseShieldAmount(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _shieldAmount -= amount;

            this.DecreaseShield(_shieldAmount);

            if (_shieldAmount <= 0)
            {
                this.ToggleShieldEffect(false);
            }
        }

        /// <summary>
        /// 방어막을 파괴합니다.
        /// </summary>
        /// <remarks>
        /// 이 함수가 호출된 후 더 이상 방어막 객체에 접근할 수 없습니다.
        /// </remarks>
        public virtual void Destroy()
        {
            _shieldAmount = 0;
            _shieldToggleObject.transform.SetParent(null);
            GameObject.Destroy(_shieldToggleObject);
        }


    }
}
