#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MoaiGolf
{
    public static class MoaiGolfStagePrefabUtility
    {
        private const string PrefabRoot = "Assets/Prefabs/Stage";
        private const string SpecRoot = "Assets/Resources/MoaiGolf/Specs";
        private const string PrefabSetPath = "Assets/Resources/MoaiGolfStagePrefabSet.asset";
        private const string WhiteSpritePath = "Assets/Textures/generated/white1x1.png";

        [MenuItem("Moai Golf/Generate Stage Prefabs")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(PrefabRoot);
            Directory.CreateDirectory(SpecRoot);
            Directory.CreateDirectory("Assets/Resources");
            Directory.CreateDirectory("Assets/Textures/generated");

            EnsureWhiteSpriteAsset();
            EnsureSpecAssets();
            var prefabSet = LoadOrCreatePrefabSet();
            prefabSet.launchMoaiPrefab = CreateLaunchMoaiPrefab();
            prefabSet.targetMoaiPrefabs = CreateTargetMoaiPrefabs();
            prefabSet.launchPedestalPrefab = CreateLaunchPedestalPrefab();
            prefabSet.targetPedestalPrefab = CreateTargetPedestalPrefab();
            prefabSet.terrainColliderPrefab = CreateTerrainColliderPrefab();
            prefabSet.successZonePrefab = CreateSuccessZonePrefab();
            prefabSet.golfClubPivotPrefab = CreateGolfClubPivotPrefab();
            prefabSet.aimMarkerPrefab = CreateAimMarkerPrefab();
            prefabSet.backgroundVisualPrefab = CreateBackgroundVisualPrefab();

            EditorUtility.SetDirty(prefabSet);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var stageView = Object.FindAnyObjectByType<MoaiGolfStageView>();
            if (stageView != null)
            {
                PlaceTargetMoaiPoolInScene(stageView, prefabSet);
            }

            Debug.Log("Moai Golf stage prefabs and MoaiGolfStagePrefabSet generated.");
        }

        [MenuItem("Moai Golf/Place Target Moai Pool")]
        public static void PlaceTargetMoaiPoolMenu()
        {
            var stageView = Object.FindAnyObjectByType<MoaiGolfStageView>();
            if (stageView == null)
            {
                Debug.LogError("MoaiGolfStageView not found in the open scene.");
                return;
            }

            var serializedStageView = new SerializedObject(stageView);
            var assignedPrefabSet = serializedStageView.FindProperty("prefabSet").objectReferenceValue as MoaiGolfStagePrefabSet;
            var prefabSet = assignedPrefabSet != null
                ? assignedPrefabSet
                : AssetDatabase.LoadAssetAtPath<MoaiGolfStagePrefabSet>(PrefabSetPath);
            if (prefabSet == null || prefabSet.targetMoaiPrefabs == null || prefabSet.targetMoaiPrefabs.Length < MoaiGolfStageView.SceneTargetMoaiPoolCount)
            {
                Debug.LogError("Target Moai prefabs are missing. Run Moai Golf/Generate Stage Prefabs first.");
                return;
            }

            PlaceTargetMoaiPoolInScene(stageView, prefabSet);
            EditorSceneManager.MarkSceneDirty(stageView.gameObject.scene);
            Debug.Log($"Placed {MoaiGolfStageView.SceneTargetMoaiPoolCount} target Moai pool instances and refreshed serialized references.");
        }

        [MenuItem("Moai Golf/Create Moai Prefab")]
        public static void CreateLegacyMoaiPrefabMenu()
        {
            GenerateAll();
        }

        private static MoaiGolfStagePrefabSet LoadOrCreatePrefabSet()
        {
            var existing = AssetDatabase.LoadAssetAtPath<MoaiGolfStagePrefabSet>(PrefabSetPath);
            if (existing != null)
            {
                return existing;
            }

            var created = ScriptableObject.CreateInstance<MoaiGolfStagePrefabSet>();
            AssetDatabase.CreateAsset(created, PrefabSetPath);
            return created;
        }

        private static void EnsureWhiteSpriteAsset()
        {
            if (File.Exists(WhiteSpritePath))
            {
                return;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            File.WriteAllBytes(WhiteSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(WhiteSpritePath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(WhiteSpritePath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.spritePixelsToUnits = 1f;
            importer.SaveAndReimport();
        }

        private static void EnsureSpecAssets()
        {
            foreach (MoaiGolfMoaiKind kind in System.Enum.GetValues(typeof(MoaiGolfMoaiKind)))
            {
                var path = $"{SpecRoot}/{kind}.asset";
                var spec = MoaiGolfMoaiSpec.Get(kind);
                var asset = AssetDatabase.LoadAssetAtPath<MoaiGolfMoaiSpecAsset>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<MoaiGolfMoaiSpecAsset>();
                    AssetDatabase.CreateAsset(asset, path);
                }

                var serialized = new SerializedObject(asset);
                serialized.FindProperty("kind").enumValueIndex = (int)kind;
                serialized.FindProperty("sprite").objectReferenceValue = MoaiGolfSpriteCatalog.GetPersistedMoai(kind);
                serialized.FindProperty("visualScale").vector2Value = spec.VisualScale;
                serialized.FindProperty("colliderSize").vector2Value = spec.ColliderSize;
                serialized.FindProperty("mass").floatValue = spec.Mass;
                serialized.FindProperty("bounciness").floatValue = spec.Bounciness;
                serialized.FindProperty("friction").floatValue = spec.Friction;
                serialized.FindProperty("feetOffset").floatValue = spec.FeetOffset;
                serialized.FindProperty("sortingOrder").intValue = kind == MoaiGolfMoaiKind.Sunglasses ? 4 : 2;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
        }

        private static GameObject CreateLaunchMoaiPrefab()
        {
            var root = CreateMoaiRoot("Launch Moai", MoaiGolfMoaiKind.Sunglasses, MoaiGolfMoaiRole.Launch);
            root.AddComponent<MoaiGolfBounceSfx>();
            root.AddComponent<MoaiGolfTargetMoaiVoiceSfx>();
            root.AddComponent<MoaiGolfWorldBoundsBounce>();
            root.AddComponent<MoaiGolfLandingJudge>();
            return SavePrefab(root, $"{PrefabRoot}/LaunchMoai.prefab");
        }

        private static GameObject[] CreateTargetMoaiPrefabs()
        {
            var prefabs = new GameObject[MoaiGolfStageView.SceneTargetMoaiPoolCount];
            for (var poolIndex = 0; poolIndex < prefabs.Length; poolIndex++)
            {
                var kind = MoaiGolfStageView.GetTargetMoaiKindForPoolIndex(poolIndex);
                var variantIndex = poolIndex % MoaiGolfStageView.TargetMoaiPerKindCount;
                prefabs[poolIndex] = CreateTargetMoaiPrefab(kind, poolIndex, variantIndex);
            }

            return prefabs;
        }

        private static GameObject CreateTargetMoaiPrefab(MoaiGolfMoaiKind kind, int poolIndex, int variantIndex)
        {
            var root = CreateMoaiRoot(
                $"Target Moai {kind} {variantIndex + 1:00}",
                kind,
                MoaiGolfMoaiRole.Target
            );
            root.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            root.AddComponent<MoaiGolfTargetMoaiMarker>();
            var marker = root.AddComponent<MoaiGolfStageElement>();
            marker.Configure(MoaiGolfStageElementKind.TargetMoai, poolIndex);
            return SavePrefab(root, $"{PrefabRoot}/TargetMoai_{kind}_{variantIndex:00}.prefab");
        }

        public static void PlaceTargetMoaiPoolInScene(MoaiGolfStageView stageView, MoaiGolfStagePrefabSet prefabSet)
        {
            if (stageView == null || prefabSet?.targetMoaiPrefabs == null)
            {
                return;
            }

            RemoveExistingTargetMoais(stageView.transform);
            for (var poolIndex = 0; poolIndex < MoaiGolfStageView.SceneTargetMoaiPoolCount; poolIndex++)
            {
                var prefab = poolIndex < prefabSet.targetMoaiPrefabs.Length
                    ? prefabSet.targetMoaiPrefabs[poolIndex]
                    : null;
                if (prefab == null)
                {
                    Debug.LogError($"Missing target Moai prefab for pool index {poolIndex}.", stageView);
                    continue;
                }

                var kind = MoaiGolfStageView.GetTargetMoaiKindForPoolIndex(poolIndex);
                var variantIndex = poolIndex % MoaiGolfStageView.TargetMoaiPerKindCount;
                var instance = PrefabUtility.InstantiatePrefab(prefab, stageView.transform) as GameObject;
                if (instance == null)
                {
                    continue;
                }

                instance.name = $"Target Moai {kind} {variantIndex + 1:00}";
                instance.transform.position = Vector3.zero;
                var marker = instance.GetComponent<MoaiGolfStageElement>() ?? instance.AddComponent<MoaiGolfStageElement>();
                marker.Configure(MoaiGolfStageElementKind.TargetMoai, poolIndex);
                var entity = instance.GetComponent<MoaiGolfMoaiEntity>();
                entity?.ConfigureTarget(kind, 2);
                instance.SetActive(false);
            }

            stageView.RefreshSerializedSceneReferencesForEditor();
            stageView.RefreshTargetLineupPreview();
            EditorUtility.SetDirty(stageView);
        }

        private static void RemoveExistingTargetMoais(Transform stageRoot)
        {
            var elements = stageRoot.GetComponentsInChildren<MoaiGolfStageElement>(true);
            foreach (var element in elements)
            {
                if (element != null && element.Kind == MoaiGolfStageElementKind.TargetMoai)
                {
                    Object.DestroyImmediate(element.gameObject);
                }
            }
        }

        private static GameObject CreateMoaiRoot(string objectName, MoaiGolfMoaiKind kind, MoaiGolfMoaiRole role)
        {
            var specAsset = AssetDatabase.LoadAssetAtPath<MoaiGolfMoaiSpecAsset>($"{SpecRoot}/{kind}.asset");
            var spec = MoaiGolfMoaiSpec.Get(kind);
            var root = new GameObject(objectName);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = specAsset != null ? specAsset.Sprite : MoaiGolfSpriteCatalog.GetPersistedMoai(kind);
            renderer.sortingOrder = role == MoaiGolfMoaiRole.Launch ? 4 : 2;

            var collider = root.AddComponent<CapsuleCollider2D>();
            collider.size = spec.ColliderSize;
            collider.direction = CapsuleDirection2D.Vertical;

            var body = root.AddComponent<Rigidbody2D>();
            body.mass = spec.Mass;
            body.bodyType = role == MoaiGolfMoaiRole.Launch ? RigidbodyType2D.Dynamic : RigidbodyType2D.Static;

            var entityComponent = root.AddComponent<MoaiGolfMoaiEntity>();
            var serialized = new SerializedObject(entityComponent);
            serialized.FindProperty("specAsset").objectReferenceValue = specAsset;
            serialized.FindProperty("role").enumValueIndex = (int)role;
            serialized.FindProperty("visualRenderer").objectReferenceValue = renderer;
            serialized.FindProperty("capsuleCollider").objectReferenceValue = collider;
            serialized.FindProperty("body").objectReferenceValue = body;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            entityComponent.ApplyKind(kind, role == MoaiGolfMoaiRole.Target ? 2 : 4);
            return root;
        }

        private static GameObject CreateLaunchPedestalPrefab()
        {
            var root = CreateBoxRoot(
                "Launch Pedestal",
                new Vector2(MoaiGolfWorldSettings.LaunchPedestalWidth, MoaiGolfWorldSettings.LaunchPedestalHeight),
                new Color(0.45f, 0.34f, 0.25f, 0.45f),
                true,
                false,
                1
            );
            return SavePrefab(root, $"{PrefabRoot}/LaunchPedestal.prefab");
        }

        private static GameObject CreateTargetPedestalPrefab()
        {
            var root = new GameObject("Target Pedestal Collider");
            var collider = root.AddComponent<PolygonCollider2D>();
            collider.points = new[]
            {
                new Vector2(MoaiGolfStageView.RightPedestalLeftX, MoaiGolfStageView.RightPedestalBaseY),
                new Vector2(MoaiGolfStageView.RightPedestalRightX, MoaiGolfStageView.RightPedestalBaseY),
                new Vector2(MoaiGolfStageView.RightPedestalRightX, MoaiGolfStageView.TargetPedestalTopSurfacePoints[^1].y),
                MoaiGolfStageView.TargetPedestalTopSurfacePoints[7],
                MoaiGolfStageView.TargetPedestalTopSurfacePoints[6],
                MoaiGolfStageView.TargetPedestalTopSurfacePoints[5],
                MoaiGolfStageView.TargetPedestalTopSurfacePoints[4],
                MoaiGolfStageView.TargetPedestalTopSurfacePoints[3],
                MoaiGolfStageView.TargetPedestalTopSurfacePoints[2],
                MoaiGolfStageView.TargetPedestalTopSurfacePoints[1],
                MoaiGolfStageView.TargetPedestalTopSurfacePoints[0]
            };
            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            return SavePrefab(root, $"{PrefabRoot}/TargetPedestal.prefab");
        }

        private static GameObject CreateTerrainColliderPrefab()
        {
            var root = new GameObject("Terrain Black Line Collider");
            var collider = root.AddComponent<EdgeCollider2D>();
            collider.points = MoaiGolfTerrainProfile.ColliderPoints;
            collider.edgeRadius = 0.035f;
            return SavePrefab(root, $"{PrefabRoot}/TerrainCollider.prefab");
        }

        private static GameObject CreateSuccessZonePrefab()
        {
            var root = CreateBoxRoot(
                "Success Zone Trigger",
                new Vector2(1.35f, 2.3f),
                new Color(1f, 0.1f, 0.08f, 0.32f),
                true,
                true,
                3
            );
            return SavePrefab(root, $"{PrefabRoot}/SuccessZone.prefab");
        }

        private static GameObject CreateGolfClubPivotPrefab()
        {
            var spriteScale = MoaiGolfLaunchAnimator.ClubSpriteScale;
            var anchorLocal = new Vector2(
                MoaiGolfLaunchAnimator.ClubAnchorLocalXPixels,
                MoaiGolfLaunchAnimator.ClubAnchorLocalYPixels
            ) / MoaiGolfWorldSettings.PixelsPerUnit * spriteScale;

            var pivot = new GameObject("Golf Club Pivot");
            pivot.transform.rotation = Quaternion.Euler(0f, 0f, MoaiGolfLaunchAnimator.ClubWindupAngleDeg);

            var visual = new GameObject("Golf Club Visual");
            visual.transform.SetParent(pivot.transform, false);
            visual.transform.localPosition = new Vector3(-anchorLocal.x, -anchorLocal.y, 0f);
            visual.transform.localScale = new Vector3(spriteScale, spriteScale, 1f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = MoaiGolfSpriteCatalog.GolfClub;
            renderer.sortingOrder = 3;
            return SavePrefab(pivot, $"{PrefabRoot}/GolfClubPivot.prefab");
        }

        private static GameObject CreateAimMarkerPrefab()
        {
            var root = new GameObject("AimMarker");
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = MoaiGolfSpriteCatalog.GetPersistedHere();
            renderer.sortingOrder = 5;
            return SavePrefab(root, $"{PrefabRoot}/AimMarker.prefab");
        }

        private static GameObject CreateBackgroundVisualPrefab()
        {
            MoaiGolfBackgroundBuilder.EnsureGeneratedAsset();
            var root = new GameObject("BackgroundVisual");
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = MoaiGolfSpriteCatalog.GetPersistedBackground();
            renderer.sortingOrder = -10;
            return SavePrefab(root, $"{PrefabRoot}/BackgroundVisual.prefab");
        }

        private static GameObject CreateBoxRoot(string objectName, Vector2 size, Color color, bool addCollider, bool isTrigger, int sortingOrder)
        {
            var root = new GameObject(objectName);
            root.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = MoaiGolfSpriteCatalog.GetPersistedWhite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            if (addCollider)
            {
                var collider = root.AddComponent<BoxCollider2D>();
                collider.isTrigger = isTrigger;
            }

            return root;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? PrefabRoot);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
#endif
