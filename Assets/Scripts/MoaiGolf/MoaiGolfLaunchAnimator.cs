using System.Collections;
using UnityEngine;

namespace MoaiGolf
{
    public sealed class MoaiGolfLaunchAnimator : MonoBehaviour
    {
        public const float ClubSpriteScale = 0.55f;
        public const float ClubAnchorLocalXPixels = 347f;
        public const float ClubAnchorLocalYPixels = 333f;
        public const float ClubAnchorHeightAboveMoaiPixels = 250f;
        public static Vector2 ClubAnchorOffsetFromMoai => new Vector2(0f, ClubAnchorHeightAboveMoaiPixels / MoaiGolfWorldSettings.PixelsPerUnit);
        public static float ClubWindupAngleDeg => ClubSlowStartAngleDeg - SpinupDegrees;
        private static float ClubSlowStartAngleDeg => ClubStrikeAngleDeg - SlowStartDegreesBeforeImpact;
        private static float ClubSlowReleaseAngleDeg => ClubStrikeAngleDeg - SlowReleaseDegreesBeforeImpact;
        private static float ClubStrikeAngleDeg => 0f;
        private static float ClubFollowThroughAngleDeg => ClubStrikeAngleDeg + FollowThroughDegrees;

        private const float RotationsPerSecond = 6f;
        private const float SpinupDuration = 1f;
        private const float SpinupDegrees = RotationsPerSecond * 360f * SpinupDuration;
        private const float SlowStartDegreesBeforeImpact = 120f;
        private const float SlowReleaseDegreesBeforeImpact = 8f;
        private const float FollowThroughDegrees = 42f;
        private const float SlowApproachDuration = 0.52f;
        private const float FinalImpactDuration = SlowReleaseDegreesBeforeImpact / (RotationsPerSecond * 360f);
        private const float HitStopDuration = 0.08f;
        private const float HitStopShakeAmplitude = 0.06f;
        private const float HitStopShakeFrequency = 85f;
        private const float FastFollowThroughDuration = 0.08f;
        private const float LaunchVelocityMultiplier = 1.18f;

        private const float SlowApproachTimeScale = 0.1f;
        private const float ZoomOrthoSizeFactor = 0.5f;
        private const float ZoomTargetYOffset = 0.3f;

        private MoaiGolfGameController gameController;
        private MoaiGolfRunState runState;
        private MoaiGolfStageView stageView;
        private Camera mainCamera;

        public bool IsPlaying { get; private set; }

        private void Start()
        {
            gameController = FindAnyObjectByType<MoaiGolfGameController>();
            runState = FindAnyObjectByType<MoaiGolfRunState>();
            stageView = FindAnyObjectByType<MoaiGolfStageView>();
            mainCamera = Camera.main;
        }

        public void BeginLaunchSequence()
        {
            if (IsPlaying)
            {
                return;
            }

            gameController ??= FindAnyObjectByType<MoaiGolfGameController>();
            runState ??= FindAnyObjectByType<MoaiGolfRunState>();
            stageView ??= FindAnyObjectByType<MoaiGolfStageView>();
            mainCamera ??= Camera.main;

            if (gameController == null || runState == null || stageView == null || mainCamera == null)
            {
                return;
            }

            if (gameController.Phase != MoaiGolfGamePhase.PowerSelect)
            {
                return;
            }

            gameController.BeginLaunchAnimation();
            StartCoroutine(PlaySequence());
        }

        public void CancelLaunchSequence()
        {
            if (!IsPlaying)
            {
                return;
            }

            StopAllCoroutines();
            IsPlaying = false;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = MoaiGolfWorldSettings.FixedTimestep;
        }

