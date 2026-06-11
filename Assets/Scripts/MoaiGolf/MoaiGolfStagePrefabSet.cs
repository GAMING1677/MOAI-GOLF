using UnityEngine;

namespace MoaiGolf
{
    [CreateAssetMenu(fileName = "MoaiGolfStagePrefabSet", menuName = "Moai Golf/Stage Prefab Set")]
    public sealed class MoaiGolfStagePrefabSet : ScriptableObject
    {
        public const string DefaultResourcePath = "MoaiGolfStagePrefabSet";

        public GameObject launchMoaiPrefab;
        public GameObject[] targetMoaiPrefabs = System.Array.Empty<GameObject>();
        public GameObject launchPedestalPrefab;
        public GameObject targetPedestalPrefab;
        public GameObject terrainColliderPrefab;
        public GameObject successZonePrefab;
        public GameObject golfClubPivotPrefab;
        public GameObject aimMarkerPrefab;
        public GameObject backgroundVisualPrefab;

        public static MoaiGolfStagePrefabSet LoadDefault()
        {
            return Resources.Load<MoaiGolfStagePrefabSet>(DefaultResourcePath);
        }
    }
}
