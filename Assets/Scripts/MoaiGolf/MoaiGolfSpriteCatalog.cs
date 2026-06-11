using UnityEngine;

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif

namespace MoaiGolf
{
    public static class MoaiGolfSpriteCatalog
    {
        public static Sprite Background => LoadWholeTextureSprite(MoaiGolfBackgroundBuilder.GeneratedPath, MoaiGolfWorldSettings.PixelsPerUnit);
        public static Sprite GolfClub => LoadSprite("Assets/Textures/golfClub.png", "golfClub_0");
        public static Sprite Arrow => LoadSprite("Assets/Textures/arrow.png", "arrow_0");
        public static Sprite Here => LoadWholeTextureSprite("Assets/Textures/here.png", 100f);

        public static Sprite GetMoai(MoaiGolfMoaiKind kind)
        {
            var path = kind switch
            {
                MoaiGolfMoaiKind.Sunglasses => "Assets/Textures/moai.png",
                MoaiGolfMoaiKind.Ribbon => "Assets/Textures/moai2.png",
                MoaiGolfMoaiKind.Macho => "Assets/Textures/moai3.png",
                MoaiGolfMoaiKind.Snowman => "Assets/Textures/moai4.png",
                _ => "Assets/Textures/moai.png"
            };
            return LoadWholeTextureSprite(path, MoaiGolfWorldSettings.PixelsPerUnit);
        }

#if UNITY_EDITOR
        public static Sprite GetPersistedMoai(MoaiGolfMoaiKind kind)
        {
            var path = kind switch
            {
                MoaiGolfMoaiKind.Sunglasses => "Assets/Textures/moai.png",
                MoaiGolfMoaiKind.Ribbon => "Assets/Textures/moai2.png",
                MoaiGolfMoaiKind.Macho => "Assets/Textures/moai3.png",
                MoaiGolfMoaiKind.Snowman => "Assets/Textures/moai4.png",
                _ => "Assets/Textures/moai.png"
            };
            return LoadPrimarySpriteAsset(path);
        }

        public static Sprite GetPersistedBackground()
        {
            MoaiGolfBackgroundBuilder.EnsureGeneratedAsset();
            return LoadPrimarySpriteAsset(MoaiGolfBackgroundBuilder.GeneratedPath);
        }

        public static Sprite GetPersistedHere()
        {
            return LoadPrimarySpriteAsset("Assets/Textures/here.png");
        }

        public static Sprite GetPersistedWhite()
        {
            return LoadPrimarySpriteAsset("Assets/Textures/generated/white1x1.png");
        }

        public static Sprite LoadPrimarySpriteAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToArray();
            if (sprites.Length == 0)
            {
                return null;
            }

            if (sprites.Length == 1)
            {
                return sprites[0];
            }

            Sprite best = null;
            var bestArea = 0f;
            foreach (var sprite in sprites)
            {
                var area = sprite.rect.width * sprite.rect.height;
                if (area > bestArea)
                {
                    bestArea = area;
                    best = sprite;
                }
            }

            return best;
        }
#endif

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
            if (assetPath == MoaiGolfBackgroundBuilder.GeneratedPath)
            {
                MoaiGolfBackgroundBuilder.EnsureGeneratedAsset();
            }

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
