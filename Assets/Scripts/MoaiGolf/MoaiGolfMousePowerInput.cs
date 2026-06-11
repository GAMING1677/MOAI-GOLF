using UnityEngine;
using UnityEngine.InputSystem;

namespace MoaiGolf
{
    public sealed class MoaiGolfMousePowerInput : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private MoaiGolfGameController gameController;
        [SerializeField] private MoaiGolfRunState runState;
        [SerializeField] private MoaiGolfStageView stageView;
        [SerializeField] private MoaiGolfLaunchAnimator launchAnimator;
        [SerializeField] private MoaiGolfHud hud;
        [SerializeField] private MoaiGolfCameraController cameraController;
        private bool isAdjustingPower;
        private bool justEntered;
        private bool hasRequestedPowerPhasePreload;

        private void Reset()
        {
            RefreshSerializedReferencesForEditor(null, null, null, null, null, null, null);
        }

        private void OnValidate()
        {
            RefreshSerializedReferencesForEditor(null, null, null, null, null, null, null);
        }

        private void Awake()
        {
            ValidateReferences();
        }

        public void RefreshSerializedReferencesForEditor(
            Camera camera,
            MoaiGolfGameController controller,
            MoaiGolfRunState state,
            MoaiGolfStageView view,
            MoaiGolfLaunchAnimator animator,
            MoaiGolfHud hudController,
            MoaiGolfCameraController cameraControl
        )
        {
            mainCamera = camera != null ? camera : mainCamera;
            gameController = controller != null ? controller : GetComponent<MoaiGolfGameController>();
            runState = state != null ? state : GetComponent<MoaiGolfRunState>();
            stageView = view != null ? view : FindAnyObjectByType<MoaiGolfStageView>();
            launchAnimator = animator != null ? animator : GetComponent<MoaiGolfLaunchAnimator>();
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
            isValid &= ValidateReference(stageView, nameof(stageView));
            isValid &= ValidateReference(launchAnimator, nameof(launchAnimator));
            isValid &= ValidateReference(hud, nameof(hud));
            isValid &= ValidateReference(cameraController, nameof(cameraController));
            return isValid;
        }

        private void Update()
        {
            if (hud != null && hud.ShouldBlockWorldInput)
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
                hasRequestedPowerPhasePreload = false;
                return;
            }

            if (!hasRequestedPowerPhasePreload)
            {
                launchAnimator?.PreloadLaunchAssets();
                hasRequestedPowerPhasePreload = true;
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

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

            if (cameraController != null && cameraController.IsPointerPanning)
            {
                return;
            }
        }

        private void BeginLaunchSequence()
        {
            if (stageView?.LaunchBody != null && launchAnimator != null)
            {
                launchAnimator.BeginLaunchSequence();
            }
        }

        private void SetPowerFromGauge(float worldX, Rect gaugeRect)
        {
            gameController.SetPower(Mathf.InverseLerp(gaugeRect.xMin, gaugeRect.xMax, worldX));
        }

        private bool ValidateReference(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(MoaiGolfMousePowerInput)} missing serialized reference: {fieldName}.", this);
            return false;
        }
    }
}
