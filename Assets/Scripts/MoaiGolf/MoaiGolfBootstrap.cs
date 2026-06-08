using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfBootstrap : MonoBehaviour
    {
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
            EnsureHud();
            EnsureStageView(runState);
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
        private const float KeyboardScrollSpeed = 12f;
        private const float FollowLerp = 5f;

        private Camera mainCamera;
        private MoaiGolfGameController gameController;
        private MoaiGolfStageView stageView;
        private Vector3 lastMousePosition;
        private bool isDragging;

        private void Start()
        {
            mainCamera = Camera.main;
            gameController = FindAnyObjectByType<MoaiGolfGameController>();
            stageView = FindAnyObjectByType<MoaiGolfStageView>();
            ClampCamera();
        }

        private void LateUpdate()
        {
            if (mainCamera == null)
            {
                return;
            }

            stageView ??= FindAnyObjectByType<MoaiGolfStageView>();
            gameController ??= FindAnyObjectByType<MoaiGolfGameController>();

            if (gameController != null && gameController.Phase == MoaiGolfGamePhase.Flying && stageView?.LaunchBody != null)
            {
                FollowLaunchBody();
            }
            else
            {
                ApplyManualScroll();
            }

            ClampCamera();
        }

        private void ApplyManualScroll()
        {
            var horizontal = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(horizontal) > 0.01f)
            {
                mainCamera.transform.position += Vector3.right * horizontal * KeyboardScrollSpeed * Time.unscaledDeltaTime;
            }

            if (Input.GetMouseButtonDown(1))
            {
                lastMousePosition = Input.mousePosition;
                isDragging = true;
            }

            if (Input.GetMouseButtonUp(1))
            {
                isDragging = false;
            }

            if (!isDragging)
            {
                return;
            }

            var currentMousePosition = Input.mousePosition;
            var deltaPixels = currentMousePosition - lastMousePosition;
            lastMousePosition = currentMousePosition;

            var worldUnitsPerPixel = (mainCamera.orthographicSize * 2f) / Screen.height;
            mainCamera.transform.position -= new Vector3(deltaPixels.x * worldUnitsPerPixel, deltaPixels.y * worldUnitsPerPixel, 0f);
        }

        private void FollowLaunchBody()
        {
            var target = stageView.LaunchBody.position;
            var current = mainCamera.transform.position;
            var desired = new Vector3(target.x, Mathf.Clamp(target.y, MoaiGolfWorldSettings.CameraMinY, MoaiGolfWorldSettings.CameraMaxY), current.z);
            mainCamera.transform.position = Vector3.Lerp(current, desired, 1f - Mathf.Exp(-FollowLerp * Time.deltaTime));
        }

        private void ClampCamera()
        {
            var position = mainCamera.transform.position;
            mainCamera.transform.position = new Vector3(
                Mathf.Clamp(position.x, MoaiGolfWorldSettings.CameraMinX, MoaiGolfWorldSettings.CameraMaxX),
                Mathf.Clamp(position.y, MoaiGolfWorldSettings.CameraMinY, MoaiGolfWorldSettings.CameraMaxY),
                MoaiGolfWorldSettings.CameraZ
            );
        }
    }
}
