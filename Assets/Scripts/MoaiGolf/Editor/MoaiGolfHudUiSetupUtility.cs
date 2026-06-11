#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MoaiGolf
{
    public static class MoaiGolfHudUiSetupUtility
    {
        private static string HudCanvasName => MoaiGolfHud.HudCanvasObjectName;
        private const string EventSystemName = "MoaiGolfEventSystem";

        [MenuItem("Moai Golf/Setup Hud UI")]
        public static void SetupHudUiMenu()
        {
            var hud = Object.FindAnyObjectByType<MoaiGolfHud>();
            if (hud == null)
            {
                Debug.LogError("MoaiGolfHud not found in the open scene.");
                return;
            }

            EnsureHudUi(hud);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            Debug.Log("Moai Golf HUD UI setup complete. Assign result sprites on MoaiGolfHud if needed.");
        }

        public static void EnsureHudUi(MoaiGolfHud hud)
        {
            if (hud == null)
            {
                return;
            }

            EnsureEventSystem();
            var canvasRoot = EnsureCanvasRoot(hud.transform);
            var gameplayChrome = EnsureChild(canvasRoot, "GameplayChrome");
            var optionsDialog = EnsureChild(canvasRoot, "OptionsDialog");
            var resultOverlay = EnsureChild(canvasRoot, "ResultOverlay");

            ConfigureGameplayChrome(gameplayChrome);
            ConfigureOptionsDialog(optionsDialog);
            ConfigureResultOverlay(resultOverlay);

            optionsDialog.SetActive(false);
            resultOverlay.SetActive(false);

            WireHudReferences(hud, gameplayChrome, optionsDialog, resultOverlay);
            hud.ResolveUiReferencesFromHierarchy();
            EditorUtility.SetDirty(hud);
        }

        public static bool HasMissingUiReferences(MoaiGolfHud hud)
        {
            return hud != null && hud.HasMissingUiReferences();
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var eventSystemObject = new GameObject(EventSystemName);
                eventSystemObject.AddComponent<EventSystem>();
                eventSystemObject.AddComponent<InputSystemUIInputModule>();
                return;
            }

            MigrateStandaloneInputModule(eventSystem.gameObject);
        }

        private static void MigrateStandaloneInputModule(GameObject eventSystemObject)
        {
            var legacyModule = eventSystemObject.GetComponent<StandaloneInputModule>();
            if (legacyModule == null)
            {
                return;
            }

            Object.DestroyImmediate(legacyModule);
            if (eventSystemObject.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystemObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static GameObject EnsureCanvasRoot(Transform hudTransform)
        {
            var existing = hudTransform.Find(HudCanvasName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var canvasObject = new GameObject(HudCanvasName);
            canvasObject.transform.SetParent(hudTransform, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(
                MoaiGolfWorldSettings.ViewportWidthPixels,
                MoaiGolfWorldSettings.ViewportHeightPixels
            );
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvasObject;
        }

        private static GameObject EnsureChild(GameObject parent, string childName)
        {
            var existing = parent.transform.Find(childName);
            if (existing != null)
            {
                if (existing is RectTransform)
                {
                    return existing.gameObject;
                }

                Object.DestroyImmediate(existing.gameObject);
            }

            var child = new GameObject(childName, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            var rect = child.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return child;
        }

        private static RectTransform EnsureUiRectChild(GameObject parent, string childName)
        {
            var childObject = EnsureChild(parent, childName);
            return childObject.GetComponent<RectTransform>();
        }

        private static void ConfigureGameplayChrome(GameObject root)
        {
            var optionsButton = EnsureButton(
                root,
                "OptionsButton",
                "オプション",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-148f, -40f),
                new Vector2(132f, 48f)
            );
            EnsureButton(
                root,
                "ResetButton",
                "リセット",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-16f, -40f),
                new Vector2(112f, 48f)
            );
            optionsButton.transform.SetSiblingIndex(0);
        }

        private static void ConfigureOptionsDialog(GameObject root)
        {
            CreateDimmer(root, "Dimmer");
            var panel = CreatePanel(root, "Panel", new Vector2(360f, 378f));
            CreateLabel(panel, "Title", "オプション", 30, FontStyle.Bold, new Vector2(0f, 142f), new Vector2(320f, 44f));

            var continueButton = CreateDialogButton(panel, "ContinueButton", "続ける", new Vector2(0f, 72f));
            var retrySameButton = CreateDialogButton(panel, "RetrySameButton", "そのままリトライ", new Vector2(0f, 10f));
            var retryRerollButton = CreateDialogButton(panel, "RetryRerollButton", "条件を変えてリトライ", new Vector2(0f, -52f));

            var bgmSlider = CreateVolumeSlider(panel, "BgmVolume", "BGM音量", new Vector2(0f, -118f));
            CreateVolumeSlider(panel, "SeVolume", "SE音量", new Vector2(0f, -154f));

            continueButton.transform.SetSiblingIndex(1);
            retrySameButton.transform.SetSiblingIndex(2);
            retryRerollButton.transform.SetSiblingIndex(3);
            bgmSlider.transform.SetSiblingIndex(4);
        }

        private static void ConfigureResultOverlay(GameObject root)
        {
            CreateDimmer(root, "Dimmer");
            var imageObject = CreateImage(root, "ResultImage", new Vector2(0f, 40f), new Vector2(720f, 720f));
            imageObject.GetComponent<Image>().preserveAspect = true;

            var fallbackLabel = CreateLabel(
                imageObject,
                "FallbackLabel",
                "SUCCESS!!",
                72,
                FontStyle.Bold,
                Vector2.zero,
                new Vector2(680f, 180f)
            );
            fallbackLabel.gameObject.SetActive(false);

            EnsureButton(
                root,
                "OptionsButton",
                "オプション",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(24f, -24f),
                new Vector2(132f, 48f),
                TextAnchor.UpperLeft
            );

            var retryRerollButton = EnsureButton(
                root,
                "RetryRerollButton",
                "条件を変えてリトライ",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(-127f, 24f),
                new Vector2(240f, 54f),
                TextAnchor.LowerCenter
            );
            EnsureButton(
                root,
                "RetrySameButton",
                "そのままリトライ",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(127f, 24f),
                new Vector2(240f, 54f),
                TextAnchor.LowerCenter
            );
            retryRerollButton.transform.SetSiblingIndex(2);
        }

        private static void WireHudReferences(
            MoaiGolfHud hud,
            GameObject gameplayChrome,
            GameObject optionsDialog,
            GameObject resultOverlay
        )
        {
            var serialized = new SerializedObject(hud);
            serialized.FindProperty("gameplayChromeRoot").objectReferenceValue = gameplayChrome;
            serialized.FindProperty("optionsDialogRoot").objectReferenceValue = optionsDialog;
            serialized.FindProperty("resultOverlayRoot").objectReferenceValue = resultOverlay;

            serialized.FindProperty("gameplayOptionsButton").objectReferenceValue =
                gameplayChrome.transform.Find("OptionsButton")?.GetComponent<Button>();
            serialized.FindProperty("gameplayResetButton").objectReferenceValue =
                gameplayChrome.transform.Find("ResetButton")?.GetComponent<Button>();

            var optionsPanel = optionsDialog.transform.Find("Panel");
            serialized.FindProperty("optionsContinueButton").objectReferenceValue =
                optionsPanel?.Find("ContinueButton")?.GetComponent<Button>();
            serialized.FindProperty("optionsRetrySameButton").objectReferenceValue =
                optionsPanel?.Find("RetrySameButton")?.GetComponent<Button>();
            serialized.FindProperty("optionsRetryRerollButton").objectReferenceValue =
                optionsPanel?.Find("RetryRerollButton")?.GetComponent<Button>();

            var bgmRoot = optionsPanel?.Find("BgmVolume");
            serialized.FindProperty("bgmVolumeSlider").objectReferenceValue = bgmRoot?.GetComponentInChildren<Slider>();
            serialized.FindProperty("bgmVolumeLabel").objectReferenceValue = bgmRoot?.Find("ValueLabel")?.GetComponent<Text>();

            var seRoot = optionsPanel?.Find("SeVolume");
            serialized.FindProperty("seVolumeSlider").objectReferenceValue = seRoot?.GetComponentInChildren<Slider>();
            serialized.FindProperty("seVolumeLabel").objectReferenceValue = seRoot?.Find("ValueLabel")?.GetComponent<Text>();

            var resultImage = resultOverlay.transform.Find("ResultImage")?.GetComponent<Image>();
            serialized.FindProperty("resultImage").objectReferenceValue = resultImage;
            serialized.FindProperty("resultFallbackLabel").objectReferenceValue =
                resultOverlay.transform.Find("ResultImage/FallbackLabel")?.GetComponent<Text>();
            serialized.FindProperty("resultOptionsButton").objectReferenceValue =
                resultOverlay.transform.Find("OptionsButton")?.GetComponent<Button>();
            serialized.FindProperty("resultRetrySameButton").objectReferenceValue =
                resultOverlay.transform.Find("RetrySameButton")?.GetComponent<Button>();
            serialized.FindProperty("resultRetryRerollButton").objectReferenceValue =
                resultOverlay.transform.Find("RetryRerollButton")?.GetComponent<Button>();

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateDimmer(GameObject parent, string objectName)
        {
            var dimmer = EnsureChild(parent, objectName);
            var image = dimmer.GetComponent<Image>() ?? dimmer.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.45f);
            image.raycastTarget = true;
            return dimmer;
        }

        private static GameObject CreatePanel(GameObject parent, string objectName, Vector2 size)
        {
            var panel = EnsureChild(parent, objectName);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;

            var image = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
            image.color = new Color(0.12f, 0.14f, 0.18f, 0.96f);
            image.raycastTarget = true;
            return panel;
        }

        private static GameObject CreateImage(GameObject parent, string objectName, Vector2 anchoredPosition, Vector2 size)
        {
            var imageObject = EnsureChild(parent, objectName);
            var rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            var image = imageObject.GetComponent<Image>() ?? imageObject.AddComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            return imageObject;
        }

        private static Text CreateLabel(
            GameObject parent,
            string objectName,
            string text,
            int fontSize,
            FontStyle fontStyle,
            Vector2 anchoredPosition,
            Vector2 size
        )
        {
            var labelObject = EnsureChild(parent, objectName);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var label = labelObject.GetComponent<Text>() ?? labelObject.AddComponent<Text>();
            label.text = text;
            label.font = GetDefaultFont();
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            return label;
        }

        private static Button EnsureButton(
            GameObject parent,
            string objectName,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 size,
            TextAnchor alignment = TextAnchor.MiddleCenter
        )
        {
            var buttonObject = EnsureChild(parent, objectName);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = alignment == TextAnchor.UpperLeft ? new Vector2(0f, 1f) : new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var image = buttonObject.GetComponent<Image>() ?? buttonObject.AddComponent<Image>();
            image.color = new Color(0.2f, 0.24f, 0.3f, 0.95f);

            var button = buttonObject.GetComponent<Button>() ?? buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var labelRect = EnsureUiRectChild(buttonObject, "Label");
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var labelText = labelRect.GetComponent<Text>() ?? labelRect.gameObject.AddComponent<Text>();
            labelText.text = label;
            labelText.font = GetDefaultFont();
            labelText.fontSize = alignment == TextAnchor.UpperLeft ? 18 : 18;
            labelText.alignment = alignment;
            labelText.color = Color.white;
            labelText.raycastTarget = false;
            return button;
        }

        private static Button CreateDialogButton(GameObject parent, string objectName, string label, Vector2 anchoredPosition)
        {
            return EnsureButton(
                parent,
                objectName,
                label,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                anchoredPosition,
                new Vector2(260f, 48f)
            );
        }

        private static GameObject CreateVolumeSlider(GameObject parent, string objectName, string label, Vector2 anchoredPosition)
        {
            var root = EnsureChild(parent, objectName);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(320f, 28f);
            rootRect.anchoredPosition = anchoredPosition;

            CreateLabel(root, "NameLabel", label, 16, FontStyle.Normal, new Vector2(-114f, 0f), new Vector2(92f, 28f))
                .alignment = TextAnchor.MiddleLeft;

            var sliderObject = EnsureChild(root, "Slider");
            var sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.sizeDelta = new Vector2(168f, 24f);
            sliderRect.anchoredPosition = new Vector2(12f, 0f);

            var background = EnsureChild(sliderObject, "Background");
            var backgroundImage = background.GetComponent<Image>() ?? background.AddComponent<Image>();
            backgroundImage.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            Stretch(background.GetComponent<RectTransform>());

            var fillArea = EnsureChild(sliderObject, "Fill Area");
            Stretch(fillArea.GetComponent<RectTransform>());
            var fill = EnsureChild(fillArea, "Fill");
            var fillImage = fill.GetComponent<Image>() ?? fill.AddComponent<Image>();
            fillImage.color = new Color(0.92f, 0.22f, 0.18f, 1f);
            Stretch(fill.GetComponent<RectTransform>());

            var handleSlideArea = EnsureChild(sliderObject, "Handle Slide Area");
            Stretch(handleSlideArea.GetComponent<RectTransform>());
            var handle = EnsureChild(handleSlideArea, "Handle");
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(18f, 18f);
            var handleImage = handle.GetComponent<Image>() ?? handle.AddComponent<Image>();
            handleImage.color = Color.white;

            var slider = sliderObject.GetComponent<Slider>() ?? sliderObject.AddComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.7f;

            var valueLabel = CreateLabel(root, "ValueLabel", "70%", 16, FontStyle.Normal, new Vector2(132f, 0f), new Vector2(48f, 28f));
            valueLabel.alignment = TextAnchor.MiddleRight;
            return root;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Font GetDefaultFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
#endif
