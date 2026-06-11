#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MoaiGolf
{
    public static class MoaiGolfTitleSceneSetupUtility
    {
        private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";
        private const string StageScenePath = "Assets/Scenes/MoaiGolfStage.unity";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string EventSystemName = "MoaiGolfEventSystem";
        private const string BgmClipPath = "Assets/Audio/BGM/BGM_main.ogg";
        private const string TitleLogoTexturePath = "Assets/Textures/MOAI_GOLF_LOGO.png";

        private const float TopRightMargin = 20f;
        private const float BottomButtonMargin = 48f;
        private const int ButtonFontSize = 22;
        private static readonly Color ButtonColor = new(0.2f, 0.24f, 0.3f, 0.95f);
        private static readonly Vector2 TopRightButtonSize = new(132f, 48f);
        private static readonly Vector2 StartButtonSize = new(320f, 56f);

        [MenuItem("Moai Golf/Setup Title Scene")]
        public static void SetupTitleSceneMenu()
        {
            EnsureStageSceneExists();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            BuildTitleSceneContents();
            EditorSceneManager.SaveScene(scene, TitleScenePath);
            ConfigureBuildSettings();
            Debug.Log("Moai Golf title scene setup complete.");
        }

        [MenuItem("Moai Golf/Ensure MoaiGolfStage Scene")]
        public static void EnsureStageSceneMenu()
        {
            EnsureStageSceneExists();
            ConfigureBuildSettings();
            Debug.Log("MoaiGolfStage scene is ready.");
        }

        public static void EnsureStageSceneExists()
        {
            if (!File.Exists(StageScenePath))
            {
                if (!File.Exists(SampleScenePath))
                {
                    Debug.LogError($"Sample scene not found: {SampleScenePath}");
                    return;
                }

                File.Copy(SampleScenePath, StageScenePath);
                AssetDatabase.ImportAsset(StageScenePath);
                Debug.Log($"Created {StageScenePath} from SampleScene.");
            }

            RepairStageSceneReferences(StageScenePath);
        }

        public static void RepairStageSceneReferences(string scenePath)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var stageView = Object.FindAnyObjectByType<MoaiGolfStageView>();
            if (stageView == null)
            {
                Debug.LogError($"MoaiGolfStageView not found in {scenePath}.");
                return;
            }

            stageView.RefreshSerializedSceneReferencesForEditor();
            var pool = stageView.GetComponentsInChildren<MoaiGolfStageElement>(true);
            var targetCount = 0;
            foreach (var element in pool)
            {
                if (element != null && element.Kind == MoaiGolfStageElementKind.TargetMoai)
                {
                    targetCount++;
                }
            }

            if (targetCount < MoaiGolfStageView.SceneTargetMoaiPoolCount)
            {
                var prefabSet = AssetDatabase.LoadAssetAtPath<MoaiGolfStagePrefabSet>("Assets/Resources/MoaiGolfStagePrefabSet.asset");
                MoaiGolfStagePrefabUtility.PlaceTargetMoaiPoolInScene(stageView, prefabSet);
            }

            var bootstrap = Object.FindAnyObjectByType<MoaiGolfBootstrap>();
            bootstrap?.RefreshSerializedReferencesForEditor(
                Camera.main,
                stageView,
                Object.FindAnyObjectByType<MoaiGolfBgmController>()
            );
            EditorUtility.SetDirty(stageView);
            if (bootstrap != null)
            {
                EditorUtility.SetDirty(bootstrap);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void ConfigureBuildSettings()
        {
            EnsureStageSceneExists();
            var scenes = new[]
            {
                new EditorBuildSettingsScene(TitleScenePath, true),
                new EditorBuildSettingsScene(StageScenePath, true),
            };
            EditorBuildSettings.scenes = scenes;
        }

        private static void BuildTitleSceneContents()
        {
            RemoveExisting("MoaiGolfTitleRoot");
            RemoveExisting("MoaiGolfBgm");
            RemoveExisting(EventSystemName);

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = MoaiGolfWorldSettings.CameraOrthographicSize;
                mainCamera.transform.position = new Vector3(
                    MoaiGolfWorldSettings.CameraCenterX,
                    MoaiGolfWorldSettings.CameraCenterY,
                    MoaiGolfWorldSettings.CameraZ
                );
                mainCamera.backgroundColor = new Color(0.19f, 0.3f, 0.47f);
            }

            var titleRoot = new GameObject("MoaiGolfTitleRoot");
            var titleView = titleRoot.AddComponent<MoaiGolfTitleView>();

            var bgmObject = new GameObject("MoaiGolfBgm");
            bgmObject.AddComponent<AudioSource>();
            var bgmController = bgmObject.AddComponent<MoaiGolfBgmController>();
            var bgmSerialized = new SerializedObject(bgmController);
            bgmSerialized.FindProperty("bgmClip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(BgmClipPath);
            bgmSerialized.ApplyModifiedPropertiesWithoutUndo();

            var landingCenter = new Vector2(MoaiGolfWorldSettings.CameraCenterX, 3.8f);
            var logoDrop = CreateLogoDrop(titleRoot.transform, landingCenter);
            CreateLandingGround(landingCenter);

            EnsureEventSystem();
            var canvasRoot = EnsureTitleCanvas(titleRoot.transform);
            var mainMenu = EnsureChild(canvasRoot, "MainMenu");
            var optionsDialog = EnsureChild(canvasRoot, "OptionsDialog");
            ConfigureMainMenu(mainMenu);
            ConfigureOptionsDialog(optionsDialog);
            optionsDialog.SetActive(false);

            WireTitleReferences(
                titleView,
                mainCamera,
                logoDrop,
                bgmController,
                mainMenu,
                optionsDialog
            );

            titleView.ResolveUiReferencesFromHierarchy();
            EditorUtility.SetDirty(titleView);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static MoaiGolfTitleLogoDrop CreateLogoDrop(Transform parent, Vector2 landingCenter)
        {
            var logoObject = new GameObject("TitleLogo");
            logoObject.transform.SetParent(parent, false);
            logoObject.transform.position = landingCenter + Vector2.up * 12f;

            var sprite = MoaiGolfSpriteCatalog.GetPersistedTitleLogo();
            var renderer = logoObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 10;

            var physicsMaterial = new PhysicsMaterial2D("TitleLogoBounce")
            {
                bounciness = 0.88f,
                friction = 0.15f
            };

            var body = logoObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 1f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            ApplyLogoCompositeColliders(logoObject, sprite, physicsMaterial);

            var logoDrop = logoObject.AddComponent<MoaiGolfTitleLogoDrop>();
            var serialized = new SerializedObject(logoDrop);
            serialized.FindProperty("body").objectReferenceValue = body;
            serialized.FindProperty("landingCenter").vector2Value = landingCenter;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return logoDrop;
        }

        private static void CreateLandingGround(Vector2 landingCenter)
        {
            var groundObject = new GameObject("TitleLandingGround");
            groundObject.transform.position = new Vector3(landingCenter.x, landingCenter.y - 1.35f, 0f);

            var collider = groundObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(MoaiGolfWorldSettings.ViewportWorldWidth * 0.9f, 0.35f);
            collider.offset = Vector2.zero;
            collider.sharedMaterial = new PhysicsMaterial2D("TitleGroundBounce")
            {
                bounciness = 0.88f,
                friction = 0.15f
            };
        }

        private static void ApplyLogoCompositeColliders(GameObject logoObject, Sprite displaySprite, PhysicsMaterial2D material)
        {
            foreach (var legacyCollider in logoObject.GetComponents<Collider2D>())
            {
                Object.DestroyImmediate(legacyCollider);
            }

            for (var i = logoObject.transform.childCount - 1; i >= 0; i--)
            {
                var child = logoObject.transform.GetChild(i);
                if (child.name.StartsWith("Collider_"))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }

            var composite = logoObject.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                composite = logoObject.AddComponent<CompositeCollider2D>();
            }

            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
            composite.sharedMaterial = material;

            var textureWorldSize = displaySprite != null
                ? new Vector2(displaySprite.rect.width, displaySprite.rect.height) / displaySprite.pixelsPerUnit
                : new Vector2(10f, 10f);
            var halfTextureWorld = textureWorldSize * 0.5f;

            var spritePieces = AssetDatabase.LoadAllAssetsAtPath(TitleLogoTexturePath)
                .OfType<Sprite>()
                .Where(piece => piece.name.StartsWith("MOAI_GOLF_LOGO_"))
                .OrderBy(piece => piece.name);

            foreach (var pieceSprite in spritePieces)
            {
                var child = new GameObject($"Collider_{pieceSprite.name}");
                child.transform.SetParent(logoObject.transform, false);

                var rect = pieceSprite.rect;
                var center = new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f) / pieceSprite.pixelsPerUnit;
                child.transform.localPosition = center - halfTextureWorld;

                var collider = child.AddComponent<PolygonCollider2D>();
                collider.compositeOperation = Collider2D.CompositeOperation.Merge;
                collider.sharedMaterial = material;
                collider.CreateFromSprite(pieceSprite, 0.25f, (byte)16, true, true);
            }
        }

        private static void WireTitleReferences(
            MoaiGolfTitleView titleView,
            Camera camera,
            MoaiGolfTitleLogoDrop logoDrop,
            MoaiGolfBgmController bgmController,
            GameObject mainMenu,
            GameObject optionsDialog
        )
        {
            var serialized = new SerializedObject(titleView);
            serialized.FindProperty("mainCamera").objectReferenceValue = camera;
            serialized.FindProperty("logoDrop").objectReferenceValue = logoDrop;
            serialized.FindProperty("bgmController").objectReferenceValue = bgmController;
            serialized.FindProperty("startButton").objectReferenceValue =
                mainMenu.transform.Find("StartButton")?.GetComponent<Button>();
            serialized.FindProperty("optionsButton").objectReferenceValue =
                mainMenu.transform.Find("OptionsButton")?.GetComponent<Button>();
            serialized.FindProperty("optionsDialogRoot").objectReferenceValue = optionsDialog;

            var optionsPanel = optionsDialog.transform.Find("Panel");
            serialized.FindProperty("optionsCloseButton").objectReferenceValue =
                optionsPanel?.Find("CloseButton")?.GetComponent<Button>();

            var bgmRoot = optionsPanel?.Find("BgmVolume");
            serialized.FindProperty("bgmVolumeSlider").objectReferenceValue = bgmRoot?.GetComponentInChildren<Slider>();
            serialized.FindProperty("bgmVolumeLabel").objectReferenceValue = bgmRoot?.Find("ValueLabel")?.GetComponent<Text>();

            var seRoot = optionsPanel?.Find("SeVolume");
            serialized.FindProperty("seVolumeSlider").objectReferenceValue = seRoot?.GetComponentInChildren<Slider>();
            serialized.FindProperty("seVolumeLabel").objectReferenceValue = seRoot?.Find("ValueLabel")?.GetComponent<Text>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureMainMenu(GameObject root)
        {
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            EnsureButton(
                root,
                "StartButton",
                "モアイ開始",
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, BottomButtonMargin),
                StartButtonSize,
                TextAnchor.LowerCenter
            );

            EnsureButton(
                root,
                "OptionsButton",
                "オプション",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-TopRightMargin, -TopRightMargin),
                TopRightButtonSize,
                TextAnchor.UpperRight
            );
        }

        private static void ConfigureOptionsDialog(GameObject root)
        {
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            CreateDimmer(root, "Dimmer");
            var panel = CreatePanel(root, "Panel", new Vector2(360f, 300f));
            CreateLabel(panel, "Title", "オプション", 30, FontStyle.Bold, new Vector2(0f, 108f), new Vector2(320f, 44f));
            CreateVolumeSlider(panel, "BgmVolume", "BGM音量", new Vector2(0f, 34f));
            CreateVolumeSlider(panel, "SeVolume", "SE音量", new Vector2(0f, -2f));
            CreateDialogButton(panel, "CloseButton", "閉じる", new Vector2(0f, -78f));
        }

        private static GameObject EnsureTitleCanvas(Transform parent)
        {
            var existing = parent.Find(MoaiGolfTitleView.TitleCanvasObjectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var canvasObject = new GameObject(MoaiGolfTitleView.TitleCanvasObjectName, typeof(RectTransform));
            canvasObject.transform.SetParent(parent, false);
            var rect = canvasObject.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

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

            var legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                Object.DestroyImmediate(legacyModule);
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static GameObject EnsureChild(GameObject parent, string childName)
        {
            var child = new GameObject(childName, typeof(RectTransform));
            child.transform.SetParent(parent.transform, false);
            return child;
        }

        private static GameObject CreateDimmer(GameObject parent, string objectName)
        {
            var dimmer = EnsureChild(parent, objectName);
            var rect = dimmer.GetComponent<RectTransform>();
            Stretch(rect);
            var image = dimmer.AddComponent<Image>();
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

            var image = panel.AddComponent<Image>();
            image.color = new Color(0.12f, 0.14f, 0.18f, 0.96f);
            image.raycastTarget = true;
            return panel;
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

            var label = labelObject.AddComponent<Text>();
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
            TextAnchor alignment
        )
        {
            var buttonObject = EnsureChild(parent, objectName);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = alignment switch
            {
                TextAnchor.UpperRight => new Vector2(1f, 1f),
                TextAnchor.LowerCenter => new Vector2(0.5f, 0f),
                _ => new Vector2(0.5f, 0.5f),
            };
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            ApplyButtonVisuals(buttonObject, label);
            return buttonObject.GetComponent<Button>();
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
                new Vector2(260f, 48f),
                TextAnchor.MiddleCenter
            );
        }

        private static void ApplyButtonVisuals(GameObject buttonObject, string label)
        {
            var image = buttonObject.GetComponent<Image>() ?? buttonObject.AddComponent<Image>();
            image.color = ButtonColor;

            var button = buttonObject.GetComponent<Button>() ?? buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            var labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(buttonObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            Stretch(labelRect);

            var labelText = labelObject.AddComponent<Text>();
            labelText.text = label;
            labelText.font = GetDefaultFont();
            labelText.fontSize = ButtonFontSize;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.raycastTarget = false;
        }

        private static GameObject CreateVolumeSlider(GameObject parent, string objectName, string label, Vector2 anchoredPosition)
        {
            var root = EnsureChild(parent, objectName);
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(320f, 28f);
            rootRect.anchoredPosition = anchoredPosition;

            var nameLabel = CreateLabel(root, "NameLabel", label, 16, FontStyle.Normal, new Vector2(-114f, 0f), new Vector2(92f, 28f));
            nameLabel.alignment = TextAnchor.MiddleLeft;

            var sliderObject = EnsureChild(root, "Slider");
            var sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.sizeDelta = new Vector2(168f, 24f);
            sliderRect.anchoredPosition = new Vector2(12f, 0f);

            var background = EnsureChild(sliderObject, "Background");
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            Stretch(background.GetComponent<RectTransform>());

            var fillArea = EnsureChild(sliderObject, "Fill Area");
            Stretch(fillArea.GetComponent<RectTransform>());
            var fill = EnsureChild(fillArea, "Fill");
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.92f, 0.22f, 0.18f, 1f);
            Stretch(fill.GetComponent<RectTransform>());

            var handleSlideArea = EnsureChild(sliderObject, "Handle Slide Area");
            Stretch(handleSlideArea.GetComponent<RectTransform>());
            var handle = EnsureChild(handleSlideArea, "Handle");
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(18f, 18f);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;

            var slider = sliderObject.AddComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = MoaiGolfBgmController.DefaultVolume;

            var valueLabel = CreateLabel(root, "ValueLabel", "20%", 16, FontStyle.Normal, new Vector2(132f, 0f), new Vector2(48f, 28f));
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

        private static void RemoveExisting(string objectName)
        {
            var existing = GameObject.Find(objectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }
    }
}
#endif
