#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MoaiGolf
{
    [CustomEditor(typeof(MoaiGolfHud))]
    public sealed class MoaiGolfHudEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var hud = (MoaiGolfHud)target;
            hud.ResolveUiReferencesFromHierarchy();

            if (MoaiGolfHudUiSetupUtility.HasMissingUiReferences(hud))
            {
                EditorGUILayout.HelpBox(
                    "HUD UI の参照が不足しています。「Setup / Rewire HUD UI」で Canvas を作成し、インスペクタへ配線してください。",
                    MessageType.Error
                );

                if (GUILayout.Button("Setup / Rewire HUD UI"))
                {
                    MoaiGolfHudUiSetupUtility.EnsureHudUi(hud);
                }
            }

            DrawDefaultInspector();
        }
    }
}
#endif
