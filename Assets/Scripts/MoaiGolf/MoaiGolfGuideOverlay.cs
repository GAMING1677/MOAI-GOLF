using System.Collections.Generic;
using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfGuideOverlay : MonoBehaviour
    {
        public const int TrajectorySampleCount = 5;
        public const int MaxPooledHistoryDots = 360;
        public const int MaxPooledHistoryLandings = 3;
        public const string GuideVisualsRootName = "GuideVisuals";

        private const float TrajectoryStepSeconds = 0.06f;
        private const float TrajectoryDotSize = 0.13f;
        private const float HistoryDotSize = 0.11f;
        private const float AngleArrowMoaiClearance = 1.0f;
        private const float PreviousLaunchArrowAlpha = 0.6f;

        private static readonly Color TrajectoryColor = new(1f, 1f, 1f, 0.85f);
        private static readonly Color HistoryDotColor = new(1f, 0.95f, 0.55f, 0.65f);
        private static readonly Color HistoryLandingColor = new(1f, 0.55f, 0.2f, 0.85f);

        [SerializeField] private Camera mainCamera;
        [SerializeField] private MoaiGolfGameController gameController;
        [SerializeField] private MoaiGolfRunState runState;
        [SerializeField] private MoaiGolfStageView stageView;

        [SerializeField] private Transform angleArrowPivot;
        [SerializeField] private Transform previousAngleArrowPivot;
        [SerializeField] private GameObject[] trajectoryDots = System.Array.Empty<GameObject>();
        [SerializeField] private GameObject powerGaugeRoot;
        [SerializeField] private Transform powerGaugeFillTransform;
        [SerializeField] private Transform powerGaugeKnobTransform;
        [SerializeField] private GameObject launchButtonRoot;
        [SerializeField] private Transform historyRoot;
        [SerializeField] private GameObject[] historyDotPool = System.Array.Empty<GameObject>();
        [SerializeField] private GameObject[] historyLandingMarkers = System.Array.Empty<GameObject>();

        private readonly List<SpriteRenderer> historyDotRenderers = new();
        private readonly List<SpriteRenderer> historyLandingRenderers = new();
        private int lastHistoryVersion = -1;

        private void Reset()
        {
            RefreshSerializedReferencesForEditor(null, null, null, null);
        }

        private void OnValidate()
        {
            RefreshSerializedReferencesForEditor(null, null, null, null);
        }

        private void Awake()
        {
            if (!ValidateReferences())
            {
                return;
            }

            CacheHistoryRenderers();
            SetInitialVisualState();
        }

        public void RefreshSerializedReferencesForEditor(
            Camera camera,
            MoaiGolfGameController controller,
            MoaiGolfRunState state,
            MoaiGolfStageView view
        )
        {
            mainCamera = camera != null ? camera : mainCamera;
            gameController = controller != null ? controller : GetComponent<MoaiGolfGameController>();
            runState = state != null ? state : GetComponent<MoaiGolfRunState>();
            stageView = view != null ? view : FindAnyObjectByType<MoaiGolfStageView>();
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        public bool HasMissingGuideReferences()
        {
            if (angleArrowPivot == null
                || previousAngleArrowPivot == null
                || trajectoryDots == null
                || trajectoryDots.Length != TrajectorySampleCount
                || powerGaugeRoot == null
                || powerGaugeFillTransform == null
                || powerGaugeKnobTransform == null
                || launchButtonRoot == null
                || historyRoot == null
                || historyDotPool == null
                || historyDotPool.Length != MaxPooledHistoryDots
                || historyLandingMarkers == null
                || historyLandingMarkers.Length != MaxPooledHistoryLandings)
            {
                return true;
            }

            foreach (var dot in trajectoryDots)
            {
                if (dot == null)
                {
                    return true;
                }
            }

            foreach (var dot in historyDotPool)
            {
                if (dot == null)
                {
                    return true;
                }
            }

            foreach (var marker in historyLandingMarkers)
            {
                if (marker == null)
                {
                    return true;
                }
            }

            return false;
        }

        public bool ValidateReferences()
        {
            var isValid = true;
            isValid &= ValidateReference(mainCamera, nameof(mainCamera));
            isValid &= ValidateReference(gameController, nameof(gameController));
            isValid &= ValidateReference(runState, nameof(runState));
            isValid &= ValidateReference(stageView, nameof(stageView));
            if (HasMissingGuideReferences())
            {
                Debug.LogError(
                    $"{nameof(MoaiGolfGuideOverlay)} guide visuals are missing or incomplete. Run Moai Golf/Setup Guide Overlay in the Editor.",
                    this
                );
                isValid = false;
            }

            return isValid;
        }

        private void LateUpdate()
        {
            if (gameController == null || runState == null || HasMissingGuideReferences())
            {
                return;
            }

            var phase = gameController.Phase;
            UpdateAngleArrow(phase == MoaiGolfGamePhase.AngleSelect);
            UpdatePreviousAngleArrow(phase == MoaiGolfGamePhase.AngleSelect);
            UpdatePowerGauge(phase == MoaiGolfGamePhase.PowerSelect);
            UpdateLaunchButton(phase == MoaiGolfGamePhase.PowerSelect);
            UpdatePredictedTrajectory(phase == MoaiGolfGamePhase.PowerSelect);
            UpdateHistoryVisuals();
        }

        private void SetInitialVisualState()
        {
            angleArrowPivot.gameObject.SetActive(false);
            previousAngleArrowPivot.gameObject.SetActive(false);
            powerGaugeRoot.SetActive(false);
            launchButtonRoot.SetActive(false);

            foreach (var dot in trajectoryDots)
            {
                dot.SetActive(false);
            }

            foreach (var dot in historyDotPool)
            {
                dot.SetActive(false);
            }

            foreach (var marker in historyLandingMarkers)
            {
                marker.SetActive(false);
            }
        }

        private void CacheHistoryRenderers()
        {
            historyDotRenderers.Clear();
            foreach (var dot in historyDotPool)
            {
                historyDotRenderers.Add(dot != null ? dot.GetComponent<SpriteRenderer>() : null);
            }

            historyLandingRenderers.Clear();
            foreach (var marker in historyLandingMarkers)
            {
                historyLandingRenderers.Add(marker != null ? marker.GetComponent<SpriteRenderer>() : null);
            }
        }

        private bool ValidateReference(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(MoaiGolfGuideOverlay)} missing serialized reference: {fieldName}.", this);
            return false;
        }

        private void UpdateAngleArrow(bool visible)
        {
            angleArrowPivot.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            ApplyAngleArrowTransform(angleArrowPivot, gameController.AngleDegrees, gameController.Power01);
        }

        private void UpdatePreviousAngleArrow(bool visible)
        {
            visible = visible && gameController.HasPreviousLaunch;
            previousAngleArrowPivot.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            ApplyAngleArrowTransform(
                previousAngleArrowPivot,
                gameController.PreviousLaunchAngleDegrees,
                gameController.PreviousLaunchPower01
            );
        }

        private void ApplyAngleArrowTransform(Transform arrowPivot, float angleDegrees, float power01)
        {
            var velocity = MoaiGolfLaunchPhysics.CalculateThrustVelocity(angleDegrees, power01, GetLaunchMass());
            var gravity = new Vector2(0f, MoaiGolfWorldSettings.GravityY);
            var start = runState.LaunchPosition;
            var pos = start;
            const float dt = 0.01f;
            const int maxSteps = 240;
            var clearanceSqr = AngleArrowMoaiClearance * AngleArrowMoaiClearance;
            for (var step = 0; step < maxSteps; step++)
            {
                if ((pos - start).sqrMagnitude >= clearanceSqr)
                {
                    break;
                }

                pos += velocity * dt;
                velocity += gravity * dt;
            }

            var arrowAngleDeg = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            arrowPivot.position = new Vector3(pos.x, pos.y, 0f);
            arrowPivot.rotation = Quaternion.Euler(0f, 0f, arrowAngleDeg);
        }

        private void UpdatePowerGauge(bool visible)
        {
            powerGaugeRoot.SetActive(visible);
            if (!visible || mainCamera == null)
            {
                return;
            }

            var gaugeCenter = MoaiGolfPowerControlsLayout.GetGaugeCenter(mainCamera);
            powerGaugeRoot.transform.position = new Vector3(gaugeCenter.x, gaugeCenter.y, 0f);

            var innerWidth = MoaiGolfPowerControlsLayout.GaugeWidth - MoaiGolfPowerControlsLayout.GaugeBorderThickness * 2f;
            var innerHeight = MoaiGolfPowerControlsLayout.GaugeHeight - MoaiGolfPowerControlsLayout.GaugeBorderThickness * 2f;
            var fillWidth = innerWidth * Mathf.Clamp01(gameController.Power01);
            powerGaugeFillTransform.localPosition = new Vector3(-innerWidth * 0.5f + fillWidth * 0.5f, 0f, 0f);
            powerGaugeFillTransform.localScale = new Vector3(fillWidth, innerHeight, 1f);

            var knobX = -innerWidth * 0.5f + innerWidth * Mathf.Clamp01(gameController.Power01);
            powerGaugeKnobTransform.localPosition = new Vector3(knobX, 0f, 0f);
        }

        private void UpdateLaunchButton(bool visible)
        {
            launchButtonRoot.SetActive(visible);
            if (!visible || mainCamera == null)
            {
                return;
            }

            var buttonCenter = MoaiGolfPowerControlsLayout.GetLaunchButtonCenter(mainCamera);
            launchButtonRoot.transform.position = new Vector3(buttonCenter.x, buttonCenter.y, 0f);
        }

        private void UpdatePredictedTrajectory(bool visible)
        {
            if (!visible)
            {
                for (var index = 0; index < trajectoryDots.Length; index++)
                {
                    trajectoryDots[index].SetActive(false);
                }

                return;
            }

            var startPos = runState.LaunchPosition;
            var velocity = MoaiGolfLaunchPhysics.CalculateThrustVelocity(
                gameController.AngleDegrees,
                gameController.Power01,
                GetLaunchMass()
            );
            var gravity = new Vector2(0f, MoaiGolfWorldSettings.GravityY);
            var pos = startPos;
            var hitTerrain = false;
            for (var index = 0; index < trajectoryDots.Length; index++)
            {
                var dot = trajectoryDots[index];
                if (hitTerrain)
                {
                    dot.SetActive(false);
                    continue;
                }

                pos += velocity * TrajectoryStepSeconds;
                velocity += gravity * TrajectoryStepSeconds;

                var terrainY = MoaiGolfTerrainProfile.GetY(pos.x);
                if (pos.y < terrainY)
                {
                    dot.SetActive(false);
                    hitTerrain = true;
                    continue;
                }

                dot.SetActive(true);
                dot.transform.position = new Vector3(pos.x, pos.y, 0f);
            }
        }

        private void UpdateHistoryVisuals()
        {
            if (runState.HistoryVersion == lastHistoryVersion)
            {
                return;
            }

            var trajectories = runState.PreviousTrajectories;
            var landings = runState.PreviousLandingPoints;
            var historyCount = trajectories.Count;
            var poolIndex = 0;

            for (var historyIndex = 0; historyIndex < historyCount; historyIndex++)
            {
                var trajectory = trajectories[historyIndex];
                if (trajectory == null || trajectory.Length == 0)
                {
                    continue;
                }

                var fade = Mathf.Lerp(0.35f, 0.75f, (historyIndex + 1f) / Mathf.Max(1, historyCount));
                var color = new Color(HistoryDotColor.r, HistoryDotColor.g, HistoryDotColor.b, HistoryDotColor.a * fade);
                for (var pointIndex = 0; pointIndex < trajectory.Length; pointIndex++)
                {
                    if ((pointIndex & 1) == 1 || poolIndex >= historyDotPool.Length)
                    {
                        continue;
                    }

                    var dot = historyDotPool[poolIndex];
                    var renderer = historyDotRenderers[poolIndex];
                    var point = trajectory[pointIndex];
                    dot.transform.position = new Vector3(point.x, point.y, 0f);
                    dot.transform.localScale = new Vector3(HistoryDotSize, HistoryDotSize, 1f);
                    if (renderer != null)
                    {
                        renderer.color = color;
                    }

                    dot.SetActive(true);
                    poolIndex++;
                }
            }

            for (var index = poolIndex; index < historyDotPool.Length; index++)
            {
                historyDotPool[index].SetActive(false);
            }

            for (var historyIndex = 0; historyIndex < historyLandingMarkers.Length; historyIndex++)
            {
                var marker = historyLandingMarkers[historyIndex];
                var renderer = historyLandingRenderers[historyIndex];
                if (historyIndex >= historyCount || historyIndex >= landings.Count)
                {
                    marker.SetActive(false);
                    continue;
                }

                var fade = Mathf.Lerp(0.35f, 0.75f, (historyIndex + 1f) / Mathf.Max(1, historyCount));
                var landing = landings[historyIndex];
                marker.transform.position = new Vector3(landing.x, landing.y, 0f);
                if (renderer != null)
                {
                    renderer.color = new Color(
                        HistoryLandingColor.r,
                        HistoryLandingColor.g,
                        HistoryLandingColor.b,
                        HistoryLandingColor.a * fade
                    );
                }

                marker.SetActive(true);
            }

            lastHistoryVersion = runState.HistoryVersion;
        }

        private float GetLaunchMass()
        {
            if (stageView?.LaunchBody != null)
            {
                return stageView.LaunchBody.mass;
            }

            return MoaiGolfWorldSettings.ReferenceLaunchMass;
        }
    }
}
