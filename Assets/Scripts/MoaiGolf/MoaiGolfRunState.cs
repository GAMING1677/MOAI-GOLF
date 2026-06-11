using System.Collections.Generic;
using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfRunState : MonoBehaviour
    {
        private const float LaunchOffsetRangePixels = 400f;
        private const int MaxHistoryCount = 3;

        private readonly List<Vector2[]> previousTrajectories = new();
        private readonly List<Vector2> previousLandingPoints = new();

        public MoaiGolfStageDefinition Stage { get; private set; }
        public MoaiGolfMoaiKind LaunchMoaiKind { get; private set; }
        public Vector2 LaunchPosition { get; private set; }
        public Vector2 LaunchPedestalCenter { get; private set; }
        public int AttemptIndex { get; private set; }
        public bool UsesSceneLayout { get; private set; }
        public bool NeedsLaunchRandomization => !launchRandomized;
        public bool NeedsTargetRandomization => !targetLineupRandomized;
        public MoaiGolfMoaiKind[] TargetLineup { get; private set; }
        private MoaiGolfSceneLayout sceneLayout;
        private bool launchRandomized;
        private bool targetLineupRandomized;
        public IReadOnlyList<Vector2[]> PreviousTrajectories => previousTrajectories;
        public IReadOnlyList<Vector2> PreviousLandingPoints => previousLandingPoints;
        public int HistoryVersion { get; private set; }

        public void InitializeNewRun(MoaiGolfStageDefinition stage)
        {
            Stage = stage;
            AttemptIndex = 1;
            UsesSceneLayout = false;
            launchRandomized = false;
            targetLineupRandomized = false;
            TargetLineup = null;
            previousTrajectories.Clear();
            previousLandingPoints.Clear();
            HistoryVersion++;
        }

        public void ApplySceneLayout(MoaiGolfSceneLayout layout)
        {
            sceneLayout = layout;
            UsesSceneLayout = true;
            Stage = Stage.WithSceneOverrides(layout.LaunchPosition, layout.SuccessZone);
            RandomizeLaunch();
            RandomizeTargetLineup();
        }

        public void RetryCurrentRun()
        {
            AttemptIndex++;
        }

        public void RerollAndRetry()
        {
            AttemptIndex++;
            RandomizeLaunch();
            RandomizeTargetLineup();
        }

        public void RandomizeLaunchForBuild()
        {
            RandomizeLaunch();
        }

        public void RandomizeTargetLineupForBuild()
        {
            RandomizeTargetLineup();
        }

        public void SetLaunchPlacement(Vector2 launchPosition, Vector2 launchPedestalCenter, float launchVisualFeetY)
        {
            LaunchPosition = launchPosition;
            LaunchPedestalCenter = launchPedestalCenter;
            if (UsesSceneLayout)
            {
                sceneLayout = new MoaiGolfSceneLayout(
                    launchPosition,
                    launchPedestalCenter,
                    launchVisualFeetY,
                    sceneLayout.SuccessZone,
                    sceneLayout.TargetMoaiSlotPositions
                );
            }
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

        private void RandomizeTargetLineup()
        {
            var slotCount = sceneLayout.TargetMoaiSlotPositions?.Length ?? 5;
            if (TargetLineup == null || TargetLineup.Length != slotCount)
            {
                TargetLineup = new MoaiGolfMoaiKind[slotCount];
            }

            for (var index = 0; index < slotCount; index++)
            {
                TargetLineup[index] = (MoaiGolfMoaiKind)Random.Range(0, 4);
            }

            targetLineupRandomized = true;
        }

        private void RandomizeLaunch()
        {
            LaunchMoaiKind = (MoaiGolfMoaiKind)Random.Range(0, 4);
            launchRandomized = true;
            if (UsesSceneLayout)
            {
                RandomizeLaunchFromScene();
                return;
            }

            RandomizeLaunchFromTerrain();
        }

        private void RandomizeLaunchFromScene()
        {
            var spec = MoaiGolfMoaiSpec.Get(LaunchMoaiKind);
            var halfRangePixels = LaunchOffsetRangePixels * 0.5f;
            var offsetX = Random.Range(-halfRangePixels, halfRangePixels) / MoaiGolfWorldSettings.PixelsPerUnit;
            var pedestalCenterX = Mathf.Clamp(
                sceneLayout.LaunchPedestalCenter.x + offsetX,
                MoaiGolfWorldSettings.WorldLeft + MoaiGolfWorldSettings.LaunchPedestalWidth * 0.5f,
                MoaiGolfWorldSettings.WorldRight - MoaiGolfWorldSettings.LaunchPedestalWidth * 0.5f
            );
            LaunchPedestalCenter = new Vector2(pedestalCenterX, sceneLayout.LaunchPedestalCenter.y);
            LaunchPosition = spec.GetBodyCenterFromVisualFeet(new Vector2(pedestalCenterX, sceneLayout.LaunchVisualFeetY));
        }

        private void RandomizeLaunchFromTerrain()
        {
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
            LaunchPosition = spec.GetBodyCenterFromVisualFeet(new Vector2(pedestalCenterX, pedestalTop + 0.02f));
        }

        public void ResetBodyForRetry(Rigidbody2D body)
        {
            body.position = LaunchPosition;
            body.centerOfMass = Vector2.zero;
            body.rotation = 0f;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.Sleep();
        }
    }
}
