using UnityEngine;
using UnityEngine.InputSystem;

namespace MoaiGolf
{
    public sealed class MoaiGolfBootstrap : MonoBehaviour
    {
        // Empty-scene fallback only. Prefer placing game systems on Scene/Prefab and wiring them in Awake.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            if (FindAnyObjectByType<MoaiGolfBootstrap>() != null)
            {
                return;
            }

            var bootstrapObject = new GameObject(nameof(MoaiGolfBootstrap));
            bootstrapObject.AddComponent<MoaiGolfBootstrap>();
        }

        private void Awake()
        {
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
            var runState = EnsureRunState();
            EnsureGameController();
            EnsureMouseInput();
            EnsureCameraController();
            EnsureBgmController();
            EnsureSeController();
            EnsureHud();
            EnsureGuideOverlay();
            EnsureLaunchAnimator();
            EnsureStageView(runState);
        }

        private void EnsureGuideOverlay()
        {
            if (FindAnyObjectByType<MoaiGolfGuideOverlay>() == null)
            {
                gameObject.AddComponent<MoaiGolfGuideOverlay>();
            }
        }

        private void EnsureLaunchAnimator()
        {
            if (FindAnyObjectByType<MoaiGolfLaunchAnimator>() == null)
            {
                gameObject.AddComponent<MoaiGolfLaunchAnimator>();
            }
        }

        private static void ApplyPhysicsBaseline()
        {
            Physics2D.gravity = new Vector2(0f, MoaiGolfWorldSettings.GravityY);
            Time.fixedDeltaTime = MoaiGolfWorldSettings.FixedTimestep;
        }

        private static void ApplyCameraBaseline()
        {
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = MoaiGolfWorldSettings.CameraOrthographicSize;
            mainCamera.transform.position = new Vector3(
                MoaiGolfWorldSettings.CameraCenterX,
                MoaiGolfWorldSettings.CameraCenterY,
                MoaiGolfWorldSettings.CameraZ
            );
            mainCamera.backgroundColor = new Color(0.19f, 0.3f, 0.47f);
        }

        private MoaiGolfRunState EnsureRunState()
        {
            var runState = FindAnyObjectByType<MoaiGolfRunState>();
            if (runState == null)
            {
                runState = gameObject.AddComponent<MoaiGolfRunState>();
            }

            runState.InitializeNewRun(MoaiGolfStageDefinition.CreateFirstStage());
            return runState;
        }

        private void EnsureStageView(MoaiGolfRunState runState)
        {
            var stageView = FindAnyObjectByType<MoaiGolfStageView>();
            if (stageView == null)
            {
                var stageObject = new GameObject(nameof(MoaiGolfStageView));
                stageView = stageObject.AddComponent<MoaiGolfStageView>();
            }

            stageView.Build(runState);
            FindAnyObjectByType<MoaiGolfCameraController>()?.FocusOnLaunchArea();
        }

        private void EnsureGameController()
        {
            if (FindAnyObjectByType<MoaiGolfGameController>() == null)
            {
                gameObject.AddComponent<MoaiGolfGameController>();
            }
        }

        private void EnsureMouseInput()
        {
            if (FindAnyObjectByType<MoaiGolfMouseAngleInput>() == null)
            {
                gameObject.AddComponent<MoaiGolfMouseAngleInput>();
            }

            if (FindAnyObjectByType<MoaiGolfMousePowerInput>() == null)
            {
                gameObject.AddComponent<MoaiGolfMousePowerInput>();
            }
        }

        private void EnsureHud()
        {
            if (FindAnyObjectByType<MoaiGolfHud>() == null)
            {
                gameObject.AddComponent<MoaiGolfHud>();
            }
        }

        private void EnsureBgmController()
        {
            if (FindAnyObjectByType<MoaiGolfBgmController>() == null)
            {
                gameObject.AddComponent<MoaiGolfBgmController>();
            }
        }

        private void EnsureSeController()
        {
            if (FindAnyObjectByType<MoaiGolfSeController>() == null)
            {
                gameObject.AddComponent<MoaiGolfSeController>();
            }
        }

        private void EnsureCameraController()
        {
            if (FindAnyObjectByType<MoaiGolfCameraController>() == null)
            {
                gameObject.AddComponent<MoaiGolfCameraController>();
            }
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

        private Camera mainCamera;
        private MoaiGolfGameController gameController;
        private MoaiGolfStageView stageView;
        private MoaiGolfLaunchAnimator launchAnimator;
        private MoaiGolfHud hud;
        private Vector3 lastMousePosition;
        private Vector3 dragStartMousePosition;
        private bool isDragging;
        private bool isLeftDrag;
        private bool isLeftDragPending;

        public bool IsPointerPanning => isDragging;

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
            mainCamera = Camera.main;
            gameController = FindAnyObjectByType<MoaiGolfGameController>();
            stageView = FindAnyObjectByType<MoaiGolfStageView>();
            launchAnimator = FindAnyObjectByType<MoaiGolfLaunchAnimator>();
            hud = FindAnyObjectByType<MoaiGolfHud>();
            FocusOnLaunchArea();
        }

        public void FocusOnLaunchArea()
        {
            mainCamera ??= Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            stageView ??= FindAnyObjectByType<MoaiGolfStageView>();
            mainCamera.transform.position = ResolveLaunchGameplayCameraPosition(stageView);
            ClampCamera();
        }

        public void ResetToInitial()
        {
            mainCamera ??= Camera.main;
            if (mainCamera == null)
            {
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

            stageView ??= FindAnyObjectByType<MoaiGolfStageView>();
            gameController ??= FindAnyObjectByType<MoaiGolfGameController>();
            launchAnimator ??= FindAnyObjectByType<MoaiGolfLaunchAnimator>();
            hud ??= FindAnyObjectByType<MoaiGolfHud>();

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
    }
}
