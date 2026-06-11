using UnityEngine;
using UnityEngine.InputSystem;

namespace MoaiGolf
{
    public sealed class MoaiGolfMouseAngleInput : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private MoaiGolfGameController gameController;
        [SerializeField] private MoaiGolfRunState runState;
        [SerializeField] private MoaiGolfHud hud;
        [SerializeField] private MoaiGolfCameraController cameraController;

        public void ConfigureDependencies(
            Camera camera,
            MoaiGolfGameController controller,
            MoaiGolfRunState state,
            MoaiGolfHud hudController,
            MoaiGolfCameraController cameraControl
        )
        {
            mainCamera = camera;
            gameController = controller;
            runState = state;
            hud = hudController;
            cameraController = cameraControl;
            ValidateReference(mainCamera, nameof(mainCamera));
            ValidateReference(gameController, nameof(gameController));
            ValidateReference(runState, nameof(runState));
            ValidateReference(hud, nameof(hud));
            ValidateReference(cameraController, nameof(cameraController));
        }

        private void Update()
        {
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

            if (mouse.leftButton.wasReleasedThisFrame && (cameraController == null || !cameraController.IsPointerPanning))
            {
                gameController.ConfirmAngle();
            }
        }

        private bool ValidateReference(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(MoaiGolfMouseAngleInput)} missing serialized reference: {fieldName}.", this);
            return false;
        }
    }
}
