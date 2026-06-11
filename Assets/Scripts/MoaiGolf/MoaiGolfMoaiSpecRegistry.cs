using System.Collections.Generic;
using UnityEngine;

namespace MoaiGolf
{
    public static class MoaiGolfMoaiSpecRegistry
    {
        private const string ResourceFolder = "MoaiGolf/Specs";
        private static readonly Dictionary<MoaiGolfMoaiKind, MoaiGolfMoaiSpecAsset> cachedAssets = new();
        private static readonly HashSet<MoaiGolfMoaiKind> loadedKinds = new();

        public static MoaiGolfMoaiSpecAsset GetAsset(MoaiGolfMoaiKind kind)
        {
            if (!loadedKinds.Contains(kind))
            {
                var asset = Resources.Load<MoaiGolfMoaiSpecAsset>(GetResourcePath(kind));
                if (asset != null && asset.Kind == kind)
                {
                    cachedAssets[kind] = asset;
                }

                loadedKinds.Add(kind);
            }

            return cachedAssets.TryGetValue(kind, out var cachedAsset) ? cachedAsset : null;
        }

        public static MoaiGolfMoaiSpec GetSpec(MoaiGolfMoaiKind kind)
        {
            var asset = GetAsset(kind);
            return asset != null ? asset.ToSpec() : MoaiGolfMoaiSpec.Get(kind);
        }

        public static Sprite GetSprite(MoaiGolfMoaiKind kind)
        {
            var asset = GetAsset(kind);
            if (asset != null && asset.Sprite != null)
            {
                return asset.Sprite;
            }

#if UNITY_EDITOR
            return MoaiGolfSpriteCatalog.GetMoai(kind);
#else
            return null;
#endif
        }

        private static string GetResourcePath(MoaiGolfMoaiKind kind)
        {
            var fileName = kind switch
            {
                MoaiGolfMoaiKind.Sunglasses => "Sunglasses",
                MoaiGolfMoaiKind.Ribbon => "Ribbon",
                MoaiGolfMoaiKind.Macho => "Macho",
                MoaiGolfMoaiKind.Snowman => "Snowman",
                _ => "Sunglasses"
            };

            return $"{ResourceFolder}/{fileName}";
        }
    }
}
