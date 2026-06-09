using UnityEngine;
using UnityEngine.InputSystem;

namespace MoaiGolf
{
    public sealed class MoaiGolfMousePowerInput : MonoBehaviour
    {
        private MoaiGolfGameController gameController;
        private MoaiGolfRunState runState;
        private MoaiGolfStageView stageView;
        private MoaiGolfLaunchAnimator launchAnimator;
        private MoaiGolfHud hud;
        private MoaiGolfCameraController cameraController;
        private Camera mainCamera;
        private bool isAdjustingPower;
        private bool justEntered;

        private void Start()
        {
            gameController = FindAnyObjectByType<MoaiGolfGameController>();
            runState = FindAnyObjectByType<MoaiGolfRunState>();
            stageView = FindAnyObjectByType<MoaiGolfStageView>();
            launchAnimator = FindAnyObjectByType<MoaiGolfLaunchAnimator>();
            hud = FindAnyObjectByType<MoaiGolfHud>();
            cameraController = FindAnyObjectByType<MoaiGolfCameraController>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            hud ??= FindAnyObjectByType<MoaiGolfHud>();
            if (hud != null && hud.IsMenuOpen)
            {
                justEntered = true;
                return;
            }

            if (gameController == null || runState == null)
            {
                return;
            }

            if (gameController.Phase != MoaiGolfGamePhase.PowerSelect)
            {
                isAdjustingPower = false;
                justEntered = true;
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            mainCamera ??= Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            // 角度確定直後の同一フレームのクリックで誤発射しないよう、1 フレーム空ける
            if (justEntered)
            {
                justEntered = false;
                return;
            }

            var mouseWorld = (Vector2)mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
            var gaugeRect = MoaiGolfPowerControlsLayout.GetGaugeRect(mainCamera);
            var launchButtonRect = MoaiGolfPowerControlsLayout.GetLaunchButtonRect(mainCamera);
            var isOnGauge = gaugeRect.Contains(mouseWorld);
            var isOnLaunchButton = launchButtonRect.Contains(mouseWorld);

            if (mouse.leftButton.wasPressedThisFrame && isOnGauge)
            {
                isAdjustingPower = true;
                SetPowerFromGauge(mouseWorld.x, gaugeRect);
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame && isOnLaunchButton)
            {
                BeginLaunchSequence();
                return;
            }

            if (isAdjustingPower)
            {
                if (mouse.leftButton.isPressed)
                {
                    SetPowerFromGauge(mouseWorld.x, gaugeRect);
                }
                else
                {
                    isAdjustingPower = false;
                }

                return;
            }

            cameraController ??= FindAnyObjectByType<MoaiGolfCameraController>();
            if (cameraController != null && cameraController.IsPointerPanning)
            {
                return;
            }
        }

        private void BeginLaunchSequence()
        {
            stageView ??= FindAnyObjectByType<MoaiGolfStageView>();
            launchAnimator ??= FindAnyObjectByType<MoaiGolfLaunchAnimator>();
            if (stageView?.LaunchBody != null && launchAnimator != null)
            {
                launchAnimator.BeginLaunchSequence();
            }
        }

        private void SetPowerFromGauge(float worldX, Rect gaugeRect)
        {
            gameController.SetPower(Mathf.InverseLerp(gaugeRect.xMin, gaugeRect.xMax, worldX));
        }
    }
}
