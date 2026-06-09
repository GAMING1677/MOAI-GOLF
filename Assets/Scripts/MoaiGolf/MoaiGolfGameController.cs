using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfGameController : MonoBehaviour
    {
        public MoaiGolfGamePhase Phase { get; private set; } = MoaiGolfGamePhase.AngleSelect;
        public float AngleDegrees { get; private set; } = 38f;
        public float Power01 { get; private set; } = 0.55f;
        public bool? LastResultSucceeded { get; private set; }

        public void SetAngle(float angleDegrees)
        {
            if (Phase != MoaiGolfGamePhase.AngleSelect)
            {
                return;
            }

            AngleDegrees = Mathf.Clamp(angleDegrees, 5f, 80f);
        }

        public void ConfirmAngle()
        {
            if (Phase == MoaiGolfGamePhase.AngleSelect)
            {
                Phase = MoaiGolfGamePhase.PowerSelect;
            }
        }

        public void SetPower(float power01)
        {
            if (Phase != MoaiGolfGamePhase.PowerSelect)
            {
                return;
            }

            Power01 = Mathf.Clamp01(power01);
        }

        public void BeginFlying()
        {
            if (Phase == MoaiGolfGamePhase.LaunchAnimation)
            {
                Phase = MoaiGolfGamePhase.Flying;
            }
        }

        public void BeginLaunchAnimation()
        {
            if (Phase == MoaiGolfGamePhase.PowerSelect)
            {
                Phase = MoaiGolfGamePhase.LaunchAnimation;
            }
        }

        public void FinishLaunchAnimation(Rigidbody2D launchBody)
        {
            if (Phase != MoaiGolfGamePhase.LaunchAnimation)
            {
                return;
            }

            launchBody.WakeUp();
            launchBody.linearVelocity = MoaiGolfLaunchPhysics.CalculateThrustVelocity(AngleDegrees, Power01);
            launchBody.angularVelocity = MoaiGolfLaunchPhysics.CalculateSpinVelocity();
            BeginFlying();
        }

        public void BeginJudging()
        {
            if (Phase == MoaiGolfGamePhase.Flying)
            {
                Phase = MoaiGolfGamePhase.Judging;
            }
        }

        public void ShowResult(bool succeeded)
        {
            if (Phase != MoaiGolfGamePhase.Judging)
            {
                return;
            }

            LastResultSucceeded = succeeded;
            Phase = MoaiGolfGamePhase.Result;
        }

        public void ResetForRetry()
        {
            Phase = MoaiGolfGamePhase.AngleSelect;
            LastResultSucceeded = null;
        }
    }
}
