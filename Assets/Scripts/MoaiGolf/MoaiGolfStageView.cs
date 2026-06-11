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
        public const int TargetMoaiPerKindCount = 5;
        public const int ActiveTargetMoaiSlotCount = 5;
        public const int SceneTargetMoaiPoolCount = TargetMoaiPerKindCount * 4;
        private const float TargetMoaiSuccessZonePadding = 0.28f;

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
        [SerializeField] private MoaiGolfStageElement backgroundElement;
        [SerializeField] private MoaiGolfStageElement terrainElement;
        [SerializeField] private MoaiGolfStageElement launchPedestalElement;
        [SerializeField] private MoaiGolfStageElement targetPedestalElement;
        [SerializeField] private MoaiGolfStageElement successZoneElement;
        [SerializeField] private MoaiGolfStageElement aimMarkerElement;
        [SerializeField] private MoaiGolfStageElement golfClubPivotElement;
        [SerializeField] private MoaiGolfStageElement launchMoaiElement;
        [SerializeField] private MoaiGolfStageElement[] targetMoaiPoolElements = System.Array.Empty<MoaiGolfStageElement>();
        [SerializeField] private MoaiGolfGameController gameController;
        [SerializeField] private MoaiGolfSeController seController;

        private MoaiGolfSceneLayout? capturedSceneLayout;
        private Vector2[] resolvedTargetPedestalTopSurfacePoints;
        private MoaiGolfGameController runtimeGameController;
        private MoaiGolfRunState runtimeRunState;
        private MoaiGolfSeController runtimeSeController;

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

        private void Reset()
        {
            RefreshSerializedSceneReferencesForEditor();
        }

        private void OnValidate()
        {
            RefreshSerializedSceneReferencesForEditor();
        }

        public void RefreshSerializedDependenciesForEditor(MoaiGolfGameController controller, MoaiGolfSeController se)
        {
            gameController = controller != null ? controller : FindAnyObjectByType<MoaiGolfGameController>();
            seController = se != null ? se : FindAnyObjectByType<MoaiGolfSeController>();
        }

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
            ApplyTargetMoaiPoolVisibility(GetDefaultPreviewPoolIndices(), 2);
        }

        public static MoaiGolfMoaiKind GetTargetMoaiKindForPoolIndex(int poolIndex)
        {
            return (MoaiGolfMoaiKind)Mathf.Clamp(poolIndex / TargetMoaiPerKindCount, 0, 3);
        }

        public static int GetTargetMoaiPoolBaseIndex(MoaiGolfMoaiKind kind)
        {
            return (int)kind * TargetMoaiPerKindCount;
        }

        private static int[] GetDefaultPreviewPoolIndices()
        {
            return new[]
            {
                GetTargetMoaiPoolBaseIndex(MoaiGolfMoaiKind.Sunglasses),
                GetTargetMoaiPoolBaseIndex(MoaiGolfMoaiKind.Ribbon),
                GetTargetMoaiPoolBaseIndex(MoaiGolfMoaiKind.Macho),
                GetTargetMoaiPoolBaseIndex(MoaiGolfMoaiKind.Sunglasses) + 1,
                GetTargetMoaiPoolBaseIndex(MoaiGolfMoaiKind.Snowman),
            };
        }

        public void Build(MoaiGolfRunState runState)
        {
            if (runState == null)
            {
                Debug.LogError($"{nameof(MoaiGolfStageView)} requires a {nameof(MoaiGolfRunState)} before Build.", this);
                return;
            }

            if (!useScenePlacedElements)
            {
                Debug.LogError($"{nameof(MoaiGolfStageView)} is configured to build runtime objects. Place stage objects in the scene and assign them with [SerializeField].", this);
                return;
            }

            if (!TryBuildFromScene(runState))
            {
                Debug.LogError($"{nameof(MoaiGolfStageView)} could not build because one or more serialized scene references are missing.", this);
            }
        }

        private bool TryBuildFromScene(MoaiGolfRunState runState)
        {
            if (!ValidateSceneReferences())
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
            ApplyTargetMoaiPoolVisibility(runState.SelectedTargetMoaiPoolIndices, 2);
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
                null
            );
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

        public static Vector3 ResolveBackgroundVisualPosition(Sprite sprite)
        {
            if (sprite == null)
            {
                return Vector3.zero;
            }

            return new Vector3(
                sprite.pivot.x / sprite.pixelsPerUnit,
                sprite.pivot.y / sprite.pixelsPerUnit,
                0f
            );
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

        public bool TryGetSuccessZoneRect(out Rect rect)
        {
            rect = TryGetSuccessZoneRect();
            return rect.width > 0f && rect.height > 0f;
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
                Debug.LogError($"{nameof(MoaiGolfStageView)} requires a serialized Launch Moai reference.", this);
                return;
            }

            var entity = element.GetComponent<MoaiGolfMoaiEntity>();
            if (entity == null)
            {
                Debug.LogError($"{element.name} requires a {nameof(MoaiGolfMoaiEntity)} component assigned in the scene or prefab.", element);
                return;
            }

            entity.ConfigureLaunch(runState.LaunchMoaiKind);
            CacheRuntimeDependencies(runState);
            ConfigureLaunchRuntimeComponents(element.gameObject);
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

        public void ApplyTargetMoaiPoolVisibility(int[] selectedPoolIndices, int sortingOrder = 2)
        {
            var pool = FindTargetMoaiPoolElements();
            if (pool.Length == 0 || selectedPoolIndices == null || selectedPoolIndices.Length == 0)
            {
                Debug.LogError($"{nameof(MoaiGolfStageView)} requires serialized target Moai pool references before showing the lineup.", this);
                return;
            }

            foreach (var element in pool)
            {
                if (element != null)
                {
                    element.gameObject.SetActive(false);
                }
            }

            var kinds = new System.Collections.Generic.List<MoaiGolfMoaiKind>(selectedPoolIndices.Length);
            foreach (var poolIndex in selectedPoolIndices)
            {
                if (poolIndex >= 0 && poolIndex < pool.Length)
                {
                    kinds.Add(GetTargetMoaiKindForPoolIndex(poolIndex));
                }
            }

            var lineupXs = BuildTargetLineupXPositionsAvoidingSuccessZone(kinds.ToArray(), TryGetSuccessZoneRect());
            var activeIndex = 0;
            foreach (var poolIndex in selectedPoolIndices)
            {
                if (poolIndex < 0 || poolIndex >= pool.Length)
                {
                    continue;
                }

                var element = pool[poolIndex];
                if (element == null)
                {
                    continue;
                }

                var kind = GetTargetMoaiKindForPoolIndex(poolIndex);
                var rootX = activeIndex < lineupXs.Count ? lineupXs[activeIndex] : element.transform.position.x;
                ConfigureTargetMoaiOnSlot(element, kind, sortingOrder, null, rootX);
                activeIndex++;
            }
        }

        private void PlaceTargetMoaiLineupOnPedestal(
            MoaiGolfMoaiKind[] kinds,
            Rect successZone,
            int sortingOrder,
            Vector2[] fixedSlotPositions = null,
            int[] selectedPoolIndices = null
        )
        {
            if (selectedPoolIndices != null && selectedPoolIndices.Length > 0)
            {
                ApplyTargetMoaiPoolVisibility(selectedPoolIndices, sortingOrder);
                return;
            }

            var pool = FindTargetMoaiPoolElements();
            if (kinds == null || kinds.Length == 0 || pool.Length == 0)
            {
                return;
            }

            if (fixedSlotPositions != null && fixedSlotPositions.Length > 0)
            {
                for (var index = 0; index < kinds.Length && index < pool.Length; index++)
                {
                    var position = index < fixedSlotPositions.Length
                        ? fixedSlotPositions[index]
                        : fixedSlotPositions[^1];
                    ConfigureTargetMoaiOnSlot(pool[index], kinds[index], sortingOrder, position);
                }
            }
            else
            {
                var lineupXs = BuildTargetLineupXPositionsAvoidingSuccessZone(kinds, successZone);
                for (var index = 0; index < kinds.Length && index < pool.Length; index++)
                {
                    ConfigureTargetMoaiOnSlot(pool[index], kinds[index], sortingOrder, null, lineupXs[index]);
                }
            }

            for (var index = kinds.Length; index < pool.Length; index++)
            {
                pool[index].gameObject.SetActive(false);
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
            var entity = slot.GetComponent<MoaiGolfMoaiEntity>();
            if (entity == null)
            {
                Debug.LogError($"{slot.name} requires a serialized {nameof(MoaiGolfMoaiEntity)} component. Add it in the prefab or scene instead of creating it at runtime.", slot);
                return;
            }

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

        private System.Collections.Generic.List<float> BuildTargetLineupXPositionsAvoidingSuccessZone(
            MoaiGolfMoaiKind[] kinds,
            Rect successZone
        )
        {
            var points = ResolveTargetPedestalTopSurfacePoints();
            var rimLeft = points[0].x;
            var rimRight = points[^1].x;
            var positions = new System.Collections.Generic.List<float>(kinds?.Length ?? 0);
            if (kinds == null || kinds.Length == 0)
            {
                return positions;
            }

            var leftVisualX = rimLeft;
            var rightVisualX = Mathf.Min(successZone.xMax + TargetMoaiSuccessZonePadding, rimRight);
            var leftLimit = Mathf.Max(rimLeft, successZone.xMin - TargetMoaiSuccessZonePadding);
            var useRightSide = false;

            for (var index = 0; index < kinds.Length; index++)
            {
                var kind = kinds[index];
                var halfWidth = GetTargetMoaiVisualHalfWidth(kind);
                var width = halfWidth * 2f;

                if (!useRightSide && leftVisualX + width <= leftLimit)
                {
                    var visualCenterX = leftVisualX + halfWidth;
                    positions.Add(GetTargetMoaiRootXForVisualCenter(kind, visualCenterX));
                    leftVisualX += width;
                    continue;
                }

                useRightSide = true;
                var rightCenterX = Mathf.Min(rightVisualX + halfWidth, rimRight - halfWidth);
                positions.Add(GetTargetMoaiRootXForVisualCenter(kind, rightCenterX));
                rightVisualX = rightCenterX + halfWidth;
            }

            return positions;
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
                LaunchBody.GetComponent<MoaiGolfLandingJudge>()?.ResetForReuse();
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
            return kind switch
            {
                MoaiGolfStageElementKind.Background => backgroundElement,
                MoaiGolfStageElementKind.Terrain => terrainElement,
                MoaiGolfStageElementKind.LaunchPedestal => launchPedestalElement,
                MoaiGolfStageElementKind.TargetPedestal => targetPedestalElement,
                MoaiGolfStageElementKind.SuccessZone => successZoneElement,
                MoaiGolfStageElementKind.AimMarker => aimMarkerElement,
                MoaiGolfStageElementKind.GolfClubPivot => golfClubPivotElement,
                MoaiGolfStageElementKind.LaunchMoai => launchMoaiElement,
                MoaiGolfStageElementKind.TargetMoai => FindTargetMoaiPoolElement(slotIndex),
                _ => null
            };
        }

        private MoaiGolfStageElement[] FindTargetMoaiPoolElements()
        {
            return targetMoaiPoolElements ?? System.Array.Empty<MoaiGolfStageElement>();
        }

        private MoaiGolfStageElement FindTargetMoaiPoolElement(int slotIndex)
        {
            var pool = FindTargetMoaiPoolElements();
            for (var index = 0; index < pool.Length; index++)
            {
                var element = pool[index];
                if (element != null && element.SlotIndex == slotIndex)
                {
                    return element;
                }
            }

            return slotIndex >= 0 && slotIndex < pool.Length ? pool[slotIndex] : null;
        }

        private void CacheRuntimeDependencies(MoaiGolfRunState runState)
        {
            runtimeRunState = runState;
            runtimeGameController = gameController;
            runtimeSeController = seController;
        }

        private bool ValidateSceneReferences()
        {
            var isValid = true;
            isValid &= ValidateReference(launchPedestalElement, nameof(launchPedestalElement));
            isValid &= ValidateReference(targetPedestalElement, nameof(targetPedestalElement));
            isValid &= ValidateReference(successZoneElement, nameof(successZoneElement));
            isValid &= ValidateReference(golfClubPivotElement, nameof(golfClubPivotElement));
            isValid &= ValidateReference(launchMoaiElement, nameof(launchMoaiElement));
            isValid &= ValidateReference(gameController, nameof(gameController));
            isValid &= ValidateReference(seController, nameof(seController));

            if (targetMoaiPoolElements == null || targetMoaiPoolElements.Length < SceneTargetMoaiPoolCount)
            {
                Debug.LogError($"{nameof(MoaiGolfStageView)} requires {SceneTargetMoaiPoolCount} serialized target Moai pool references.", this);
                isValid = false;
            }
            else
            {
                for (var index = 0; index < targetMoaiPoolElements.Length; index++)
                {
                    isValid &= ValidateReference(targetMoaiPoolElements[index], $"{nameof(targetMoaiPoolElements)}[{index}]");
                }
            }

            return isValid;
        }

        private bool ValidateReference(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(MoaiGolfStageView)} missing serialized reference: {fieldName}.", this);
            return false;
        }

        public void RefreshSerializedSceneReferencesForEditor()
        {
            var elements = GetComponentsInChildren<MoaiGolfStageElement>(true);
            var targets = new System.Collections.Generic.List<MoaiGolfStageElement>();
            foreach (var element in elements)
            {
                switch (element.Kind)
                {
                    case MoaiGolfStageElementKind.Background:
                        backgroundElement = element;
                        break;
                    case MoaiGolfStageElementKind.Terrain:
                        terrainElement = element;
                        break;
                    case MoaiGolfStageElementKind.LaunchPedestal:
                        launchPedestalElement = element;
                        break;
                    case MoaiGolfStageElementKind.TargetPedestal:
                        targetPedestalElement = element;
                        break;
                    case MoaiGolfStageElementKind.SuccessZone:
                        successZoneElement = element;
                        break;
                    case MoaiGolfStageElementKind.AimMarker:
                        aimMarkerElement = element;
                        break;
                    case MoaiGolfStageElementKind.GolfClubPivot:
                        golfClubPivotElement = element;
                        break;
                    case MoaiGolfStageElementKind.LaunchMoai:
                        launchMoaiElement = element;
                        break;
                    case MoaiGolfStageElementKind.TargetMoai:
                        targets.Add(element);
                        break;
                }
            }

            targets.Sort((left, right) => left.SlotIndex.CompareTo(right.SlotIndex));
            targetMoaiPoolElements = targets.ToArray();
        }

        private void ConfigureLaunchRuntimeComponents(GameObject instance)
        {
            instance.GetComponent<MoaiGolfBounceSfx>()?.ConfigureDependencies(runtimeGameController, runtimeSeController);
            instance.GetComponent<MoaiGolfTargetMoaiVoiceSfx>()?.ConfigureDependencies(runtimeGameController, runtimeSeController);
            instance.GetComponent<MoaiGolfLandingJudge>()?.ConfigureDependencies(runtimeGameController, runtimeRunState, this);
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

    }
}
