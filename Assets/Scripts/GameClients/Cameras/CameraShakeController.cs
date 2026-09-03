#nullable enable
using UnityEngine;

namespace SamMul.GameClients.Cameras
{
    /// <summary>
    /// 카메라 흔들림 프리셋 모음. 외부 카메라 셰이크 라이브러리를 대체하는 자체 구현으로,
    /// 실제 흔들림은 메인 카메라에 붙는 <see cref="SimpleCameraShaker"/>가 수행한다.
    /// </summary>
    public class CameraShakeController
    {
        public readonly struct ShakeParams
        {
            public readonly float PositionStrength;
            public readonly float RotationStrength;
            public readonly float Frequency;
            public readonly float Duration;
            public readonly Vector2 AxesMultiplier;

            public ShakeParams(float positionStrength, float rotationStrength, float frequency, float duration, Vector2 axesMultiplier)
            {
                PositionStrength = positionStrength;
                RotationStrength = rotationStrength;
                Frequency = frequency;
                Duration = duration;
                AxesMultiplier = axesMultiplier;
            }
        }

        private static readonly ShakeParams s_enemyDead = new ShakeParams(0.08f, 0.5f, 40f, 0.06f, Vector2.one);
        private static readonly ShakeParams s_weaponAttack = new ShakeParams(0.12f, 0.5f, 40f, 0.2f, Vector2.one);
        private static readonly ShakeParams s_switchingGun = new ShakeParams(0.055f, 0f, 40f, 0.1f, Vector2.up);
        private static readonly ShakeParams s_enterEffect = new ShakeParams(0.5f, 0f, 40f, 0.3f, Vector2.right);
        private static readonly ShakeParams s_stageFailPopup = new ShakeParams(1f, 0f, 40f, 0.3f, Vector2.up);

        private SimpleCameraShaker? _shaker;

        public void SetTargetCamera(Camera mainCamera)
        {
            Debug.Assert(mainCamera != null);
            _shaker = mainCamera!.gameObject.GetComponent<SimpleCameraShaker>();
            if (_shaker == null)
            {
                _shaker = mainCamera.gameObject.AddComponent<SimpleCameraShaker>();
            }
        }

        public void EnemyDeadShake(Vector2 deadDir) => _shaker?.Kick(s_enemyDead, deadDir);
        public void WeaponAttackShake(Vector3 attackDir) => _shaker?.Kick(s_weaponAttack, attackDir);
        public void SwitchingGunShake() => _shaker?.Shake(s_switchingGun);
        public void StageEnterEffectShake() => _shaker?.Shake(s_enterEffect);
        public void StageFailPopupShake() => _shaker?.Shake(s_stageFailPopup);

        /// <summary>짧고 정확한 2D 흔들림.</summary>
        public void CameraShortShake2D(float positionStrength, float rotationStrength, float frequency, int numBounces)
            => _shaker?.Shake(new ShakeParams(positionStrength, rotationStrength, frequency, numBounces / Mathf.Max(frequency, 1f), Vector2.one));

        /// <summary>폭발처럼 강하게 시작해 잦아드는 흔들림.</summary>
        public void CameraExplosion2D(float positionStrength, float rotationStrength, float duration)
            => _shaker?.Shake(new ShakeParams(positionStrength, rotationStrength, 25f, duration, Vector2.one));

        public void CameraBounceShake(ShakeParams bounceParams) => _shaker?.Shake(bounceParams);

        public void SettingUnscaledTime(bool unscaledTime)
        {
            if (_shaker != null)
            {
                _shaker.UseUnscaledTime = unscaledTime;
            }
        }
    }

    /// <summary>카메라 로컬 트랜스폼에 감쇠 노이즈를 더하는 최소 셰이커.</summary>
    public class SimpleCameraShaker : MonoBehaviour
    {
        private struct ActiveShake
        {
            public CameraShakeController.ShakeParams Params;
            public Vector2 Direction;
            public float Elapsed;
            public float Seed;
        }

        private readonly System.Collections.Generic.List<ActiveShake> _shakes = new System.Collections.Generic.List<ActiveShake>();
        private Vector3 _lastOffset;
        private float _lastAngle;

        public bool UseUnscaledTime { get; set; }

        public void Shake(CameraShakeController.ShakeParams shakeParams)
        {
            _shakes.Add(new ActiveShake { Params = shakeParams, Direction = Vector2.zero, Elapsed = 0f, Seed = Random.value * 100f });
        }

        public void Kick(CameraShakeController.ShakeParams shakeParams, Vector2 direction)
        {
            _shakes.Add(new ActiveShake { Params = shakeParams, Direction = direction.normalized, Elapsed = 0f, Seed = Random.value * 100f });
        }

        private void LateUpdate()
        {
            // 이전 프레임의 흔들림을 되돌린 뒤 이번 프레임 값을 더한다.
            transform.localPosition -= _lastOffset;
            transform.localRotation *= Quaternion.Euler(0f, 0f, -_lastAngle);
            _lastOffset = Vector3.zero;
            _lastAngle = 0f;

            float dt = UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            Vector2 offset = Vector2.zero;
            float angle = 0f;

            for (int i = _shakes.Count - 1; i >= 0; --i)
            {
                var shake = _shakes[i];
                shake.Elapsed += dt;
                float duration = Mathf.Max(shake.Params.Duration, 0.01f);
                if (shake.Elapsed >= duration)
                {
                    _shakes.RemoveAt(i);
                    continue;
                }

                float attenuation = 1f - (shake.Elapsed / duration);
                float phase = shake.Elapsed * shake.Params.Frequency;
                Vector2 noise = shake.Direction == Vector2.zero
                    ? new Vector2(Mathf.PerlinNoise(shake.Seed, phase) - 0.5f, Mathf.PerlinNoise(phase, shake.Seed) - 0.5f) * 2f
                    : shake.Direction * Mathf.Sin(phase * Mathf.PI);

                offset += noise * shake.Params.AxesMultiplier * shake.Params.PositionStrength * attenuation;
                angle += (Mathf.PerlinNoise(shake.Seed + 7f, phase) - 0.5f) * 2f * shake.Params.RotationStrength * attenuation;
                _shakes[i] = shake;
            }

            _lastOffset = new Vector3(offset.x, offset.y, 0f);
            _lastAngle = angle;
            transform.localPosition += _lastOffset;
            transform.localRotation *= Quaternion.Euler(0f, 0f, _lastAngle);
        }
    }
}
