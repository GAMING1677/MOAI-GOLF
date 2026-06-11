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
        private float dwellSeconds;
        private Vector2 dwellAnchorPosition;
        private bool hasDwellAnchor;
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
                flyingSeconds = 0f;
                ResetDwellTracking();
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
                ResetDwellTracking();
                return;
            }

            UpdateDwellTracking();
            if (dwellSeconds < MoaiGolfWorldSettings.LandingDwellSeconds)
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
            // 成功枠内に接地しているかを、足元点だけでなく実際の Collider 重なりでも見る。
            var spec = MoaiGolfMoaiSpecRegistry.GetSpec(runState.LaunchMoaiKind);
            var landingPoint = spec.GetVisualFeetPosition(body.position);
            var successZone = runState.Stage.SuccessZone;
            return IsGrounded()
                && (successZone.Contains(landingPoint) || IsOverlappingSuccessZone(successZone));
        }

        private bool IsOverlappingSuccessZone(Rect successZone)
        {
            var overlaps = Physics2D.OverlapBoxAll(successZone.center, successZone.size, 0f);
            foreach (var overlap in overlaps)
            {
                if (overlap == null || overlap.isTrigger)
                {
                    continue;
                }

                if (overlap == bodyCollider || overlap.attachedRigidbody == body)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsGrounded()
        {
            var contactFilter = new ContactFilter2D
            {
                useTriggers = false
            };
            return bodyCollider.IsTouching(contactFilter);
        }

        private void UpdateDwellTracking()
        {
            var currentPosition = body.position;
            if (!hasDwellAnchor
                || Vector2.Distance(currentPosition, dwellAnchorPosition) > MoaiGolfWorldSettings.LandingDwellPositionTolerance)
            {
                dwellAnchorPosition = currentPosition;
                dwellSeconds = 0f;
                hasDwellAnchor = true;
                return;
            }

            dwellSeconds += Time.fixedDeltaTime;
        }

        private void ResetDwellTracking()
        {
            dwellSeconds = 0f;
            hasDwellAnchor = false;
        }

        private void CompleteJudgement(bool succeeded)
        {
            // 着地点と軌道を履歴へ
            var spec = MoaiGolfMoaiSpecRegistry.GetSpec(runState.LaunchMoaiKind);
            var landingPoint = spec.GetVisualFeetPosition(body.position);
            trajectorySamples.Add(body.position);
            runState.RecordAttempt(trajectorySamples.ToArray(), landingPoint);
            trajectorySamples.Clear();

            gameController.BeginJudging();
            gameController.ShowResult(succeeded);
        }
    }
}
