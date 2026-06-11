using UnityEngine;

namespace MoaiGolf
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class MoaiGolfBounceSfx : MonoBehaviour
    {
        private const string BounceResourcePath = "Audio/BOS_HH_Drum_Kick_One_Shot_Ultron_G";
        private const float BounceVolume = 0.5061069f; // -5 dB (from 0.9)
        private const float MinImpactSpeed = 2.5f;
        private const float SoundCooldownSeconds = 0.08f;

        private AudioSource sfxAudioSource;
        private AudioClip bounceClip;
        private MoaiGolfGameController gameController;
        private MoaiGolfSeController seController;
        private float lastPlayedTime = float.NegativeInfinity;

        private void Awake()
        {
            sfxAudioSource = gameObject.AddComponent<AudioSource>();
            sfxAudioSource.playOnAwake = false;
            sfxAudioSource.loop = false;
            sfxAudioSource.spatialBlend = 0f;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryPlayBounce(collision.relativeVelocity.magnitude);
        }

        public void TryPlayBounce(float impactSpeed)
        {
            if (impactSpeed < MinImpactSpeed)
            {
                return;
            }

            if (Time.unscaledTime - lastPlayedTime < SoundCooldownSeconds)
            {
                return;
            }

            gameController ??= FindAnyObjectByType<MoaiGolfGameController>();
            if (gameController == null || gameController.Phase != MoaiGolfGamePhase.Flying)
            {
                return;
            }

            bounceClip ??= Resources.Load<AudioClip>(BounceResourcePath);
            seController ??= FindAnyObjectByType<MoaiGolfSeController>();
            if (sfxAudioSource == null || bounceClip == null)
            {
                return;
            }

            var seVolume = seController != null ? seController.Volume : MoaiGolfSeController.DefaultVolume;
            sfxAudioSource.PlayOneShot(bounceClip, BounceVolume * seVolume);
            lastPlayedTime = Time.unscaledTime;
        }
    }
}
