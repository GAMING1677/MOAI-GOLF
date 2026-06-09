using UnityEngine;
using UnityEngine.InputSystem;

namespace MoaiGolf
{
    public sealed class MoaiGolfHud : MonoBehaviour
    {
        private MoaiGolfGameController gameController;
        private MoaiGolfRunState runState;
        private MoaiGolfStageView stageView;
        private MoaiGolfCameraController cameraController;
        private MoaiGolfLaunchAnimator launchAnimator;
        private MoaiGolfBgmController bgmController;
        private bool isMenuOpen;
        private int blockWorldInputUntilFrame = -1;

        public bool IsMenuOpen => isMenuOpen;
        public bool ShouldBlockWorldInput =>
            isMenuOpen || gameController?.Phase == MoaiGolfGamePhase.Result || Time.frameCount <= blockWorldInputUntilFrame;

        private void Start()
        {
            gameController = FindAnyObjectByType<MoaiGolfGameController>();
            runState = FindAnyObjectByType<MoaiGolfRunState>();
            stageView = FindAnyObjectByType<MoaiGolfStageView>();
            cameraController = FindAnyObjectByType<MoaiGolfCameraController>();
            launchAnimator = FindAnyObjectByType<MoaiGolfLaunchAnimator>();
            bgmController = FindAnyObjectByType<MoaiGolfBgmController>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                gameController ??= FindAnyObjectByType<MoaiGolfGameController>();
                if (gameController != null && gameController.Phase == MoaiGolfGamePhase.Result)
                {
                    isMenuOpen = false;
                }
                else
                {
                    isMenuOpen = !isMenuOpen;
                }
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                Retry();
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
                isMenuOpen = false;
                ApplyBgmMenuDucking();
                DrawResultDialog();
                return;
            }

            if (isMenuOpen)
            {
                DrawMenuDialog();
            }
        }

        private void DrawMenuDialog()
        {
            const float dialogWidth = 360f;
            const float dialogHeight = 342f;
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
            GUI.Label(new Rect(dialogRect.x, dialogRect.y + 24f, dialogRect.width, 44f), "MENU", titleStyle);

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
            if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "そのままリトライ (R)", buttonStyle))
            {
                Retry();
            }

            buttonY += buttonHeight + buttonGap;
            if (GUI.Button(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), "条件を変えてリトライ", buttonStyle))
            {
                RerollAndRetry();
            }

            DrawBgmVolumeSlider(dialogRect, buttonY + buttonHeight + 20f);
        }

        private void DrawBgmVolumeSlider(Rect dialogRect, float sliderY)
        {
            bgmController ??= FindAnyObjectByType<MoaiGolfBgmController>();

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
            var volume = bgmController != null ? bgmController.Volume : MoaiGolfBgmController.DefaultVolume;

            GUI.Label(new Rect(sliderX, sliderY - 2f, labelWidth, 28f), "BGM音量", labelStyle);
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

            if (bgmController != null && !Mathf.Approximately(newVolume, volume))
            {
                bgmController.SetVolume(newVolume);
            }
        }

        private void DrawResultDialog()
        {
            const float dialogWidth = 480f;
            const float dialogHeight = 240f;
            var dialogRect = new Rect(
                (Screen.width - dialogWidth) * 0.5f,
                (Screen.height - dialogHeight) * 0.5f,
                dialogWidth,
                dialogHeight
            );

            // 半透明の暗幕で背景を覆う
            var prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prevColor;

            GUI.Box(dialogRect, string.Empty);

            var succeeded = gameController.LastResultSucceeded == true;
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 36,
                fontStyle = FontStyle.Bold,
                normal = { textColor = succeeded ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.4f, 0.4f) }
            };
            GUI.Label(
                new Rect(dialogRect.x, dialogRect.y + 24f, dialogRect.width, 60f),
                succeeded ? "SUCCESS!" : "FAILED",
                titleStyle
            );

            var subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                normal = { textColor = Color.white }
            };
            GUI.Label(
                new Rect(dialogRect.x, dialogRect.y + 90f, dialogRect.width, 28f),
                succeeded ? "ナイスショット！" : "もういちどチャレンジ！",
                subtitleStyle
            );

            var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            const float buttonHeight = 56f;
            const float buttonGap = 20f;
            var buttonWidth = (dialogRect.width - buttonGap * 3f) * 0.5f;
            var buttonY = dialogRect.y + dialogRect.height - buttonHeight - 24f;

            if (GUI.Button(
                    new Rect(dialogRect.x + buttonGap, buttonY, buttonWidth, buttonHeight),
                    "条件を変えてリトライ",
                    buttonStyle))
            {
                RerollAndRetry();
            }

            if (GUI.Button(
                    new Rect(dialogRect.x + buttonGap * 2f + buttonWidth, buttonY, buttonWidth, buttonHeight),
                    "そのままリトライ (R)",
                    buttonStyle))
            {
                Retry();
            }
        }

        private void Retry()
        {
            if (!ResolveDependencies())
            {
                return;
            }

            runState.RetryCurrentRun();
            RebuildAfterRetry();
        }

        private void RerollAndRetry()
        {
            if (!ResolveDependencies())
            {
                return;
            }

            runState.RerollAndRetry();
            RebuildAfterRetry();
        }

        private bool ResolveDependencies()
        {
            gameController ??= FindAnyObjectByType<MoaiGolfGameController>();
            runState ??= FindAnyObjectByType<MoaiGolfRunState>();
            stageView ??= FindAnyObjectByType<MoaiGolfStageView>();
            cameraController ??= FindAnyObjectByType<MoaiGolfCameraController>();
            launchAnimator ??= FindAnyObjectByType<MoaiGolfLaunchAnimator>();
            bgmController ??= FindAnyObjectByType<MoaiGolfBgmController>();
            return gameController != null && runState != null && stageView != null;
        }

        private void ApplyBgmMenuDucking()
        {
            bgmController ??= FindAnyObjectByType<MoaiGolfBgmController>();
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
    }
}
