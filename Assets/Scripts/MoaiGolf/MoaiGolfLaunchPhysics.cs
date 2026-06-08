using UnityEngine;

namespace MoaiGolf
{
    public static class MoaiGolfLaunchPhysics
    {
        public static Vector2 CalculateInitialVelocity(float angleDegrees, float power01)
        {
            var clampedPower = Mathf.Clamp01(power01);
            var speed = Mathf.Lerp(
                MoaiGolfWorldSettings.MinimumLaunchSpeed,
                MoaiGolfWorldSettings.MaximumLaunchSpeed,
                clampedPower
            );
            var radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * speed;
        }
    }
}
