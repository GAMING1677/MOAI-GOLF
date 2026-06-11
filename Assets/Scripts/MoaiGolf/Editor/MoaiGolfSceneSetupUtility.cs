#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoaiGolf
{
    public static class MoaiGolfSceneSetupUtility
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string PrefabSetPath = "Assets/Resources/MoaiGolfStagePrefabSet.asset";

        public static void RebuildSceneLayout()
        {
            MoaiGolfStagePrefabUtility.GenerateAll();
            SetupSampleScene();
        }

        [MenuItem("Moai Golf/Rebuild Scene Layout")]
        public static void RebuildSceneLayoutMenu()
        {
            RebuildSceneLayout();
        }

        [MenuItem("Moai Golf/Refresh Target Moai Positions")]
        public static void RefreshTargetMoaiPositionsMenu()
        {
            var stageView = Object.FindAnyObjectByType<MoaiGolfStageView>();
            if (stageView == null)
            {
                Debug.LogError("MoaiGolfStageView not found in the open scene.");
                return;
            }

            stageView.RefreshTargetLineupPreview();
            ConfigureScenePreview(stageView);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Target moai positions refreshed from success zone and pedestal colliders.");
        }

        [MenuItem("Moai Golf/Setup Sample Scene")]
        public static void SetupSampleScene()
        {
            if (!System.IO.File.Exists(SampleScenePath))
            {
                Debug.LogError($"Scene not found: {SampleScenePath}");
                return;
            }

            var scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            var prefabSet = AssetDatabase.LoadAssetAtPath<MoaiGolfStagePrefabSet>(PrefabSetPath);
            if (prefabSet == null)
            {
                MoaiGolfStagePrefabUtility.GenerateAll();
                prefabSet = AssetDatabase.LoadAssetAtPath<MoaiGolfStagePrefabSet>(PrefabSetPath);
            }

            RemoveExisting("MoaiGolfGameRoot");
            RemoveExisting("MoaiGolfStage");

            var gameRoot = new GameObject("MoaiGolfGameRoot");
            AddIfMissing<MoaiGolfBootstrap>(gameRoot);
            AddIfMissing<MoaiGolfRunState>(gameRoot);
            AddIfMissing<MoaiGolfGameController>(gameRoot);
            AddIfMissing<MoaiGolfMouseAngleInput>(gameRoot);
            AddIfMissing<MoaiGolfMousePowerInput>(gameRoot);
            AddIfMissing<MoaiGolfHud>(gameRoot);
            AddIfMissing<MoaiGolfGuideOverlay>(gameRoot);
            AddIfMissing<MoaiGolfLaunchAnimator>(gameRoot);
            AddIfMissing<MoaiGolfSeController>(gameRoot);
            AddIfMissing<MoaiGolfCameraController>(gameRoot);

            var existingBgm = Object.FindAnyObjectByType<MoaiGolfBgmController>();
            if (existingBgm == null)
            {
                var bgmObject = new GameObject("MoaiGolfBgm");
                bgmObject.AddComponent<AudioSource>();
                bgmObject.AddComponent<MoaiGolfBgmController>();
            }

            var stageRoot = new GameObject("MoaiGolfStage");
            var stageView = stageRoot.AddComponent<MoaiGolfStageView>();
            var stageSerialized = new SerializedObject(stageView);
            stageSerialized.FindProperty("prefabSet").objectReferenceValue = prefabSet;
            stageSerialized.FindProperty("useScenePlacedElements").boolValue = true;
            stageSerialized.ApplyModifiedPropertiesWithoutUndo();

            PlaceStagePrefabs(stageRoot.transform, prefabSet);
            ConfigureScenePreview(stageView);

            ConfigureMainCamera();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Moai Golf sample scene setup complete.");
        }

        private static void PlaceStagePrefabs(Transform stageRoot, MoaiGolfStagePrefabSet prefabSet)
        {
            var stage = MoaiGolfStageDefinition.CreateFirstStage();
            var previewLaunchX = stage.LaunchPosition.x;
            var terrainY = MoaiGolfTerrainProfile.GetY(previewLaunchX);
            var launchPedestalCenter = new Vector2(
                previewLaunchX,
                terrainY + MoaiGolfWorldSettings.LaunchPedestalHeight * 0.5f
            );
            var launchSpec = MoaiGolfMoaiSpec.Get(MoaiGolfMoaiKind.Sunglasses);
            var launchPosition = launchSpec.GetBodyCenterFromVisualFeet(
                new Vector2(previewLaunchX, terrainY + MoaiGolfWorldSettings.LaunchPedestalHeight + 0.02f)
            );

            InstantiateMarked(prefabSet.backgroundVisualPrefab, stageRoot, MoaiGolfStageElementKind.Background, 0, Vector3.zero, Vector3.one);
            InstantiateMarked(prefabSet.terrainColliderPrefab, stageRoot, MoaiGolfStageElementKind.Terrain, 0, Vector3.zero, Vector3.one);
            InstantiateMarked(
                prefabSet.launchPedestalPrefab,
                stageRoot,
                MoaiGolfStageElementKind.LaunchPedestal,
                0,
                new Vector3(launchPedestalCenter.x, launchPedestalCenter.y, 0f),
                Vector3.one
            );
            InstantiateMarked(prefabSet.targetPedestalPrefab, stageRoot, MoaiGolfStageElementKind.TargetPedestal, 0, Vector3.zero, Vector3.one);
            InstantiateMarked(
                prefabSet.successZonePrefab,
                stageRoot,
                MoaiGolfStageElementKind.SuccessZone,
                0,
                new Vector3(stage.SuccessZone.center.x, stage.SuccessZone.center.y, 0f),
                new Vector3(stage.SuccessZone.width, stage.SuccessZone.height, 1f)
            );
            InstantiateMarked(prefabSet.aimMarkerPrefab, stageRoot, MoaiGolfStageElementKind.AimMarker, 0, (Vector3)stage.SuccessZone.center, Vector3.one);

            var clubPivotPos = launchPosition + MoaiGolfLaunchAnimator.ClubAnchorOffsetFromMoai;
            var club = InstantiateMarked(
                prefabSet.golfClubPivotPrefab,
                stageRoot,
                MoaiGolfStageElementKind.GolfClubPivot,
                0,
                new Vector3(clubPivotPos.x, clubPivotPos.y, 0f),
                Vector3.one
            );
            if (club != null)
            {
                club.transform.rotation = Quaternion.Euler(0f, 0f, MoaiGolfLaunchAnimator.ClubWindupAngleDeg);
            }

            InstantiateMarked(
                prefabSet.launchMoaiPrefab,
                stageRoot,
                MoaiGolfStageElementKind.LaunchMoai,
                0,
                new Vector3(launchPosition.x, launchPosition.y, 0f),
                Vector3.one
            );

            var targetPositions = BuildPreviewTargetPositions(stage);
            for (var index = 0; index < targetPositions.Count; index++)
            {
                InstantiateMarked(
                    prefabSet.targetMoaiPrefab,
                    stageRoot,
                    MoaiGolfStageElementKind.TargetMoai,
                    index,
                    new Vector3(targetPositions[index].x, targetPositions[index].y, 0f),
                    Vector3.one
                );
            }
        }

        private static readonly MoaiGolfMoaiKind[] PreviewTargetLineup =
        {
            MoaiGolfMoaiKind.Sunglasses,
            MoaiGolfMoaiKind.Ribbon,
            MoaiGolfMoaiKind.Macho,
            MoaiGolfMoaiKind.Sunglasses,
            MoaiGolfMoaiKind.Snowman,
        };

        private static void ConfigureScenePreview(MoaiGolfStageView stageView)
        {
            foreach (var element in stageView.GetComponentsInChildren<MoaiGolfStageElement>(true))
            {
                switch (element.Kind)
                {
                    case MoaiGolfStageElementKind.LaunchMoai:
                    {
                        var entity = element.GetComponent<MoaiGolfMoaiEntity>() ?? element.gameObject.AddComponent<MoaiGolfMoaiEntity>();
                        entity.ConfigureLaunch(MoaiGolfMoaiKind.Sunglasses);
                        element.gameObject.name = "Launch Moai";
                        break;
                    }
                    case MoaiGolfStageElementKind.TargetMoai:
                    {
                        var kindIndex = Mathf.Clamp(element.SlotIndex, 0, PreviewTargetLineup.Length - 1);
                        var kind = PreviewTargetLineup[kindIndex];
                        var entity = element.GetComponent<MoaiGolfMoaiEntity>() ?? element.gameObject.AddComponent<MoaiGolfMoaiEntity>();
                        entity.ConfigureTarget(kind, 2);
                        element.gameObject.name = $"Target Moai {kind}";
                        break;
                    }
                }
            }
        }

        private static List<Vector2> BuildPreviewTargetPositions(MoaiGolfStageDefinition stage)
        {
            var kinds = PreviewTargetLineup;
            var rimPoints = MoaiGolfStageView.TargetPedestalTopSurfacePoints;
            var xs = MoaiGolfStageView.BuildTargetLineupXPositions(kinds, rimPoints[0].x, rimPoints[^1].x);

            var positions = new List<Vector2>();
            for (var index = 0; index < kinds.Length && index < xs.Count; index++)
            {
                var rootX = xs[index];
                var visualFeetY = GetTargetPedestalTopY(rootX) + MoaiGolfStageView.TargetMoaiVisualFeetLift;
                var rootY = MoaiGolfStageView.ResolveTargetMoaiRootY(kinds[index], visualFeetY);
                positions.Add(new Vector2(rootX, rootY));
            }

            return positions;
        }

        private static float GetTargetPedestalTopY(float x)
        {
            var points = MoaiGolfStageView.TargetPedestalTopSurfacePoints;
            if (x <= points[0].x)
            {
                return points[0].y;
            }

            for (var index = 1; index < points.Length; index++)
            {
                var previous = points[index - 1];
                var next = points[index];
                if (x > next.x)
                {
                    continue;
                }

                var t = Mathf.InverseLerp(previous.x, next.x, x);
                return Mathf.Lerp(previous.y, next.y, t);
            }

            return points[^1].y;
        }

        private static GameObject InstantiateMarked(
            GameObject prefab,
            Transform parent,
            MoaiGolfStageElementKind kind,
            int slotIndex,
            Vector3 position,
            Vector3 scale
        )
        {
            if (prefab == null)
            {
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
            {
                return null;
            }

            instance.transform.position = position;
            instance.transform.localScale = scale;
            var marker = instance.GetComponent<MoaiGolfStageElement>() ?? instance.AddComponent<MoaiGolfStageElement>();
            marker.Configure(kind, slotIndex);
            return instance;
        }

        private static void ConfigureMainCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.orthographic = true;
            camera.orthographicSize = MoaiGolfWorldSettings.CameraOrthographicSize;
            camera.transform.position = new Vector3(
                MoaiGolfWorldSettings.CameraCenterX,
                MoaiGolfWorldSettings.CameraCenterY,
                MoaiGolfWorldSettings.CameraZ
            );
            camera.backgroundColor = new Color(0.19f, 0.3f, 0.47f);
        }

        private static void RemoveExisting(string objectName)
        {
            var existing = GameObject.Find(objectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        private static T AddIfMissing<T>(GameObject host) where T : Component
        {
            return host.GetComponent<T>() ?? host.AddComponent<T>();
        }
    }
}
#endif
