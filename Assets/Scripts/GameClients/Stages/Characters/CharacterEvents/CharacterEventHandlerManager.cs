#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Z.GameClients.Stages.Characters.CharacterEvents
{
    /// <remarks>attacker may be null (environmental damage, self damage, ...)</remarks>
    public delegate void OnHittedHandler(Stage stage, Character owner, Character? attacker, float damage, Vector2 hitPoint);

    public delegate void OnDeadHandler(Stage stage, Character owner, Vector2 hitVector);

    public interface ICharacterEventHandler
    {
        public void OnHitted(Stage stage, Character owner, Character attacker, float damage, Vector2 hitPoint);
        public void OnDead(Stage stage, Character owner, Vector2 hitVector);
    }

    /// <summary>
    /// Per-character registry of hit/death callbacks. Handlers are keyed by the id returned on registration
    /// so the same delegate can be registered more than once and removed individually.
    /// </summary>
    public class CharacterEventHandlerManager : ICharacterEventHandler
    {
        private long _nextHandlerId;
        private readonly Dictionary<long, OnHittedHandler> _onHittedHandlers = new Dictionary<long, OnHittedHandler>();
        private readonly Dictionary<long, OnDeadHandler> _onDeadHandlers = new Dictionary<long, OnDeadHandler>();

        // Reused snapshot buffers so a handler may unregister itself while being invoked.
        private readonly List<OnHittedHandler> _hittedSnapshot = new List<OnHittedHandler>();
        private readonly List<OnDeadHandler> _deadSnapshot = new List<OnDeadHandler>();

        public void Clear()
        {
            _onHittedHandlers.Clear();
            _onDeadHandlers.Clear();
        }

        public void OnHitted(Stage stage, Character owner, Character? attacker, float damage, Vector2 hitPoint)
        {
            _hittedSnapshot.Clear();
            _hittedSnapshot.AddRange(_onHittedHandlers.Values);
            foreach (var handler in _hittedSnapshot)
            {
                handler(stage, owner, attacker, damage, hitPoint);
            }
        }

        public long AddOnDeadHandler(OnDeadHandler handler)
        {
            long id = _nextHandlerId++;
            _onDeadHandlers.Add(id, handler);
            return id;
        }

        public void OnDead(Stage stage, Character owner, Vector2 hitVector)
        {
            _deadSnapshot.Clear();
            _deadSnapshot.AddRange(_onDeadHandlers.Values);
            foreach (var handler in _deadSnapshot)
            {
                handler(stage, owner, hitVector);
            }
        }
    }
}
