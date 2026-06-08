using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfHud : MonoBehaviour
    {
        private MoaiGolfGameController gameController;
        private MoaiGolfRunState runState;
        private MoaiGolfStageView stageView;
        private MoaiGolfCameraController cameraController;

        private void Start()
        {
            gameController = FindAnyObjectByType<MoaiGolfGameController>();
            runState = FindAnyObjectByType<MoaiGolfRunState>();
            stageView = FindAnyObjectByType<MoaiGolfStageView>();
            cameraController = FindAnyObjectByType<MoaiGolfCameraController>();
        }

        private void OnGUI()
        {
            if (gameController == null)
            {
                return;
            }

            GUI.Box(new Rect(20f, 20f, 300f, 120f), string.Empty);
            GUI.Label(new Rect(36f, 34f, 260f, 24f), $"Phase: {gameController.Phase}");
            GUI.Label(new Rect(36f, 60f, 260f, 24f), $"Angle: {gameController.AngleDegrees:0} deg");
            GUI.Label(new Rect(36f, 86f, 260f, 24f), $"Power: {gameController.Power01 * 100f:0}%");

            if (gameController.Phase == MoaiGolfGamePhase.Result)
            {
                var label = gameController.LastResultSucceeded == true ? "SUCCESS" : "FAILED";
                GUI.Box(new Rect(Screen.width * 0.5f - 110f, 24f, 220f, 56f), label);
            }

            if (GUI.Button(new Rect(Screen.width - 140f, 20f, 120f, 40f), "Retry"))
            {
                Retry();
            }
        }

        private void Retry()
        {
            runState ??= FindAnyObjectByType<MoaiGolfRunState>();
            stageView ??= FindAnyObjectByType<MoaiGolfStageView>();
            cameraController ??= FindAnyObjectByType<MoaiGolfCameraController>();
            if (runState == null || stageView == null)
            {
                return;
            }

            runState.RetryCurrentRun();
            gameController.ResetForRetry();
            stageView.Build(runState);
            cameraController?.ResetToInitial();
        }
    }
}
