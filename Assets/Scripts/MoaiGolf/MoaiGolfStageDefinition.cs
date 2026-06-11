using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace MoaiGolf
{
    public static class MoaiGolfTerrainProfile
    {
        private const string ResourcePath = "MoaiGolfTerrainProfile";

        private static readonly Vector2[] FallbackPoints =
        {
            new Vector2(0f, 2.25f),
            new Vector2(3.2f, 2.42f),
            new Vector2(7.2f, 2.12f),
            new Vector2(12.2f, 1.72f),
            new Vector2(18.6f, 1.95f),
            new Vector2(25.6f, 3.05f),
            new Vector2(33.8f, 3.9f),
            new Vector2(43.2f, 4.62f),
            new Vector2(57.4f, 4.62f),
            new Vector2(68.4f, 4.45f),
            new Vector2(MoaiGolfWorldSettings.WorldRight, 4.25f)
        };

        private static Vector2[] cachedPoints;

        public static Vector2[] ColliderPoints
        {
            get
            {
                if (cachedPoints != null)
                {
                    return cachedPoints;
                }

                var loaded = LoadGeneratedPoints();
                if (loaded == null)
                {
                    return FallbackPoints;
                }

                cachedPoints = loaded;
                return cachedPoints;
            }
        }

        public static float GetY(float x)
        {
            var points = ColliderPoints;
            if (x <= points[0].x)
            {
                return points[0].y;
            }

            for (var index = 1; index < points.Length; index++)
            {
                var previous = points[index - 1];
                var next = points[index];
                if (x > next.x)
                {
                    continue;
                }

                var segmentT = Mathf.InverseLerp(previous.x, next.x, x);
                return Mathf.Lerp(previous.y, next.y, segmentT);
            }

            return points[^1].y;
        }

        private static Vector2[] LoadGeneratedPoints()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null || string.IsNullOrEmpty(asset.text))
            {
                return null;
            }

            var lines = asset.text.Split('\n');
            var points = new List<Vector2>(lines.Length);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                var parts = line.Split(',');
                if (parts.Length != 2)
                {
                    continue;
                }

                if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                    || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                {
                    continue;
                }

                points.Add(new Vector2(x, y));
            }

            return points.Count >= 2 ? points.ToArray() : null;
        }
    }

    public readonly struct MoaiGolfStageDefinition
    {
        public MoaiGolfStageDefinition(
            string id,
            string backgroundSpritePath,
            Rect worldBounds,
            Vector2 groundCenter,
            Vector2 groundSize,
            Vector2 launchPosition,
            Vector2 targetMoaiPosition,
            Rect successZone
        )
        {
            Id = id;
            BackgroundSpritePath = backgroundSpritePath;
            WorldBounds = worldBounds;
            GroundCenter = groundCenter;
            GroundSize = groundSize;
            LaunchPosition = launchPosition;
            TargetMoaiPosition = targetMoaiPosition;
            SuccessZone = successZone;
        }

        public string Id { get; }
        public string BackgroundSpritePath { get; }
        public Rect WorldBounds { get; }
        public Vector2 GroundCenter { get; }
        public Vector2 GroundSize { get; }
        public Vector2 LaunchPosition { get; }
        public Vector2 TargetMoaiPosition { get; }
        public Rect SuccessZone { get; }

        public MoaiGolfStageDefinition WithSceneOverrides(Vector2 launchPosition, Rect successZone)
        {
            return new MoaiGolfStageDefinition(
                Id,
                BackgroundSpritePath,
                WorldBounds,
                GroundCenter,
                GroundSize,
                launchPosition,
                TargetMoaiPosition,
                successZone
            );
        }

        public static MoaiGolfStageDefinition CreateFirstStage()
        {
            return new MoaiGolfStageDefinition(
                "stage-001",
                "Textures/background_0",
                new Rect(
                    MoaiGolfWorldSettings.WorldLeft,
                    0f,
                    MoaiGolfWorldSettings.WorldWidth,
                    MoaiGolfWorldSettings.WorldHeight
                ),
                new Vector2(MoaiGolfWorldSettings.CameraCenterX, MoaiGolfTerrainProfile.GetY(MoaiGolfWorldSettings.CameraCenterX)),
                new Vector2(MoaiGolfWorldSettings.WorldWidth, 0.35f),
                new Vector2(3.2f, MoaiGolfTerrainProfile.GetY(3.2f) + MoaiGolfWorldSettings.LaunchPedestalHeight * 0.5f),
                new Vector2(32.75f, 7.2f + 0.68f),
                new Rect(29.765f, 7.68f, 1.35f, 2.3f)
            );
        }
    }
}
