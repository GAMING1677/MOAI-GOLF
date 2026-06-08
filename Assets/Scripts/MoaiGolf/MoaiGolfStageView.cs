using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoaiGolf
{
    public sealed class MoaiGolfStageView : MonoBehaviour
    {
        // 背景画像に描かれている台座本体の左端 x にあわせる
        public const float RightPedestalLeftX = 26.7f;
        public const float RightPedestalRightX = 35.28f;
        public const float RightPedestalTopY = 7.2f;
        // ティキ面が描かれている下段部分まで当たり判定を伸ばすため、地面付近まで下げる
        public const float RightPedestalBaseY = 3.0f;
        // 背景の手描き台座に沿わせるため、見た目の足元だけ少し持ち上げる
        private const float TargetMoaiVisualFeetLift = 0.08f;

        private static readonly Vector2[] TargetPedestalTopSurfacePoints =
        {
            new Vector2(26.7f, 6.95f),
            new Vector2(27.2f, 7.02f),
            new Vector2(28.5f, 7.1f),
            new Vector2(29.3f, 7.18f),
            new Vector2(30.0f, 7.65f),
            new Vector2(31.2f, 7.76f),
            new Vector2(32.4f, 7.86f),
            new Vector2(34.2f, 7.96f),
            new Vector2(35.28f, 8.02f)
        };

        private static Sprite whiteSprite;

        public Rigidbody2D LaunchBody { get; private set; }
        public Transform LaunchVisual { get; private set; }
        public Transform GolfClubPivot { get; private set; }
        public Vector2 LaunchVisualFocusPosition
        {
            get
            {
                if (LaunchVisual == null)
                {
                    return LaunchBody != null ? LaunchBody.position : Vector2.zero;
                }

                var renderer = LaunchVisual.GetComponent<SpriteRenderer>();
                return renderer != null ? (Vector2)renderer.bounds.center : (Vector2)LaunchVisual.position;
            }
        }

        public void Build(MoaiGolfRunState runState)
        {
            ClearExistingStage();

            var stage = runState.Stage;
            CreatePixelPerfectSprite("Background Visual", MoaiGolfSpriteCatalog.Background, Vector2.zero, Color.white, -10);
            CreateTerrainCollider();
            var launchPedestalCenter = runState.LaunchPedestalCenter;
            GolfClubPivot = CreateGolfClub(runState).transform;
            CreateBox("Launch Pedestal Collider", launchPedestalCenter, new Vector2(MoaiGolfWorldSettings.LaunchPedestalWidth, MoaiGolfWorldSettings.LaunchPedestalHeight), new Color(0.45f, 0.34f, 0.25f, 0.45f), true, false, 1);
            CreateTargetPedestalCollider();
            CreateTargetMoaiLineupOnPedestal(new[]
            {
                MoaiGolfMoaiKind.Sunglasses,
                MoaiGolfMoaiKind.Ribbon,
                MoaiGolfMoaiKind.Snowman,
                MoaiGolfMoaiKind.Sunglasses,
                MoaiGolfMoaiKind.Macho,
            }, stage, 2);
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

        private GameObject CreateGolfClub(MoaiGolfRunState runState)
        {
            // golfClub.png は 347x333 / pivot 左下。右上をモアイ中心の約 200px 上に固定し、
            // 子のスプライトを逆方向にオフセットして親 Transform の回転だけで振る。
            var spriteScale = MoaiGolfLaunchAnimator.ClubSpriteScale;
            var anchorLocal = new Vector2(
                MoaiGolfLaunchAnimator.ClubAnchorLocalXPixels,
                MoaiGolfLaunchAnimator.ClubAnchorLocalYPixels
            ) / MoaiGolfWorldSettings.PixelsPerUnit * spriteScale;

            var pivot = new GameObject("Golf Club Pivot");
            pivot.transform.SetParent(transform, false);

            var pivotPos = runState.LaunchPosition + MoaiGolfLaunchAnimator.ClubAnchorOffsetFromMoai;
            pivot.transform.position = new Vector3(pivotPos.x, pivotPos.y, 0f);
            pivot.transform.rotation = Quaternion.Euler(0f, 0f, MoaiGolfLaunchAnimator.ClubWindupAngleDeg);

            var visual = new GameObject("Golf Club Visual");
            visual.transform.SetParent(pivot.transform, false);
            visual.transform.localPosition = new Vector3(-anchorLocal.x, -anchorLocal.y, 0f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(spriteScale, spriteScale, 1f);

            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = MoaiGolfSpriteCatalog.GolfClub;
            renderer.sortingOrder = 3;
            return pivot;
        }

        private void CreateAimMarker(MoaiGolfStageDefinition stage)
        {
            var zone = stage.SuccessZone;
            var hereSprite = MoaiGolfSpriteCatalog.Here;
            var spriteSize = hereSprite.bounds.size;

            // here.png の左側の破線枠の実測値（200x150 px テクスチャ内で x=[15..102] y=[28..145]、
            // PPU=100 でワールド単位に換算すると 0.87 x 1.17、スプライト中心からのオフセットは (-0.415, -0.115)）。
            // この破線枠が成功ゾーンを囲うように非等倍スケールで引き伸ばす。
            var dashedBoxSize = new Vector2(0.87f, 1.17f);
            var dashedBoxOffsetFromCenter = new Vector2(-0.415f, -0.115f);
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

        private void CreateTargetMoai(MoaiGolfMoaiKind kind, float x, float visualFeetY, int sortingOrder)
        {
            var spec = MoaiGolfMoaiSpec.Get(kind);
            var sprite = MoaiGolfSpriteCatalog.GetMoai(kind);
            var visualHalfHeight = sprite.bounds.size.y * spec.VisualScale.y * 0.5f;
            // 各テクスチャの下端透明余白 (FeetOffset) を相殺して、どのモアイも見た目の足元が visualFeetY に揃うようにする
            var feetOffsetWorld = spec.FeetOffset * spec.VisualScale.y;
            var centerY = visualFeetY + visualHalfHeight - feetOffsetWorld;
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
            CreateTargetMoai(kind, x, GetTargetPedestalTopY(x) + TargetMoaiVisualFeetLift, sortingOrder);
        }

        private void CreateTargetMoaiLineupOnPedestal(MoaiGolfMoaiKind[] kinds, MoaiGolfStageDefinition stage, int sortingOrder)
        {
            if (kinds == null || kinds.Length == 0)
            {
                return;
            }

            const float leftEdgeMargin = 0.55f;
            const float rightEdgeMargin = 1.15f;
            const float zoneGap = 0.45f;

            var leftEdge = RightPedestalLeftX + leftEdgeMargin;
            var rightEdge = RightPedestalRightX - rightEdgeMargin;
            var zoneLeft = Mathf.Max(leftEdge, stage.SuccessZone.xMin - zoneGap);
            var zoneRight = Mathf.Min(rightEdge, stage.SuccessZone.xMax + zoneGap);

            var leftSpan = Mathf.Max(0f, zoneLeft - leftEdge);
            var rightSpan = Mathf.Max(0f, rightEdge - zoneRight);
            var totalSpan = leftSpan + rightSpan;

            if (totalSpan <= 0f)
            {
                // 成功ゾーンが台座全幅を覆ってしまう場合のフォールバック（左から順に詰める）
                PlaceMoaiAcrossRange(kinds, 0, kinds.Length, leftEdge, rightEdge, sortingOrder);
                return;
            }

            // 左右の空き幅に比例して個数を割り振りつつ、可能なら片側 0 を避ける
            var leftCount = Mathf.RoundToInt(kinds.Length * (leftSpan / totalSpan));
            if (kinds.Length >= 2)
            {
                if (rightSpan > 0f)
                {
                    leftCount = Mathf.Clamp(leftCount, 1, kinds.Length - 1);
                }
                else
                {
                    leftCount = kinds.Length;
                }
            }
            else
            {
                leftCount = leftSpan >= rightSpan ? 1 : 0;
            }
            var rightCount = kinds.Length - leftCount;

            PlaceMoaiAcrossRange(kinds, 0, leftCount, leftEdge, zoneLeft, sortingOrder);
            PlaceMoaiAcrossRange(kinds, leftCount, rightCount, zoneRight, rightEdge, sortingOrder);
        }

        private void PlaceMoaiAcrossRange(MoaiGolfMoaiKind[] kinds, int startIndex, int count, float leftX, float rightX, int sortingOrder)
        {
            if (count <= 0)
            {
                return;
            }

            var span = rightX - leftX;
            for (var index = 0; index < count; index++)
            {
                var t = count == 1 ? 0.5f : (float)index / (count - 1);
                var x = leftX + span * t;
                CreateTargetMoaiOnPedestal(kinds[startIndex + index], x, sortingOrder);
            }
        }

        private void CreateTargetPedestalCollider()
        {
            var pedestal = new GameObject("Target Pedestal Collider");
            pedestal.transform.SetParent(transform);
            pedestal.transform.position = Vector3.zero;

            var collider = pedestal.AddComponent<PolygonCollider2D>();
            collider.points = new[]
            {
                new Vector2(RightPedestalLeftX, RightPedestalBaseY),
                new Vector2(RightPedestalRightX, RightPedestalBaseY),
                new Vector2(RightPedestalRightX, TargetPedestalTopSurfacePoints[^1].y),
                TargetPedestalTopSurfacePoints[7],
                TargetPedestalTopSurfacePoints[6],
                TargetPedestalTopSurfacePoints[5],
                TargetPedestalTopSurfacePoints[4],
                TargetPedestalTopSurfacePoints[3],
                TargetPedestalTopSurfacePoints[2],
                TargetPedestalTopSurfacePoints[1],
                TargetPedestalTopSurfacePoints[0]
            };
            collider.isTrigger = false;

            var body = pedestal.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
        }

        private static float GetTargetPedestalTopY(float x)
        {
            if (x <= TargetPedestalTopSurfacePoints[0].x)
            {
                return TargetPedestalTopSurfacePoints[0].y;
            }

            for (var index = 1; index < TargetPedestalTopSurfacePoints.Length; index++)
            {
                var previous = TargetPedestalTopSurfacePoints[index - 1];
                var next = TargetPedestalTopSurfacePoints[index];
                if (x > next.x)
                {
                    continue;
                }

                var t = Mathf.InverseLerp(previous.x, next.x, x);
                return Mathf.Lerp(previous.y, next.y, t);
            }

            return TargetPedestalTopSurfacePoints[^1].y;
        }

        private void CreateLaunchMoai(MoaiGolfRunState runState)
        {
            var spec = MoaiGolfMoaiSpec.Get(runState.LaunchMoaiKind);
            var moai = new GameObject("Launch Moai Collider");
            moai.transform.SetParent(transform);
            moai.transform.position = runState.LaunchPosition;
            LaunchVisual = CreateCenteredSpriteChild("Launch Moai Visual", moai.transform, MoaiGolfSpriteCatalog.GetMoai(runState.LaunchMoaiKind), spec.VisualScale, Color.white, 4).transform;

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
