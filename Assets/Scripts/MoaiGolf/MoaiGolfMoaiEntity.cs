using UnityEngine;

namespace MoaiGolf
{
    public enum MoaiGolfMoaiRole
    {
        Launch,
        Target
    }

    [DisallowMultipleComponent]
    public sealed class MoaiGolfMoaiEntity : MonoBehaviour
    {
        [SerializeField] private MoaiGolfMoaiSpecAsset specAsset;
        [SerializeField] private MoaiGolfMoaiRole role = MoaiGolfMoaiRole.Launch;
        [SerializeField] private SpriteRenderer visualRenderer;
        [SerializeField] private CapsuleCollider2D capsuleCollider;
        [SerializeField] private Rigidbody2D body;

        public Rigidbody2D Body => body;
        public Transform Visual => visualRenderer != null ? visualRenderer.transform : transform;
        public MoaiGolfMoaiKind CurrentKind { get; private set; }

        private void Reset()
        {
            capsuleCollider = GetComponent<CapsuleCollider2D>();
            body = GetComponent<Rigidbody2D>();
            visualRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Awake()
        {
            capsuleCollider ??= GetComponent<CapsuleCollider2D>();
            body ??= GetComponent<Rigidbody2D>();
            visualRenderer ??= GetComponentInChildren<SpriteRenderer>();
        }

        public void ConfigureLaunch(MoaiGolfMoaiKind kind)
        {
            role = MoaiGolfMoaiRole.Launch;
            ApplyKind(kind);
            EnsureLaunchComponents();
            ConfigureBodyForLaunch();
        }

        public void ConfigureTarget(MoaiGolfMoaiKind kind, int sortingOrder)
        {
            role = MoaiGolfMoaiRole.Target;
            ApplyKind(kind, sortingOrder);
            EnsureTargetComponents();
            ConfigureBodyForTarget();
        }

        public void ApplyKind(MoaiGolfMoaiKind kind, int? sortingOrderOverride = null)
        {
            CurrentKind = kind;
            var asset = specAsset != null && specAsset.Kind == kind
                ? specAsset
                : MoaiGolfMoaiSpecRegistry.GetAsset(kind);
            var spec = asset != null ? asset.ToSpec() : MoaiGolfMoaiSpecRegistry.GetSpec(kind);
            var sprite = asset != null && asset.Sprite != null
                ? asset.Sprite
                : MoaiGolfMoaiSpecRegistry.GetSprite(kind);
            var sortingOrder = sortingOrderOverride ?? asset?.SortingOrder ?? 4;

            ApplyVisual(sprite, spec, sortingOrder);
            ApplyCollider(spec);
            ApplyPhysicsMaterial(spec);
            if (body != null)
            {
                body.mass = spec.Mass;
            }
        }

        private void ApplyVisual(Sprite sprite, MoaiGolfMoaiSpec spec, int sortingOrder)
        {
            if (visualRenderer == null)
            {
                return;
            }

            if (sprite != null)
            {
                visualRenderer.sprite = sprite;
            }

            visualRenderer.sortingOrder = sortingOrder;
            visualRenderer.transform.localScale = new Vector3(spec.VisualScale.x, spec.VisualScale.y, 1f);

            if (visualRenderer.sprite == null)
            {
                visualRenderer.transform.localPosition = Vector3.zero;
                return;
            }

            var bounds = visualRenderer.sprite.bounds;
            var feetOffsetWorld = spec.FeetOffset * spec.VisualScale.y;
            var feetLocalY = -(spec.VisualHalfHeightWorld - feetOffsetWorld);
            var spriteHalfHeight = bounds.size.y * spec.VisualScale.y * 0.5f;
            visualRenderer.transform.localPosition = new Vector3(
                -bounds.center.x * spec.VisualScale.x,
                feetLocalY + spriteHalfHeight,
                0f
            );
        }

        private void ApplyCollider(MoaiGolfMoaiSpec spec)
        {
            if (capsuleCollider == null)
            {
                return;
            }

            capsuleCollider.size = spec.ColliderSize;
            capsuleCollider.direction = CapsuleDirection2D.Vertical;
            capsuleCollider.offset = Vector2.zero;
        }

        private void ApplyPhysicsMaterial(MoaiGolfMoaiSpec spec)
        {
            if (capsuleCollider == null || role != MoaiGolfMoaiRole.Launch)
            {
                return;
            }

            var material = new PhysicsMaterial2D($"{spec.Kind} Material")
            {
                bounciness = spec.Bounciness,
                friction = spec.Friction
            };
            capsuleCollider.sharedMaterial = material;
        }

        private void ConfigureBodyForLaunch()
        {
            if (body == null)
            {
                return;
            }

            body.bodyType = RigidbodyType2D.Dynamic;
            body.centerOfMass = Vector2.zero;
            body.Sleep();
        }

        private void ConfigureBodyForTarget()
        {
            if (body == null)
            {
                return;
            }

            body.bodyType = RigidbodyType2D.Static;
        }

        private void EnsureLaunchComponents()
        {
            EnsureComponent<MoaiGolfBounceSfx>();
            EnsureComponent<MoaiGolfTargetMoaiVoiceSfx>();
            EnsureComponent<MoaiGolfWorldBoundsBounce>();
            EnsureComponent<MoaiGolfLandingJudge>();
            RemoveComponent<MoaiGolfTargetMoaiMarker>();
        }

        private void EnsureTargetComponents()
        {
            EnsureComponent<MoaiGolfTargetMoaiMarker>();
            RemoveComponent<MoaiGolfBounceSfx>();
            RemoveComponent<MoaiGolfTargetMoaiVoiceSfx>();
            RemoveComponent<MoaiGolfWorldBoundsBounce>();
            RemoveComponent<MoaiGolfLandingJudge>();
        }

        private T EnsureComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            return gameObject.AddComponent<T>();
        }

        private void RemoveComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            if (component == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(component);
                return;
            }
#endif
            Destroy(component);
        }
    }
}
