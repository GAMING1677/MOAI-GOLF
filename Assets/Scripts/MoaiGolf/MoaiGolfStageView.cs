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
            CreateBox("Background", stage.WorldBounds.center, stage.WorldBounds.size, new Color(0.66f, 0.84f, 0.93f), false, false, -10);
            CreateBox("Ground Collider", stage.GroundCenter, stage.GroundSize, new Color(0.39f, 0.65f, 0.29f), true, false, 0);
            CreateBox("Launch Pedestal Collider", runState.LaunchPosition + new Vector2(0f, -0.55f), new Vector2(1.3f, 0.35f), new Color(0.45f, 0.34f, 0.25f), true, false, 1);
            CreateBox("Target Moai Collider", stage.TargetMoaiPosition, new Vector2(0.95f, 1.35f), new Color(0.38f, 0.35f, 0.31f), true, false, 2);
            CreateBox("Success Zone Trigger", stage.SuccessZone.center, stage.SuccessZone.size, new Color(1f, 0.1f, 0.08f, 0.32f), true, true, 3);
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

        private void CreateLaunchMoai(MoaiGolfRunState runState)
        {
            var spec = MoaiGolfMoaiSpec.Get(runState.LaunchMoaiKind);
            var moai = CreateBox("Launch Moai Collider", runState.LaunchPosition, spec.ColliderSize, Color.white, false, false, 4);
            moai.transform.localScale = new Vector3(spec.VisualScale.x, spec.VisualScale.y, 1f);

            var renderer = moai.GetComponent<SpriteRenderer>();
            renderer.color = MoaiColor(runState.LaunchMoaiKind);
            renderer.sortingOrder = 4;

            var collider = moai.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(spec.ColliderSize.x / spec.VisualScale.x, spec.ColliderSize.y / spec.VisualScale.y);
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
