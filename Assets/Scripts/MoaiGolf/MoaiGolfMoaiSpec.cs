using UnityEngine;

namespace MoaiGolf
{
    public readonly struct MoaiGolfMoaiSpec
    {
        public MoaiGolfMoaiSpec(
            MoaiGolfMoaiKind kind,
            string spritePath,
            Vector2 visualScale,
            Vector2 colliderSize,
            float mass,
            float bounciness,
            float friction,
            float feetOffset = 0f
        )
        {
            Kind = kind;
            SpritePath = spritePath;
            VisualScale = visualScale;
            ColliderSize = colliderSize;
            Mass = mass;
            Bounciness = bounciness;
            Friction = friction;
            FeetOffset = feetOffset;
        }

        public MoaiGolfMoaiKind Kind { get; }
        public string SpritePath { get; }
        public Vector2 VisualScale { get; }
        public Vector2 ColliderSize { get; }
        public float Mass { get; }
        public float Bounciness { get; }
        public float Friction { get; }
        /// <summary>スプライト下端と見た目の足元との間にある透明余白のワールド単位距離（VisualScale 適用前）。</summary>
        public float FeetOffset { get; }

        public static MoaiGolfMoaiSpec Get(MoaiGolfMoaiKind kind)
        {
            // FeetOffset は各テクスチャの下端透明ピクセル数 / PixelsPerUnit(100)
            return kind switch
            {
                MoaiGolfMoaiKind.Sunglasses => new MoaiGolfMoaiSpec(kind, "Textures/moai_0", new Vector2(1.2f, 1.2f), new Vector2(0.85f, 1.05f), 1.0f, 0.38f, 0.45f, 0.32f),
                MoaiGolfMoaiKind.Ribbon => new MoaiGolfMoaiSpec(kind, "Textures/moai2", new Vector2(1.05f, 1.05f), new Vector2(0.8f, 1.0f), 0.82f, 0.48f, 0.35f, 0f),
                MoaiGolfMoaiKind.Macho => new MoaiGolfMoaiSpec(kind, "Textures/moai3", new Vector2(1.35f, 1.2f), new Vector2(1.05f, 1.0f), 1.35f, 0.25f, 0.62f, 0f),
                MoaiGolfMoaiKind.Snowman => new MoaiGolfMoaiSpec(kind, "Textures/moai4", new Vector2(1.15f, 1.25f), new Vector2(0.9f, 1.1f), 1.12f, 0.58f, 0.18f, 0f),
                _ => new MoaiGolfMoaiSpec(kind, "Textures/moai_0", Vector2.one, new Vector2(0.85f, 1.05f), 1f, 0.4f, 0.4f, 0f)
            };
        }
    }
}
