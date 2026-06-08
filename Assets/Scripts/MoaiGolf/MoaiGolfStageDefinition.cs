using UnityEngine;

namespace MoaiGolf
{
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
                new Vector2(MoaiGolfWorldSettings.CameraCenterX, MoaiGolfWorldSettings.GroundY),
                new Vector2(MoaiGolfWorldSettings.WorldWidth, 0.35f),
                new Vector2(3.2f, MoaiGolfWorldSettings.GroundY + MoaiGolfWorldSettings.LaunchPedestalHeight * 0.5f),
                new Vector2(31.2f, 2.0f),
                new Rect(29.9f, 1.15f, 1.35f, 2.3f)
            );
        }
    }
}
