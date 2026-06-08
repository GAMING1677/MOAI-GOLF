using UnityEngine;

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif

namespace MoaiGolf
{
    public static class MoaiGolfSpriteCatalog
    {
        public static Sprite Background => LoadSprite("Assets/Textures/background.png", "background_0");
        public static Sprite GolfClub => LoadSprite("Assets/Textures/golfClub.png", "golfClub_0");
        public static Sprite Arrow => LoadSprite("Assets/Textures/arrow.png", "arrow_0");
        public static Sprite Here => LoadWholeTextureSprite("Assets/Textures/here.png", 100f);

        public static Sprite GetMoai(MoaiGolfMoaiKind kind)
        {
            return kind switch
            {
                MoaiGolfMoaiKind.Sunglasses => LoadSprite("Assets/Textures/moai.png", "moai_1"),
                MoaiGolfMoaiKind.Ribbon => LoadSprite("Assets/Textures/moai2.png", "moai2_0"),
                MoaiGolfMoaiKind.Macho => LoadSprite("Assets/Textures/moai3.png", "moai3_0"),
                MoaiGolfMoaiKind.Snowman => LoadSprite("Assets/Textures/moai4.png", "moai4_0"),
                _ => LoadSprite("Assets/Textures/moai.png", "moai_1")
            };
        }

        private static Sprite LoadSprite(string assetPath, string spriteName)
        {
#if UNITY_EDITOR
            return AssetDatabase
                .LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .FirstOrDefault(sprite => sprite.name == spriteName);
#else
            return null;
#endif
        }

        private static Sprite LoadWholeTextureSprite(string assetPath, float pixelsPerUnit)
        {
#if UNITY_EDITOR
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                Vector2.zero,
                pixelsPerUnit
            );
#else
            return null;
#endif
        }
    }
}
