#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SamMul.ObjectPools;

namespace SamMul.UnityHelpers.Sounds
{
    /// <summary>
    /// AudioSource 하나와 후보 클립 목록을 가진 사운드 오브젝트. <see cref="SoundManager"/>가 풀링합니다.
    /// </summary>
    public class SoundObject : MonoBehaviour, IPoolible<string>
    {
        // 프리팹 직렬화 호환을 위해 필드 이름을 유지한다.
        [SerializeField] private List<AudioClip> audioClips = new List<AudioClip>();

        private AudioSource _audioSource = null!;
        private string _poolKey = string.Empty;

        public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;
        public string SoundResourcePathFromPoolKey => _poolKey;

        Scene IPoolible<string>.RelatedScene => this.gameObject.scene;
        string IPoolible<string>.PoolKey => _poolKey;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;
        }

        /// <param name="volume">0.0 ~ 1.0</param>
        public void SetVolume(float volume)
        {
            _audioSource.volume = volume;
        }

        public void Allocated(string key)
        {
            _poolKey = key;
        }

        public void PlayRandomClip(bool loop)
        {
            if (audioClips.Count == 0)
            {
                Debug.LogWarning($"[{_poolKey}]에 오디오 클립이 없습니다. 재생하지 않습니다.");
                return;
            }

            _audioSource.clip = audioClips[Random.Range(0, audioClips.Count)];
            _audioSource.loop = loop;
            _audioSource.Play();
        }

        public void PlayRandomClip()
        {
            this.PlayRandomClip(loop: false);
        }

        // 정지는 SoundManager 를 통해서만 한다.
        private void Stop()
        {
            _audioSource.Stop();
            this.gameObject.SetActive(false);
        }

        public void PuttingBackToPool()
        {
            this.Stop();
        }
    }
}
