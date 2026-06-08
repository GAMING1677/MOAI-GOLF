using UnityEngine;

namespace MoaiGolf
{
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
