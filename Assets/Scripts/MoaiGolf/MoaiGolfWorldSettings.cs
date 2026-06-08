using UnityEngine;

namespace MoaiGolf
{
    public static class MoaiGolfWorldSettings
    {
        public const int ReferenceWidthPixels = 3840;
        public const int ReferenceHeightPixels = 1200;
        public const float ReferenceAspect = (float)ReferenceWidthPixels / ReferenceHeightPixels;
        public const float PixelsPerUnit = 100f;
        public const float WorldWidth = ReferenceWidthPixels / PixelsPerUnit;
        public const float WorldHeight = ReferenceHeightPixels / PixelsPerUnit;
        public const float WorldLeft = 0f;
        public const float WorldRight = WorldWidth;
        public const float GroundY = 1.15f;
        public const float CameraOrthographicSize = WorldHeight * 0.5f;
        public const float CameraCenterX = WorldWidth * 0.5f;
        public const float CameraCenterY = WorldHeight * 0.5f;
        public const float CameraZ = -10f;
        public const float GravityY = -9.81f;
        public const float FixedTimestep = 1f / 60f;
        public const float StopVelocityThreshold = 0.08f;
        public const float StopAngularVelocityThreshold = 4f;
        public const float MinimumLaunchSpeed = 7.5f;
        public const float MaximumLaunchSpeed = 18f;
        public const float LaunchAngularVelocity = -520f;
    }
}
