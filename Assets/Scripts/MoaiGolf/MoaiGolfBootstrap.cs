using UnityEngine;
using UnityEngine.InputSystem;

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
            ConfigureDependencies();
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

        private void ConfigureDependencies()
        {
            gameController.ConfigureDependencies(seController);
            stageView.ConfigureDependencies(gameController, seController);
            cameraController.ConfigureDependencies(mainCamera, gameController, stageView, launchAnimator, hud);
            guideOverlay.ConfigureDependencies(mainCamera, gameController, runState, stageView);
            launchAnimator.ConfigureDependencies(mainCamera, gameController, runState, stageView, seController);
            hud.ConfigureDependencies(gameController, runState, stageView, cameraController, launchAnimator, bgmController, seController);
            mouseAngleInput.ConfigureDependencies(mainCamera, gameController, runState, hud, cameraController);
            mousePowerInput.ConfigureDependencies(mainCamera, gameController, runState, stageView, launchAnimator, hud, cameraController);
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
            mainCamera = camera;
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
        }
    }

    public sealed class MoaiGolfCameraController : MonoBehaviour
    {
        public const float LaunchGameplayCameraYOffset = 0.35f;

        private const float KeyboardScrollSpeed = 12f;
        private const float EdgeScrollSpeed = 9f;
        private const float EdgeScrollMarginPixels = 72f;
        private const float DragStartThresholdPixels = 6f;
        private const float FollowLerp = 5f;

        [SerializeField] private Camera mainCamera;
        [SerializeField] private MoaiGolfGameController gameController;
        [SerializeField] private MoaiGolfStageView stageView;
        [SerializeField] private MoaiGolfLaunchAnimator launchAnimator;
        [SerializeField] private MoaiGolfHud hud;
        private Vector3 lastMousePosition;
        private Vector3 dragStartMousePosition;
        private bool isDragging;
        private bool isLeftDrag;
        private bool isLeftDragPending;

        public bool IsPointerPanning => isDragging;

        public void ConfigureDependencies(
            Camera camera,
            MoaiGolfGameController controller,
            MoaiGolfStageView view,
            MoaiGolfLaunchAnimator animator,
            MoaiGolfHud hudController
        )
        {
            mainCamera = camera;
            gameController = controller;
            stageView = view;
            launchAnimator = animator;
            hud = hudController;
            ValidateReference(mainCamera, nameof(mainCamera));
            ValidateReference(gameController, nameof(gameController));
            ValidateReference(stageView, nameof(stageView));
            ValidateReference(launchAnimator, nameof(launchAnimator));
            ValidateReference(hud, nameof(hud));
        }

        public static Vector3 ResolveLaunchGameplayCameraPosition(MoaiGolfStageView stageView)
        {
            if (stageView?.LaunchBody == null)
            {
                return new Vector3(
                    MoaiGolfWorldSettings.CameraCenterX,
                    MoaiGolfWorldSettings.CameraCenterY,
                    MoaiGolfWorldSettings.CameraZ
                );
            }

            var focus = stageView.LaunchCameraFocusPosition;
            return new Vector3(
                focus.x,
                Mathf.Clamp(
                    focus.y + LaunchGameplayCameraYOffset,
                    MoaiGolfWorldSettings.CameraMinY,
                    MoaiGolfWorldSettings.CameraMaxY
                ),
                MoaiGolfWorldSettings.CameraZ
            );
        }

        public static float ResolveCameraMinX(MoaiGolfGamePhase? phase, MoaiGolfStageView stageView)
        {
            if (stageView?.LaunchBody == null)
            {
                return MoaiGolfWorldSettings.CameraMinX;
            }

            var launchFocusX = stageView.LaunchCameraFocusPosition.x;
            if (ShouldRelaxLaunchHorizontalClamp(phase))
            {
                return Mathf.Min(MoaiGolfWorldSettings.CameraMinX, launchFocusX);
            }

            if (phase == MoaiGolfGamePhase.Flying
                && stageView.LaunchBody.position.x < MoaiGolfWorldSettings.CameraMinX)
            {
                return Mathf.Min(MoaiGolfWorldSettings.CameraMinX, launchFocusX);
            }

            return MoaiGolfWorldSettings.CameraMinX;
        }

        public static Vector3 ClampCameraPosition(Vector3 position, MoaiGolfGamePhase? phase, MoaiGolfStageView stageView)
        {
            return new Vector3(
                Mathf.Clamp(position.x, ResolveCameraMinX(phase, stageView), MoaiGolfWorldSettings.CameraMaxX),
                Mathf.Clamp(position.y, MoaiGolfWorldSettings.CameraMinY, MoaiGolfWorldSettings.CameraMaxY),
                MoaiGolfWorldSettings.CameraZ
            );
        }

        private static bool ShouldRelaxLaunchHorizontalClamp(MoaiGolfGamePhase? phase)
        {
            return phase == MoaiGolfGamePhase.AngleSelect
                || phase == MoaiGolfGamePhase.PowerSelect
                || phase == MoaiGolfGamePhase.LaunchAnimation
                || phase == MoaiGolfGamePhase.Result;
        }

        private void Start()
        {
            FocusOnLaunchArea();
        }

        public void FocusOnLaunchArea()
        {
            if (mainCamera == null)
            {
                Debug.LogError($"{nameof(MoaiGolfCameraController)} missing serialized reference: {nameof(mainCamera)}.", this);
                return;
            }

            mainCamera.transform.position = ResolveLaunchGameplayCameraPosition(stageView);
            ClampCamera();
        }

        public void ResetToInitial()
        {
            if (mainCamera == null)
            {
                Debug.LogError($"{nameof(MoaiGolfCameraController)} missing serialized reference: {nameof(mainCamera)}.", this);
                return;
            }

            isDragging = false;
            isLeftDrag = false;
            isLeftDragPending = false;
            FocusOnLaunchArea();
        }

        private void LateUpdate()
        {
            if (mainCamera == null)
            {
                return;
            }

            // 射出アニメ中はアニメーターがカメラを直接操作するので何もしない
            if (launchAnimator != null && launchAnimator.IsPlaying)
            {
                return;
            }

            if (gameController != null && gameController.Phase == MoaiGolfGamePhase.Flying && stageView?.LaunchBody != null)
            {
                FollowLaunchVisual();
            }
            else
            {
                ApplyManualScroll();
            }

            ClampCamera();
        }

        private void ApplyManualScroll()
        {
            var keyboard = Keyboard.current;
            var horizontal = 0f;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    horizontal -= 1f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    horizontal += 1f;
                }
            }

            if (Mathf.Abs(horizontal) > 0.01f)
            {
                mainCamera.transform.position += Vector3.right * horizontal * KeyboardScrollSpeed * Time.unscaledDeltaTime;
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                isDragging = false;
                isLeftDrag = false;
                isLeftDragPending = false;
                return;
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                lastMousePosition = mouse.position.ReadValue();
                isDragging = true;
                isLeftDrag = false;
                isLeftDragPending = false;
            }

            if (mouse.leftButton.wasPressedThisFrame && CanStartLeftDrag(mouse.position.ReadValue()))
            {
                dragStartMousePosition = mouse.position.ReadValue();
                lastMousePosition = dragStartMousePosition;
                isLeftDragPending = true;
            }

            if (isLeftDragPending && mouse.leftButton.isPressed)
            {
                var pendingMousePosition = (Vector3)mouse.position.ReadValue();
                if ((pendingMousePosition - dragStartMousePosition).sqrMagnitude >= DragStartThresholdPixels * DragStartThresholdPixels)
                {
                    isDragging = true;
                    isLeftDrag = true;
                    isLeftDragPending = false;
                    lastMousePosition = pendingMousePosition;
                }
            }

            if (mouse.rightButton.wasReleasedThisFrame && !isLeftDrag)
            {
                isDragging = false;
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                if (isLeftDrag)
                {
                    isDragging = false;
                }

                isLeftDrag = false;
                isLeftDragPending = false;
            }

            if (!isDragging && !mouse.leftButton.isPressed)
            {
                ApplyEdgeScroll(mouse.position.ReadValue());
            }

            if (!isDragging)
            {
                return;
            }

            var currentMousePosition = (Vector3)mouse.position.ReadValue();
            var deltaPixels = currentMousePosition - lastMousePosition;
            lastMousePosition = currentMousePosition;

            var worldUnitsPerPixel = (mainCamera.orthographicSize * 2f) / Screen.height;
            mainCamera.transform.position -= new Vector3(deltaPixels.x * worldUnitsPerPixel, deltaPixels.y * worldUnitsPerPixel, 0f);
        }

        private bool CanStartLeftDrag(Vector2 mousePosition)
        {
            if (hud != null && hud.ShouldBlockWorldInput)
            {
                return false;
            }

            return gameController == null
                || gameController.Phase != MoaiGolfGamePhase.PowerSelect
                || !MoaiGolfPowerControlsLayout.IsPointerOnPowerControls(mainCamera, mousePosition);
        }

        private void ApplyEdgeScroll(Vector2 mousePosition)
        {
            if (mousePosition.x < 0f || mousePosition.x > Screen.width || mousePosition.y < 0f || mousePosition.y > Screen.height)
            {
                return;
            }

            var movement = Vector2.zero;
            if (mousePosition.x <= EdgeScrollMarginPixels)
            {
                movement.x -= 1f;
            }
            else if (mousePosition.x >= Screen.width - EdgeScrollMarginPixels)
            {
                movement.x += 1f;
            }

            if (mousePosition.y <= EdgeScrollMarginPixels)
            {
                movement.y -= 1f;
            }
            else if (mousePosition.y >= Screen.height - EdgeScrollMarginPixels)
            {
                movement.y += 1f;
            }

            if (movement.sqrMagnitude <= 0f)
            {
                return;
            }

            movement = movement.normalized * EdgeScrollSpeed * Time.unscaledDeltaTime;
            mainCamera.transform.position += new Vector3(movement.x, movement.y, 0f);
        }

        private void FollowLaunchVisual()
        {
            var target = stageView.LaunchCameraFocusPosition;
            var current = mainCamera.transform.position;
            var desired = new Vector3(target.x, Mathf.Clamp(target.y, MoaiGolfWorldSettings.CameraMinY, MoaiGolfWorldSettings.CameraMaxY), current.z);
            mainCamera.transform.position = Vector3.Lerp(current, desired, 1f - Mathf.Exp(-FollowLerp * Time.deltaTime));
        }

        private void ClampCamera()
        {
            MoaiGolfGamePhase? phase = gameController != null ? gameController.Phase : null;
            mainCamera.transform.position = ClampCameraPosition(mainCamera.transform.position, phase, stageView);
        }

        private bool ValidateReference(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(MoaiGolfCameraController)} missing serialized reference: {fieldName}.", this);
            return false;
        }
    }
}

