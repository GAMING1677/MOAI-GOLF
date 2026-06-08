using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoaiGolf
{
    public static class MoaiGolfBackgroundBuilder
    {
        public const string SourceAPath = "Assets/Textures/background.png";
        public const string SourceBPath = "Assets/Textures/background_2.png";
        public const string GeneratedPath = "Assets/Textures/generated/background_fullscreen.png";
        public const int SourceSegmentWidth = MoaiGolfWorldSettings.ReferenceWidthPixels;
        public const int OutputWidth = MoaiGolfWorldSettings.ReferenceWidthPixels * 2;
        public const int OutputHeight = MoaiGolfWorldSettings.ReferenceHeightPixels;

#if UNITY_EDITOR
        [MenuItem("Moai Golf/Generate Fullscreen Background")]
        public static void GenerateAsset()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GeneratedPath));

            var first = LoadReadableTexture(SourceAPath);
            var second = File.Exists(SourceBPath) ? LoadReadableTexture(SourceBPath) : first;
            var output = Build(first, second, SourceSegmentWidth, OutputHeight);
            File.WriteAllBytes(GeneratedPath, output.EncodeToPNG());
            Object.DestroyImmediate(output);
            Object.DestroyImmediate(first);
            if (second != first)
            {
                Object.DestroyImmediate(second);
            }

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

        public static Texture2D Build(Texture2D first, Texture2D second, int segmentWidth, int outputHeight)
        {
            var outputWidth = segmentWidth * 2;
            var output = new Texture2D(outputWidth, outputHeight, TextureFormat.RGBA32, false);
            for (var y = 0; y < outputHeight; y++)
            {
                for (var x = 0; x < outputWidth; x++)
                {
                    var source = x < segmentWidth ? first : second;
                    var sourceX = x < segmentWidth ? x : x - segmentWidth;
                    output.SetPixel(x, y, Sample(source, sourceX, segmentWidth, y, outputHeight));
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
