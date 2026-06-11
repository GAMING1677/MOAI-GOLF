using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MoaiGolf
{
    public sealed class MoaiGolfHud : MonoBehaviour
    {
        [SerializeField] private MoaiGolfGameController gameController;
        [SerializeField] private MoaiGolfRunState runState;
        [SerializeField] private MoaiGolfStageView stageView;
        [SerializeField] private MoaiGolfCameraController cameraController;
        [SerializeField] private MoaiGolfLaunchAnimator launchAnimator;
        [SerializeField] private MoaiGolfBgmController bgmController;
        [SerializeField] private MoaiGolfSeController seController;
        private Texture2D resultSuccessTexture;
        private Texture2D resultFailedTexture;
        private bool isMenuOpen;
        private int blockWorldInputUntilFrame = -1;
        private Rect optionsButtonRect;
        private Rect resetButtonRect;

        public bool IsMenuOpen => isMenuOpen;
        public bool ShouldBlockWorldInput =>
            isMenuOpen
            || gameController?.Phase == MoaiGolfGamePhase.Result
            || Time.frameCount <= blockWorldInputUntilFrame
            || IsPointerOverHudChrome();

        public void ConfigureDependencies(
            MoaiGolfGameController controller,
            MoaiGolfRunState state,
            MoaiGolfStageView view,
            MoaiGolfCameraController cameraControl,
            MoaiGolfLaunchAnimator animator,
            MoaiGolfBgmController bgm,
            MoaiGolfSeController se
        )
        {
            gameController = controller;
            runState = state;
            stageView = view;
            cameraController = cameraControl;
            launchAnimator = animator;
            bgmController = bgm;
            seController = se;
            ValidateReference(gameController, nameof(gameController));
            ValidateReference(runState, nameof(runState));
            ValidateReference(stageView, nameof(stageView));
            ValidateReference(cameraController, nameof(cameraController));
            ValidateReference(launchAnimator, nameof(launchAnimator));
            ValidateReference(bgmController, nameof(bgmController));
            ValidateReference(seController, nameof(seController));
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
        }

        private void OnGUI()
        {
            if (gameController == null)
            {
                return;
            }

            if (gameController.Phase == MoaiGolfGamePhase.Result)
            {
                ApplyBgmMenuDucking();
                DrawResultOverlay();
                if (isMenuOpen)
                {
                    DrawOptionsDialog();
                }

                return;
            }

            DrawGameplayChrome();

            if (isMenuOpen)
            {
                DrawOptionsDialog();
            }
        }

        private void ToggleOptionsMenu()
        {
            if (gameController != null && gameController.Phase == MoaiGolfGamePhase.Result && isMenuOpen)
            {
                isMenuOpen = false;
                BlockWorldInputBriefly();
                return;
            }

            isMenuOpen = !isMenuOpen;
            if (isMenuOpen)
            {
                BlockWorldInputBriefly();
            }
        }

        private void DrawGameplayChrome()
        {
            const float padding = 16f;
            const float buttonHeight = 48f;
            const float buttonGap = 10f;
            const float optionsWidth = 132f;
            const float resetWidth = 112f;
            var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 17 };

            resetButtonRect = new Rect(
                Screen.width - padding - resetWidth,
                padding,
                resetWidth,
                buttonHeight);
            optionsButtonRect = new Rect(
                resetButtonRect.x - buttonGap - optionsWidth,
                padding,
                optionsWidth,
                buttonHeight);

            if (GUI.Button(optionsButtonRect, "オプション", buttonStyle))
            {
                isMenuOpen = true;
                BlockWorldInputBriefly();
            }

            if (GUI.Button(resetButtonRect, "リセット", buttonStyle))
            {
                Retry();
            }
        }

        private bool IsPointerOverHudChrome()
        {
            if (gameController == null || isMenuOpen)
            {
                return false;
            }

            var pointer = GetPointerScreenPosition();
            if (!pointer.HasValue)
            {
                return false;
            }

            var screenPoint = pointer.Value;
            if (gameController.Phase == MoaiGolfGamePhase.Result)
            {
                return optionsButtonRect.Contains(screenPoint);
            }

            return optionsButtonRect.Contains(screenPoint) || resetButtonRect.Contains(screenPoint);
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

        private void DrawOptionsDialog()
        {
            const float dialogWidth = 360f;
            const float dialogHeight = 378f;
            var dialogRect = new Rect(
                (Screen.width - dialogWidth) * 0.5f,
                (Screen.height - dialogHeight) * 0.5f,
                dialogWidth,
                dialogHeight
            );

            var prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prevColor;

            GUI.Box(dialogRect, string.Empty);

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(dialogRect.x, dialogRect.y + 24f, dialogRect.width, 44f), "オプション", titleStyle);

            var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            const float buttonWidth = 260f;
            const float buttonHeight = 48f;
            const float buttonGap = 14f;
            var buttonX = dialogRect.x + (dialogRect.width - buttonWidth) * 0.5f;
            var buttonY = dialogRect.y + 88f;

            if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "続ける", buttonStyle))
            {
                isMenuOpen = false;
                BlockWorldInputBriefly();
            }

            buttonY += buttonHeight + buttonGap;
            if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "そのままリトライ", buttonStyle))
            {
                Retry();
            }

            buttonY += buttonHeight + buttonGap;
            if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "条件を変えてリトライ", buttonStyle))
            {
                RerollAndRetry();
            }

            var sliderY = buttonY + buttonHeight + 20f;
            DrawBgmVolumeSlider(dialogRect, sliderY);
            DrawSeVolumeSlider(dialogRect, sliderY + 36f);
        }

        private void DrawBgmVolumeSlider(Rect dialogRect, float sliderY)
        {
            DrawVolumeSlider(
                dialogRect,
                sliderY,
                "BGM音量",
                bgmController != null ? bgmController.Volume : MoaiGolfBgmController.DefaultVolume,
                value => bgmController?.SetVolume(value)
            );
        }

        private void DrawSeVolumeSlider(Rect dialogRect, float sliderY)
        {
            DrawVolumeSlider(
                dialogRect,
                sliderY,
                "SE音量",
                seController != null ? seController.Volume : MoaiGolfSeController.DefaultVolume,
                value => seController?.SetVolume(value)
            );
        }

        private static void DrawVolumeSlider(Rect dialogRect, float sliderY, string label, float volume, System.Action<float> setVolume)
        {
            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = Color.white }
            };
            var valueStyle = new GUIStyle(labelStyle)
            {
                alignment = TextAnchor.MiddleRight
            };

            const float labelWidth = 92f;
            const float valueWidth = 48f;
            const float sliderWidth = 168f;
            var sliderX = dialogRect.x + (dialogRect.width - labelWidth - sliderWidth - valueWidth - 16f) * 0.5f;

            GUI.Label(new Rect(sliderX, sliderY - 2f, labelWidth, 28f), label, labelStyle);
            var newVolume = GUI.HorizontalSlider(
                new Rect(sliderX + labelWidth + 8f, sliderY + 4f, sliderWidth, 24f),
                volume,
                0f,
                1f
            );
            GUI.Label(
                new Rect(sliderX + labelWidth + sliderWidth + 16f, sliderY - 2f, valueWidth, 28f),
                $"{Mathf.RoundToInt(newVolume * 100f)}%",
                valueStyle
            );

            if (setVolume != null && !Mathf.Approximately(newVolume, volume))
            {
                setVolume(newVolume);
            }
        }

        private void DrawResultOverlay()
        {
            var succeeded = gameController.LastResultSucceeded == true;
            var resultTexture = GetResultTexture(succeeded);

            var prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prevColor;

            var imageRect = CalculateResultImageRect();
            if (resultTexture != null)
            {
                GUI.DrawTexture(imageRect, resultTexture, ScaleMode.ScaleToFit, true);
            }
            else
            {
                DrawResultFallbackText(imageRect, succeeded);
            }

            var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            const float buttonWidth = 240f;
            const float buttonHeight = 54f;
            const float buttonGap = 14f;
            const float screenPadding = 24f;
            const float chromeButtonHeight = 48f;
            const float chromeOptionsWidth = 132f;
            optionsButtonRect = new Rect(
                screenPadding,
                screenPadding,
                chromeOptionsWidth,
                chromeButtonHeight);
            if (GUI.Button(optionsButtonRect, "オプション", buttonStyle))
            {
                isMenuOpen = true;
                BlockWorldInputBriefly();
            }

            var buttonGroupWidth = buttonWidth * 2f + buttonGap;
            var buttonX = Mathf.Clamp(imageRect.xMax - buttonGroupWidth, screenPadding, Screen.width - buttonGroupWidth - screenPadding);
            var buttonY = Mathf.Min(imageRect.yMax + 14f, Screen.height - buttonHeight - screenPadding);

            if (GUI.Button(
                    new Rect(buttonX, buttonY, buttonWidth, buttonHeight),
                    "条件を変えてリトライ",
                    buttonStyle))
            {
                RerollAndRetry();
            }

            if (GUI.Button(
                    new Rect(buttonX + buttonWidth + buttonGap, buttonY, buttonWidth, buttonHeight),
                    "そのままリトライ",
                    buttonStyle))
            {
                Retry();
            }
        }

        private static Rect CalculateResultImageRect()
        {
            var imageSize = Mathf.Min(Screen.width * 0.62f, Screen.height * 0.7f);
            imageSize = Mathf.Clamp(imageSize, 360f, 920f);
            return new Rect(
                (Screen.width - imageSize) * 0.5f,
                (Screen.height - imageSize) * 0.44f,
                imageSize,
                imageSize
            );
        }

        private void DrawResultFallbackText(Rect imageRect, bool succeeded)
        {
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.09f, 54f, 120f)),
                fontStyle = FontStyle.Bold,
                normal = { textColor = succeeded ? new Color(1f, 0.12f, 0.08f) : new Color(0.88f, 0.94f, 1f) }
            };
            GUI.Label(imageRect, succeeded ? "SUCCESS!!" : "FAILED", titleStyle);
        }

        private Texture2D GetResultTexture(bool succeeded)
        {
            if (succeeded)
            {
                resultSuccessTexture ??= LoadTextureFromAssets("result_success.png");
                return resultSuccessTexture;
            }

            resultFailedTexture ??= LoadTextureFromAssets("result_failed.png");
            return resultFailedTexture;
        }

        private static Texture2D LoadTextureFromAssets(string fileName)
        {
            var texturePath = Path.Combine(Application.dataPath, "Textures", fileName);
            if (!File.Exists(texturePath))
            {
                return null;
            }

            var texture = new Texture2D(2, 2);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(texturePath)))
            {
                Object.Destroy(texture);
                return null;
            }

            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
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
            return gameController.Phase == MoaiGolfGamePhase.Result
                || gameController.Phase == MoaiGolfGamePhase.AngleSelect
                || gameController.Phase == MoaiGolfGamePhase.PowerSelect;
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
        }

        private void BlockWorldInputBriefly()
        {
            blockWorldInputUntilFrame = Time.frameCount + 1;
        }

        private bool ValidateReference(UnityEngine.Object reference, string fieldName)
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
