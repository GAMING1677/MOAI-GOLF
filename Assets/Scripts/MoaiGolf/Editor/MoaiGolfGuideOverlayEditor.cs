#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MoaiGolf
{
    [CustomEditor(typeof(MoaiGolfGuideOverlay))]
    public sealed class MoaiGolfGuideOverlayEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var guideOverlay = (MoaiGolfGuideOverlay)target;
            if (MoaiGolfGuideOverlaySetupUtility.HasMissingGuideReferences(guideOverlay))
            {
                EditorGUILayout.HelpBox(
                    "ガイド表示の参照が不足しています。「Setup / Rewire Guide Overlay」でシーン内オブジェクトを作成し、インスペクタへ配線してください。",
                    MessageType.Error
                );

                if (GUILayout.Button("Setup / Rewire Guide Overlay"))
                {
                    MoaiGolfGuideOverlaySetupUtility.EnsureGuideVisuals(guideOverlay);
                }
            }

            DrawDefaultInspector();
        }
    }
}
#endif
