using UnityEngine;

namespace MoaiGolf
{
    [CreateAssetMenu(fileName = "MoaiSpec", menuName = "Moai Golf/Moai Spec")]
    public sealed class MoaiGolfMoaiSpecAsset : ScriptableObject
    {
        [SerializeField] private MoaiGolfMoaiKind kind;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Vector2 visualScale = Vector2.one;
        [SerializeField] private Vector2 colliderSize = new Vector2(0.85f, 1.05f);
        [SerializeField] private float mass = 1f;
        [SerializeField] private float bounciness = 0.38f;
        [SerializeField] private float friction = 0.45f;
        [SerializeField] private float feetOffset;
        [SerializeField] private int sortingOrder = 4;

        public MoaiGolfMoaiKind Kind => kind;
        public Sprite Sprite => sprite;
        public Vector2 VisualScale => visualScale;
        public Vector2 ColliderSize => colliderSize;
        public float Mass => mass;
        public float Bounciness => bounciness;
        public float Friction => friction;
        public float FeetOffset => feetOffset;
        public int SortingOrder => sortingOrder;

        public MoaiGolfMoaiSpec ToSpec()
        {
            return new MoaiGolfMoaiSpec(kind, string.Empty, visualScale, colliderSize, mass, bounciness, friction, feetOffset);
        }
    }
}
