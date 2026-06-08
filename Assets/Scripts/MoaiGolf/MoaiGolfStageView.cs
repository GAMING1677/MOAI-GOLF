using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfStageView : MonoBehaviour
    {
        private static Sprite whiteSprite;

        public Rigidbody2D LaunchBody { get; private set; }

        public void Build(MoaiGolfRunState runState)
        {
            ClearExistingStage();

            var stage = runState.Stage;
            CreateSpriteFitted("Background Visual", MoaiGolfSpriteCatalog.Background, stage.WorldBounds.position, stage.WorldBounds.size, Color.white, -10);
            CreateBox("Ground Collider", stage.GroundCenter, stage.GroundSize, new Color(0.39f, 0.65f, 0.29f, 0.28f), true, false, 0);
            var launchPedestalCenter = new Vector2(runState.LaunchPosition.x, stage.LaunchPosition.y);
            CreateSprite("Golf Club Visual", MoaiGolfSpriteCatalog.GolfClub, launchPedestalCenter + new Vector2(-1.05f, -0.18f), new Vector2(0.55f, 0.55f), Color.white, 3);
            CreateBox("Launch Pedestal Collider", launchPedestalCenter, new Vector2(MoaiGolfWorldSettings.LaunchPedestalWidth, MoaiGolfWorldSettings.LaunchPedestalHeight), new Color(0.45f, 0.34f, 0.25f, 0.45f), true, false, 1);
            CreateSprite("Target Moai Visual", MoaiGolfSpriteCatalog.GetMoai(MoaiGolfMoaiKind.Sunglasses), stage.TargetMoaiPosition + new Vector2(-0.48f, -0.65f), new Vector2(1.6f, 1.6f), Color.white, 2);
            CreateBox("Target Moai Collider", stage.TargetMoaiPosition, new Vector2(0.95f, 1.35f), new Color(0.38f, 0.35f, 0.31f, 0.15f), true, false, 2);
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

        private void CreateAimMarker(MoaiGolfStageDefinition stage)
        {
            var markerX = stage.SuccessZone.center.x;
            var markerTop = stage.SuccessZone.yMax;
            CreateSprite("Here Label Visual", MoaiGolfSpriteCatalog.Here, new Vector2(markerX - 0.55f, markerTop + 0.62f), new Vector2(2.2f, 2.2f), Color.white, 5);
            var arrow = CreateSprite("Arrow Visual", MoaiGolfSpriteCatalog.Arrow, new Vector2(markerX - 0.38f, markerTop + 0.18f), new Vector2(1.1f, 1.1f), Color.white, 5);
            arrow.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
        }

        private void CreateLaunchMoai(MoaiGolfRunState runState)
        {
            var spec = MoaiGolfMoaiSpec.Get(runState.LaunchMoaiKind);
            var moai = new GameObject("Launch Moai Collider");
            moai.transform.SetParent(transform);
            moai.transform.position = runState.LaunchPosition;
            CreateSpriteChild("Launch Moai Visual", moai.transform, MoaiGolfSpriteCatalog.GetMoai(runState.LaunchMoaiKind), new Vector2(-0.45f, -0.52f), spec.VisualScale, Color.white, 4);

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
}
