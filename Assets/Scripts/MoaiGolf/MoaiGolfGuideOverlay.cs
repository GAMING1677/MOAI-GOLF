using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfGuideOverlay : MonoBehaviour
    {
        private const int TrajectorySamples = 32;
        private const float TrajectoryStepSeconds = 0.06f;
        private const float TrajectoryDotSize = 0.13f;
        private const float HistoryDotSize = 0.11f;
        private const float AngleArrowWorldLength = 1.1f;
        private const float AngleArrowMoaiClearance = 1.0f;
        private const float LaunchButtonLabelCharacterSize = 0.075f;
        private const float PreviousLaunchArrowAlpha = 0.6f;

        private static readonly Color TrajectoryColor = new(1f, 1f, 1f, 0.85f);
        private static readonly Color PowerFilledColor = new(0.92f, 0.22f, 0.18f);
        private static readonly Color PowerEmptyColor = new(0.08f, 0.08f, 0.08f);
        private static readonly Color PowerBorderColor = new(0f, 0f, 0f, 0.85f);
        private static readonly Color LaunchButtonColor = new(0.92f, 0.22f, 0.18f, 0.96f);
        private static readonly Color LaunchButtonTextColor = Color.white;
        private static readonly Color HistoryDotColor = new(1f, 0.95f, 0.55f, 0.65f);
        private static readonly Color HistoryLandingColor = new(1f, 0.55f, 0.2f, 0.85f);

        private static Sprite whitePixelSprite;

        [SerializeField] private Camera mainCamera;
        [SerializeField] private MoaiGolfGameController gameController;
        [SerializeField] private MoaiGolfRunState runState;
        [SerializeField] private MoaiGolfStageView stageView;

        private Transform angleArrowPivot;
        private SpriteRenderer angleArrowRenderer;
        private Transform previousAngleArrowPivot;
        private SpriteRenderer previousAngleArrowRenderer;
        private GameObject[] trajectoryDots;
        private GameObject powerGaugeRoot;
        private Transform powerGaugeFillTransform;
        private Transform powerGaugeKnobTransform;
        private GameObject launchButtonRoot;
        private Transform historyRoot;
        private readonly List<GameObject> historyVisuals = new();
        private int lastHistoryVersion = -1;
        private bool powerVisualsPrewarmStarted;

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
            ValidateReferences();
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

        public bool ValidateReferences()
        {
            var isValid = true;
            isValid &= ValidateReference(mainCamera, nameof(mainCamera));
            isValid &= ValidateReference(gameController, nameof(gameController));
            isValid &= ValidateReference(runState, nameof(runState));
            isValid &= ValidateReference(stageView, nameof(stageView));
            return isValid;
        }

        private void LateUpdate()
        {
            if (gameController == null || runState == null)
            {
                return;
            }

            var phase = gameController.Phase;
            if (phase == MoaiGolfGamePhase.AngleSelect)
            {
                EnsureAngleVisuals();
            }
            else if (phase == MoaiGolfGamePhase.PowerSelect)
            {
                EnsurePowerVisuals();
            }

            EnsureHistoryRoot();

            UpdateAngleArrow(phase == MoaiGolfGamePhase.AngleSelect);
            UpdatePreviousAngleArrow(phase == MoaiGolfGamePhase.AngleSelect);
            UpdatePowerGauge(phase == MoaiGolfGamePhase.PowerSelect);
            UpdateLaunchButton(phase == MoaiGolfGamePhase.PowerSelect);
            UpdatePredictedTrajectory(phase == MoaiGolfGamePhase.PowerSelect);
            UpdateHistoryVisuals();
        }

        private void EnsureAngleVisuals()
        {
            if (angleArrowPivot == null)
            {
                CreateAngleArrow();
            }

            if (previousAngleArrowPivot == null)
            {
                CreatePreviousAngleArrow();
            }
        }

        private void EnsurePowerVisuals()
        {
            if (powerVisualsPrewarmStarted)
            {
                return;
            }

            StartCoroutine(PrewarmPowerVisuals());
            powerVisualsPrewarmStarted = true;
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

        private IEnumerator PrewarmPowerVisuals()
        {
            if (trajectoryDots == null)
            {
                CreateTrajectoryDots();
                yield return null;
            }

            if (powerGaugeRoot == null)
            {
                CreatePowerGauge();
                yield return null;
            }

            if (launchButtonRoot == null)
            {
                CreateLaunchButton();
                yield return null;
            }
        }

        private void EnsureHistoryRoot()
        {
            if (historyRoot == null)
            {
                var rootObj = new GameObject("History Trails");
                rootObj.transform.SetParent(transform);
                historyRoot = rootObj.transform;
            }
        }

        private void CreateAngleArrow()
        {
            angleArrowPivot = CreateAngleArrowVisual("Angle Arrow Pivot", "Angle Arrow", 6, Color.white, out angleArrowRenderer);
        }

        private void CreatePreviousAngleArrow()
        {
            previousAngleArrowPivot = CreateAngleArrowVisual(
                "Previous Angle Arrow Pivot",
                "Previous Angle Arrow",
                5,
                new Color(1f, 1f, 1f, PreviousLaunchArrowAlpha),
                out previousAngleArrowRenderer
            );
            previousAngleArrowPivot.gameObject.SetActive(false);
        }

        private Transform CreateAngleArrowVisual(string pivotName, string spriteName, int sortingOrder, Color color, out SpriteRenderer renderer)
        {
            var pivot = new GameObject(pivotName);
            pivot.transform.SetParent(transform);

            var spriteObj = new GameObject(spriteName);
            spriteObj.transform.SetParent(pivot.transform);
            var arrowSprite = MoaiGolfSpriteCatalog.Arrow;
            renderer = spriteObj.AddComponent<SpriteRenderer>();
            renderer.sprite = arrowSprite != null ? arrowSprite : GetWhitePixelSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            // arrow.png points up. Rotate the child sprite so the parent pivot's +X
            // direction is the visible arrow direction used by launch angles.
            var renderedSprite = renderer.sprite;
            var spriteSize = renderedSprite != null ? renderedSprite.bounds.size : new Vector3(1f, 1f, 0f);
            var scale = AngleArrowWorldLength / Mathf.Max(spriteSize.y, 0.001f);
            spriteObj.transform.localScale = new Vector3(scale, scale, 1f);
            spriteObj.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            spriteObj.transform.localPosition = new Vector3(0f, spriteSize.x * scale * 0.5f, 0f);
            return pivot.transform;
        }

        private void CreateTrajectoryDots()
        {
            trajectoryDots = new GameObject[TrajectorySamples];
            for (var index = 0; index < TrajectorySamples; index++)
            {
                var dot = new GameObject($"Trajectory Dot {index}");
                dot.transform.SetParent(transform);
                dot.transform.localScale = new Vector3(TrajectoryDotSize, TrajectoryDotSize, 1f);
                var renderer = dot.AddComponent<SpriteRenderer>();
                renderer.sprite = GetWhitePixelSprite();
                renderer.color = TrajectoryColor;
                renderer.sortingOrder = 5;
                dot.SetActive(false);
                trajectoryDots[index] = dot;
            }
        }

        private void CreatePowerGauge()
        {
            powerGaugeRoot = new GameObject("Power Gauge");
            powerGaugeRoot.transform.SetParent(transform);

            var border = new GameObject("Border");
            border.transform.SetParent(powerGaugeRoot.transform);
            border.transform.localScale = new Vector3(MoaiGolfPowerControlsLayout.GaugeWidth, MoaiGolfPowerControlsLayout.GaugeHeight, 1f);
            border.transform.localPosition = Vector3.zero;
            var borderRenderer = border.AddComponent<SpriteRenderer>();
            borderRenderer.sprite = GetWhitePixelSprite();
            borderRenderer.color = PowerBorderColor;
            borderRenderer.sortingOrder = 5;

            var background = new GameObject("Background");
            background.transform.SetParent(powerGaugeRoot.transform);
            var innerWidth = MoaiGolfPowerControlsLayout.GaugeWidth - MoaiGolfPowerControlsLayout.GaugeBorderThickness * 2f;
            var innerHeight = MoaiGolfPowerControlsLayout.GaugeHeight - MoaiGolfPowerControlsLayout.GaugeBorderThickness * 2f;
            background.transform.localScale = new Vector3(innerWidth, innerHeight, 1f);
            background.transform.localPosition = Vector3.zero;
            var bgRenderer = background.AddComponent<SpriteRenderer>();
            bgRenderer.sprite = GetWhitePixelSprite();
            bgRenderer.color = PowerEmptyColor;
            bgRenderer.sortingOrder = 6;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(powerGaugeRoot.transform);
            var fillRenderer = fill.AddComponent<SpriteRenderer>();
            fillRenderer.sprite = GetWhitePixelSprite();
            fillRenderer.color = PowerFilledColor;
            fillRenderer.sortingOrder = 7;
            powerGaugeFillTransform = fill.transform;

            var knob = new GameObject("Knob");
            knob.transform.SetParent(powerGaugeRoot.transform);
            knob.transform.localScale = new Vector3(MoaiGolfPowerControlsLayout.GaugeKnobSize, MoaiGolfPowerControlsLayout.GaugeKnobSize, 1f);
            var knobRenderer = knob.AddComponent<SpriteRenderer>();
            knobRenderer.sprite = GetWhitePixelSprite();
            knobRenderer.color = new Color(1f, 0.95f, 0.36f, 1f);
            knobRenderer.sortingOrder = 8;
            powerGaugeKnobTransform = knob.transform;
        }

        private void CreateLaunchButton()
        {
            launchButtonRoot = new GameObject("Launch Button");
            launchButtonRoot.transform.SetParent(transform);

            var plate = new GameObject("Plate");
            plate.transform.SetParent(launchButtonRoot.transform);
            plate.transform.localScale = new Vector3(MoaiGolfPowerControlsLayout.LaunchButtonWidth, MoaiGolfPowerControlsLayout.LaunchButtonHeight, 1f);
            plate.transform.localPosition = Vector3.zero;
            var plateRenderer = plate.AddComponent<SpriteRenderer>();
            plateRenderer.sprite = GetWhitePixelSprite();
            plateRenderer.color = LaunchButtonColor;
            plateRenderer.sortingOrder = 8;

            var labelObject = new GameObject("Label");
            labelObject.transform.SetParent(launchButtonRoot.transform);
            labelObject.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.text = "発射";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = LaunchButtonLabelCharacterSize;
            label.fontSize = 56;
            label.color = LaunchButtonTextColor;
            var labelRenderer = labelObject.GetComponent<MeshRenderer>();
            labelRenderer.sortingOrder = 9;
        }

        private void UpdateAngleArrow(bool visible)
        {
            if (angleArrowPivot == null)
            {
                return;
            }

            angleArrowPivot.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            ApplyAngleArrowTransform(angleArrowPivot, gameController.AngleDegrees, gameController.Power01);
        }

        private void UpdatePreviousAngleArrow(bool visible)
        {
            if (previousAngleArrowPivot == null)
            {
                return;
            }

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
            if (arrowPivot == null)
            {
                return;
            }

            // 矢印を予測放物線そのものに沿わせる。
            // 重力込みでミニシミュレーションし、モアイから AngleArrowMoaiClearance だけ進んだ地点に
            // 矢印の尾を置き、その時点の速度ベクトルの向きに矢印を回す。
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
            if (powerGaugeRoot == null)
            {
                return;
            }

            powerGaugeRoot.SetActive(visible);
            if (!visible)
            {
                return;
            }

            if (mainCamera == null)
            {
                return;
            }

            var gaugeCenter = MoaiGolfPowerControlsLayout.GetGaugeCenter(mainCamera);
            powerGaugeRoot.transform.position = new Vector3(gaugeCenter.x, gaugeCenter.y, 0f);

            if (powerGaugeFillTransform != null)
            {
                var innerWidth = MoaiGolfPowerControlsLayout.GaugeWidth - MoaiGolfPowerControlsLayout.GaugeBorderThickness * 2f;
                var innerHeight = MoaiGolfPowerControlsLayout.GaugeHeight - MoaiGolfPowerControlsLayout.GaugeBorderThickness * 2f;
                var fillWidth = innerWidth * Mathf.Clamp01(gameController.Power01);
                powerGaugeFillTransform.localPosition = new Vector3(-innerWidth * 0.5f + fillWidth * 0.5f, 0f, 0f);
                powerGaugeFillTransform.localScale = new Vector3(fillWidth, innerHeight, 1f);
            }

            if (powerGaugeKnobTransform != null)
            {
                var innerWidth = MoaiGolfPowerControlsLayout.GaugeWidth - MoaiGolfPowerControlsLayout.GaugeBorderThickness * 2f;
                var knobX = -innerWidth * 0.5f + innerWidth * Mathf.Clamp01(gameController.Power01);
                powerGaugeKnobTransform.localPosition = new Vector3(knobX, 0f, 0f);
            }
        }

        private void UpdateLaunchButton(bool visible)
        {
            if (launchButtonRoot == null)
            {
                return;
            }

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
            if (trajectoryDots == null)
            {
                return;
            }

            if (!visible)
            {
                for (var index = 0; index < trajectoryDots.Length; index++)
                {
                    trajectoryDots[index].SetActive(false);
                }
                return;
            }

            var startPos = runState.LaunchPosition;
            var velocity = MoaiGolfLaunchPhysics.CalculateThrustVelocity(gameController.AngleDegrees, gameController.Power01, GetLaunchMass());
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

                // 1 つおきに表示して点線にする
                if ((index & 1) == 1)
                {
                    dot.SetActive(false);
                    continue;
                }

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

            for (var index = historyVisuals.Count - 1; index >= 0; index--)
            {
                if (historyVisuals[index] != null)
                {
                    Destroy(historyVisuals[index]);
                }
            }
            historyVisuals.Clear();

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
                    if ((pointIndex & 1) == 1)
                    {
                        continue;
                    }

                    var point = trajectory[pointIndex];
                    var dot = new GameObject($"History Dot {historyIndex}.{pointIndex}");
                    dot.transform.SetParent(historyRoot);
                    dot.transform.position = new Vector3(point.x, point.y, 0f);
                    dot.transform.localScale = new Vector3(HistoryDotSize, HistoryDotSize, 1f);
                    var renderer = dot.AddComponent<SpriteRenderer>();
                    renderer.sprite = GetWhitePixelSprite();
                    renderer.color = color;
                    renderer.sortingOrder = 4;
                    historyVisuals.Add(dot);
                }

                if (historyIndex < landings.Count)
                {
                    var landing = landings[historyIndex];
                    var marker = new GameObject($"History Landing {historyIndex}");
                    marker.transform.SetParent(historyRoot);
                    marker.transform.position = new Vector3(landing.x, landing.y, 0f);
                    marker.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
                    var renderer = marker.AddComponent<SpriteRenderer>();
                    renderer.sprite = GetWhitePixelSprite();
                    renderer.color = new Color(HistoryLandingColor.r, HistoryLandingColor.g, HistoryLandingColor.b, HistoryLandingColor.a * fade);
                    renderer.sortingOrder = 5;
                    historyVisuals.Add(marker);
                }
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

        private static Sprite GetWhitePixelSprite()
        {
            if (whitePixelSprite != null)
            {
                return whitePixelSprite;
            }

            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            whitePixelSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return whitePixelSprite;
        }
    }
}
