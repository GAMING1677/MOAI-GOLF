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
                MoaiGolfMoaiKind.Macho => "Assets/Textures/moai4.png",
                MoaiGolfMoaiKind.Snowman => "Assets/Textures/moai3.png",
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
                MoaiGolfMoaiKind.Macho => "Assets/Textures/moai4.png",
                MoaiGolfMoaiKind.Snowman => "Assets/Textures/moai3.png",
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

        public static Sprite GetPersistedArrow()
        {
            return LoadNamedSpriteAsset("Assets/Textures/arrow.png", "arrow_0");
        }

        public static Sprite GetPersistedResultSuccess()
        {
            return LoadFullTextureSpriteAsset("Assets/Textures/result_success.png");
        }

        public static Sprite GetPersistedResultFailed()
        {
            return LoadFullTextureSpriteAsset("Assets/Textures/result_failed.png");
        }

        public static Sprite GetPersistedTitleLogo()
        {
            return LoadFullTextureSpriteAsset("Assets/Textures/MOAI_GOLF_LOGO.png", 100f)
                ?? LoadPrimarySpriteAsset("Assets/Textures/MOAI_GOLF_LOGO.png");
        }

        public static Sprite GetPersistedWhite()
        {
            return LoadPrimarySpriteAsset("Assets/Textures/generated/white1x1.png");
        }

        public static Sprite LoadNamedSpriteAsset(string assetPath, string spriteName)
        {
            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(spriteName))
            {
                return null;
            }

            return AssetDatabase
                .LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .FirstOrDefault(sprite => sprite.name == spriteName);
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

        public static Sprite LoadFullTextureSpriteAsset(string assetPath, float pixelsPerUnit = 100f)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                return null;
            }

            var fullRect = new Rect(0f, 0f, texture.width, texture.height);
            foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>())
            {
                if (Mathf.Approximately(sprite.rect.x, fullRect.x)
                    && Mathf.Approximately(sprite.rect.y, fullRect.y)
                    && Mathf.Approximately(sprite.rect.width, fullRect.width)
                    && Mathf.Approximately(sprite.rect.height, fullRect.height))
                {
                    return sprite;
                }
            }

            return Sprite.Create(texture, fullRect, new Vector2(0.5f, 0.5f), pixelsPerUnit);
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
