using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoaiGolf
{
    public static class MoaiGolfBackgroundBuilder
    {
        public const string SourcePath = "Assets/Textures/background.png";
        public const string GeneratedPath = "Assets/Textures/generated/background_fullscreen.png";
        public const int OutputWidth = MoaiGolfWorldSettings.BackgroundWidthPixels;
        public const int OutputHeight = MoaiGolfWorldSettings.BackgroundHeightPixels;

#if UNITY_EDITOR
        [MenuItem("Moai Golf/Generate Fullscreen Background")]
        public static void GenerateAsset()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GeneratedPath));

            var source = LoadReadableTexture(SourcePath);
            var output = Build(source, OutputWidth, OutputHeight);
            File.WriteAllBytes(GeneratedPath, output.EncodeToPNG());
            Object.DestroyImmediate(output);
            Object.DestroyImmediate(source);

            AssetDatabase.ImportAsset(GeneratedPath, ImportAssetOptions.ForceUpdate);
            ConfigureGeneratedImporter();
        }

        public static void EnsureGeneratedAsset()
        {
            if (!File.Exists(GeneratedPath))
            {
                GenerateAsset();
            }
        }

        private static Texture2D LoadReadableTexture(string assetPath)
        {
            var bytes = File.ReadAllBytes(assetPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(bytes);
            return texture;
        }

        private static void ConfigureGeneratedImporter()
        {
            var importer = AssetImporter.GetAtPath(GeneratedPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 8192;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
#endif

        public static Texture2D Build(Texture2D source, int outputWidth, int outputHeight)
        {
            var output = new Texture2D(outputWidth, outputHeight, TextureFormat.RGBA32, false);
            for (var y = 0; y < outputHeight; y++)
            {
                for (var x = 0; x < outputWidth; x++)
                {
                    output.SetPixel(x, y, Sample(source, x, outputWidth, y, outputHeight));
                }
            }

            output.Apply();
            return output;
        }

        private static Color Sample(Texture2D source, int outputX, int outputWidth, int outputY, int outputHeight)
        {
            var sourceX01 = outputWidth <= 1 ? 0f : (float)outputX / (outputWidth - 1);
            var sourceY01 = outputHeight <= 1 ? 0f : (float)outputY / (outputHeight - 1);
            var sourceX = Mathf.RoundToInt(sourceX01 * (source.width - 1));
            var sourceY = Mathf.RoundToInt(sourceY01 * (source.height - 1));
            return source.GetPixel(sourceX, sourceY);
        }
    }
}
