using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfBgmController : MonoBehaviour
    {
        public const float DefaultVolume = 0.2f;
        private const float MenuDuckingMultiplier = 0.5011872f; // -6 dB

        [SerializeField] private AudioClip bgmClip;
        [SerializeField, Range(0f, 1f)] private float volume = DefaultVolume;

        private AudioSource audioSource;
        private bool isMenuDucked;

        public float Volume
        {
            get => volume;
            set
            {
                volume = Mathf.Clamp01(value);
                MoaiGolfAudioSettingsStore.BgmVolume = volume;
                ApplyVolume();
            }
        }

        private void Awake()
        {
            volume = MoaiGolfAudioSettingsStore.BgmVolume;
            EnsureAudioSource();
            ApplyVolume();
        }

        private void Start()
        {
            Play();
        }

        public void SetVolume(float volume01)
        {
            Volume = volume01;
        }

        public void SetMenuDucked(bool ducked)
        {
            if (isMenuDucked == ducked)
            {
                return;
            }

            isMenuDucked = ducked;
            ApplyVolume();
        }

        private void Play()
        {
            EnsureAudioSource();
            if (audioSource == null || audioSource.clip == null || audioSource.isPlaying)
            {
                return;
            }

            audioSource.Play();
        }

        private void EnsureAudioSource()
        {
            audioSource ??= GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
            audioSource.clip = bgmClip;
        }

        private void ApplyVolume()
        {
            if (audioSource != null)
            {
                audioSource.volume = volume * (isMenuDucked ? MenuDuckingMultiplier : 1f);
            }
        }

        private void OnValidate()
        {
            volume = Mathf.Clamp01(volume);
            audioSource = GetComponent<AudioSource>();
            ApplyVolume();
        }
    }
}
