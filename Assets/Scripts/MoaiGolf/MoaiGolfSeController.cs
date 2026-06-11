using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfSeController : MonoBehaviour
    {
        public const float DefaultVolume = 0.2f;
        private const float TargetMoaiVoiceVolume = 1.4125375f; // +3 dB (from 1.0)
        private const string SuccessResourcePath = "Audio/se_success";

        private static readonly string[] TargetMoaiVoiceResourcePaths =
        {
            "Audio/moaivoice01",
            "Audio/moaivoice02",
            "Audio/moaivoice03",
            "Audio/moaivoice04",
            "Audio/moaivoice05",
        };

        [SerializeField, Range(0f, 1f)] private float volume = DefaultVolume;

        private AudioSource voiceAudioSource;
        private AudioClip successClip;
        private AudioClip[] targetMoaiVoiceClips;
        private int nextTargetMoaiVoiceIndex;

        public float Volume
        {
            get => volume;
            set => volume = Mathf.Clamp01(value);
        }

        public void SetVolume(float volume01)
        {
            Volume = volume01;
        }

        public void PlaySuccess()
        {
            EnsureVoiceAudioSource();
            successClip ??= Resources.Load<AudioClip>(SuccessResourcePath);
            if (voiceAudioSource == null || successClip == null)
            {
                return;
            }

            voiceAudioSource.PlayOneShot(successClip, volume);
        }

        public bool TryPlayNextTargetMoaiVoice()
        {
            EnsureVoiceAudioSource();
            EnsureTargetMoaiVoiceClips();

            if (voiceAudioSource == null || targetMoaiVoiceClips == null || targetMoaiVoiceClips.Length == 0)
            {
                return false;
            }

            var clip = targetMoaiVoiceClips[nextTargetMoaiVoiceIndex];
            nextTargetMoaiVoiceIndex = (nextTargetMoaiVoiceIndex + 1) % targetMoaiVoiceClips.Length;
            if (clip == null)
            {
                return false;
            }

            voiceAudioSource.PlayOneShot(clip, TargetMoaiVoiceVolume * volume);
            return true;
        }

        private void EnsureVoiceAudioSource()
        {
            voiceAudioSource ??= GetComponent<AudioSource>();
            if (voiceAudioSource != null)
            {
                return;
            }

            voiceAudioSource = gameObject.AddComponent<AudioSource>();
            voiceAudioSource.playOnAwake = false;
            voiceAudioSource.loop = false;
            voiceAudioSource.spatialBlend = 0f;
        }

        private void EnsureTargetMoaiVoiceClips()
        {
            if (targetMoaiVoiceClips != null)
            {
                return;
            }

            targetMoaiVoiceClips = new AudioClip[TargetMoaiVoiceResourcePaths.Length];
            for (var index = 0; index < TargetMoaiVoiceResourcePaths.Length; index++)
            {
                targetMoaiVoiceClips[index] = Resources.Load<AudioClip>(TargetMoaiVoiceResourcePaths[index]);
            }
        }

        private void OnValidate()
        {
            volume = Mathf.Clamp01(volume);
        }
    }
}
