using UnityEngine;

namespace MoaiGolf
{
    public static class MoaiGolfPowerControlsLayout
    {
        public const float GaugeWidth = 7.8f;
        public const float GaugeHeight = 0.46f;
        public const float GaugeBorderThickness = 0.05f;
        public const float GaugeBottomMargin = 0.52f;
        public const float GaugeKnobSize = 0.58f;
        public const float GaugeHitHeight = 0.95f;
        public const float LaunchButtonWidth = 1.72f;
        public const float LaunchButtonHeight = 0.72f;
        public const float LaunchButtonGap = 0.38f;

        public static Vector2 GetGaugeCenter(Camera camera)
        {
            var left = GetControlsLeft(camera);
            return new Vector2(left + GaugeWidth * 0.5f, GetControlsCenterY(camera));
        }

        public static Vector2 GetLaunchButtonCenter(Camera camera)
        {
            var left = GetControlsLeft(camera);
            return new Vector2(left + GaugeWidth + LaunchButtonGap + LaunchButtonWidth * 0.5f, GetControlsCenterY(camera));
        }

        public static Rect GetGaugeRect(Camera camera)
        {
            var center = GetGaugeCenter(camera);
            return new Rect(
                center.x - GaugeWidth * 0.5f,
                center.y - GaugeHitHeight * 0.5f,
                GaugeWidth,
                GaugeHitHeight
            );
        }

        public static Rect GetLaunchButtonRect(Camera camera)
        {
            var center = GetLaunchButtonCenter(camera);
            return new Rect(
                center.x - LaunchButtonWidth * 0.5f,
                center.y - LaunchButtonHeight * 0.5f,
                LaunchButtonWidth,
                LaunchButtonHeight
            );
        }

        public static bool IsPointerOnPowerControls(Camera camera, Vector2 screenPosition)
        {
            if (camera == null)
            {
                return false;
            }

            var worldPosition = (Vector2)camera.ScreenToWorldPoint(screenPosition);
            return GetGaugeRect(camera).Contains(worldPosition) || GetLaunchButtonRect(camera).Contains(worldPosition);
        }

        private static float GetControlsLeft(Camera camera)
        {
            var totalWidth = GaugeWidth + LaunchButtonGap + LaunchButtonWidth;
            return camera.transform.position.x - totalWidth * 0.5f;
        }

        private static float GetControlsCenterY(Camera camera)
        {
            return camera.transform.position.y - camera.orthographicSize + GaugeBottomMargin + LaunchButtonHeight * 0.5f;
        }
    }
}
