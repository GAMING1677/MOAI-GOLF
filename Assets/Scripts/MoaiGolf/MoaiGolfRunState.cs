using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfRunState : MonoBehaviour
    {
        private static readonly Vector2[] LaunchPositionOffsets =
        {
            new Vector2(0f, 0f),
            new Vector2(0.35f, 0f),
            new Vector2(-0.25f, 0f),
            new Vector2(0.15f, 0f)
        };

        public MoaiGolfStageDefinition Stage { get; private set; }
        public MoaiGolfMoaiKind LaunchMoaiKind { get; private set; }
        public Vector2 LaunchPosition { get; private set; }
        public int AttemptIndex { get; private set; }

        public void InitializeNewRun(MoaiGolfStageDefinition stage)
        {
            Stage = stage;
            LaunchMoaiKind = (MoaiGolfMoaiKind)Random.Range(0, 4);
            var spec = MoaiGolfMoaiSpec.Get(LaunchMoaiKind);
            var pedestalCenter = stage.LaunchPosition + LaunchPositionOffsets[Random.Range(0, LaunchPositionOffsets.Length)];
            var pedestalTop = pedestalCenter.y + MoaiGolfWorldSettings.LaunchPedestalHeight * 0.5f;
            LaunchPosition = new Vector2(pedestalCenter.x, pedestalTop + spec.ColliderSize.y * 0.5f + 0.02f);
            AttemptIndex = 1;
        }

        public void RetryCurrentRun()
        {
            AttemptIndex++;
        }

        public void ResetBodyForRetry(Rigidbody2D body)
        {
            body.position = LaunchPosition;
            body.rotation = 0f;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.Sleep();
        }
    }
}
