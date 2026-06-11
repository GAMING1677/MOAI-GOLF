#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MoaiGolf
{
    public static class MoaiGolfGuideOverlaySetupUtility
    {
        private const float AngleArrowWorldLength = 1.1f;
        private const float LaunchButtonLabelCharacterSize = 0.075f;

        private static readonly Color TrajectoryColor = new(1f, 1f, 1f, 0.85f);
        private static readonly Color PowerFilledColor = new(0.92f, 0.22f, 0.18f);
        private static readonly Color PowerEmptyColor = new(0.08f, 0.08f, 0.08f);
        private static readonly Color PowerBorderColor = new(0f, 0f, 0f, 0.85f);
        private static readonly Color LaunchButtonColor = new(0.92f, 0.22f, 0.18f, 0.96f);
        private static readonly Color LaunchButtonTextColor = Color.white;
        private static readonly Color HistoryDotColor = new(1f, 0.95f, 0.55f, 0.65f);
        private static readonly Color HistoryLandingColor = new(1f, 0.55f, 0.2f, 0.85f);
        private const float PreviousLaunchArrowAlpha = 0.6f;
        private const float TrajectoryDotSize = 0.13f;
        private const float HistoryDotSize = 0.11f;

        [MenuItem("Moai Golf/Setup Guide Overlay")]
        public static void SetupGuideOverlayMenu()
        {
            var guideOverlay = Object.FindAnyObjectByType<MoaiGolfGuideOverlay>();
            if (guideOverlay == null)
            {
                Debug.LogError("MoaiGolfGuideOverlay not found in the open scene.");
                return;
            }

            EnsureGuideVisuals(guideOverlay);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(guideOverlay.gameObject.scene);
            Debug.Log("Moai Golf guide overlay setup complete.");
        }

        public static void EnsureGuideVisuals(MoaiGolfGuideOverlay guideOverlay)
        {
            if (guideOverlay == null)
            {
                return;
            }

            var existingRoot = guideOverlay.transform.Find(MoaiGolfGuideOverlay.GuideVisualsRootName);
            if (existingRoot != null)
            {
                Object.DestroyImmediate(existingRoot.gameObject);
            }

            var root = EnsureChild(guideOverlay.transform, MoaiGolfGuideOverlay.GuideVisualsRootName);
            var whiteSprite = MoaiGolfSpriteCatalog.GetPersistedWhite();
            var arrowSprite = MoaiGolfSpriteCatalog.GetPersistedArrow();

            var angleArrowPivot = EnsureAngleArrow(root.transform, "Angle Arrow Pivot", "Angle Arrow", arrowSprite, 6, Color.white);
            var previousAngleArrowPivot = EnsureAngleArrow(
                root.transform,
                "Previous Angle Arrow Pivot",
                "Previous Angle Arrow",
                arrowSprite,
                5,
                new Color(1f, 1f, 1f, PreviousLaunchArrowAlpha)
            );
            previousAngleArrowPivot.gameObject.SetActive(false);

            var trajectoryDots = EnsureTrajectoryDots(root.transform, whiteSprite);
            var powerGauge = EnsurePowerGauge(root.transform, whiteSprite, out var fillTransform, out var knobTransform);
            var launchButton = EnsureLaunchButton(root.transform, whiteSprite);
            var historyRoot = EnsureFreshChild(root.transform, "History Trails").transform;
            var historyDots = EnsureHistoryDots(historyRoot, whiteSprite);
            var historyLandings = EnsureHistoryLandings(historyRoot, whiteSprite);

            WireGuideOverlayReferences(
                guideOverlay,
                angleArrowPivot,
                previousAngleArrowPivot,
                trajectoryDots,
                powerGauge,
                fillTransform,
                knobTransform,
                launchButton,
                historyRoot,
                historyDots,
                historyLandings
            );
            EditorUtility.SetDirty(guideOverlay);
        }

        public static bool HasMissingGuideReferences(MoaiGolfGuideOverlay guideOverlay)
        {
            return guideOverlay != null && guideOverlay.HasMissingGuideReferences();
        }

        private static void WireGuideOverlayReferences(
            MoaiGolfGuideOverlay guideOverlay,
            Transform angleArrowPivot,
            Transform previousAngleArrowPivot,
            GameObject[] trajectoryDots,
            GameObject powerGaugeRoot,
            Transform powerGaugeFillTransform,
            Transform powerGaugeKnobTransform,
            GameObject launchButtonRoot,
            Transform historyRoot,
            GameObject[] historyDotPool,
            GameObject[] historyLandingMarkers
        )
        {
            var serialized = new SerializedObject(guideOverlay);
            serialized.FindProperty("angleArrowPivot").objectReferenceValue = angleArrowPivot;
            serialized.FindProperty("previousAngleArrowPivot").objectReferenceValue = previousAngleArrowPivot;
            serialized.FindProperty("trajectoryDots").arraySize = trajectoryDots.Length;
            for (var index = 0; index < trajectoryDots.Length; index++)
            {
                serialized.FindProperty("trajectoryDots").GetArrayElementAtIndex(index).objectReferenceValue = trajectoryDots[index];
            }

            serialized.FindProperty("powerGaugeRoot").objectReferenceValue = powerGaugeRoot;
            serialized.FindProperty("powerGaugeFillTransform").objectReferenceValue = powerGaugeFillTransform;
            serialized.FindProperty("powerGaugeKnobTransform").objectReferenceValue = powerGaugeKnobTransform;
            serialized.FindProperty("launchButtonRoot").objectReferenceValue = launchButtonRoot;
            serialized.FindProperty("historyRoot").objectReferenceValue = historyRoot;
            serialized.FindProperty("historyDotPool").arraySize = historyDotPool.Length;
            for (var index = 0; index < historyDotPool.Length; index++)
            {
                serialized.FindProperty("historyDotPool").GetArrayElementAtIndex(index).objectReferenceValue = historyDotPool[index];
            }

            serialized.FindProperty("historyLandingMarkers").arraySize = historyLandingMarkers.Length;
            for (var index = 0; index < historyLandingMarkers.Length; index++)
            {
                serialized.FindProperty("historyLandingMarkers").GetArrayElementAtIndex(index).objectReferenceValue = historyLandingMarkers[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform EnsureAngleArrow(
            Transform parent,
            string pivotName,
            string spriteName,
            Sprite arrowSprite,
            int sortingOrder,
            Color color
        )
        {
            var pivot = EnsureFreshChild(parent, pivotName).transform;
            var spriteObject = EnsureFreshChild(pivot, spriteName);
            var resolvedSprite = arrowSprite != null ? arrowSprite : MoaiGolfSpriteCatalog.GetPersistedWhite();
            var renderer = EnsureSpriteRenderer(spriteObject);
            var arrowVisual = spriteObject.GetComponent<MoaiGolfAngleArrowVisual>()
                ?? spriteObject.AddComponent<MoaiGolfAngleArrowVisual>();
            var serializedArrow = new SerializedObject(arrowVisual);
            serializedArrow.FindProperty("arrowSprite").objectReferenceValue = resolvedSprite;
            serializedArrow.FindProperty("spriteRenderer").objectReferenceValue = renderer;
            serializedArrow.FindProperty("tint").colorValue = color;
            serializedArrow.FindProperty("sortingOrder").intValue = sortingOrder;
            serializedArrow.ApplyModifiedPropertiesWithoutUndo();
            arrowVisual.ApplyVisual();

            var spriteSize = resolvedSprite != null ? resolvedSprite.bounds.size : new Vector3(1f, 1f, 0f);
            var scale = AngleArrowWorldLength / Mathf.Max(spriteSize.y, 0.001f);
            spriteObject.transform.localScale = new Vector3(scale, scale, 1f);
            spriteObject.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            spriteObject.transform.localPosition = new Vector3(0f, spriteSize.x * scale * 0.5f, 0f);
            return pivot;
        }

        private static GameObject[] EnsureTrajectoryDots(Transform parent, Sprite whiteSprite)
        {
            var dots = new GameObject[MoaiGolfGuideOverlay.TrajectorySampleCount];
            for (var index = 0; index < dots.Length; index++)
            {
                var dot = EnsureFreshChild(parent, $"Trajectory Dot {index}");
                dot.transform.localScale = new Vector3(TrajectoryDotSize, TrajectoryDotSize, 1f);
                var renderer = EnsureSpriteRenderer(dot);
                renderer.sprite = whiteSprite;
                renderer.color = TrajectoryColor;
                renderer.sortingOrder = 5;
                dot.SetActive(false);
                dots[index] = dot;
            }

            return dots;
        }

        private static GameObject EnsurePowerGauge(
            Transform parent,
            Sprite whiteSprite,
            out Transform fillTransform,
            out Transform knobTransform
        )
        {
            var gaugeRoot = EnsureFreshChild(parent, "Power Gauge");

            var border = EnsureFreshChild(gaugeRoot.transform, "Border");
            border.transform.localScale = new Vector3(MoaiGolfPowerControlsLayout.GaugeWidth, MoaiGolfPowerControlsLayout.GaugeHeight, 1f);
            var borderRenderer = EnsureSpriteRenderer(border);
            borderRenderer.sprite = whiteSprite;
            borderRenderer.color = PowerBorderColor;
            borderRenderer.sortingOrder = 5;

            var background = EnsureFreshChild(gaugeRoot.transform, "Background");
            var innerWidth = MoaiGolfPowerControlsLayout.GaugeWidth - MoaiGolfPowerControlsLayout.GaugeBorderThickness * 2f;
            var innerHeight = MoaiGolfPowerControlsLayout.GaugeHeight - MoaiGolfPowerControlsLayout.GaugeBorderThickness * 2f;
            background.transform.localScale = new Vector3(innerWidth, innerHeight, 1f);
            var bgRenderer = EnsureSpriteRenderer(background);
            bgRenderer.sprite = whiteSprite;
            bgRenderer.color = PowerEmptyColor;
            bgRenderer.sortingOrder = 6;

            var fill = EnsureFreshChild(gaugeRoot.transform, "Fill");
            var fillRenderer = EnsureSpriteRenderer(fill);
            fillRenderer.sprite = whiteSprite;
            fillRenderer.color = PowerFilledColor;
            fillRenderer.sortingOrder = 7;
            fillTransform = fill.transform;

            var knob = EnsureFreshChild(gaugeRoot.transform, "Knob");
            knob.transform.localScale = new Vector3(MoaiGolfPowerControlsLayout.GaugeKnobSize, MoaiGolfPowerControlsLayout.GaugeKnobSize, 1f);
            var knobRenderer = EnsureSpriteRenderer(knob);
            knobRenderer.sprite = whiteSprite;
            knobRenderer.color = new Color(1f, 0.95f, 0.36f, 1f);
            knobRenderer.sortingOrder = 8;
            knobTransform = knob.transform;

            gaugeRoot.SetActive(false);
            return gaugeRoot;
        }

        private static GameObject EnsureLaunchButton(Transform parent, Sprite whiteSprite)
        {
            var launchButton = EnsureFreshChild(parent, "Launch Button");

            var plate = EnsureFreshChild(launchButton.transform, "Plate");
            plate.transform.localScale = new Vector3(
                MoaiGolfPowerControlsLayout.LaunchButtonWidth,
                MoaiGolfPowerControlsLayout.LaunchButtonHeight,
                1f
            );
            var plateRenderer = EnsureSpriteRenderer(plate);
            plateRenderer.sprite = whiteSprite;
            plateRenderer.color = LaunchButtonColor;
            plateRenderer.sortingOrder = 8;

            var labelObject = EnsureFreshChild(launchButton.transform, "Label");
            labelObject.transform.localPosition = new Vector3(0f, -0.02f, 0f);
            var label = labelObject.GetComponent<TextMesh>();
            if (label == null)
            {
                label = labelObject.AddComponent<TextMesh>();
            }

            label.text = "発射";
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = LaunchButtonLabelCharacterSize;
            label.fontSize = 56;
            label.color = LaunchButtonTextColor;
            var meshRenderer = labelObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = labelObject.AddComponent<MeshRenderer>();
            }

            meshRenderer.sortingOrder = 9;

            launchButton.SetActive(false);
            return launchButton;
        }

        private static GameObject[] EnsureHistoryDots(Transform parent, Sprite whiteSprite)
        {
            var dots = new GameObject[MoaiGolfGuideOverlay.MaxPooledHistoryDots];
            for (var index = 0; index < dots.Length; index++)
            {
                var dot = EnsureFreshChild(parent, $"History Dot {index:000}");
                dot.transform.localScale = new Vector3(HistoryDotSize, HistoryDotSize, 1f);
                var renderer = EnsureSpriteRenderer(dot);
                renderer.sprite = whiteSprite;
                renderer.color = HistoryDotColor;
                renderer.sortingOrder = 4;
                dot.SetActive(false);
                dots[index] = dot;
            }

            return dots;
        }

        private static GameObject[] EnsureHistoryLandings(Transform parent, Sprite whiteSprite)
        {
            var markers = new GameObject[MoaiGolfGuideOverlay.MaxPooledHistoryLandings];
            for (var index = 0; index < markers.Length; index++)
            {
                var marker = EnsureFreshChild(parent, $"History Landing {index}");
                marker.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
                var renderer = EnsureSpriteRenderer(marker);
                renderer.sprite = whiteSprite;
                renderer.color = HistoryLandingColor;
                renderer.sortingOrder = 5;
                marker.SetActive(false);
                markers[index] = marker;
            }

            return markers;
        }

        private static GameObject EnsureChild(Transform parent, string childName)
        {
            var existing = parent.Find(childName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var child = new GameObject(childName);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject EnsureFreshChild(Transform parent, string childName)
        {
            var existing = parent.Find(childName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            return EnsureChild(parent, childName);
        }

        private static SpriteRenderer EnsureSpriteRenderer(GameObject gameObject)
        {
            var renderer = gameObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            return renderer;
        }
    }
}
#endif