        private IEnumerator PlaySequence()
        {
            IsPlaying = true;
            var club = stageView.GolfClubPivot;

            // クラブ右上の座標をモアイ中心の約 200px 上に固定し、親 Transform の回転だけで振る。
            var moaiCenter = runState.LaunchPosition;
            var launchBody = stageView.LaunchBody;
            HoldLaunchBodyAt(launchBody, moaiCenter);
            var visualFocus = stageView.LaunchVisualFocusPosition;
            var clubAnchorPos = moaiCenter + ClubAnchorOffsetFromMoai;
            if (club != null)
            {
                club.position = new Vector3(clubAnchorPos.x, clubAnchorPos.y, 0f);
                club.rotation = Quaternion.Euler(0f, 0f, ClubWindupAngleDeg);
            }

            var originalTimeScale = Time.timeScale;
            var originalFixedDt = Time.fixedDeltaTime;

            var originalCameraSize = mainCamera.orthographicSize;
            var originalCameraPos = mainCamera.transform.position;
            var targetCameraPos = new Vector3(visualFocus.x, visualFocus.y + ZoomTargetYOffset, originalCameraPos.z);
            var targetSize = originalCameraSize * ZoomOrthoSizeFactor;
            mainCamera.orthographicSize = targetSize;
            mainCamera.transform.position = targetCameraPos;

            yield return SpinClubAndCamera(
                mainCamera,
                club,
                ClubWindupAngleDeg,
                ClubSlowStartAngleDeg,
                targetSize,
                targetSize,
                targetCameraPos,
                targetCameraPos,
                SpinupDuration,
                lockedLaunchBody: launchBody,
                lockedLaunchPosition: moaiCenter
            );

            Time.timeScale = SlowApproachTimeScale;
            Time.fixedDeltaTime = originalFixedDt * SlowApproachTimeScale;

            yield return SpinClubAndCamera(
                mainCamera,
                club,
                ClubSlowStartAngleDeg,
                ClubSlowReleaseAngleDeg,
                targetSize,
                targetSize,
                targetCameraPos,
                targetCameraPos,
                SlowApproachDuration,
                lockedLaunchBody: launchBody,
                lockedLaunchPosition: moaiCenter
            );

            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = originalFixedDt;

            yield return SpinClubAndCamera(
                mainCamera,
                club,
                ClubSlowReleaseAngleDeg,
                ClubStrikeAngleDeg,
                targetSize,
                targetSize,
                targetCameraPos,
                targetCameraPos,
                FinalImpactDuration,
                lockedLaunchBody: launchBody,
                lockedLaunchPosition: moaiCenter
            );

            if (club != null)
            {
                club.rotation = Quaternion.Euler(0f, 0f, ClubStrikeAngleDeg);
            }

            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = originalFixedDt;
            HoldLaunchBodyAt(launchBody, moaiCenter);

            Time.timeScale = 0f;
            Time.fixedDeltaTime = originalFixedDt;
            var launchVisual = launchBody != null ? launchBody.transform.Find("Launch Moai Visual") : null;
            var originalLaunchVisualPosition = launchVisual != null ? launchVisual.localPosition : Vector3.zero;
            var hitStopElapsed = 0f;
            while (hitStopElapsed < HitStopDuration)
            {
                hitStopElapsed += Time.unscaledDeltaTime;
                HoldLaunchBodyAt(launchBody, moaiCenter);
                if (launchVisual != null)
                {
                    var shakeX = Mathf.Sin(hitStopElapsed * HitStopShakeFrequency) * HitStopShakeAmplitude;
                    var shakeY = Mathf.Cos(hitStopElapsed * HitStopShakeFrequency * 1.37f) * HitStopShakeAmplitude * 0.65f;
                    launchVisual.localPosition = originalLaunchVisualPosition + new Vector3(shakeX, shakeY, 0f);
                }

                yield return null;
            }

            if (launchVisual != null)
            {
                launchVisual.localPosition = originalLaunchVisualPosition;
            }

            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = originalFixedDt;
            if (launchBody != null)
            {
                HoldLaunchBodyAt(launchBody, moaiCenter);
                gameController.FinishLaunchAnimation(launchBody);
                launchBody.linearVelocity *= LaunchVelocityMultiplier;
                launchBody.angularVelocity *= LaunchVelocityMultiplier;
            }

            yield return SpinClubAndCamera(
                mainCamera,
                club,
                ClubStrikeAngleDeg,
                ClubFollowThroughAngleDeg,
                targetSize,
                targetSize,
                targetCameraPos,
                targetCameraPos,
                FastFollowThroughDuration
            );

            if (club != null)
            {
                club.rotation = Quaternion.Euler(0f, 0f, ClubFollowThroughAngleDeg);
            }

            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = originalFixedDt;
            mainCamera.orthographicSize = originalCameraSize;
            if (gameController.Phase == MoaiGolfGamePhase.LaunchAnimation)
            {
                mainCamera.transform.position = targetCameraPos;
            }

            IsPlaying = false;
        }

        private static void HoldLaunchBodyAt(Rigidbody2D launchBody, Vector2 position)
        {
            if (launchBody == null)
            {
                return;
            }

            launchBody.linearVelocity = Vector2.zero;
            launchBody.angularVelocity = 0f;
            launchBody.position = position;
            launchBody.transform.position = new Vector3(position.x, position.y, launchBody.transform.position.z);
            launchBody.Sleep();
        }

        private static IEnumerator SpinClubAndCamera(
            Camera camera,
            Transform club,
            float fromAngleDeg,
            float toAngleDeg,
            float fromCameraSize,
            float toCameraSize,
            Vector3 fromCameraPos,
            Vector3 toCameraPos,
            float duration,
            bool increaseSlowMotion = false,
            float fromTimeScale = 1f,
            float toTimeScale = 1f,
            float originalFixedDt = MoaiGolfWorldSettings.FixedTimestep,
            Rigidbody2D lockedLaunchBody = null,
            Vector2 lockedLaunchPosition = default(Vector2)
        )
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                HoldLaunchBodyAt(lockedLaunchBody, lockedLaunchPosition);
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = Mathf.SmoothStep(0f, 1f, t);
                var clubT = increaseSlowMotion ? 1f - Mathf.Pow(1f - t, 2.35f) : t;
                if (club != null)
                {
                    var rotZ = Mathf.LerpUnclamped(fromAngleDeg, toAngleDeg, clubT);
                    club.rotation = Quaternion.Euler(0f, 0f, rotZ);
                }

                if (increaseSlowMotion)
                {
                    var timeScale = Mathf.Lerp(fromTimeScale, toTimeScale, eased);
                    Time.timeScale = timeScale;
                    Time.fixedDeltaTime = originalFixedDt * timeScale;
                }

                if (camera != null)
                {
                    camera.orthographicSize = toCameraSize;
                    camera.transform.position = toCameraPos;
                }

                yield return null;
            }
        }
    }
}
