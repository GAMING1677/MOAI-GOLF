using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MoaiGolf
{
    public sealed class MoaiGolfHud : MonoBehaviour
    {
        public const string HudCanvasObjectName = "MoaiGolfHudCanvas";

        [SerializeField] private MoaiGolfGameController gameController;
        [SerializeField] private MoaiGolfRunState runState;
        [SerializeField] private MoaiGolfStageView stageView;
        [SerializeField] private MoaiGolfCameraController cameraController;
        [SerializeField] private MoaiGolfLaunchAnimator launchAnimator;
        [SerializeField] private MoaiGolfBgmController bgmController;
        [SerializeField] private MoaiGolfSeController seController;

        [Header("UI Roots")]
        [SerializeField] private GameObject gameplayChromeRoot;
        [SerializeField] private GameObject optionsDialogRoot;
        [SerializeField] private GameObject resultOverlayRoot;

        [Header("Gameplay Chrome")]
        [SerializeField] private Button gameplayOptionsButton;
        [SerializeField] private Button gameplayResetButton;

        [Header("Options Dialog")]
        [SerializeField] private Button optionsContinueButton;
        [SerializeField] private Button optionsRetrySameButton;
        [SerializeField] private Button optionsRetryRerollButton;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider seVolumeSlider;
        [SerializeField] private Text bgmVolumeLabel;
        [SerializeField] private Text seVolumeLabel;

        [Header("Result Overlay")]
        [SerializeField] private Image resultImage;
        [SerializeField] private Text resultFallbackLabel;
        [SerializeField] private Sprite resultSuccessSprite;
        [SerializeField] private Sprite resultFailedSprite;
        [SerializeField] private Button resultOptionsButton;
        [SerializeField] private Button resultRetrySameButton;
        [SerializeField] private Button resultRetryRerollButton;

        private bool isMenuOpen;
        private int blockWorldInputUntilFrame = -1;
        private bool uiListenersRegistered;

        public bool IsMenuOpen => isMenuOpen;
        public bool ShouldBlockWorldInput =>
            isMenuOpen
            || gameController?.Phase == MoaiGolfGamePhase.Result
            || Time.frameCount <= blockWorldInputUntilFrame
            || IsPointerOverHudChrome();

        private void Awake()
        {
            ResolveUiReferencesFromHierarchy();
            EnsureHudCanvasScale();
            EnsureResultSprites();

            if (!ValidateReferences())
            {
                return;
            }

            RegisterUiListeners();
            InitializeVolumeSliders();
            RefreshUiVisibility();
        }

        public bool HasMissingUiReferences()
        {
            return gameplayChromeRoot == null
                || optionsDialogRoot == null
                || resultOverlayRoot == null
                || gameplayOptionsButton == null
                || gameplayResetButton == null
                || optionsContinueButton == null
                || optionsRetrySameButton == null
                || optionsRetryRerollButton == null
                || bgmVolumeSlider == null
                || seVolumeSlider == null
                || resultOptionsButton == null
                || resultRetrySameButton == null
                || resultRetryRerollButton == null;
        }

        public void ResolveUiReferencesFromHierarchy()
        {
            var canvas = transform.Find(HudCanvasObjectName);
            if (canvas == null)
            {
                return;
            }

            gameplayChromeRoot ??= FindUiObject(canvas, "GameplayChrome");
            optionsDialogRoot ??= FindUiObject(canvas, "OptionsDialog");
            resultOverlayRoot ??= FindUiObject(canvas, "ResultOverlay");

            gameplayOptionsButton ??= FindUiButton(canvas, "GameplayChrome/OptionsButton");
            gameplayResetButton ??= FindUiButton(canvas, "GameplayChrome/ResetButton");

            optionsContinueButton ??= FindUiButton(canvas, "OptionsDialog/Panel/ContinueButton");
            optionsRetrySameButton ??= FindUiButton(canvas, "OptionsDialog/Panel/RetrySameButton");
            optionsRetryRerollButton ??= FindUiButton(canvas, "OptionsDialog/Panel/RetryRerollButton");

            var bgmRoot = canvas.Find("OptionsDialog/Panel/BgmVolume");
            if (bgmRoot != null)
            {
                bgmVolumeSlider ??= bgmRoot.GetComponentInChildren<Slider>(true);
                bgmVolumeLabel ??= bgmRoot.Find("ValueLabel")?.GetComponent<Text>();
            }

            var seRoot = canvas.Find("OptionsDialog/Panel/SeVolume");
            if (seRoot != null)
            {
                seVolumeSlider ??= seRoot.GetComponentInChildren<Slider>(true);
                seVolumeLabel ??= seRoot.Find("ValueLabel")?.GetComponent<Text>();
            }

            resultImage ??= canvas.Find("ResultOverlay/ResultImage")?.GetComponent<Image>();
            resultFallbackLabel ??= canvas.Find("ResultOverlay/ResultImage/FallbackLabel")?.GetComponent<Text>();
            resultOptionsButton ??= FindUiButton(canvas, "ResultOverlay/OptionsButton");
            resultRetrySameButton ??= FindUiButton(canvas, "ResultOverlay/BottomButtonRow/RetrySameButton")
                ?? FindUiButton(canvas, "ResultOverlay/RetrySameButton");
            resultRetryRerollButton ??= FindUiButton(canvas, "ResultOverlay/BottomButtonRow/RetryRerollButton")
                ?? FindUiButton(canvas, "ResultOverlay/RetryRerollButton");
        }

        private static GameObject FindUiObject(Transform root, string path)
        {
            return root.Find(path)?.gameObject;
        }

        private static Button FindUiButton(Transform root, string path)
        {
            return root.Find(path)?.GetComponent<Button>();
        }

        public void RefreshSerializedReferencesForEditor(
            MoaiGolfGameController controller,
            MoaiGolfRunState state,
            MoaiGolfStageView view,
            MoaiGolfCameraController cameraControl,
            MoaiGolfLaunchAnimator animator,
            MoaiGolfBgmController bgm,
            MoaiGolfSeController se
        )
        {
            if (controller != null)
            {
                gameController = controller;
            }

            if (state != null)
            {
                runState = state;
            }

            if (view != null)
            {
                stageView = view;
            }

            if (cameraControl != null)
            {
                cameraController = cameraControl;
            }

            if (animator != null)
            {
                launchAnimator = animator;
            }

            if (bgm != null)
            {
                bgmController = bgm;
            }

            if (se != null)
            {
                seController = se;
            }
        }

        public bool ValidateReferences()
        {
            var isValid = true;
            isValid &= ValidateReference(gameController, nameof(gameController));
            isValid &= ValidateReference(runState, nameof(runState));
            isValid &= ValidateReference(stageView, nameof(stageView));
            isValid &= ValidateReference(cameraController, nameof(cameraController));
            isValid &= ValidateReference(launchAnimator, nameof(launchAnimator));
            isValid &= ValidateReference(bgmController, nameof(bgmController));
            isValid &= ValidateReference(seController, nameof(seController));
            isValid &= ValidateReference(gameplayChromeRoot, nameof(gameplayChromeRoot));
            isValid &= ValidateReference(optionsDialogRoot, nameof(optionsDialogRoot));
            isValid &= ValidateReference(resultOverlayRoot, nameof(resultOverlayRoot));
            isValid &= ValidateReference(gameplayOptionsButton, nameof(gameplayOptionsButton));
            isValid &= ValidateReference(gameplayResetButton, nameof(gameplayResetButton));
            isValid &= ValidateReference(optionsContinueButton, nameof(optionsContinueButton));
            isValid &= ValidateReference(optionsRetrySameButton, nameof(optionsRetrySameButton));
            isValid &= ValidateReference(optionsRetryRerollButton, nameof(optionsRetryRerollButton));
            isValid &= ValidateReference(bgmVolumeSlider, nameof(bgmVolumeSlider));
            isValid &= ValidateReference(seVolumeSlider, nameof(seVolumeSlider));
            isValid &= ValidateReference(resultOptionsButton, nameof(resultOptionsButton));
            isValid &= ValidateReference(resultRetrySameButton, nameof(resultRetrySameButton));
            isValid &= ValidateReference(resultRetryRerollButton, nameof(resultRetryRerollButton));
            return isValid;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    ToggleOptionsMenu();
                }

                if (keyboard.rKey.wasPressedThisFrame)
                {
                    Retry();
                }
            }

            ApplyBgmMenuDucking();
        }

        private void LateUpdate()
        {
            ApplyBgmMenuDucking();
            RefreshUiVisibility();
            RefreshVolumeLabels();
        }

        private void RegisterUiListeners()
        {
            if (uiListenersRegistered)
            {
                return;
            }

            BindButton(gameplayOptionsButton, OpenOptionsMenu);
            BindButton(gameplayResetButton, Retry);
            BindButton(optionsContinueButton, CloseOptionsMenu);
            BindButton(optionsRetrySameButton, Retry);
            BindButton(optionsRetryRerollButton, RerollAndRetry);
            BindButton(resultOptionsButton, OpenOptionsMenu);
            BindButton(resultRetrySameButton, Retry);
            BindButton(resultRetryRerollButton, RerollAndRetry);

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.AddListener(value => bgmController?.SetVolume(value));
            }

            if (seVolumeSlider != null)
            {
                seVolumeSlider.onValueChanged.AddListener(value => seController?.SetVolume(value));
            }

            uiListenersRegistered = true;
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void InitializeVolumeSliders()
        {
            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.SetValueWithoutNotify(
                    bgmController != null ? bgmController.Volume : MoaiGolfBgmController.DefaultVolume
                );
            }

            if (seVolumeSlider != null)
            {
                seVolumeSlider.SetValueWithoutNotify(
                    seController != null ? seController.Volume : MoaiGolfSeController.DefaultVolume
                );
            }

            RefreshVolumeLabels();
        }

        private void RefreshVolumeLabels()
        {
            if (bgmVolumeLabel != null && bgmVolumeSlider != null)
            {
                bgmVolumeLabel.text = $"{Mathf.RoundToInt(bgmVolumeSlider.value * 100f)}%";
            }

            if (seVolumeLabel != null && seVolumeSlider != null)
            {
                seVolumeLabel.text = $"{Mathf.RoundToInt(seVolumeSlider.value * 100f)}%";
            }
        }

        private void RefreshUiVisibility()
        {
            if (gameController == null)
            {
                return;
            }

            var isResultPhase = gameController.Phase == MoaiGolfGamePhase.Result;
            if (gameplayChromeRoot != null)
            {
                gameplayChromeRoot.SetActive(!isResultPhase);
            }

            if (resultOverlayRoot != null)
            {
                resultOverlayRoot.SetActive(isResultPhase);
            }

            if (optionsDialogRoot != null)
            {
                optionsDialogRoot.SetActive(isMenuOpen);
                if (isMenuOpen)
                {
                    optionsDialogRoot.transform.SetAsLastSibling();
                }
            }

            if (isResultPhase)
            {
                RefreshResultPresentation();
            }
        }

        private void RefreshResultPresentation()
        {
            EnsureResultSprites();

            var succeeded = gameController.LastResultSucceeded == true;
            var sprite = succeeded ? resultSuccessSprite : resultFailedSprite;
            if (resultImage != null)
            {
                resultImage.sprite = sprite;
                resultImage.enabled = sprite;
                resultImage.preserveAspect = true;
                resultImage.type = Image.Type.Simple;
            }

            if (resultFallbackLabel != null)
            {
                if (sprite)
                {
                    resultFallbackLabel.gameObject.SetActive(false);
                }
                else
                {
                    resultFallbackLabel.text = succeeded ? "SUCCESS!!" : "FAILED...";
                    resultFallbackLabel.gameObject.SetActive(true);
                }
            }
        }

        private void EnsureHudCanvasScale()
        {
            var canvas = transform.Find(HudCanvasObjectName) as RectTransform;
            if (canvas == null)
            {
                return;
            }

            if (canvas.localScale == Vector3.zero)
            {
                canvas.localScale = Vector3.one;
            }

            canvas.anchorMin = Vector2.zero;
            canvas.anchorMax = Vector2.one;
            canvas.offsetMin = Vector2.zero;
            canvas.offsetMax = Vector2.zero;
            canvas.pivot = new Vector2(0.5f, 0.5f);
        }

        private void EnsureResultSprites()
        {
            if (!resultSuccessSprite)
            {
                resultSuccessSprite = LoadResultSprite("MoaiGolf/UI/result_success");
            }

            if (!resultFailedSprite)
            {
                resultFailedSprite = LoadResultSprite("MoaiGolf/UI/result_failed");
            }
        }

        private static Sprite LoadResultSprite(string resourcePath)
        {
            var sprites = Resources.LoadAll<Sprite>(resourcePath);
            if (sprites.Length > 0)
            {
                return sprites[0];
            }

            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite)
            {
                return sprite;
            }

            var texture = Resources.Load<Texture2D>(resourcePath);
            if (!texture)
            {
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }

        private void OpenOptionsMenu()
        {
            isMenuOpen = true;
            BlockWorldInputBriefly();
            RefreshUiVisibility();
        }

        private void CloseOptionsMenu()
        {
            isMenuOpen = false;
            BlockWorldInputBriefly();
            RefreshUiVisibility();
        }

        private void ToggleOptionsMenu()
        {
            if (gameController != null && gameController.Phase == MoaiGolfGamePhase.Result && isMenuOpen)
            {
                CloseOptionsMenu();
                return;
            }

            if (isMenuOpen)
            {
                CloseOptionsMenu();
            }
            else
            {
                OpenOptionsMenu();
            }
        }

        private bool IsPointerOverHudChrome()
        {
            if (gameController == null || isMenuOpen)
            {
                return false;
            }

            if (gameController.Phase == MoaiGolfGamePhase.Result)
            {
                return IsPointerOverInteractiveUi(resultOptionsButton, resultRetrySameButton, resultRetryRerollButton);
            }

            return IsPointerOverInteractiveUi(gameplayOptionsButton, gameplayResetButton);
        }

        private static bool IsPointerOverInteractiveUi(params Button[] buttons)
        {
            var pointer = GetPointerScreenPosition();
            if (!pointer.HasValue)
            {
                return false;
            }

            var screenPoint = pointer.Value;
            for (var index = 0; index < buttons.Length; index++)
            {
                var button = buttons[index];
                if (button == null || !button.isActiveAndEnabled || !button.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!(button.transform is RectTransform rect))
                {
                    continue;
                }

                if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null))
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2? GetPointerScreenPosition()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                return mouse.position.ReadValue();
            }

            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                return touch.primaryTouch.position.ReadValue();
            }

            return null;
        }

        private void Retry()
        {
            if (!ResolveDependencies() || !CanRestartRunFromHud())
            {
                return;
            }

            runState.RetryCurrentRun();
            RebuildAfterRetry();
        }

        private void RerollAndRetry()
        {
            if (!ResolveDependencies() || !CanRestartRunFromHud())
            {
                return;
            }

            runState.RerollAndRetry();
            RebuildAfterRetry();
        }

        private bool CanRestartRunFromHud()
        {
            return gameController != null;
        }

        private bool ResolveDependencies()
        {
            return ValidateReference(gameController, nameof(gameController))
                && ValidateReference(runState, nameof(runState))
                && ValidateReference(stageView, nameof(stageView));
        }

        private void ApplyBgmMenuDucking()
        {
            bgmController?.SetMenuDucked(isMenuOpen);
        }

        private void RebuildAfterRetry()
        {
            isMenuOpen = false;
            BlockWorldInputBriefly();
            ApplyBgmMenuDucking();
            launchAnimator?.CancelLaunchSequence();
            gameController.ResetForRetry();
            stageView.Build(runState);
            cameraController?.ResetToInitial();
            RefreshUiVisibility();
        }

        private void BlockWorldInputBriefly()
        {
            blockWorldInputUntilFrame = Time.frameCount + 1;
        }

        private bool ValidateReference(Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(MoaiGolfHud)} missing serialized reference: {fieldName}.", this);
            return false;
        }
    }
}
