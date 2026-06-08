using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfRunState : MonoBehaviour
    {
        private const float LaunchOffsetRangePixels = 400f;
        private const float MoaiTextureWorldHeight = 1.5f;

        public MoaiGolfStageDefinition Stage { get; private set; }
        public MoaiGolfMoaiKind LaunchMoaiKind { get; private set; }
        public Vector2 LaunchPosition { get; private set; }
        public Vector2 LaunchPedestalCenter { get; private set; }
        public int AttemptIndex { get; private set; }

        public void InitializeNewRun(MoaiGolfStageDefinition stage)
        {
            Stage = stage;
            LaunchMoaiKind = (MoaiGolfMoaiKind)Random.Range(0, 4);
            var spec = MoaiGolfMoaiSpec.Get(LaunchMoaiKind);
            var halfRangePixels = LaunchOffsetRangePixels * 0.5f;
            var offsetX = Random.Range(-halfRangePixels, halfRangePixels) / MoaiGolfWorldSettings.PixelsPerUnit;
            var pedestalCenterX = Mathf.Clamp(
                stage.LaunchPosition.x + offsetX,
                MoaiGolfWorldSettings.WorldLeft + MoaiGolfWorldSettings.LaunchPedestalWidth * 0.5f,
                MoaiGolfWorldSettings.WorldRight - MoaiGolfWorldSettings.LaunchPedestalWidth * 0.5f
            );
            var terrainY = MoaiGolfTerrainProfile.GetY(pedestalCenterX);
            LaunchPedestalCenter = new Vector2(pedestalCenterX, terrainY + MoaiGolfWorldSettings.LaunchPedestalHeight * 0.5f);
            var pedestalTop = terrainY + MoaiGolfWorldSettings.LaunchPedestalHeight;
            var visualHalfHeight = MoaiTextureWorldHeight * spec.VisualScale.y * 0.5f;
            LaunchPosition = new Vector2(pedestalCenterX, pedestalTop + visualHalfHeight + 0.02f);
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
