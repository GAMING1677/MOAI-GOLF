using UnityEngine;

namespace MoaiGolf
{
    public readonly struct MoaiGolfMoaiSpec
    {
        public const float ReferenceTextureWorldHeight = 1.5f;

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

        public float VisualHalfHeightWorld => ReferenceTextureWorldHeight * VisualScale.y * 0.5f;

        public float FeetOffsetWorld => FeetOffset * VisualScale.y;

        /// <summary>剛体中心から見た目の足元座標へ変換する。</summary>
        public Vector2 GetVisualFeetPosition(Vector2 bodyCenter)
        {
            return bodyCenter + Vector2.down * (VisualHalfHeightWorld - FeetOffsetWorld);
        }

        /// <summary>見た目の足元を指定したときの剛体中心座標。</summary>
        public Vector2 GetBodyCenterFromVisualFeet(Vector2 visualFeet)
        {
            return visualFeet + Vector2.up * (VisualHalfHeightWorld - FeetOffsetWorld);
        }

        public static MoaiGolfMoaiSpec Get(MoaiGolfMoaiKind kind)
        {
            // FeetOffset は各テクスチャの下端透明ピクセル数 / PixelsPerUnit(100)
            return kind switch
            {
                MoaiGolfMoaiKind.Sunglasses => new MoaiGolfMoaiSpec(kind, "Textures/moai_0", new Vector2(1.2f, 1.2f), new Vector2(0.85f, 1.05f), 0.6f, 0.38f, 0.45f, 0.32f),
                MoaiGolfMoaiKind.Ribbon => new MoaiGolfMoaiSpec(kind, "Textures/moai2", new Vector2(1.05f, 1.05f), new Vector2(0.8f, 1.0f), 1.23f, 0.48f, 0.35f, 0f),
                MoaiGolfMoaiKind.Macho => new MoaiGolfMoaiSpec(kind, "Textures/moai3", new Vector2(1.35f, 1.2f), new Vector2(1.05f, 1.0f), 2.025f, 0.25f, 0.62f, 0f),
                MoaiGolfMoaiKind.Snowman => new MoaiGolfMoaiSpec(kind, "Textures/moai4", new Vector2(1.15f, 1.25f), new Vector2(0.9f, 1.1f), 0.896f, 0.58f, 0.18f, 0f),
                _ => new MoaiGolfMoaiSpec(kind, "Textures/moai_0", Vector2.one, new Vector2(0.85f, 1.05f), 1f, 0.4f, 0.4f, 0f)
            };
        }
    }
}
