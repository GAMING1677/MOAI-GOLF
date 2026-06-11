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
        private ResourceRequest successClipRequest;
        private ResourceRequest[] targetMoaiVoiceClipRequests;
        private int nextTargetMoaiVoiceIndex;

        public float Volume
        {
            get => volume;
            set
            {
                volume = Mathf.Clamp01(value);
                MoaiGolfAudioSettingsStore.SeVolume = volume;
            }
        }

        public void SetVolume(float volume01)
        {
            Volume = volume01;
        }

        private void Awake()
        {
            volume = MoaiGolfAudioSettingsStore.SeVolume;
            EnsureVoiceAudioSource();
        }

        public void PlaySuccess()
        {
            EnsureVoiceAudioSource();
            PreloadAudio();
            var clip = GetLoadedClip(ref successClip, ref successClipRequest);
            if (voiceAudioSource == null || clip == null)
            {
                return;
            }

            voiceAudioSource.PlayOneShot(clip, volume);
        }

        public bool TryPlayNextTargetMoaiVoice()
        {
            EnsureVoiceAudioSource();
            PreloadAudio();

            if (voiceAudioSource == null || targetMoaiVoiceClips == null || targetMoaiVoiceClips.Length == 0)
            {
                return false;
            }

            var clip = GetLoadedTargetMoaiVoiceClip(nextTargetMoaiVoiceIndex);
            if (clip == null)
            {
                return false;
            }

            nextTargetMoaiVoiceIndex = (nextTargetMoaiVoiceIndex + 1) % targetMoaiVoiceClips.Length;
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

        public void PreloadAudio()
        {
            if (successClip == null && successClipRequest == null)
            {
                successClipRequest = Resources.LoadAsync<AudioClip>(SuccessResourcePath);
            }

            PreloadTargetMoaiVoiceClips();
        }

        private void PreloadTargetMoaiVoiceClips()
        {
            if (targetMoaiVoiceClips != null)
            {
                return;
            }

            targetMoaiVoiceClips = new AudioClip[TargetMoaiVoiceResourcePaths.Length];
            targetMoaiVoiceClipRequests = new ResourceRequest[TargetMoaiVoiceResourcePaths.Length];
            for (var index = 0; index < TargetMoaiVoiceResourcePaths.Length; index++)
            {
                targetMoaiVoiceClipRequests[index] = Resources.LoadAsync<AudioClip>(TargetMoaiVoiceResourcePaths[index]);
            }
        }

        private AudioClip GetLoadedTargetMoaiVoiceClip(int index)
        {
            if (targetMoaiVoiceClips == null
                || targetMoaiVoiceClipRequests == null
                || index < 0
                || index >= targetMoaiVoiceClips.Length)
            {
                return null;
            }

            return GetLoadedClip(ref targetMoaiVoiceClips[index], ref targetMoaiVoiceClipRequests[index]);
        }

        private static AudioClip GetLoadedClip(ref AudioClip clip, ref ResourceRequest request)
        {
            if (clip != null)
            {
                return clip;
            }

            if (request == null || !request.isDone)
            {
                return null;
            }

            clip = request.asset as AudioClip;
            request = null;
            return clip;
        }

        private void OnValidate()
        {
            volume = Mathf.Clamp01(volume);
        }
    }
}
