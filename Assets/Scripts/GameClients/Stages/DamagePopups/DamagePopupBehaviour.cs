#nullable enable
using System;
using TMPro;
using UnityEngine;

namespace Z.GameClients.Stages.DamagePopups
{
    /// <summary>
    /// A pooled floating number. Pops in, drifts along the hit direction, fades out and then returns itself to the
    /// manager through the release callback.
    /// </summary>
    public class DamagePopupBehaviour : MonoBehaviour
    {
        private const float LIFETIME = 1.2f;
        private const float POP_IN_DURATION = 0.15f;
        private const float FADE_START = 0.5f;
        private const float START_SCALE = 0.35f;
        private const float PEAK_SCALE = 0.75f;
        private const float REST_SCALE = 0.45f;
        private const float DRIFT_SPEED = 1f;
        private const float MAX_DRIFT_SPEED = 1.4f;

        private TextMeshPro _text = null!;
        private Action<DamagePopupBehaviour>? _release;
        private Vector2 _drift;
        private float _shownAt;
        private bool _isShowing;

        public static DamagePopupBehaviour Create(Transform? parent, Action<DamagePopupBehaviour> release)
        {
            var gameObject = new GameObject("DamagePopup");
            gameObject.transform.SetParent(parent, worldPositionStays: false);

            var text = gameObject.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 6f;
            text.fontStyle = FontStyles.Bold;
            text.sortingOrder = 1000;
            text.rectTransform.sizeDelta = new Vector2(4f, 1f);

            var popup = gameObject.AddComponent<DamagePopupBehaviour>();
            popup._text = text;
            popup._release = release;
            gameObject.SetActive(false);

            return popup;
        }

        public void Show(Vector3 position, string text, Color color, Vector2 hitVector)
        {
            position.y += 0.2f;
            position.x += UnityEngine.Random.Range(-0.1f, 0.1f);
            position.y += UnityEngine.Random.Range(-0.1f, 0.1f);
            transform.position = position;

            _text.text = text;
            _text.color = color;
            _text.alpha = 1f;
            _text.transform.localScale = Vector3.one * START_SCALE;

            _drift = Vector2.ClampMagnitude(hitVector * 2.7f, MAX_DRIFT_SPEED) * DRIFT_SPEED;
            _shownAt = Time.time;
            _isShowing = true;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _isShowing = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!_isShowing)
            {
                return;
            }

            float elapsed = Time.time - _shownAt;
            if (elapsed >= LIFETIME)
            {
                this.Hide();
                _release?.Invoke(this);
                return;
            }

            float scale = elapsed < POP_IN_DURATION
                ? Mathf.Lerp(START_SCALE, PEAK_SCALE, elapsed / POP_IN_DURATION)
                : Mathf.Lerp(PEAK_SCALE, REST_SCALE, (elapsed - POP_IN_DURATION) / (LIFETIME - POP_IN_DURATION));
            _text.transform.localScale = Vector3.one * scale;

            if (elapsed > FADE_START)
            {
                _text.alpha = Mathf.Lerp(1f, 0.1f, (elapsed - FADE_START) / (LIFETIME - FADE_START));
            }

            transform.position += (Vector3)(_drift * Time.deltaTime);
        }
    }
}
