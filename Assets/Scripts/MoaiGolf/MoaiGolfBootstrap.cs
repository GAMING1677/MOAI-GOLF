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

            ApplyPhysicsBaseline();
            ApplyCameraBaseline();
            var runState = EnsureRunState();
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
    }
}
