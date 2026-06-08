using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoaiGolf
{
    public sealed class MoaiGolfStageView : MonoBehaviour
    {
        private const float RightPedestalLeftX = 29.0f;
        private const float RightPedestalRightX = 35.28f;
        private const float RightPedestalTopY = 7.2f;
        private const float RightPedestalBaseY = 4.2f;

        private static Sprite whiteSprite;

        public Rigidbody2D LaunchBody { get; private set; }

        public void Build(MoaiGolfRunState runState)
        {
            ClearExistingStage();

            var stage = runState.Stage;
            CreatePixelPerfectSprite("Background Visual", MoaiGolfSpriteCatalog.Background, Vector2.zero, Color.white, -10);
            CreateTerrainCollider();
            var launchPedestalCenter = runState.LaunchPedestalCenter;
            CreateSprite("Golf Club Visual", MoaiGolfSpriteCatalog.GolfClub, launchPedestalCenter + new Vector2(-1.05f, -0.18f), new Vector2(0.55f, 0.55f), Color.white, 3);
            CreateBox("Launch Pedestal Collider", launchPedestalCenter, new Vector2(MoaiGolfWorldSettings.LaunchPedestalWidth, MoaiGolfWorldSettings.LaunchPedestalHeight), new Color(0.45f, 0.34f, 0.25f, 0.45f), true, false, 1);
            CreateTargetPedestalCollider();
            CreateTargetMoaiOnPedestal(MoaiGolfMoaiKind.Sunglasses, 29.6f, 2);
            CreateTargetMoaiOnPedestal(MoaiGolfMoaiKind.Ribbon, 30.7f, 2);
            CreateTargetMoaiOnPedestal(MoaiGolfMoaiKind.Snowman, 31.8f, 2);
            CreateTargetMoaiOnPedestal(MoaiGolfMoaiKind.Sunglasses, 33.7f, 2);
            CreateTargetMoaiOnPedestal(MoaiGolfMoaiKind.Macho, 34.8f, 2);
            CreateBox("Success Zone Trigger", stage.SuccessZone.center, stage.SuccessZone.size, new Color(1f, 0.1f, 0.08f, 0.32f), true, true, 3);
            CreateAimMarker(stage);
            CreateLaunchMoai(runState);
        }

        private void ClearExistingStage()
        {
            for (var childIndex = transform.childCount - 1; childIndex >= 0; childIndex--)
            {
                Destroy(transform.GetChild(childIndex).gameObject);
            }
        }

        private GameObject CreateBox(string objectName, Vector2 center, Vector2 size, Color color, bool addCollider, bool isTrigger, int sortingOrder)
        {
            var box = new GameObject(objectName);
            box.transform.SetParent(transform);
            box.transform.position = new Vector3(center.x, center.y, 0f);
            box.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = box.AddComponent<SpriteRenderer>();
            renderer.sprite = GetWhiteSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            if (addCollider)
            {
                var collider = box.AddComponent<BoxCollider2D>();
                collider.isTrigger = isTrigger;
            }

            return box;
        }

        private void CreateTerrainCollider()
        {
            var terrainObject = new GameObject("Terrain Black Line Collider");
            terrainObject.transform.SetParent(transform);
            terrainObject.transform.position = Vector3.zero;

            var collider = terrainObject.AddComponent<EdgeCollider2D>();
            collider.points = MoaiGolfTerrainProfile.ColliderPoints;
            collider.edgeRadius = 0.035f;
        }

        private GameObject CreateSprite(string objectName, Sprite sprite, Vector2 bottomLeft, Vector2 scale, Color color, int sortingOrder)
        {
            var spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(transform);
            spriteObject.transform.position = new Vector3(bottomLeft.x, bottomLeft.y, 0f);
            spriteObject.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            var renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : GetWhiteSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return spriteObject;
        }

        private GameObject CreateSpriteFitted(string objectName, Sprite sprite, Vector2 bottomLeft, Vector2 worldSize, Color color, int sortingOrder)
        {
            var spriteObject = CreateSprite(objectName, sprite, bottomLeft, Vector2.one, color, sortingOrder);
            var renderer = spriteObject.GetComponent<SpriteRenderer>();
            if (renderer.sprite == null)
            {
                spriteObject.transform.localScale = new Vector3(worldSize.x, worldSize.y, 1f);
                return spriteObject;
            }

            var spriteSize = renderer.sprite.bounds.size;
            spriteObject.transform.localScale = new Vector3(worldSize.x / spriteSize.x, worldSize.y / spriteSize.y, 1f);
            return spriteObject;
        }

        private GameObject CreatePixelPerfectSprite(string objectName, Sprite sprite, Vector2 bottomLeft, Color color, int sortingOrder)
        {
            return CreateSprite(objectName, sprite, bottomLeft, Vector2.one, color, sortingOrder);
        }

        private GameObject CreateCenteredSprite(string objectName, Sprite sprite, Vector2 center, Vector2 scale, Color color, int sortingOrder)
        {
            var spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(transform);
            spriteObject.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            var renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : GetWhiteSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            var boundsCenter = renderer.sprite.bounds.center;
            spriteObject.transform.position = new Vector3(center.x - boundsCenter.x * scale.x, center.y - boundsCenter.y * scale.y, 0f);
            return spriteObject;
        }

        private GameObject CreateSpriteChild(string objectName, Transform parent, Sprite sprite, Vector2 localBottomLeft, Vector2 scale, Color color, int sortingOrder)
        {
            var spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(parent);
            spriteObject.transform.localPosition = new Vector3(localBottomLeft.x, localBottomLeft.y, 0f);
            spriteObject.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            var renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : GetWhiteSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return spriteObject;
        }

        private GameObject CreateCenteredSpriteChild(string objectName, Transform parent, Sprite sprite, Vector2 scale, Color color, int sortingOrder)
        {
            var spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(parent);
            spriteObject.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            var renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : GetWhiteSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            var boundsCenter = renderer.sprite.bounds.center;
            spriteObject.transform.localPosition = new Vector3(-boundsCenter.x * scale.x, -boundsCenter.y * scale.y, 0f);
            return spriteObject;
        }

        private void CreateAimMarker(MoaiGolfStageDefinition stage)
        {
            var zone = stage.SuccessZone;
            var hereSprite = MoaiGolfSpriteCatalog.Here;
            var spriteSize = hereSprite.bounds.size;

            // here.png の左側の破線枠（テクスチャ内で約 1.3 x 1.1 ワールド単位、
            // スプライト中心から左に 0.25 ずれた位置にある）が成功ゾーンを囲うように
            // 非等倍スケールで引き伸ばし、右に寄せて配置する。
            var dashedBoxSize = new Vector2(1.3f, 1.1f);
            var dashedBoxOffsetFromCenter = new Vector2(-0.25f, 0f);
            var padding = new Vector2(0.6f, 0.5f);

            var scale = new Vector2(
                (zone.width + padding.x) / dashedBoxSize.x,
                (zone.height + padding.y) / dashedBoxSize.y
            );

            var center = zone.center - new Vector2(
                dashedBoxOffsetFromCenter.x * scale.x,
                dashedBoxOffsetFromCenter.y * scale.y
            );

            CreateCenteredSprite("Here Label Visual", hereSprite, center, scale, Color.white, 5);
        }

        private void CreateTargetMoai(MoaiGolfMoaiKind kind, float x, float groundY, int sortingOrder)
        {
            var spec = MoaiGolfMoaiSpec.Get(kind);
            var sprite = MoaiGolfSpriteCatalog.GetMoai(kind);
            var visualHalfHeight = sprite.bounds.size.y * spec.VisualScale.y * 0.5f;
            var centerY = groundY + visualHalfHeight + 0.02f;
            var moai = new GameObject($"Target Moai {kind}");
            moai.transform.SetParent(transform);
            moai.transform.position = new Vector2(x, centerY);
            CreateCenteredSpriteChild("Target Moai Visual", moai.transform, sprite, spec.VisualScale, Color.white, sortingOrder);

            var collider = moai.AddComponent<CapsuleCollider2D>();
            collider.size = spec.ColliderSize;
            collider.direction = CapsuleDirection2D.Vertical;

            var body = moai.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
        }

        private void CreateTargetMoaiOnPedestal(MoaiGolfMoaiKind kind, float x, int sortingOrder)
        {
            CreateTargetMoai(kind, x, RightPedestalTopY, sortingOrder);
        }

        private void CreateTargetPedestalCollider()
        {
            var centerX = (RightPedestalLeftX + RightPedestalRightX) * 0.5f;
            var centerY = (RightPedestalBaseY + RightPedestalTopY) * 0.5f;
            var width = RightPedestalRightX - RightPedestalLeftX;
            var height = RightPedestalTopY - RightPedestalBaseY;

            var pedestal = new GameObject("Target Pedestal Collider");
            pedestal.transform.SetParent(transform);
            pedestal.transform.position = new Vector3(centerX, centerY, 0f);

            var collider = pedestal.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(width, height);
            collider.isTrigger = false;

            var body = pedestal.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
        }

        private void CreateLaunchMoai(MoaiGolfRunState runState)
        {
            var spec = MoaiGolfMoaiSpec.Get(runState.LaunchMoaiKind);
            var moai = new GameObject("Launch Moai Collider");
            moai.transform.SetParent(transform);
            moai.transform.position = runState.LaunchPosition;
            CreateCenteredSpriteChild("Launch Moai Visual", moai.transform, MoaiGolfSpriteCatalog.GetMoai(runState.LaunchMoaiKind), spec.VisualScale, Color.white, 4);

            var collider = moai.AddComponent<CapsuleCollider2D>();
            collider.size = spec.ColliderSize;
            collider.direction = CapsuleDirection2D.Vertical;

            var material = new PhysicsMaterial2D($"{runState.LaunchMoaiKind} Material")
            {
                bounciness = spec.Bounciness,
                friction = spec.Friction
            };
            collider.sharedMaterial = material;

            LaunchBody = moai.AddComponent<Rigidbody2D>();
            LaunchBody.mass = spec.Mass;
            LaunchBody.bodyType = RigidbodyType2D.Dynamic;
            LaunchBody.Sleep();
            moai.AddComponent<MoaiGolfWorldBoundsBounce>();
            moai.AddComponent<MoaiGolfLandingJudge>();
        }

        private static Sprite GetWhiteSprite()
        {
            if (whiteSprite != null)
            {
                return whiteSprite;
            }

            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return whiteSprite;
        }

        private static Color MoaiColor(MoaiGolfMoaiKind kind)
        {
            return kind switch
            {
                MoaiGolfMoaiKind.Sunglasses => new Color(0.25f, 0.29f, 0.34f),
                MoaiGolfMoaiKind.Ribbon => new Color(0.86f, 0.35f, 0.55f),
                MoaiGolfMoaiKind.Macho => new Color(0.48f, 0.38f, 0.31f),
                MoaiGolfMoaiKind.Snowman => new Color(0.86f, 0.93f, 0.96f),
                _ => Color.gray
            };
        }
    }

#if UNITY_EDITOR
    public static class MoaiGolfMoaiPrefabUtility
    {
        private const string PrefabPath = "Assets/Prefabs/Moai.prefab";

        [InitializeOnLoadMethod]
        private static void EnsurePrefabAssetOnLoad()
        {
            if (!System.IO.File.Exists(PrefabPath))
            {
                CreatePrefabAsset();
            }
        }

        [MenuItem("Moai Golf/Create Moai Prefab")]
        public static void CreatePrefabAsset()
        {
            var spec = MoaiGolfMoaiSpec.Get(MoaiGolfMoaiKind.Sunglasses);
            var root = new GameObject("Moai");

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform);
            visual.transform.localScale = new Vector3(spec.VisualScale.x, spec.VisualScale.y, 1f);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = MoaiGolfSpriteCatalog.GetMoai(MoaiGolfMoaiKind.Sunglasses);
            renderer.sortingOrder = 4;
            var visualSize = renderer.sprite != null ? renderer.sprite.bounds.size : new Vector3(1f, 1f, 0f);
            visual.transform.localPosition = new Vector3(-visualSize.x * spec.VisualScale.x * 0.5f, -visualSize.y * spec.VisualScale.y * 0.5f, 0f);

            var collider = root.AddComponent<CapsuleCollider2D>();
            collider.size = spec.ColliderSize;
            collider.direction = CapsuleDirection2D.Vertical;

            var body = root.AddComponent<Rigidbody2D>();
            body.mass = spec.Mass;
            body.bodyType = RigidbodyType2D.Dynamic;

            System.IO.Directory.CreateDirectory("Assets/Prefabs");
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
        }
    }
#endif
}
