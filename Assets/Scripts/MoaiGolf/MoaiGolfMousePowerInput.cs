using UnityEngine;
using UnityEngine.InputSystem;

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

            var mouse = Mouse.current;
            if (mouse == null)
            {
                isDragging = false;
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                dragStart = mouse.position.ReadValue();
                isDragging = true;
            }

            if (isDragging && mouse.leftButton.isPressed)
            {
                var dragDistance = Vector2.Distance(dragStart, mouse.position.ReadValue());
                gameController.SetPower(dragDistance / DragPixelsForFullPower);
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                isDragging = false;
            }
        }
    }
}
