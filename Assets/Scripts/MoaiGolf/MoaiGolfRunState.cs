using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfRunState : MonoBehaviour
    {
        private static readonly Vector2[] LaunchPositionOffsets =
        {
            new Vector2(0f, 0f),
            new Vector2(0.35f, 0f),
            new Vector2(0f, 0.25f),
            new Vector2(0.35f, 0.25f)
        };

        public MoaiGolfStageDefinition Stage { get; private set; }
        public MoaiGolfMoaiKind LaunchMoaiKind { get; private set; }
        public Vector2 LaunchPosition { get; private set; }

        public void InitializeNewRun(MoaiGolfStageDefinition stage)
        {
            Stage = stage;
            LaunchMoaiKind = (MoaiGolfMoaiKind)Random.Range(0, 4);
            LaunchPosition = stage.LaunchPosition + LaunchPositionOffsets[Random.Range(0, LaunchPositionOffsets.Length)];
        }
    }
}
