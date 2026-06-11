using UnityEngine;

namespace MoaiGolf
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class MoaiGolfTargetMoaiVoiceSfx : MonoBehaviour
    {
        private const float MinImpactSpeed = 1.5f;
        private const float SoundCooldownSeconds = 0.12f;

        private MoaiGolfGameController gameController;
        private MoaiGolfSeController seController;
        private float lastPlayedTime = float.NegativeInfinity;

        public void ConfigureDependencies(MoaiGolfGameController controller, MoaiGolfSeController se)
        {
            gameController = controller;
            seController = se;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.GetComponentInParent<MoaiGolfTargetMoaiMarker>() == null)
            {
                return;
            }

            if (collision.relativeVelocity.magnitude < MinImpactSpeed)
            {
                return;
            }

            if (Time.unscaledTime - lastPlayedTime < SoundCooldownSeconds)
            {
                return;
            }

            if (gameController == null || gameController.Phase != MoaiGolfGamePhase.Flying)
            {
                return;
            }

            if (seController == null || !seController.TryPlayNextTargetMoaiVoice())
            {
                return;
            }

            lastPlayedTime = Time.unscaledTime;
        }
    }
}
