using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfStageView : MonoBehaviour
    {
        public const float RightPedestalLeftX = 26.7f;
        public const float RightPedestalRightX = 35.28f;
        public const float RightPedestalTopY = 7.2f;
        public const float RightPedestalBaseY = 3.0f;
        public const float TargetMoaiVisualFeetLift = 0.08f;

        public static readonly Vector2[] TargetPedestalTopSurfacePoints =
        {
            new Vector2(26.7f, 6.95f),
            new Vector2(27.2f, 7.02f),
            new Vector2(28.5f, 7.1f),
            new Vector2(29.3f, 7.18f),
            new Vector2(30.0f, 7.65f),
            new Vector2(31.2f, 7.76f),
            new Vector2(32.4f, 7.86f),
            new Vector2(34.2f, 7.96f),
            new Vector2(35.28f, 8.02f)
        };

        [SerializeField] private MoaiGolfStagePrefabSet prefabSet;
        [SerializeField] private bool useScenePlacedElements = true;

        private MoaiGolfSceneLayout? capturedSceneLayout;
        private Vector2[] resolvedTargetPedestalTopSurfacePoints;

        private static readonly MoaiGolfMoaiKind[] DefaultTargetLineup =
        {
            MoaiGolfMoaiKind.Sunglasses,
            MoaiGolfMoaiKind.Ribbon,
            MoaiGolfMoaiKind.Macho,
            MoaiGolfMoaiKind.Sunglasses,
            MoaiGolfMoaiKind.Snowman,
        };

        public Rigidbody2D LaunchBody { get; private set; }
        public Transform LaunchVisual { get; private set; }
        public Transform GolfClubPivot { get; private set; }

        public Vector2 LaunchVisualFocusPosition
        {
            get
            {
                if (LaunchVisual == null)
                {
                    return LaunchBody != null ? LaunchBody.position : Vector2.zero;
                }

                var renderer = LaunchVisual.GetComponent<SpriteRenderer>();
                return renderer != null ? (Vector2)renderer.bounds.center : (Vector2)LaunchVisual.position;
            }
        }

        public Vector2 LaunchCameraFocusPosition
        {
            get
            {
                if (LaunchBody == null)
                {
                    return LaunchVisualFocusPosition;
                }

                var entity = LaunchBody.GetComponent<MoaiGolfMoaiEntity>();
                var spec = MoaiGolfMoaiSpec.Get(entity != null ? entity.CurrentKind : MoaiGolfMoaiKind.Sunglasses);
                return (Vector2)LaunchBody.position + Vector2.up * (spec.VisualHalfHeightWorld * 0.42f);
            }
        }

        public float SampleLaunchSurfaceY(float worldX)
        {
            var launchPedestal = FindElement(MoaiGolfStageElementKind.LaunchPedestal);
            if (launchPedestal != null)
            {
                var collider = launchPedestal.GetComponent<Collider2D>();
                if (collider != null)
                {
                    return collider.bounds.max.y + 0.02f;
                }
            }

            var origin = new Vector2(worldX, MoaiGolfWorldSettings.WorldHeight + 2f);
            var hits = Physics2D.RaycastAll(origin, Vector2.down, MoaiGolfWorldSettings.WorldHeight + 4f);
            var bestY = float.NegativeInfinity;
            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.isTrigger)
                {
                    continue;
                }

                if (LaunchBody != null && hit.collider.attachedRigidbody == LaunchBody)
                {
                    continue;
                }

                if (hit.point.y > bestY)
                {
                    bestY = hit.point.y;
                }
            }

            if (!float.IsNegativeInfinity(bestY))
            {
                return bestY + 0.02f;
            }

            return MoaiGolfTerrainProfile.GetY(worldX) + 0.02f;
        }

        public void RefreshTargetLineupPreview()
        {
            resolvedTargetPedestalTopSurfacePoints = null;
            PlaceTargetMoaiLineupOnPedestal(
                DefaultTargetLineup,
                TryGetSuccessZoneRect(),
                2,
                CaptureTargetMoaiSlotPositions()
            );
        }

        public void Build(MoaiGolfRunState runState)
        {
            if (useScenePlacedElements && TryBuildFromScene(runState))
            {
                return;
            }

            capturedSceneLayout = null;
            if (!runState.UsesSceneLayout && runState.NeedsLaunchRandomization)
            {
                runState.RandomizeLaunchForBuild();
            }

            if (!runState.UsesSceneLayout && runState.NeedsTargetRandomization)
            {
                runState.RandomizeTargetLineupForBuild();
            }

            ClearExistingStage();

            var prefabs = ResolvePrefabSet();
            var stage = runState.Stage;

            PlaceBackground(prefabs);
            PlaceTerrainCollider(prefabs);
            var launchPedestalCenter = runState.LaunchPedestalCenter;
            PlaceGolfClub(prefabs, runState);
            PlaceLaunchPedestal(prefabs, launchPedestalCenter);
            PlaceTargetPedestal(prefabs);
            PlaceTargetMoaiLineup(ResolveTargetLineup(runState), stage.SuccessZone, 2, prefabs);
            PlaceSuccessZone(prefabs, stage.SuccessZone.center, stage.SuccessZone.size);
            PlaceAimMarker(prefabs, stage);
            PlaceLaunchMoai(prefabs, runState);
        }

        private bool TryBuildFromScene(MoaiGolfRunState runState)
        {
            var elements = GetComponentsInChildren<MoaiGolfStageElement>(true);
            if (elements.Length == 0)
            {
                return false;
            }

            LaunchBody = null;
            LaunchVisual = null;
            GolfClubPivot = null;

            if (!capturedSceneLayout.HasValue)
            {
                capturedSceneLayout = CaptureSceneLayout();
                runState.ApplySceneLayout(capturedSceneLayout.Value);
            }

            BindLaunchMoaiFromScene(runState);
            BindGolfClubFromScene();
            PlaceTargetMoaiLineupOnPedestal(
                ResolveTargetLineup(runState),
                TryGetSuccessZoneRect(),
                2,
                capturedSceneLayout.Value.TargetMoaiSlotPositions
            );
            ApplyLaunchPlacement(runState);

            return LaunchBody != null;
        }

        private MoaiGolfSceneLayout CaptureSceneLayout()
        {
            var launchPosition = Vector2.zero;
            var launchElement = FindElement(MoaiGolfStageElementKind.LaunchMoai);
            if (launchElement != null)
            {
                launchPosition = launchElement.transform.position;
            }

            var launchPedestalCenter = TryGetLaunchPedestalCenter(launchPosition);
            var launchKind = launchElement != null
                ? launchElement.GetComponent<MoaiGolfMoaiEntity>()?.CurrentKind ?? MoaiGolfMoaiKind.Sunglasses
                : MoaiGolfMoaiKind.Sunglasses;
            var launchVisualFeetY = SampleLaunchSurfaceY(launchPedestalCenter.x);
            var successZone = TryGetSuccessZoneRect();

            return new MoaiGolfSceneLayout(
                launchPosition,
                launchPedestalCenter,
                launchVisualFeetY,
                successZone,
                CaptureTargetMoaiSlotPositions()
            );
        }

        private Vector2[] CaptureTargetMoaiSlotPositions()
        {
            var slots = FindTargetMoaiElements();
            var positions = new Vector2[slots.Length];
            for (var index = 0; index < slots.Length; index++)
            {
                positions[index] = new Vector2(
                    GetTargetMoaiVisualCenterX(slots[index]),
                    slots[index].transform.position.y
                );
            }

            return positions;
        }

        public static float GetTargetMoaiVisualCenterX(MoaiGolfStageElement slot)
        {
            if (slot == null)
            {
                return 0f;
            }

            var entity = slot.GetComponent<MoaiGolfMoaiEntity>();
            if (entity != null && entity.Visual != null)
            {
                var renderer = entity.Visual.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    return renderer.bounds.center.x;
                }
            }

            return slot.transform.position.x;
        }

        private static MoaiGolfMoaiKind[] ResolveTargetLineup(MoaiGolfRunState runState)
        {
            return runState.TargetLineup != null && runState.TargetLineup.Length > 0
                ? runState.TargetLineup
                : DefaultTargetLineup;
        }

        private Vector2 TryGetLaunchPedestalCenter(Vector2 fallbackLaunchPosition)
        {
            var launchPedestal = FindElement(MoaiGolfStageElementKind.LaunchPedestal);
            if (launchPedestal != null)
            {
                var collider = launchPedestal.GetComponent<Collider2D>();
                if (collider != null)
                {
                    return collider.bounds.center;
                }

                return launchPedestal.transform.position;
            }

            return fallbackLaunchPosition;
        }

        private Rect TryGetSuccessZoneRect()
        {
            var successZoneElement = FindElement(MoaiGolfStageElementKind.SuccessZone);
            if (successZoneElement == null)
            {
                return MoaiGolfStageDefinition.CreateFirstStage().SuccessZone;
            }

            var collider = successZoneElement.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                return MoaiGolfStageDefinition.CreateFirstStage().SuccessZone;
            }

            var worldScale = successZoneElement.transform.lossyScale;
            var worldSize = Vector2.Scale(collider.size, worldScale);
            var worldCenter = (Vector2)successZoneElement.transform.position
                + Vector2.Scale(collider.offset, worldScale);
            if (worldSize.x > 0f && worldSize.y > 0f)
            {
                return new Rect(
                    worldCenter.x - worldSize.x * 0.5f,
                    worldCenter.y - worldSize.y * 0.5f,
                    worldSize.x,
                    worldSize.y
                );
            }

            var bounds = collider.bounds;
            return new Rect(bounds.min.x, bounds.min.y, bounds.size.x, bounds.size.y);
        }

        private void BindLaunchMoaiFromScene(MoaiGolfRunState runState)
        {
            var element = FindElement(MoaiGolfStageElementKind.LaunchMoai);
            if (element == null)
            {
                return;
            }

            var entity = element.GetComponent<MoaiGolfMoaiEntity>() ?? element.gameObject.AddComponent<MoaiGolfMoaiEntity>();
            entity.ConfigureLaunch(runState.LaunchMoaiKind);
            LaunchBody = entity.Body;
            LaunchVisual = entity.Visual;
        }

        private void BindGolfClubFromScene()
        {
            var element = FindElement(MoaiGolfStageElementKind.GolfClubPivot);
            if (element == null)
            {
                return;
            }

            GolfClubPivot = element.transform;
        }

        private void PlaceTargetMoaiLineupOnPedestal(
            MoaiGolfMoaiKind[] kinds,
            Rect successZone,
            int sortingOrder,
            Vector2[] fixedSlotPositions = null
        )
        {
            var slots = FindTargetMoaiElements();
            if (kinds == null || kinds.Length == 0 || slots.Length == 0)
            {
                return;
            }

            if (fixedSlotPositions != null && fixedSlotPositions.Length > 0)
            {
                for (var index = 0; index < kinds.Length && index < slots.Length; index++)
                {
                    var position = index < fixedSlotPositions.Length
                        ? fixedSlotPositions[index]
                        : fixedSlotPositions[^1];
                    ConfigureTargetMoaiOnSlot(slots[index], kinds[index], sortingOrder, position);
                }
            }
            else
            {
                var lineupXs = BuildTargetLineupXPositions(kinds);
                for (var index = 0; index < kinds.Length && index < slots.Length; index++)
                {
                    ConfigureTargetMoaiOnSlot(slots[index], kinds[index], sortingOrder, null, lineupXs[index]);
                }
            }

            for (var index = kinds.Length; index < slots.Length; index++)
            {
                slots[index].gameObject.SetActive(false);
            }
        }

        private void ConfigureTargetMoaiOnSlot(
            MoaiGolfStageElement slot,
            MoaiGolfMoaiKind kind,
            int sortingOrder,
            Vector2? fixedRootPosition = null,
            float? computedRootX = null
        )
        {
            slot.gameObject.SetActive(true);
            var entity = slot.GetComponent<MoaiGolfMoaiEntity>() ?? slot.gameObject.AddComponent<MoaiGolfMoaiEntity>();
            entity.ConfigureTarget(kind, sortingOrder);
            if (fixedRootPosition.HasValue)
            {
                var anchor = fixedRootPosition.Value;
                var anchoredRootX = GetTargetMoaiRootXForVisualCenter(kind, anchor.x);
                slot.transform.position = new Vector2(anchoredRootX, anchor.y);
                return;
            }

            var rootX = computedRootX ?? slot.transform.position.x;
            var visualFeetY = GetTargetPedestalTopY(rootX) + TargetMoaiVisualFeetLift;
            slot.transform.position = new Vector2(rootX, ResolveTargetMoaiRootY(kind, visualFeetY));
        }

        public static float GetTargetMoaiVisualHalfWidth(MoaiGolfMoaiKind kind)
        {
            var spec = MoaiGolfMoaiSpecRegistry.GetSpec(kind);
            var sprite = MoaiGolfMoaiSpecRegistry.GetSprite(kind) ?? MoaiGolfSpriteCatalog.GetMoai(kind);
            if (sprite == null)
            {
                return spec.ColliderSize.x * 0.5f;
            }

            return sprite.bounds.size.x * spec.VisualScale.x * 0.5f;
        }

        public static float GetTargetMoaiVisualLocalX(MoaiGolfMoaiKind kind)
        {
            var spec = MoaiGolfMoaiSpecRegistry.GetSpec(kind);
            var sprite = MoaiGolfMoaiSpecRegistry.GetSprite(kind) ?? MoaiGolfSpriteCatalog.GetMoai(kind);
            if (sprite == null)
            {
                return 0f;
            }

            return -sprite.bounds.center.x * spec.VisualScale.x;
        }

        public static float GetTargetMoaiRootXForVisualCenter(MoaiGolfMoaiKind kind, float visualCenterX)
        {
            return visualCenterX - GetTargetMoaiVisualLocalX(kind);
        }

        public static float ClampTargetMoaiRootXToRim(MoaiGolfMoaiKind kind, float rootX, float rimLeft, float rimRight)
        {
            var visualLocalX = GetTargetMoaiVisualLocalX(kind);
            var halfWidth = GetTargetMoaiVisualHalfWidth(kind);
            var visualCenterX = rootX + visualLocalX;
            var visualLeft = visualCenterX - halfWidth;
            var visualRight = visualCenterX + halfWidth;
            if (visualLeft < rimLeft)
            {
                visualCenterX += rimLeft - visualLeft;
            }

            if (visualRight > rimRight)
            {
                visualCenterX -= visualRight - rimRight;
            }

            return visualCenterX - visualLocalX;
        }

        public static float ResolveTargetMoaiRootY(MoaiGolfMoaiKind kind, float visualFeetY)
        {
            var spec = MoaiGolfMoaiSpecRegistry.GetSpec(kind);
            return visualFeetY + spec.VisualHalfHeightWorld - spec.FeetOffsetWorld;
        }

        public static System.Collections.Generic.List<float> BuildTargetLineupXPositions(
            MoaiGolfMoaiKind[] kinds,
            float rimLeft,
            float rimRight)
        {
            var positions = new System.Collections.Generic.List<float>(kinds?.Length ?? 0);
            if (kinds == null || kinds.Length == 0)
            {
                return positions;
            }

            var visualLeft = rimLeft;
            for (var index = 0; index < kinds.Length; index++)
            {
                var halfWidth = GetTargetMoaiVisualHalfWidth(kinds[index]);
                var visualCenterX = visualLeft + halfWidth;
                positions.Add(GetTargetMoaiRootXForVisualCenter(kinds[index], visualCenterX));
                visualLeft = visualCenterX + halfWidth;
            }

            return positions;
        }

        private System.Collections.Generic.List<float> BuildTargetLineupXPositions(MoaiGolfMoaiKind[] kinds)
        {
            var points = ResolveTargetPedestalTopSurfacePoints();
            return BuildTargetLineupXPositions(kinds, points[0].x, points[^1].x);
        }

        private Vector2[] ResolveTargetPedestalTopSurfacePoints()
        {
            if (resolvedTargetPedestalTopSurfacePoints != null)
            {
                return resolvedTargetPedestalTopSurfacePoints;
            }

            var pedestal = FindElement(MoaiGolfStageElementKind.TargetPedestal);
            if (pedestal != null)
            {
                var polygon = pedestal.GetComponent<PolygonCollider2D>();
                if (polygon != null && polygon.points.Length >= 3)
                {
                    var bounds = polygon.bounds;
                    var rimThreshold = bounds.min.y + bounds.size.y * 0.55f;
                    var rimPoints = new System.Collections.Generic.List<Vector2>();
                    foreach (var localPoint in polygon.points)
                    {
                        var worldPoint = pedestal.transform.TransformPoint(localPoint);
                        if (worldPoint.y >= rimThreshold)
                        {
                            rimPoints.Add(worldPoint);
                        }
                    }

                    if (rimPoints.Count >= 2)
                    {
                        rimPoints.Sort((left, right) => left.x.CompareTo(right.x));
                        resolvedTargetPedestalTopSurfacePoints = rimPoints.ToArray();
                        return resolvedTargetPedestalTopSurfacePoints;
                    }
                }
            }

            resolvedTargetPedestalTopSurfacePoints = TargetPedestalTopSurfacePoints;
            return resolvedTargetPedestalTopSurfacePoints;
        }

        private void ApplyLaunchPlacement(MoaiGolfRunState runState)
        {
            var launchX = runState.LaunchPedestalCenter.x;
            var surfaceY = SampleLaunchSurfaceY(launchX);
            var spec = MoaiGolfMoaiSpec.Get(runState.LaunchMoaiKind);
            runState.SetLaunchPlacement(
                spec.GetBodyCenterFromVisualFeet(new Vector2(launchX, surfaceY)),
                new Vector2(launchX, runState.LaunchPedestalCenter.y),
                surfaceY
            );

            if (LaunchBody != null)
            {
                LaunchBody.rotation = 0f;
                runState.ResetBodyForRetry(LaunchBody);
            }

            var launchElement = FindElement(MoaiGolfStageElementKind.LaunchMoai);
            if (launchElement != null)
            {
                launchElement.transform.position = runState.LaunchPosition;
            }

            var launchPedestal = FindElement(MoaiGolfStageElementKind.LaunchPedestal);
            if (launchPedestal != null)
            {
                launchPedestal.transform.position = new Vector3(
                    runState.LaunchPedestalCenter.x,
                    runState.LaunchPedestalCenter.y,
                    0f
                );
            }

            if (GolfClubPivot != null)
            {
                var pivotPos = runState.LaunchPosition + MoaiGolfLaunchAnimator.ClubAnchorOffsetFromMoai;
                GolfClubPivot.position = new Vector3(pivotPos.x, pivotPos.y, 0f);
                GolfClubPivot.rotation = Quaternion.Euler(0f, 0f, MoaiGolfLaunchAnimator.ClubWindupAngleDeg);
            }
        }

        private MoaiGolfStageElement FindElement(MoaiGolfStageElementKind kind, int slotIndex = 0)
        {
            foreach (var element in GetComponentsInChildren<MoaiGolfStageElement>(true))
            {
                if (element.Kind == kind && element.SlotIndex == slotIndex)
                {
                    return element;
                }
            }

            return null;
        }

        private MoaiGolfStageElement[] FindTargetMoaiElements()
        {
            var targets = new System.Collections.Generic.List<MoaiGolfStageElement>();
            foreach (var element in GetComponentsInChildren<MoaiGolfStageElement>(true))
            {
                if (element.Kind == MoaiGolfStageElementKind.TargetMoai)
                {
                    targets.Add(element);
                }
            }

            targets.Sort((left, right) => left.SlotIndex.CompareTo(right.SlotIndex));
            return targets.ToArray();
        }

        private MoaiGolfStagePrefabSet ResolvePrefabSet()
        {
            return prefabSet != null ? prefabSet : MoaiGolfStagePrefabSet.LoadDefault();
        }

        private void ClearExistingStage()
        {
            for (var childIndex = transform.childCount - 1; childIndex >= 0; childIndex--)
            {
                Destroy(transform.GetChild(childIndex).gameObject);
            }

            LaunchBody = null;
            LaunchVisual = null;
            GolfClubPivot = null;
        }

        private void PlaceBackground(MoaiGolfStagePrefabSet prefabs)
        {
            if (prefabs != null && prefabs.backgroundVisualPrefab != null)
            {
                var instance = Instantiate(prefabs.backgroundVisualPrefab, transform);
                instance.name = prefabs.backgroundVisualPrefab.name;
                instance.transform.position = Vector3.zero;
                return;
            }

            CreatePixelPerfectSprite("Background Visual", MoaiGolfSpriteCatalog.Background, Vector2.zero, Color.white, -10);
        }

        private void PlaceTerrainCollider(MoaiGolfStagePrefabSet prefabs)
        {
            if (prefabs != null && prefabs.terrainColliderPrefab != null)
            {
                var instance = Instantiate(prefabs.terrainColliderPrefab, transform);
                instance.name = prefabs.terrainColliderPrefab.name;
                instance.transform.position = Vector3.zero;
                return;
            }

            CreateTerrainColliderFallback();
        }

        private void PlaceLaunchPedestal(MoaiGolfStagePrefabSet prefabs, Vector2 launchPedestalCenter)
        {
            if (prefabs != null && prefabs.launchPedestalPrefab != null)
            {
                var instance = Instantiate(prefabs.launchPedestalPrefab, transform);
                instance.name = prefabs.launchPedestalPrefab.name;
                instance.transform.position = new Vector3(launchPedestalCenter.x, launchPedestalCenter.y, 0f);
                return;
            }

            CreateBoxFallback(
                "Launch Pedestal Collider",
                launchPedestalCenter,
                new Vector2(MoaiGolfWorldSettings.LaunchPedestalWidth, MoaiGolfWorldSettings.LaunchPedestalHeight),
                new Color(0.45f, 0.34f, 0.25f, 0.45f),
                true,
                false,
                1
            );
        }

        private void PlaceTargetPedestal(MoaiGolfStagePrefabSet prefabs)
        {
            if (prefabs != null && prefabs.targetPedestalPrefab != null)
            {
                var instance = Instantiate(prefabs.targetPedestalPrefab, transform);
                instance.name = prefabs.targetPedestalPrefab.name;
                instance.transform.position = Vector3.zero;
                return;
            }

            CreateTargetPedestalColliderFallback();
        }

        private void PlaceSuccessZone(MoaiGolfStagePrefabSet prefabs, Vector2 center, Vector2 size)
        {
            if (prefabs != null && prefabs.successZonePrefab != null)
            {
                var instance = Instantiate(prefabs.successZonePrefab, transform);
                instance.name = prefabs.successZonePrefab.name;
                instance.transform.position = new Vector3(center.x, center.y, 0f);
                instance.transform.localScale = new Vector3(size.x, size.y, 1f);
                return;
            }

            CreateBoxFallback("Success Zone Trigger", center, size, new Color(1f, 0.1f, 0.08f, 0.32f), true, true, 3);
        }

        private void PlaceGolfClub(MoaiGolfStagePrefabSet prefabs, MoaiGolfRunState runState)
        {
            if (prefabs != null && prefabs.golfClubPivotPrefab != null)
            {
                var instance = Instantiate(prefabs.golfClubPivotPrefab, transform);
                instance.name = prefabs.golfClubPivotPrefab.name;
                var pivotPos = runState.LaunchPosition + MoaiGolfLaunchAnimator.ClubAnchorOffsetFromMoai;
                instance.transform.position = new Vector3(pivotPos.x, pivotPos.y, 0f);
                instance.transform.rotation = Quaternion.Euler(0f, 0f, MoaiGolfLaunchAnimator.ClubWindupAngleDeg);
                GolfClubPivot = instance.transform;
                return;
            }

            GolfClubPivot = CreateGolfClubFallback(runState).transform;
        }

        private void PlaceAimMarker(MoaiGolfStagePrefabSet prefabs, MoaiGolfStageDefinition stage)
        {
            var zone = stage.SuccessZone;
            var hereSprite = MoaiGolfSpriteCatalog.Here;
            var dashedBoxSize = new Vector2(0.87f, 1.17f);
            var dashedBoxOffsetFromCenter = new Vector2(-0.415f, -0.115f);
            var padding = new Vector2(0.6f, 0.5f);
            var scale = new Vector2(
                (zone.width + padding.x) / dashedBoxSize.x,
                (zone.height + padding.y) / dashedBoxSize.y
            );
            var center = zone.center - new Vector2(
                dashedBoxOffsetFromCenter.x * scale.x,
                dashedBoxOffsetFromCenter.y * scale.y
            );

            if (prefabs != null && prefabs.aimMarkerPrefab != null)
            {
                var instance = Instantiate(prefabs.aimMarkerPrefab, transform);
                instance.name = prefabs.aimMarkerPrefab.name;
                instance.transform.localScale = new Vector3(scale.x, scale.y, 1f);
                var renderer = instance.GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null)
                {
                    var boundsCenter = renderer.sprite.bounds.center;
                    instance.transform.position = new Vector3(
                        center.x - boundsCenter.x * scale.x,
                        center.y - boundsCenter.y * scale.y,
                        0f
                    );
                }
                else
                {
                    instance.transform.position = new Vector3(center.x, center.y, 0f);
                }

                return;
            }

            CreateCenteredSprite("Here Label Visual", hereSprite, center, scale, Color.white, 5);
        }

        private void PlaceTargetMoaiLineup(MoaiGolfMoaiKind[] kinds, Rect successZone, int sortingOrder, MoaiGolfStagePrefabSet prefabs)
        {
            if (kinds == null || kinds.Length == 0)
            {
                return;
            }

            var lineupXs = BuildTargetLineupXPositions(kinds);
            for (var index = 0; index < kinds.Length && index < lineupXs.Count; index++)
            {
                var x = lineupXs[index];
                PlaceTargetMoai(kinds[index], x, GetTargetPedestalTopY(x) + TargetMoaiVisualFeetLift, sortingOrder, prefabs);
            }
        }

        private void PlaceTargetMoai(MoaiGolfMoaiKind kind, float rootX, float visualFeetY, int sortingOrder, MoaiGolfStagePrefabSet prefabs)
        {
            var rootY = ResolveTargetMoaiRootY(kind, visualFeetY);

            if (prefabs != null && prefabs.targetMoaiPrefab != null)
            {
                var instance = Instantiate(prefabs.targetMoaiPrefab, transform);
                instance.name = $"Target Moai {kind}";
                var entity = instance.GetComponent<MoaiGolfMoaiEntity>() ?? instance.AddComponent<MoaiGolfMoaiEntity>();
                entity.ConfigureTarget(kind, sortingOrder);
                instance.transform.position = new Vector2(rootX, rootY);
                return;
            }

            CreateTargetMoaiFallback(kind, rootX, rootY, sortingOrder);
        }

        private void PlaceLaunchMoai(MoaiGolfStagePrefabSet prefabs, MoaiGolfRunState runState)
        {
            if (prefabs != null && prefabs.launchMoaiPrefab != null)
            {
                var instance = Instantiate(prefabs.launchMoaiPrefab, transform);
                instance.name = "Launch Moai";
                instance.transform.position = runState.LaunchPosition;
                var entity = instance.GetComponent<MoaiGolfMoaiEntity>() ?? instance.AddComponent<MoaiGolfMoaiEntity>();
                entity.ConfigureLaunch(runState.LaunchMoaiKind);
                LaunchBody = entity.Body;
                LaunchVisual = entity.Visual;
                return;
            }

            CreateLaunchMoaiFallback(runState);
        }

        private float GetTargetPedestalTopY(float x)
        {
            var points = ResolveTargetPedestalTopSurfacePoints();
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

                var t = Mathf.InverseLerp(previous.x, next.x, x);
                return Mathf.Lerp(previous.y, next.y, t);
            }

            return points[^1].y;
        }

        #region Runtime fallback

        private static Sprite whiteSprite;

        private GameObject CreateBoxFallback(string objectName, Vector2 center, Vector2 size, Color color, bool addCollider, bool isTrigger, int sortingOrder)
        {
            var box = new GameObject(objectName);
            box.transform.SetParent(transform);
            box.transform.position = new Vector3(center.x, center.y, 0f);
            box.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = box.AddComponent<SpriteRenderer>();
            renderer.sprite = GetWhiteSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            if (addCollider)
            {
                var collider = box.AddComponent<BoxCollider2D>();
                collider.isTrigger = isTrigger;
            }

            return box;
        }

        private void CreateTerrainColliderFallback()
        {
            var terrainObject = new GameObject("Terrain Black Line Collider");
            terrainObject.transform.SetParent(transform);
            terrainObject.transform.position = Vector3.zero;

            var collider = terrainObject.AddComponent<EdgeCollider2D>();
            collider.points = MoaiGolfTerrainProfile.ColliderPoints;
            collider.edgeRadius = 0.035f;
        }

        private void CreateTargetPedestalColliderFallback()
        {
            var pedestal = new GameObject("Target Pedestal Collider");
            pedestal.transform.SetParent(transform);
            pedestal.transform.position = Vector3.zero;

            var collider = pedestal.AddComponent<PolygonCollider2D>();
            collider.points = new[]
            {
                new Vector2(RightPedestalLeftX, RightPedestalBaseY),
                new Vector2(RightPedestalRightX, RightPedestalBaseY),
                new Vector2(RightPedestalRightX, TargetPedestalTopSurfacePoints[^1].y),
                TargetPedestalTopSurfacePoints[7],
                TargetPedestalTopSurfacePoints[6],
                TargetPedestalTopSurfacePoints[5],
                TargetPedestalTopSurfacePoints[4],
                TargetPedestalTopSurfacePoints[3],
                TargetPedestalTopSurfacePoints[2],
                TargetPedestalTopSurfacePoints[1],
                TargetPedestalTopSurfacePoints[0]
            };

            var body = pedestal.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
        }

        private GameObject CreateGolfClubFallback(MoaiGolfRunState runState)
        {
            var spriteScale = MoaiGolfLaunchAnimator.ClubSpriteScale;
            var anchorLocal = new Vector2(
                MoaiGolfLaunchAnimator.ClubAnchorLocalXPixels,
                MoaiGolfLaunchAnimator.ClubAnchorLocalYPixels
            ) / MoaiGolfWorldSettings.PixelsPerUnit * spriteScale;

            var pivot = new GameObject("Golf Club Pivot");
            pivot.transform.SetParent(transform, false);
            var pivotPos = runState.LaunchPosition + MoaiGolfLaunchAnimator.ClubAnchorOffsetFromMoai;
            pivot.transform.position = new Vector3(pivotPos.x, pivotPos.y, 0f);
            pivot.transform.rotation = Quaternion.Euler(0f, 0f, MoaiGolfLaunchAnimator.ClubWindupAngleDeg);

            var visual = new GameObject("Golf Club Visual");
            visual.transform.SetParent(pivot.transform, false);
            visual.transform.localPosition = new Vector3(-anchorLocal.x, -anchorLocal.y, 0f);
            visual.transform.localScale = new Vector3(spriteScale, spriteScale, 1f);

            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = MoaiGolfSpriteCatalog.GolfClub;
            renderer.sortingOrder = 3;
            return pivot;
        }

        private void CreateTargetMoaiFallback(MoaiGolfMoaiKind kind, float x, float centerY, int sortingOrder)
        {
            var spec = MoaiGolfMoaiSpecRegistry.GetSpec(kind);
            var moai = new GameObject($"Target Moai {kind}");
            moai.transform.SetParent(transform);
            moai.transform.position = new Vector2(x, centerY);
            CreateCenteredSpriteChild(
                "Target Moai Visual",
                moai.transform,
                MoaiGolfMoaiSpecRegistry.GetSprite(kind) ?? MoaiGolfSpriteCatalog.GetMoai(kind),
                spec.VisualScale,
                Color.white,
                sortingOrder
            );

            var collider = moai.AddComponent<CapsuleCollider2D>();
            collider.size = spec.ColliderSize;
            collider.direction = CapsuleDirection2D.Vertical;

            var body = moai.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            moai.AddComponent<MoaiGolfTargetMoaiMarker>();
        }

        private void CreateLaunchMoaiFallback(MoaiGolfRunState runState)
        {
            var spec = MoaiGolfMoaiSpecRegistry.GetSpec(runState.LaunchMoaiKind);
            var moai = new GameObject("Launch Moai Collider");
            moai.transform.SetParent(transform);
            moai.transform.position = runState.LaunchPosition;
            LaunchVisual = CreateCenteredSpriteChild(
                "Launch Moai Visual",
                moai.transform,
                MoaiGolfMoaiSpecRegistry.GetSprite(runState.LaunchMoaiKind) ?? MoaiGolfSpriteCatalog.GetMoai(runState.LaunchMoaiKind),
                spec.VisualScale,
                Color.white,
                4
            ).transform;

            var collider = moai.AddComponent<CapsuleCollider2D>();
            collider.size = spec.ColliderSize;
            collider.direction = CapsuleDirection2D.Vertical;

            var material = new PhysicsMaterial2D($"{runState.LaunchMoaiKind} Material")
            {
                bounciness = spec.Bounciness,
                friction = spec.Friction
            };
            collider.sharedMaterial = material;

            LaunchBody = moai.AddComponent<Rigidbody2D>();
            LaunchBody.mass = spec.Mass;
            LaunchBody.bodyType = RigidbodyType2D.Dynamic;
            LaunchBody.Sleep();
            moai.AddComponent<MoaiGolfBounceSfx>();
            moai.AddComponent<MoaiGolfTargetMoaiVoiceSfx>();
            moai.AddComponent<MoaiGolfWorldBoundsBounce>();
            moai.AddComponent<MoaiGolfLandingJudge>();
        }

        private GameObject CreatePixelPerfectSprite(string objectName, Sprite sprite, Vector2 bottomLeft, Color color, int sortingOrder)
        {
            return CreateSprite(objectName, sprite, bottomLeft, Vector2.one, color, sortingOrder);
        }

        private GameObject CreateSprite(string objectName, Sprite sprite, Vector2 bottomLeft, Vector2 scale, Color color, int sortingOrder)
        {
            var spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(transform);
            spriteObject.transform.position = new Vector3(bottomLeft.x, bottomLeft.y, 0f);
            spriteObject.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            var renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : GetWhiteSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return spriteObject;
        }

        private GameObject CreateCenteredSprite(string objectName, Sprite sprite, Vector2 center, Vector2 scale, Color color, int sortingOrder)
        {
            var spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(transform);
            spriteObject.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            var renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : GetWhiteSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            var boundsCenter = renderer.sprite.bounds.center;
            spriteObject.transform.position = new Vector3(center.x - boundsCenter.x * scale.x, center.y - boundsCenter.y * scale.y, 0f);
            return spriteObject;
        }

        private GameObject CreateCenteredSpriteChild(string objectName, Transform parent, Sprite sprite, Vector2 scale, Color color, int sortingOrder)
        {
            var spriteObject = new GameObject(objectName);
            spriteObject.transform.SetParent(parent);
            spriteObject.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            var renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : GetWhiteSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            var boundsCenter = renderer.sprite.bounds.center;
            spriteObject.transform.localPosition = new Vector3(-boundsCenter.x * scale.x, -boundsCenter.y * scale.y, 0f);
            return spriteObject;
        }

        private static Sprite GetWhiteSprite()
        {
            if (whiteSprite != null)
            {
                return whiteSprite;
            }

            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            return whiteSprite;
        }

        #endregion
    }
}
