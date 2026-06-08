using UnityEngine;

namespace MoaiGolf
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class MoaiGolfLandingJudge : MonoBehaviour
    {
        private const float MinimumFlightTime = 0.75f;
        private const float MaxFlightTime = 12f;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private MoaiGolfGameController gameController;
        private MoaiGolfRunState runState;
        private float flyingSeconds;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
        }

        private void Start()
        {
            gameController = FindAnyObjectByType<MoaiGolfGameController>();
            runState = FindAnyObjectByType<MoaiGolfRunState>();
        }

        private void FixedUpdate()
        {
            if (gameController == null || runState == null || gameController.Phase != MoaiGolfGamePhase.Flying)
            {
                return;
            }

            flyingSeconds += Time.fixedDeltaTime;
            if (flyingSeconds >= MaxFlightTime)
            {
                CompleteJudgement(false);
                return;
            }

            if (flyingSeconds < MinimumFlightTime || !IsStopped())
            {
                return;
            }

            CompleteJudgement(IsSuccessfulLanding());
        }

        private bool IsStopped()
        {
            return body.linearVelocity.magnitude <= MoaiGolfWorldSettings.StopVelocityThreshold
                && Mathf.Abs(body.angularVelocity) <= MoaiGolfWorldSettings.StopAngularVelocityThreshold;
        }

        private bool IsSuccessfulLanding()
        {
            return runState.Stage.SuccessZone.Contains(body.position) && bodyCollider.IsTouchingLayers();
        }

        private void CompleteJudgement(bool succeeded)
        {
            gameController.BeginJudging();
            gameController.ShowResult(succeeded);
        }
    }
}
