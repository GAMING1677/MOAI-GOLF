using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfHud : MonoBehaviour
    {
        private MoaiGolfGameController gameController;
        private MoaiGolfStageView stageView;

        private void Start()
        {
            gameController = FindAnyObjectByType<MoaiGolfGameController>();
            stageView = FindAnyObjectByType<MoaiGolfStageView>();
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

            if (gameController.Phase == MoaiGolfGamePhase.PowerSelect && GUI.Button(new Rect(36f, 112f, 120f, 36f), "OK"))
            {
                stageView ??= FindAnyObjectByType<MoaiGolfStageView>();
                if (stageView?.LaunchBody != null)
                {
                    gameController.Launch(stageView.LaunchBody);
                }
            }

            if (gameController.Phase == MoaiGolfGamePhase.Result)
            {
                var label = gameController.LastResultSucceeded == true ? "SUCCESS" : "FAILED";
                GUI.Box(new Rect(Screen.width * 0.5f - 110f, 24f, 220f, 56f), label);
            }
        }
    }
}
