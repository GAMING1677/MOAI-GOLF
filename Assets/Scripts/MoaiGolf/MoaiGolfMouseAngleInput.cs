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

        private void Reset()
        {
            RefreshSerializedReferencesForEditor(null, null, null, null, null);
        }

        private void OnValidate()
        {
            RefreshSerializedReferencesForEditor(null, null, null, null, null);
        }

        private void Awake()
        {
            ValidateReferences();
        }

        public void RefreshSerializedReferencesForEditor(
            Camera camera,
            MoaiGolfGameController controller,
            MoaiGolfRunState state,
            MoaiGolfHud hudController,
            MoaiGolfCameraController cameraControl
        )
        {
            mainCamera = camera != null ? camera : mainCamera;
            gameController = controller != null ? controller : GetComponent<MoaiGolfGameController>();
            runState = state != null ? state : GetComponent<MoaiGolfRunState>();
            hud = hudController != null ? hudController : GetComponent<MoaiGolfHud>();
            cameraController = cameraControl != null ? cameraControl : GetComponent<MoaiGolfCameraController>();
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        public bool ValidateReferences()
        {
            var isValid = true;
            isValid &= ValidateReference(mainCamera, nameof(mainCamera));
            isValid &= ValidateReference(gameController, nameof(gameController));
            isValid &= ValidateReference(runState, nameof(runState));
            isValid &= ValidateReference(hud, nameof(hud));
            isValid &= ValidateReference(cameraController, nameof(cameraController));
            return isValid;
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
