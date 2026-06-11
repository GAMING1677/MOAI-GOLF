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
        private const float FlightMotionDistanceThreshold = 0.45f;
        private const float FlightMotionSpeedThreshold = 1.25f;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        [SerializeField] private MoaiGolfGameController gameController;
        [SerializeField] private MoaiGolfRunState runState;
        [SerializeField] private MoaiGolfStageView stageView;
        private float flyingSeconds;
        private float lastSampleSeconds;
        private float dwellSeconds;
        private Vector2 dwellAnchorPosition;
        private Vector2 flightStartPosition;
        private bool hasDwellAnchor;
        private bool hasTouchedSuccessZone;
        private bool isTrackingFlight;
        private bool hasObservedFlightMotion;
        private bool judgementCompleted;
        private readonly List<Vector2> trajectorySamples = new();

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
        }

        public void ConfigureDependencies(
            MoaiGolfGameController controller,
            MoaiGolfRunState state,
            MoaiGolfStageView view
        )
        {
            gameController = controller;
            runState = state;
            stageView = view;
            ValidateReference(gameController, nameof(gameController));
            ValidateReference(runState, nameof(runState));
            ValidateReference(stageView, nameof(stageView));
        }

        public void ResetForReuse()
        {
            isTrackingFlight = false;
            ResetTrackingState();
        }

        public void BeginFlight()
        {
            ResetTrackingState();
            isTrackingFlight = true;
            flightStartPosition = body.position;
            trajectorySamples.Clear();
            trajectorySamples.Add(body.position);
            lastSampleSeconds = 0f;
        }

        private void ResetTrackingState()
        {
            flyingSeconds = 0f;
            lastSampleSeconds = 0f;
            dwellSeconds = 0f;
            dwellAnchorPosition = Vector2.zero;
            flightStartPosition = body.position;
            hasDwellAnchor = false;
            hasTouchedSuccessZone = false;
            hasObservedFlightMotion = false;
            judgementCompleted = false;
            trajectorySamples.Clear();
        }

        private void FixedUpdate()
        {
            if (!isTrackingFlight
                || judgementCompleted
                || gameController == null
                || runState == null
                || gameController.Phase != MoaiGolfGamePhase.Flying)
            {
                return;
            }

            flyingSeconds += Time.fixedDeltaTime;
            UpdateFlightMotionObservation();

            if (flyingSeconds - lastSampleSeconds >= TrajectorySampleIntervalSeconds)
            {
                trajectorySamples.Add(body.position);
                lastSampleSeconds = flyingSeconds;
            }

            if (flyingSeconds >= MaxFlightTime)
            {
                CompleteJudgement(IsInSuccessZoneNow());
                return;
            }

            if (flyingSeconds < MinimumFlightTime || !CanJudgeLanding())
            {
                ResetDwellTracking();
                return;
            }

            UpdateDwellTracking();
            if (dwellSeconds < MoaiGolfWorldSettings.LandingDwellSeconds)
            {
                return;
            }

            CompleteJudgement(IsInSuccessZoneNow());
        }

        private void UpdateFlightMotionObservation()
        {
            if (hasObservedFlightMotion)
            {
                return;
            }

            if (Vector2.Distance(body.position, flightStartPosition) >= FlightMotionDistanceThreshold
                || body.linearVelocity.magnitude >= FlightMotionSpeedThreshold
                || Mathf.Abs(body.angularVelocity) >= FlightMotionSpeedThreshold * 45f)
            {
                hasObservedFlightMotion = true;
            }
        }

        private bool CanJudgeLanding()
        {
            return hasObservedFlightMotion && IsStopped();
        }

        private bool IsStopped()
        {
            return body.linearVelocity.magnitude <= MoaiGolfWorldSettings.StopVelocityThreshold
                && Mathf.Abs(body.angularVelocity) <= MoaiGolfWorldSettings.StopAngularVelocityThreshold;
        }

        private bool IsInSuccessZoneNow()
        {
            var successZone = ResolveSuccessZoneRect();
            var spec = MoaiGolfMoaiSpecRegistry.GetSpec(runState.LaunchMoaiKind);
            var landingPoint = spec.GetVisualFeetPosition(body.position);
            return hasTouchedSuccessZone
                || successZone.Contains(landingPoint)
                || IsMoaiColliderTouchingSuccessZone(successZone);
        }

        private Rect ResolveSuccessZoneRect()
        {
            if (stageView != null && stageView.TryGetSuccessZoneRect(out var rect))
            {
                return rect;
            }

            return runState.Stage.SuccessZone;
        }

        private bool IsMoaiColliderTouchingSuccessZone(Rect successZone)
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

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryMarkSuccessZoneTouch(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryMarkSuccessZoneTouch(other);
        }

        private void TryMarkSuccessZoneTouch(Collider2D other)
        {
            if (other == null
                || !isTrackingFlight
                || judgementCompleted
                || gameController == null
                || gameController.Phase != MoaiGolfGamePhase.Flying
                || flyingSeconds < MinimumFlightTime)
            {
                return;
            }

            var stageElement = other.GetComponentInParent<MoaiGolfStageElement>();
            if (stageElement != null && stageElement.Kind == MoaiGolfStageElementKind.SuccessZone)
            {
                hasTouchedSuccessZone = true;
            }
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
            if (judgementCompleted)
            {
                return;
            }

            judgementCompleted = true;
            isTrackingFlight = false;

            var spec = MoaiGolfMoaiSpecRegistry.GetSpec(runState.LaunchMoaiKind);
            var landingPoint = spec.GetVisualFeetPosition(body.position);
            trajectorySamples.Add(body.position);
            runState.RecordAttempt(trajectorySamples.ToArray(), landingPoint);
            trajectorySamples.Clear();

            gameController.BeginJudging();
            gameController.ShowResult(succeeded);
        }

        private bool ValidateReference(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(MoaiGolfLandingJudge)} missing serialized reference: {fieldName}.", this);
            return false;
        }
    }
}
