#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MoaiGolf
{
    public static class MoaiGolfTerrainBuilder
    {
        private const string SourcePath = "Assets/Textures/background.png";
        private const string OutputDirectory = "Assets/Resources";
        private const string OutputPath = "Assets/Resources/MoaiGolfTerrainProfile.txt";
        private const int ColumnStride = 16;
        private const float GreenDominanceThreshold = 0.05f;
        private const float MinGreenValue = 0.22f;

        [InitializeOnLoadMethod]
        private static void EnsureGeneratedOnLoad()
        {
            if (!File.Exists(OutputPath))
            {
                Generate();
            }
        }

        [MenuItem("Moai Golf/Generate Terrain From Background")]
        public static void Generate()
        {
            var texture = LoadReadableTexture(SourcePath);
            if (texture == null)
            {
                Debug.LogError($"Could not load {SourcePath}");
                return;
            }

            var points = SampleGroundSilhouette(texture);
            WriteCsv(points);
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(OutputPath, ImportAssetOptions.ForceUpdate);
            Debug.Log($"Generated terrain profile: {points.Count} points → {OutputPath}");
        }

        private static List<Vector2> SampleGroundSilhouette(Texture2D texture)
        {
            var points = new List<Vector2>();
            var width = texture.width;
            var height = texture.height;

            void AppendColumn(int pixelX)
            {
                var pixelY = FindTopGreenPixelY(texture, pixelX, height);
                if (pixelY < 0)
                {
                    return;
                }

                var worldX = pixelX / MoaiGolfWorldSettings.PixelsPerUnit;
                var worldY = pixelY / MoaiGolfWorldSettings.PixelsPerUnit;
                points.Add(new Vector2(worldX, worldY));
            }

            AppendColumn(0);
            for (var x = ColumnStride; x < width - 1; x += ColumnStride)
            {
                AppendColumn(x);
            }

            AppendColumn(width - 1);
            return points;
        }

        private const int ForegroundGapTolerance = 3;

        private static int FindTopGreenPixelY(Texture2D texture, int pixelX, int height)
        {
            if (!IsGreen(texture.GetPixel(pixelX, 0)))
            {
                return -1;
            }

            var topGreenY = 0;
            var gapStreak = 0;
            for (var y = 1; y < height; y++)
            {
                if (IsGreen(texture.GetPixel(pixelX, y)))
                {
                    topGreenY = y;
                    gapStreak = 0;
                    continue;
                }

                gapStreak++;
                if (gapStreak > ForegroundGapTolerance)
                {
                    break;
                }
            }

            return topGreenY;
        }

        private static bool IsGreen(Color pixel)
        {
            return pixel.a > 0.5f
                && pixel.g > pixel.r + GreenDominanceThreshold
                && pixel.g > pixel.b + GreenDominanceThreshold
                && pixel.g > MinGreenValue;
        }

        private static void WriteCsv(List<Vector2> points)
        {
            Directory.CreateDirectory(OutputDirectory);

            var builder = new StringBuilder();
            foreach (var point in points)
            {
                builder.Append(point.x.ToString("0.####", CultureInfo.InvariantCulture));
                builder.Append(',');
                builder.Append(point.y.ToString("0.####", CultureInfo.InvariantCulture));
                builder.Append('\n');
            }

            File.WriteAllText(OutputPath, builder.ToString());
        }

        private static Texture2D LoadReadableTexture(string assetPath)
        {
            if (!File.Exists(assetPath))
            {
                return null;
            }

            var bytes = File.ReadAllBytes(assetPath);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(bytes);
            return texture;
        }
    }
}
#endif
