using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfMousePowerInput : MonoBehaviour
    {
        private const float DragPixelsForFullPower = 420f;

        private MoaiGolfGameController gameController;
        private Vector2 dragStart;
        private bool isDragging;

        private void Start()
        {
            gameController = FindAnyObjectByType<MoaiGolfGameController>();
        }

        private void Update()
        {
            if (gameController == null || gameController.Phase != MoaiGolfGamePhase.PowerSelect)
            {
                isDragging = false;
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                dragStart = Input.mousePosition;
                isDragging = true;
            }

            if (isDragging && Input.GetMouseButton(0))
            {
                var dragDistance = Vector2.Distance(dragStart, Input.mousePosition);
                gameController.SetPower(dragDistance / DragPixelsForFullPower);
            }

            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }
        }
    }
}
