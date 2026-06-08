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
        private bool isMenuOpen;

        public bool IsMenuOpen => isMenuOpen;

        private void Start()
        {
            gameController = FindAnyObjectByType<MoaiGolfGameController>();
            runState = FindAnyObjectByType<MoaiGolfRunState>();
            stageView = FindAnyObjectByType<MoaiGolfStageView>();
            cameraController = FindAnyObjectByType<MoaiGolfCameraController>();
            launchAnimator = FindAnyObjectByType<MoaiGolfLaunchAnimator>();
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
            const float dialogHeight = 280f;
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
            return gameController != null && runState != null && stageView != null;
        }

        private void RebuildAfterRetry()
        {
            isMenuOpen = false;
            launchAnimator?.CancelLaunchSequence();
            gameController.ResetForRetry();
            stageView.Build(runState);
            cameraController?.ResetToInitial();
        }
    }
}
