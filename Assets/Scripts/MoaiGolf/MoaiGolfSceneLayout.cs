using UnityEngine;

namespace MoaiGolf
{
    public readonly struct MoaiGolfSceneLayout
    {
        public MoaiGolfSceneLayout(
            Vector2 launchPosition,
            Vector2 launchPedestalCenter,
            float launchVisualFeetY,
            Rect successZone,
            Vector2[] targetMoaiSlotPositions = null
        )
        {
            LaunchPosition = launchPosition;
            LaunchPedestalCenter = launchPedestalCenter;
            LaunchVisualFeetY = launchVisualFeetY;
            SuccessZone = successZone;
            TargetMoaiSlotPositions = targetMoaiSlotPositions;
        }

        public Vector2 LaunchPosition { get; }
        public Vector2 LaunchPedestalCenter { get; }
        public float LaunchVisualFeetY { get; }
        public Rect SuccessZone { get; }
        public Vector2[] TargetMoaiSlotPositions { get; }
    }
}
