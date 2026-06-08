using UnityEngine;
using UnityEngine.InputSystem;

namespace MoaiGolf
{
    public sealed class MoaiGolfMousePowerInput : MonoBehaviour
    {
        private const float PixelsForFullPower = 360f;

        private MoaiGolfGameController gameController;
        private MoaiGolfStageView stageView;
        private Vector2 anchorScreenPosition;
        private bool hasAnchor;

        private void Start()
        {
            gameController = FindAnyObjectByType<MoaiGolfGameController>();
            stageView = FindAnyObjectByType<MoaiGolfStageView>();
        }

        private void Update()
        {
            if (gameController == null)
            {
                return;
            }

            if (gameController.Phase != MoaiGolfGamePhase.PowerSelect)
            {
                hasAnchor = false;
                return;
            }

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            var screenPosition = mouse.position.ReadValue();
            var anchorJustSet = false;
            if (!hasAnchor)
            {
                anchorScreenPosition = screenPosition;
                hasAnchor = true;
                anchorJustSet = true;
            }

            var distancePixels = Vector2.Distance(anchorScreenPosition, screenPosition);
            gameController.SetPower(distancePixels / PixelsForFullPower);

            if (!anchorJustSet && mouse.leftButton.wasPressedThisFrame)
            {
                stageView ??= FindAnyObjectByType<MoaiGolfStageView>();
                if (stageView?.LaunchBody != null)
                {
                    gameController.Launch(stageView.LaunchBody);
                }
                hasAnchor = false;
            }
        }
    }
}
