using UnityEngine;
using UnityEngine.InputSystem;

namespace MoaiGolf
{
    public sealed class MoaiGolfMousePowerInput : MonoBehaviour
    {
        // モアイ中心からどれだけ離したら 100% パワーになるか（ワールドユニット）
        private const float WorldUnitsForFullPower = 5.0f;

        private MoaiGolfGameController gameController;
        private MoaiGolfRunState runState;
        private MoaiGolfStageView stageView;
        private MoaiGolfLaunchAnimator launchAnimator;
        private MoaiGolfHud hud;
        private Camera mainCamera;
        private bool justEntered;

        private void Start()
        {
            gameController = FindAnyObjectByType<MoaiGolfGameController>();
            runState = FindAnyObjectByType<MoaiGolfRunState>();
            stageView = FindAnyObjectByType<MoaiGolfStageView>();
            launchAnimator = FindAnyObjectByType<MoaiGolfLaunchAnimator>();
            hud = FindAnyObjectByType<MoaiGolfHud>();
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

            var mouseWorld = (Vector2)mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
            var distance = Vector2.Distance(mouseWorld, runState.LaunchPosition);
            gameController.SetPower(distance / WorldUnitsForFullPower);

            // 角度確定直後の同一フレームのクリックで誤発射しないよう、1 フレーム空ける
            if (justEntered)
            {
                justEntered = false;
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                stageView ??= FindAnyObjectByType<MoaiGolfStageView>();
                launchAnimator ??= FindAnyObjectByType<MoaiGolfLaunchAnimator>();
                if (stageView?.LaunchBody != null && launchAnimator != null)
                {
                    launchAnimator.BeginLaunchSequence();
                }
            }
        }
    }
}
