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
            var bootstrap = AddIfMissing<MoaiGolfBootstrap>(gameRoot);
            AddIfMissing<MoaiGolfRunState>(gameRoot);
            AddIfMissing<MoaiGolfGameController>(gameRoot);
            AddIfMissing<MoaiGolfMouseAngleInput>(gameRoot);
            AddIfMissing<MoaiGolfMousePowerInput>(gameRoot);
            AddIfMissing<MoaiGolfHud>(gameRoot);
            AddIfMissing<MoaiGolfGuideOverlay>(gameRoot);
            var launchAnimator = AddIfMissing<MoaiGolfLaunchAnimator>(gameRoot);
            AddIfMissing<MoaiGolfSeController>(gameRoot);
            AddIfMissing<MoaiGolfCameraController>(gameRoot);
            EnsureLaunchAnimatorAudioSource(launchAnimator);

            var existingBgm = Object.FindAnyObjectByType<MoaiGolfBgmController>();
            if (existingBgm == null)
            {
                var bgmObject = new GameObject("MoaiGolfBgm");
                bgmObject.AddComponent<AudioSource>();
                existingBgm = bgmObject.AddComponent<MoaiGolfBgmController>();
            }

            var stageRoot = new GameObject("MoaiGolfStage");
            var stageView = stageRoot.AddComponent<MoaiGolfStageView>();
            var stageSerialized = new SerializedObject(stageView);
            stageSerialized.FindProperty("prefabSet").objectReferenceValue = prefabSet;
            stageSerialized.FindProperty("useScenePlacedElements").boolValue = true;
            stageSerialized.ApplyModifiedPropertiesWithoutUndo();

            PlaceStagePrefabs(stageRoot.transform, prefabSet);
            stageView.RefreshSerializedSceneReferencesForEditor();
            ConfigureScenePreview(stageView);

            var hud = gameRoot.GetComponent<MoaiGolfHud>();
            MoaiGolfHudUiSetupUtility.EnsureHudUi(hud);

            var mainCamera = ConfigureMainCamera();
            bootstrap.RefreshSerializedReferencesForEditor(mainCamera, stageView, existingBgm);
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

            InstantiateMarked(prefabSet.backgroundVisualPrefab, stageRoot, MoaiGolfStageElementKind.Background, 0, ResolveBackgroundScenePosition(prefabSet.backgroundVisualPrefab), Vector3.one);
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

        }

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
                        var kind = MoaiGolfStageView.GetTargetMoaiKindForPoolIndex(element.SlotIndex);
                        var variantIndex = element.SlotIndex % MoaiGolfStageView.TargetMoaiPerKindCount;
                        var entity = element.GetComponent<MoaiGolfMoaiEntity>() ?? element.gameObject.AddComponent<MoaiGolfMoaiEntity>();
                        entity.ConfigureTarget(kind, 2);
                        element.gameObject.name = $"Target Moai {kind} {variantIndex + 1:00}";
                        break;
                    }
                }
            }
        }

        private static Vector3 ResolveBackgroundScenePosition(GameObject prefab)
        {
            if (prefab == null)
            {
                return Vector3.zero;
            }

            var renderer = prefab.GetComponent<SpriteRenderer>();
            return MoaiGolfStageView.ResolveBackgroundVisualPosition(renderer != null ? renderer.sprite : null);
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

        private static Camera ConfigureMainCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return null;
            }

            camera.orthographic = true;
            camera.orthographicSize = MoaiGolfWorldSettings.CameraOrthographicSize;
            camera.transform.position = new Vector3(
                MoaiGolfWorldSettings.CameraCenterX,
                MoaiGolfWorldSettings.CameraCenterY,
                MoaiGolfWorldSettings.CameraZ
            );
            camera.backgroundColor = new Color(0.19f, 0.3f, 0.47f);
            return camera;
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

        private static void EnsureLaunchAnimatorAudioSource(MoaiGolfLaunchAnimator launchAnimator)
        {
            if (launchAnimator == null)
            {
                return;
            }

            var audioSource = launchAnimator.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = launchAnimator.gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 0f;
            }

            var serialized = new SerializedObject(launchAnimator);
            serialized.FindProperty("sfxAudioSource").objectReferenceValue = audioSource;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
