using System.Collections.Generic;
using UnityEngine;

namespace MoaiGolf
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class MoaiGolfLandingJudge : MonoBehaviour
    {
        private const float MinimumFlightTime = 0.75f;
        private const float MaxFlightTime = 12f;
        private const float TrajectorySampleIntervalSeconds = 0.05f;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private MoaiGolfGameController gameController;
        private MoaiGolfRunState runState;
        private float flyingSeconds;
        private float lastSampleSeconds;
        private readonly List<Vector2> trajectorySamples = new();

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

            if (trajectorySamples.Count == 0)
            {
                trajectorySamples.Add(body.position);
                lastSampleSeconds = 0f;
            }

            flyingSeconds += Time.fixedDeltaTime;
            if (flyingSeconds - lastSampleSeconds >= TrajectorySampleIntervalSeconds)
            {
                trajectorySamples.Add(body.position);
                lastSampleSeconds = flyingSeconds;
            }

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
            // 速度が閾値以下
            return body.linearVelocity.magnitude <= MoaiGolfWorldSettings.StopVelocityThreshold
                && Mathf.Abs(body.angularVelocity) <= MoaiGolfWorldSettings.StopAngularVelocityThreshold;
        }

        private bool IsSuccessfulLanding()
        {
            // 中心点が成功枠内 + 接地中 (速度判定は IsStopped で済み)
            var successZone = runState.Stage.SuccessZone;
            return successZone.Contains(body.position)
                && bodyCollider.IsTouchingLayers();
        }

        private void CompleteJudgement(bool succeeded)
        {
            // 着地点と軌道を履歴へ
            trajectorySamples.Add(body.position);
            runState.RecordAttempt(trajectorySamples.ToArray(), body.position);
            trajectorySamples.Clear();

            gameController.BeginJudging();
            gameController.ShowResult(succeeded);
        }
    }
}
