using System.Collections.Generic;
using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfRunState : MonoBehaviour
    {
        private const float LaunchOffsetRangePixels = 400f;
        private const float MoaiTextureWorldHeight = 1.5f;
        private const int MaxHistoryCount = 3;

        private readonly List<Vector2[]> previousTrajectories = new();
        private readonly List<Vector2> previousLandingPoints = new();

        public MoaiGolfStageDefinition Stage { get; private set; }
        public MoaiGolfMoaiKind LaunchMoaiKind { get; private set; }
        public Vector2 LaunchPosition { get; private set; }
        public Vector2 LaunchPedestalCenter { get; private set; }
        public int AttemptIndex { get; private set; }
        public IReadOnlyList<Vector2[]> PreviousTrajectories => previousTrajectories;
        public IReadOnlyList<Vector2> PreviousLandingPoints => previousLandingPoints;
        public int HistoryVersion { get; private set; }

        public void InitializeNewRun(MoaiGolfStageDefinition stage)
        {
            Stage = stage;
            AttemptIndex = 1;
            previousTrajectories.Clear();
            previousLandingPoints.Clear();
            HistoryVersion++;
            RandomizeLaunch();
        }

        public void RetryCurrentRun()
        {
            AttemptIndex++;
        }

        public void RerollAndRetry()
        {
            AttemptIndex++;
            RandomizeLaunch();
        }

        public void RecordAttempt(Vector2[] trajectory, Vector2 landingPoint)
        {
            if (trajectory == null || trajectory.Length < 2)
            {
                return;
            }

            previousTrajectories.Add(trajectory);
            previousLandingPoints.Add(landingPoint);
            while (previousTrajectories.Count > MaxHistoryCount)
            {
                previousTrajectories.RemoveAt(0);
                previousLandingPoints.RemoveAt(0);
            }

            HistoryVersion++;
        }

        private void RandomizeLaunch()
        {
            LaunchMoaiKind = (MoaiGolfMoaiKind)Random.Range(0, 4);
            var spec = MoaiGolfMoaiSpec.Get(LaunchMoaiKind);
            var halfRangePixels = LaunchOffsetRangePixels * 0.5f;
            var offsetX = Random.Range(-halfRangePixels, halfRangePixels) / MoaiGolfWorldSettings.PixelsPerUnit;
            var pedestalCenterX = Mathf.Clamp(
                Stage.LaunchPosition.x + offsetX,
                MoaiGolfWorldSettings.WorldLeft + MoaiGolfWorldSettings.LaunchPedestalWidth * 0.5f,
                MoaiGolfWorldSettings.WorldRight - MoaiGolfWorldSettings.LaunchPedestalWidth * 0.5f
            );
            var terrainY = MoaiGolfTerrainProfile.GetY(pedestalCenterX);
            LaunchPedestalCenter = new Vector2(pedestalCenterX, terrainY + MoaiGolfWorldSettings.LaunchPedestalHeight * 0.5f);
            var pedestalTop = terrainY + MoaiGolfWorldSettings.LaunchPedestalHeight;
            var visualHalfHeight = MoaiTextureWorldHeight * spec.VisualScale.y * 0.5f;
            LaunchPosition = new Vector2(pedestalCenterX, pedestalTop + visualHalfHeight + 0.02f);
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
