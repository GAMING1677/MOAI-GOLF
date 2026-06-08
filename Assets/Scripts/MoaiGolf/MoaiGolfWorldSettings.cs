using UnityEngine;

namespace MoaiGolf
{
    public static class MoaiGolfWorldSettings
    {
        public const int ReferenceWidthPixels = 3840;
        public const int ReferenceHeightPixels = 1200;
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
    }
}
