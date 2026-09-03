#nullable enable
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SamMul.ObjectPools;
using SamMul.ResourcePools;

namespace SamMul.UnityHelpers.Sounds
{
    public enum BGMPlayRule
    {
        ForcePlay,              // 무조건 새로 재생한다.
        SkipRePlayIfSameBGM,    // 같은 BGM 이 재생 중이면 그대로 둔다.
    }

    /// <summary>
    /// 사운드 프리팹을 풀링하여 재생합니다. 볼륨은 AudioSource 볼륨으로 직접 적용합니다.
    /// </summary>
    public class SoundManager
    {
        public static readonly float VOLUME_SLIDER_MINVALUE = 0.0001f;

        private static readonly string STORAGE_KEY_SFX_VOLUME = "KEY_SFX_VOLUME";
        private static readonly string STORAGE_KEY_BGM_VOLUME = "KEY_BGM_VOLUME";
        private static readonly string STORAGE_KEY_VIBRATION = "KEY_VIBRATION";

        // 같은 사운드가 이 프레임 수 안에 다시 요청되면 무시한다.
        private const int DUPLICATE_SUPPRESS_FRAMES = 18;

        private readonly ObjectPool<string, SoundObject> _prefabInstancePool;
        private readonly List<SoundObject> _activeSoundObjects = new List<SoundObject>();
        private readonly Dictionary<string, int> _lastPlayedFrameByPath = new Dictionary<string, int>();

        private SoundObject? _currentBGM;
        public SoundObject? CurrentBGM => _currentBGM;

        public bool IsVibrationActivated { get; private set; }

        public SoundManager()
        {
            _prefabInstancePool = new ObjectPool<string, SoundObject>(this.AllocateFromPrefab);
        }

        public void Initialize()
        {
            IsVibrationActivated = this.ReadVibrationStatus();
        }

        // 오디오 믹서를 쓰지 않으므로 할 일이 없다. UnityGlobal 호환용.
        public IEnumerator AudioMixerVolumeInitializationErrorWorkaround()
        {
            yield break;
        }

        /// <param name="volume">0 ~ 1.0f</param>
        public void SetBGMVolume(float volume)
        {
            if (volume <= VOLUME_SLIDER_MINVALUE)
            {
                volume = 0f;
            }
            PlayerPrefs.SetFloat(STORAGE_KEY_BGM_VOLUME, volume);
            if (_currentBGM != null)
            {
                _currentBGM.SetVolume(volume);
            }
        }

        public float GetBGMVolume()
        {
            return PlayerPrefs.GetFloat(STORAGE_KEY_BGM_VOLUME, 1f);
        }

        /// <param name="volume">0 ~ 1.0f</param>
        public void SetEffectVolume(float volume)
        {
            PlayerPrefs.SetFloat(STORAGE_KEY_SFX_VOLUME, volume);
        }

        public float GetEffectVolume()
        {
            return PlayerPrefs.GetFloat(STORAGE_KEY_SFX_VOLUME, 1f);
        }

        /// <returns>on == true</returns>
        public bool ReadVibrationStatus()
        {
            return PlayerPrefs.GetInt(STORAGE_KEY_VIBRATION, 1) == 1;
        }

        public void ClearBeforeChangingScene(Scene relatedScene)
        {
            if (_currentBGM != null && _currentBGM.gameObject.scene == relatedScene)
            {
                this.Stop(_currentBGM);
            }

            for (int i = _activeSoundObjects.Count - 1; i >= 0; --i)
            {
                var element = _activeSoundObjects[i];
                if (element == null || element.gameObject.scene == relatedScene)
                {
                    _activeSoundObjects.RemoveAt(i);
                    if (element != null)
                    {
                        _prefabInstancePool.PutBack(element);
                    }
                }
            }

            _prefabInstancePool.DestroyAll(relatedScene, destroyFunction: (SoundObject element) =>
            {
                if (element != null)
                {
                    GameObject.Destroy(element.gameObject);
                }
            });
        }

        /// <summary>
        /// 매 프레임 호출. 재생이 끝난 사운드 오브젝트를 풀로 돌려보냅니다.
        /// </summary>
        public void CheckAndReleaseSoundObjects()
        {
            for (int i = _activeSoundObjects.Count - 1; i >= 0; --i)
            {
                var element = _activeSoundObjects[i];
                if (element == null)
                {
                    _activeSoundObjects.RemoveAt(i);
                }
                else if (!element.IsPlaying)
                {
                    _activeSoundObjects.RemoveAt(i);
                    _prefabInstancePool.PutBack(element);
                }
            }
        }

        public void PlayBGM(string soundObjectPrefabPath, BGMPlayRule playRule)
        {
            if (_currentBGM != null)
            {
                if (playRule == BGMPlayRule.SkipRePlayIfSameBGM &&
                    _currentBGM.SoundResourcePathFromPoolKey == soundObjectPrefabPath)
                {
                    return;
                }
                this.Stop(_currentBGM);
            }

            _currentBGM = this.PlayInternal(soundObjectPrefabPath, Vector3.zero, loop: true, this.GetBGMVolume());
        }

        public void PlayBySoundPrefab(string soundObjectPrefabPath, Vector3 pos)
        {
            if (string.IsNullOrEmpty(soundObjectPrefabPath))
            {
                return;
            }

            int currentFrame = Time.frameCount;
            if (_lastPlayedFrameByPath.TryGetValue(soundObjectPrefabPath, out int lastFrame) &&
                currentFrame - lastFrame <= DUPLICATE_SUPPRESS_FRAMES)
            {
                return;
            }

            var soundObject = this.PlayInternal(soundObjectPrefabPath, pos, loop: false, this.GetEffectVolume());
            if (soundObject == null)
            {
                return;
            }

            _lastPlayedFrameByPath[soundObjectPrefabPath] = currentFrame;
            _activeSoundObjects.Add(soundObject);
        }

        public void Stop(SoundObject soundObject)
        {
            if (soundObject == _currentBGM)
            {
                _currentBGM = null;
            }
            _activeSoundObjects.Remove(soundObject);
            _prefabInstancePool.PutBack(soundObject);
        }

        private SoundObject? PlayInternal(string soundObjectPrefabPath, Vector3 pos, bool loop, float volume)
        {
            if (string.IsNullOrEmpty(soundObjectPrefabPath))
            {
                return null;
            }

            var soundObject = _prefabInstancePool.TakeOne<SoundObject>(soundObjectPrefabPath);
            if (soundObject == null)
            {
                Debug.LogWarning($"{soundObjectPrefabPath} 없음. 사운드 재생 실패.");
                return null;
            }

            soundObject.gameObject.SetActive(true);
            soundObject.transform.position = pos;
            soundObject.SetVolume(volume);
            soundObject.PlayRandomClip(loop);
            return soundObject;
        }

        private SoundObject AllocateFromPrefab(string soundObjectPrefabPath)
        {
            var prefab = ResourcePool.Instance.LoadResource<GameObject>(soundObjectPrefabPath);
            if (prefab == null)
            {
                return null!;
            }

            var soundObject = GameObject.Instantiate(prefab).GetComponent<SoundObject>();
            Debug.Assert(soundObject != null, $"{soundObjectPrefabPath} 프리팹에 SoundObject 컴포넌트가 없습니다.");
            soundObject.Allocated(key: soundObjectPrefabPath);
            return soundObject;
        }
    }
}
