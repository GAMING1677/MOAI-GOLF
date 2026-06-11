using UnityEngine;

namespace MoaiGolf
{
    public static class MoaiGolfWorldSettings
    {
        public const int SourceBackgroundWidthPixels = 3840;
        public const int SourceBackgroundHeightPixels = 1200;
        public const int BackgroundWidthPixels = SourceBackgroundWidthPixels;
        public const int BackgroundHeightPixels = SourceBackgroundHeightPixels;
        public const int ViewportWidthPixels = 1920;
        public const int ViewportHeightPixels = 1080;
        public const float ReferenceAspect = (float)ViewportWidthPixels / ViewportHeightPixels;
        public const float PixelsPerUnit = 100f;
        public const float WorldWidth = BackgroundWidthPixels / PixelsPerUnit;
        public const float WorldHeight = BackgroundHeightPixels / PixelsPerUnit;
        public const float ViewportWorldWidth = ViewportWidthPixels / PixelsPerUnit;
        public const float ViewportWorldHeight = ViewportHeightPixels / PixelsPerUnit;
        public const float WorldLeft = 0f;
        public const float WorldRight = WorldWidth;
        public const float GroundY = 1.15f;
        public const float CameraOrthographicSize = ViewportWorldHeight * 0.5f;
        public const float CameraCenterX = ViewportWorldWidth * 0.5f;
        public const float CameraCenterY = ViewportWorldHeight * 0.5f;
        public const float CameraZ = -10f;
        public const float CameraMinX = ViewportWorldWidth * 0.5f;
        public const float CameraMaxX = WorldWidth - ViewportWorldWidth * 0.5f;
        public const float CameraMinY = ViewportWorldHeight * 0.5f;
        public const float CameraMaxY = WorldHeight - ViewportWorldHeight * 0.5f;
        public const float LaunchPedestalWidth = 1.3f;
        public const float LaunchPedestalHeight = 0.35f;
        public const float GravityY = -9.81f;
        public const float FixedTimestep = 1f / 60f;
        public const float StopVelocityThreshold = 0.08f;
        public const float StopAngularVelocityThreshold = 4f;
        public const float LandingDwellSeconds = 3f;
        public const float LandingDwellPositionTolerance = 0.12f;
        public const float MinimumLaunchSpeed = 10f;
        public const float MaximumLaunchSpeed = 24f;
        public const float LaunchAngularVelocity = -520f;
    }
}
