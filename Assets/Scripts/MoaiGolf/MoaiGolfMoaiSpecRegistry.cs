using UnityEngine;

namespace MoaiGolf
{
    public static class MoaiGolfMoaiSpecRegistry
    {
        private const string ResourceFolder = "MoaiGolf/Specs";
        private static MoaiGolfMoaiSpecAsset[] cachedAssets;

        public static MoaiGolfMoaiSpecAsset GetAsset(MoaiGolfMoaiKind kind)
        {
            EnsureLoaded();
            if (cachedAssets == null)
            {
                return null;
            }

            foreach (var asset in cachedAssets)
            {
                if (asset != null && asset.Kind == kind)
                {
                    return asset;
                }
            }

            return null;
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

        private static void EnsureLoaded()
        {
            if (cachedAssets != null)
            {
                return;
            }

            cachedAssets = Resources.LoadAll<MoaiGolfMoaiSpecAsset>(ResourceFolder);
        }
    }
}
