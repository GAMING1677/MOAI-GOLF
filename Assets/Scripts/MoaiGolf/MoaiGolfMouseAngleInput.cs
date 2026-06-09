using UnityEngine;
using UnityEngine.InputSystem;

namespace MoaiGolf
{
    public sealed class MoaiGolfMouseAngleInput : MonoBehaviour
    {
        private MoaiGolfGameController gameController;
        private MoaiGolfRunState runState;
        private MoaiGolfHud hud;
        private MoaiGolfCameraController cameraController;
        private Camera mainCamera;

        private void Start()
        {
            gameController = FindAnyObjectByType<MoaiGolfGameController>();
            runState = FindAnyObjectByType<MoaiGolfRunState>();
            hud = FindAnyObjectByType<MoaiGolfHud>();
            cameraController = FindAnyObjectByType<MoaiGolfCameraController>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            hud ??= FindAnyObjectByType<MoaiGolfHud>();
            if (hud != null && hud.ShouldBlockWorldInput)
            {
                return;
            }

            if (gameController == null || runState == null || mainCamera == null || gameController.Phase != MoaiGolfGamePhase.AngleSelect)
            {
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            var mouseWorld = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
            var aimVector = (Vector2)mouseWorld - runState.LaunchPosition;
            if (aimVector.sqrMagnitude > 0.01f)
            {
                gameController.SetAngle(Mathf.Atan2(aimVector.y, aimVector.x) * Mathf.Rad2Deg);
            }

            cameraController ??= FindAnyObjectByType<MoaiGolfCameraController>();
            if (mouse.leftButton.wasReleasedThisFrame && (cameraController == null || !cameraController.IsPointerPanning))
            {
                gameController.ConfirmAngle();
            }
        }
    }
}
