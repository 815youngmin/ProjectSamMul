#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Z.GameClients.Stages.Characters;

namespace Z.GameClients.Stages.DamagePopups
{
    /// <summary>
    /// Pools and shows floating damage / heal / avoid numbers in the stage scene.
    /// </summary>
    public class DamagePopupManager
    {
        private const int MAX_POPUPS_ON_SCREEN = 60;
        private const int INITIAL_POOL_SIZE = 64;

        private static readonly Color ORANGE = new Color32(255, 170, 0, 255);

        private readonly Stack<DamagePopupBehaviour> _freePopups = new Stack<DamagePopupBehaviour>();
        private readonly List<DamagePopupBehaviour> _alivePopups = new List<DamagePopupBehaviour>();

        // Damage values repeat a lot; cache their formatted strings to avoid per-hit allocations.
        private readonly Dictionary<long, string> _damageStrings = new Dictionary<long, string>();

        private GameObject? _root;

        public void InitializeForStageScene()
        {
            if (_root != null)
            {
                return;
            }

            _root = new GameObject("@DamagePopupsRoot");
            for (int i = 0; i < INITIAL_POOL_SIZE; ++i)
            {
                _freePopups.Push(DamagePopupBehaviour.Create(_root.transform, this.Release));
            }
        }

        public void ClearBeforeChangingScene(Scene relatedScene)
        {
            _damageStrings.Clear();

            foreach (var popup in _alivePopups)
            {
                popup.Hide();
                _freePopups.Push(popup);
            }
            _alivePopups.Clear();

            // Popups created in the leaving scene are destroyed together with it; forget them.
            var survivors = new List<DamagePopupBehaviour>();
            while (_freePopups.Count > 0)
            {
                var popup = _freePopups.Pop();
                if (popup.gameObject.scene == relatedScene)
                {
                    Object.Destroy(popup.gameObject);
                }
                else
                {
                    survivors.Add(popup);
                }
            }
            foreach (var popup in survivors)
            {
                _freePopups.Push(popup);
            }

            if (_root != null && _root.scene == relatedScene)
            {
                Object.Destroy(_root);
                _root = null;
            }
        }

        public DamagePopupBehaviour CreateDamagePopup(Character? attacker, Vector3 pos, float damage, Vector2 hitVector, bool isAttack)
        {
            var color = isAttack ? GetAttackColor(attacker, damage) : Color.green;
            return this.Show(pos, GetDamageString(damage), color, hitVector);
        }

        public DamagePopupBehaviour CreateAvoidPopup(Character? attacker, Vector3 pos, Vector2 hitVector)
        {
            return this.Show(pos, "AVOID!", Color.white, hitVector);
        }

        private DamagePopupBehaviour Show(Vector3 pos, string text, Color color, Vector2 hitVector)
        {
            if (_alivePopups.Count >= MAX_POPUPS_ON_SCREEN)
            {
                var oldest = _alivePopups[0];
                _alivePopups.RemoveAt(0);
                oldest.Hide();
                _freePopups.Push(oldest);
            }

            var popup = _freePopups.Count > 0
                ? _freePopups.Pop()
                : DamagePopupBehaviour.Create(_root != null ? _root.transform : null, this.Release);

            popup.Show(pos, text, color, hitVector);
            _alivePopups.Add(popup);
            return popup;
        }

        private void Release(DamagePopupBehaviour popup)
        {
            if (_alivePopups.Remove(popup))
            {
                _freePopups.Push(popup);
            }
        }

        // Damage is tinted by how far it exceeds the attacker's base attack power.
        private static Color GetAttackColor(Character? attacker, float damage)
        {
            if (attacker == null)
            {
                return Color.white;
            }

            float attackPower = attacker.Stats.AttackPower.Value;
            if (damage <= 1.3f * attackPower)
            {
                return Color.white;
            }
            if (damage <= 1.95f * attackPower)
            {
                return Color.yellow;
            }
            if (damage <= 2.6f * attackPower)
            {
                return ORANGE;
            }
            return Color.red;
        }

        private string GetDamageString(float damage)
        {
            long value = (long)damage;
            if (!_damageStrings.TryGetValue(value, out var text))
            {
                text = FormatShort(value);
                _damageStrings.Add(value, text);
            }
            return text;
        }

        private static string FormatShort(long value)
        {
            if (value >= 1_000_000L)
            {
                return (value / 1_000_000f).ToString("0.#") + "M";
            }
            if (value >= 10_000L)
            {
                return (value / 1_000f).ToString("0.#") + "K";
            }
            return value.ToString();
        }
    }
}
