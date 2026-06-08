using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfMouseAngleInput : MonoBehaviour
    {
        private MoaiGolfGameController gameController;
        private MoaiGolfRunState runState;
        private Camera mainCamera;

        private void Start()
        {
            gameController = FindAnyObjectByType<MoaiGolfGameController>();
            runState = FindAnyObjectByType<MoaiGolfRunState>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (gameController == null || runState == null || mainCamera == null || gameController.Phase != MoaiGolfGamePhase.AngleSelect)
            {
                return;
            }

            var mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            var aimVector = (Vector2)mouseWorld - runState.LaunchPosition;
            if (aimVector.sqrMagnitude > 0.01f)
            {
                gameController.SetAngle(Mathf.Atan2(aimVector.y, aimVector.x) * Mathf.Rad2Deg);
            }

            if (Input.GetMouseButtonDown(0))
            {
                gameController.ConfirmAngle();
            }
        }
    }
}
