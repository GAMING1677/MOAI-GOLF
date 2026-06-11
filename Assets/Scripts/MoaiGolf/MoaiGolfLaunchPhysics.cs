using UnityEngine;

namespace MoaiGolf
{
    public static class MoaiGolfLaunchPhysics
    {
        public static Vector2 CalculateThrustVelocity(float angleDegrees, float power01, float launchMass)
        {
            var clampedPower = Mathf.Clamp01(power01);
            var baseSpeed = Mathf.Lerp(
                MoaiGolfWorldSettings.MinimumLaunchSpeed,
                MoaiGolfWorldSettings.MaximumLaunchSpeed,
                clampedPower
            );
            var speed = baseSpeed * CalculateMassSpeedMultiplier(launchMass);
            var radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * speed;
        }

        public static float CalculateMassSpeedMultiplier(float launchMass)
        {
            var safeMass = Mathf.Max(launchMass, 0.001f);
            var multiplier = Mathf.Sqrt(MoaiGolfWorldSettings.ReferenceLaunchMass / safeMass);
            return Mathf.Clamp(
                multiplier,
                MoaiGolfWorldSettings.MinimumLaunchMassSpeedMultiplier,
                MoaiGolfWorldSettings.MaximumLaunchMassSpeedMultiplier
            );
        }

        public static float CalculateSpinVelocity()
        {
            return MoaiGolfWorldSettings.LaunchAngularVelocity;
        }
    }
}
