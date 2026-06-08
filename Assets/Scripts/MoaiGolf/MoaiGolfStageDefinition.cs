using UnityEngine;

namespace MoaiGolf
{
    public static class MoaiGolfTerrainProfile
    {
        private static readonly Vector2[] Points =
        {
            new Vector2(0f, 2.25f),
            new Vector2(3.2f, 2.42f),
            new Vector2(7.2f, 2.12f),
            new Vector2(12.2f, 1.72f),
            new Vector2(18.6f, 1.95f),
            new Vector2(25.6f, 3.05f),
            new Vector2(33.8f, 3.9f),
            new Vector2(43.2f, 4.62f),
            new Vector2(57.4f, 4.62f),
            new Vector2(68.4f, 4.45f),
            new Vector2(MoaiGolfWorldSettings.WorldRight, 4.25f)
        };

        public static Vector2[] ColliderPoints => Points;

        public static float GetY(float x)
        {
            if (x <= Points[0].x)
            {
                return Points[0].y;
            }

            for (var index = 1; index < Points.Length; index++)
            {
                var previous = Points[index - 1];
                var next = Points[index];
                if (x > next.x)
                {
                    continue;
                }

                var segmentT = Mathf.InverseLerp(previous.x, next.x, x);
                return Mathf.Lerp(previous.y, next.y, segmentT);
            }

            return Points[^1].y;
        }
    }

    public readonly struct MoaiGolfStageDefinition
    {
        public MoaiGolfStageDefinition(
            string id,
            string backgroundSpritePath,
            Rect worldBounds,
            Vector2 groundCenter,
            Vector2 groundSize,
            Vector2 launchPosition,
            Vector2 targetMoaiPosition,
            Rect successZone
        )
        {
            Id = id;
            BackgroundSpritePath = backgroundSpritePath;
            WorldBounds = worldBounds;
            GroundCenter = groundCenter;
            GroundSize = groundSize;
            LaunchPosition = launchPosition;
            TargetMoaiPosition = targetMoaiPosition;
            SuccessZone = successZone;
        }

        public string Id { get; }
        public string BackgroundSpritePath { get; }
        public Rect WorldBounds { get; }
        public Vector2 GroundCenter { get; }
        public Vector2 GroundSize { get; }
        public Vector2 LaunchPosition { get; }
        public Vector2 TargetMoaiPosition { get; }
        public Rect SuccessZone { get; }

        public static MoaiGolfStageDefinition CreateFirstStage()
        {
            return new MoaiGolfStageDefinition(
                "stage-001",
                "Textures/background_0",
                new Rect(
                    MoaiGolfWorldSettings.WorldLeft,
                    0f,
                    MoaiGolfWorldSettings.WorldWidth,
                    MoaiGolfWorldSettings.WorldHeight
                ),
                new Vector2(MoaiGolfWorldSettings.CameraCenterX, MoaiGolfTerrainProfile.GetY(MoaiGolfWorldSettings.CameraCenterX)),
                new Vector2(MoaiGolfWorldSettings.WorldWidth, 0.35f),
                new Vector2(3.2f, MoaiGolfTerrainProfile.GetY(3.2f) + MoaiGolfWorldSettings.LaunchPedestalHeight * 0.5f),
                new Vector2(48.4f, MoaiGolfTerrainProfile.GetY(48.4f) + 0.68f),
                new Rect(53.6f, MoaiGolfTerrainProfile.GetY(53.6f), 1.35f, 2.3f)
            );
        }
    }
}
