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
        private const float PowerGaugeWidth = 2.6f;
        private const float PowerGaugeHeight = 0.34f;
        private const float PowerGaugeBorderThickness = 0.05f;
        private const float AngleArrowWorldLength = 1.1f;
        private const float AngleArrowMoaiClearance = 1.0f;

        private static readonly Color TrajectoryColor = new(1f, 1f, 1f, 0.85f);
        private static readonly Color PowerFilledColor = new(0.92f, 0.22f, 0.18f);
        private static readonly Color PowerEmptyColor = new(0.08f, 0.08f, 0.08f);
        private static readonly Color PowerBorderColor = new(0f, 0f, 0f, 0.85f);
        private static readonly Color HistoryDotColor = new(1f, 0.95f, 0.55f, 0.65f);
        private static readonly Color HistoryLandingColor = new(1f, 0.55f, 0.2f, 0.85f);

        private static Sprite whitePixelSprite;

        private MoaiGolfGameController gameController;
        private MoaiGolfRunState runState;

        private Transform angleArrowPivot;
        private SpriteRenderer angleArrowRenderer;
        private GameObject[] trajectoryDots;
        private GameObject powerGaugeRoot;
        private Transform powerGaugeFillTransform;
        private Transform historyRoot;
        private readonly List<GameObject> historyVisuals = new();
        private int lastHistoryVersion = -1;

        private void Start()
        {
            gameController = FindAnyObjectByType<MoaiGolfGameController>();
            runState = FindAnyObjectByType<MoaiGolfRunState>();
            EnsureVisuals();
        }

        private void LateUpdate()
        {
            gameController ??= FindAnyObjectByType<MoaiGolfGameController>();
            runState ??= FindAnyObjectByType<MoaiGolfRunState>();
            if (gameController == null || runState == null)
            {
                return;
            }

            EnsureVisuals();

            var phase = gameController.Phase;
            var showPreLaunch = phase == MoaiGolfGamePhase.AngleSelect || phase == MoaiGolfGamePhase.PowerSelect;

            UpdateAngleArrow(phase == MoaiGolfGamePhase.AngleSelect);
            UpdatePowerGauge(phase == MoaiGolfGamePhase.PowerSelect);
            UpdatePredictedTrajectory(showPreLaunch);
            UpdateHistoryVisuals();
        }

        private void EnsureVisuals()
        {
            if (angleArrowPivot == null)
            {
                CreateAngleArrow();
            }

            if (trajectoryDots == null)
            {
                CreateTrajectoryDots();
            }

            if (powerGaugeRoot == null)
            {
                CreatePowerGauge();
            }

            if (historyRoot == null)
            {
                var rootObj = new GameObject("History Trails");
                rootObj.transform.SetParent(transform);
                historyRoot = rootObj.transform;
            }
        }

        private void CreateAngleArrow()
        {
            var pivot = new GameObject("Angle Arrow Pivot");
            pivot.transform.SetParent(transform);
            angleArrowPivot = pivot.transform;

            var spriteObj = new GameObject("Angle Arrow");
            spriteObj.transform.SetParent(pivot.transform);
            var arrowSprite = MoaiGolfSpriteCatalog.Arrow;
            angleArrowRenderer = spriteObj.AddComponent<SpriteRenderer>();
            angleArrowRenderer.sprite = arrowSprite != null ? arrowSprite : GetWhitePixelSprite();
            angleArrowRenderer.sortingOrder = 6;
            // arrow.png は上向きの赤矢印（ピボット左下、bounds は (~0.96, ~1.45)）。
            // 子スプライトをローカル -90° 回し、頭がローカル +X 方向を向くようにする。
            // 親 (Pivot) を Z 軸で AngleDegrees だけ回せば、その向きに矢印が伸びる。
            var renderedSprite = angleArrowRenderer.sprite;
            var spriteSize = renderedSprite != null ? renderedSprite.bounds.size : new Vector3(1f, 1f, 0f);
            var scale = AngleArrowWorldLength / Mathf.Max(spriteSize.y, 0.001f);
            spriteObj.transform.localScale = new Vector3(scale, scale, 1f);
            spriteObj.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            // -90° 回転後、矢印は (0, -spriteSize.x*scale) → (spriteSize.y*scale, 0) の範囲。
            // 矢印の縦方向中央 (元の x 中心 = spriteSize.x*0.5) が原点と並ぶよう Y を補正する。
            spriteObj.transform.localPosition = new Vector3(0f, spriteSize.x * scale * 0.5f, 0f);
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
            border.transform.localScale = new Vector3(PowerGaugeWidth, PowerGaugeHeight, 1f);
            border.transform.localPosition = Vector3.zero;
            var borderRenderer = border.AddComponent<SpriteRenderer>();
            borderRenderer.sprite = GetWhitePixelSprite();
            borderRenderer.color = PowerBorderColor;
            borderRenderer.sortingOrder = 5;

            var background = new GameObject("Background");
            background.transform.SetParent(powerGaugeRoot.transform);
            var innerWidth = PowerGaugeWidth - PowerGaugeBorderThickness * 2f;
            var innerHeight = PowerGaugeHeight - PowerGaugeBorderThickness * 2f;
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

            // 矢印を予測放物線そのものに沿わせる。
            // 重力込みでミニシミュレーションし、モアイから AngleArrowMoaiClearance だけ進んだ地点に
            // 矢印の尾を置き、その時点の速度ベクトルの向きに矢印を回す。
            var velocity = MoaiGolfLaunchPhysics.CalculateThrustVelocity(gameController.AngleDegrees, gameController.Power01);
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
            angleArrowPivot.position = new Vector3(pos.x, pos.y, 0f);
            angleArrowPivot.rotation = Quaternion.Euler(0f, 0f, arrowAngleDeg);
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

            // モアイの一番上から 20px (= 0.2 ワールドユニット) ほど上にバーを置く。
            // 発射モアイの種類ごとに見た目の高さが違うので、現在の Launch Moai の VisualScale を参照する。
            var spec = MoaiGolfMoaiSpec.Get(runState.LaunchMoaiKind);
            var moaiVisualHalfHeight = 1.5f * spec.VisualScale.y * 0.5f;
            var moaiTopY = runState.LaunchPosition.y + moaiVisualHalfHeight;
            var rowY = moaiTopY + 0.2f + PowerGaugeHeight * 0.5f;
            powerGaugeRoot.transform.position = new Vector3(runState.LaunchPosition.x, rowY, 0f);

            if (powerGaugeFillTransform != null)
            {
                var innerWidth = PowerGaugeWidth - PowerGaugeBorderThickness * 2f;
                var innerHeight = PowerGaugeHeight - PowerGaugeBorderThickness * 2f;
                var fillWidth = innerWidth * Mathf.Clamp01(gameController.Power01);
                powerGaugeFillTransform.localPosition = new Vector3(-innerWidth * 0.5f + fillWidth * 0.5f, 0f, 0f);
                powerGaugeFillTransform.localScale = new Vector3(fillWidth, innerHeight, 1f);
            }
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
            var velocity = MoaiGolfLaunchPhysics.CalculateThrustVelocity(gameController.AngleDegrees, gameController.Power01);
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
