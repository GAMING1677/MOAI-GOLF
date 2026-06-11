using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfBootstrap : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private MoaiGolfRunState runState;
        [SerializeField] private MoaiGolfStageView stageView;
        [SerializeField] private MoaiGolfGameController gameController;
        [SerializeField] private MoaiGolfMouseAngleInput mouseAngleInput;
        [SerializeField] private MoaiGolfMousePowerInput mousePowerInput;
        [SerializeField] private MoaiGolfCameraController cameraController;
        [SerializeField] private MoaiGolfBgmController bgmController;
        [SerializeField] private MoaiGolfSeController seController;
        [SerializeField] private MoaiGolfHud hud;
        [SerializeField] private MoaiGolfGuideOverlay guideOverlay;
        [SerializeField] private MoaiGolfLaunchAnimator launchAnimator;

        private void Reset()
        {
            RefreshSerializedReferencesForEditor(mainCamera, stageView, bgmController);
        }

        private void OnValidate()
        {
            RefreshSerializedReferencesForEditor(mainCamera, stageView, bgmController);
        }

        private void Awake()
        {
            if (!ValidateReferences())
            {
                return;
            }

            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.SetResolution(
                MoaiGolfWorldSettings.ViewportWidthPixels,
                MoaiGolfWorldSettings.ViewportHeightPixels,
                FullScreenMode.FullScreenWindow
            );

            ApplyPhysicsBaseline();
            ApplyCameraBaseline();
            if (!ValidateConfiguredComponents())
            {
                return;
            }

            runState.InitializeNewRun(MoaiGolfStageDefinition.CreateFirstStage());
            stageView.Build(runState);
            cameraController.FocusOnLaunchArea();
        }

        private static void ApplyPhysicsBaseline()
        {
            Physics2D.gravity = new Vector2(0f, MoaiGolfWorldSettings.GravityY);
            Time.fixedDeltaTime = MoaiGolfWorldSettings.FixedTimestep;
        }

        private void ApplyCameraBaseline()
        {
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = MoaiGolfWorldSettings.CameraOrthographicSize;
            mainCamera.transform.position = new Vector3(
                MoaiGolfWorldSettings.CameraCenterX,
                MoaiGolfWorldSettings.CameraCenterY,
                MoaiGolfWorldSettings.CameraZ
            );
            mainCamera.backgroundColor = new Color(0.19f, 0.3f, 0.47f);
        }

        private bool ValidateConfiguredComponents()
        {
            var isValid = true;
            isValid &= gameController.ValidateReferences();
            isValid &= cameraController.ValidateReferences();
            isValid &= guideOverlay.ValidateReferences();
            isValid &= launchAnimator.ValidateReferences();
            isValid &= hud.ValidateReferences();
            isValid &= mouseAngleInput.ValidateReferences();
            isValid &= mousePowerInput.ValidateReferences();
            return isValid;
        }

        private bool ValidateReferences()
        {
            var isValid = true;
            isValid &= ValidateReference(mainCamera, nameof(mainCamera));
            isValid &= ValidateReference(runState, nameof(runState));
            isValid &= ValidateReference(stageView, nameof(stageView));
            isValid &= ValidateReference(gameController, nameof(gameController));
            isValid &= ValidateReference(mouseAngleInput, nameof(mouseAngleInput));
            isValid &= ValidateReference(mousePowerInput, nameof(mousePowerInput));
            isValid &= ValidateReference(cameraController, nameof(cameraController));
            isValid &= ValidateReference(bgmController, nameof(bgmController));
            isValid &= ValidateReference(seController, nameof(seController));
            isValid &= ValidateReference(hud, nameof(hud));
            isValid &= ValidateReference(guideOverlay, nameof(guideOverlay));
            isValid &= ValidateReference(launchAnimator, nameof(launchAnimator));
            return isValid;
        }

        private bool ValidateReference(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(MoaiGolfBootstrap)} missing serialized reference: {fieldName}.", this);
            return false;
        }

        public void RefreshSerializedReferencesForEditor(
            Camera camera,
            MoaiGolfStageView view,
            MoaiGolfBgmController bgm
        )
        {
            mainCamera = camera != null ? camera : mainCamera;
            stageView = view;
            bgmController = bgm;
            runState = GetComponent<MoaiGolfRunState>();
            gameController = GetComponent<MoaiGolfGameController>();
            mouseAngleInput = GetComponent<MoaiGolfMouseAngleInput>();
            mousePowerInput = GetComponent<MoaiGolfMousePowerInput>();
            cameraController = GetComponent<MoaiGolfCameraController>();
            seController = GetComponent<MoaiGolfSeController>();
            hud = GetComponent<MoaiGolfHud>();
            guideOverlay = GetComponent<MoaiGolfGuideOverlay>();
            launchAnimator = GetComponent<MoaiGolfLaunchAnimator>();
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            gameController?.RefreshSerializedReferencesForEditor(seController);
            cameraController?.RefreshSerializedReferencesForEditor(mainCamera, gameController, stageView, launchAnimator, hud);
            guideOverlay?.RefreshSerializedReferencesForEditor(mainCamera, gameController, runState, stageView);
            launchAnimator?.RefreshSerializedReferencesForEditor(mainCamera, gameController, runState, stageView, seController);
            hud?.RefreshSerializedReferencesForEditor(gameController, runState, stageView, cameraController, launchAnimator, bgmController, seController);
            mouseAngleInput?.RefreshSerializedReferencesForEditor(mainCamera, gameController, runState, hud, cameraController);
            mousePowerInput?.RefreshSerializedReferencesForEditor(mainCamera, gameController, runState, stageView, launchAnimator, hud, cameraController);
            stageView?.RefreshSerializedDependenciesForEditor(gameController, seController);
        }
    }
}

