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
        private static float ClubFullSpinFollowThroughAngleDeg => ClubFollowThroughAngleDeg + FullSpinFollowThroughDegrees;

        private const float RotationsPerSecond = 6f;
        private const float SpinupDuration = 1f;
        private const float SpinupDegrees = RotationsPerSecond * 360f * SpinupDuration;
        private const float SlowStartDegreesBeforeImpact = 120f;
        private const float SlowReleaseDegreesBeforeImpact = 8f;
        private const float FollowThroughDegrees = 42f;
        private const float FullSpinFollowThroughDegrees = 360f;
        private const float SlowApproachDuration = 1.45f;
        private const float FinalImpactDuration = SlowReleaseDegreesBeforeImpact / (RotationsPerSecond * 360f);
        private const float HitStopDuration = 0.6f;
        private const float HitStopShakeAmplitude = 0.06f;
        private const float HitStopShakeFrequency = 85f;
        private const float FastFollowThroughDuration = 0.18f;
        private const float LaunchVelocityMultiplier = 1.18f;

        private const float SlowApproachTimeScale = 0.015f;
        private const float ZoomOrthoSizeFactor = 0.5f;
        private const float ZoomTargetYOffset = 0.3f;
        private const float LaunchReadyCameraMoveDuration = 0.22f;
        private const string ClubSpinWhooshResourcePath = "Audio/ESM_SG_fx_combat_whoosh_swing_wind_throw_03";
        private const float ClubSpinWhooshVolume = 0.9f;
        private const string ImpactHitResourcePath = "Audio/MO_WS_160_kick_hardstyle_raw_piep_F";
        private const float ImpactHitVolume = 0.451f;

        [SerializeField] private Camera mainCamera;
        [SerializeField] private MoaiGolfGameController gameController;
        [SerializeField] private MoaiGolfRunState runState;
        [SerializeField] private MoaiGolfStageView stageView;
        [SerializeField] private MoaiGolfSeController seController;
        [SerializeField] private AudioSource sfxAudioSource;
        private AudioClip clubSpinWhooshClip;
        private AudioClip impactHitClip;
        private ResourceRequest clubSpinWhooshClipRequest;
        private ResourceRequest impactHitClipRequest;

        public bool IsPlaying { get; private set; }

        private void Reset()
        {
            RefreshSerializedReferencesForEditor(null, null, null, null, null);
        }

        private void OnValidate()
        {
            RefreshSerializedReferencesForEditor(null, null, null, null, null);
        }

        private void Awake()
        {
            EnsureSfxAudioSource();
            ValidateReferences();
        }

        public void RefreshSerializedReferencesForEditor(
            Camera camera,
            MoaiGolfGameController controller,
            MoaiGolfRunState state,
            MoaiGolfStageView view,
            MoaiGolfSeController se
        )
        {
            mainCamera = camera != null ? camera : mainCamera;
            gameController = controller != null ? controller : GetComponent<MoaiGolfGameController>();
            runState = state != null ? state : GetComponent<MoaiGolfRunState>();
            stageView = view != null ? view : FindAnyObjectByType<MoaiGolfStageView>();
            seController = se != null ? se : GetComponent<MoaiGolfSeController>();
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            EnsureSfxAudioSource();
        }

        private void EnsureSfxAudioSource()
        {
            if (sfxAudioSource != null)
            {
                return;
            }

            sfxAudioSource = GetComponent<AudioSource>();
            if (sfxAudioSource != null)
            {
                return;
            }

            sfxAudioSource = gameObject.AddComponent<AudioSource>();
            sfxAudioSource.playOnAwake = false;
            sfxAudioSource.loop = false;
            sfxAudioSource.spatialBlend = 0f;
        }

        public bool ValidateReferences()
        {
            var isValid = true;
            isValid &= ValidateReference(mainCamera, nameof(mainCamera));
            isValid &= ValidateReference(gameController, nameof(gameController));
            isValid &= ValidateReference(runState, nameof(runState));
            isValid &= ValidateReference(stageView, nameof(stageView));
            isValid &= ValidateReference(seController, nameof(seController));
            isValid &= ValidateReference(sfxAudioSource, nameof(sfxAudioSource));
            return isValid;
        }

        public void PreloadLaunchAssets()
        {
            PreloadLaunchAudio();
            seController?.PreloadAudio();
            stageView?.LaunchBody?.GetComponent<MoaiGolfBounceSfx>()?.PreloadBounceClip();
            MoaiGolfImpactBlurOverlay.Prewarm(mainCamera);
            MoaiGolfImpactParticles.Prewarm();
        }

        public void BeginLaunchSequence()
        {
            if (IsPlaying)
            {
                return;
            }

            PreloadLaunchAssets();

            if (gameController == null || runState == null || stageView == null || mainCamera == null)
            {
                return;
            }

            if (gameController.Phase != MoaiGolfGamePhase.PowerSelect)
            {
                return;
            }

            gameController.BeginLaunchAnimation();
            IsPlaying = true;
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

        private void PreloadLaunchAudio()
        {
            if (clubSpinWhooshClip == null && clubSpinWhooshClipRequest == null)
            {
                clubSpinWhooshClipRequest = Resources.LoadAsync<AudioClip>(ClubSpinWhooshResourcePath);
            }

            if (impactHitClip == null && impactHitClipRequest == null)
            {
                impactHitClipRequest = Resources.LoadAsync<AudioClip>(ImpactHitResourcePath);
            }
        }

        private static AudioClip GetLoadedClip(ref AudioClip clip, ref ResourceRequest request)
        {
            if (clip != null)
            {
                return clip;
            }

            if (request == null)
            {
                return null;
            }

            if (!request.isDone)
            {
                return null;
            }

            clip = request.asset as AudioClip;
            request = null;
            return clip;
        }

        private void PlayClubSpinWhoosh()
        {
            var clip = GetLoadedClip(ref clubSpinWhooshClip, ref clubSpinWhooshClipRequest);
            if (sfxAudioSource == null || clip == null)
            {
                return;
            }

            sfxAudioSource.PlayOneShot(clip, ClubSpinWhooshVolume * GetSeVolume());
        }

        private void PlayImpactHitSound()
        {
            var clip = GetLoadedClip(ref impactHitClip, ref impactHitClipRequest);
            if (sfxAudioSource == null || clip == null)
            {
                return;
            }

            sfxAudioSource.PlayOneShot(clip, ImpactHitVolume * GetSeVolume());
        }

        private float GetSeVolume()
        {
            return seController != null ? seController.Volume : MoaiGolfSeController.DefaultVolume;
        }

        private bool ValidateReference(UnityEngine.Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"{nameof(MoaiGolfLaunchAnimator)} missing serialized reference: {fieldName}.", this);
            return false;
        }

        private IEnumerator PlaySequence()
        {
            IsPlaying = true;
            var club = stageView.GolfClubPivot;
            var cameraFocus = stageView.LaunchCameraFocusPosition;

            yield return MoveCameraToLaunchReadyPosition(cameraFocus);

            // クラブ右上の座標をモアイ中心の約 200px 上に固定し、親 Transform の回転だけで振る。
            var moaiCenter = runState.LaunchPosition;
            var launchBody = stageView.LaunchBody;
            HoldLaunchBodyAt(launchBody, moaiCenter);
            if (launchBody != null)
            {
                launchBody.rotation = 0f;
            }
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
            var zoomFocusY = Mathf.Max(
                originalCameraPos.y - 0.05f,
                cameraFocus.y + ZoomTargetYOffset
            );
            var targetCameraPos = new Vector3(cameraFocus.x, zoomFocusY, originalCameraPos.z);
            var targetSize = originalCameraSize * ZoomOrthoSizeFactor;

            yield return SpinClubAndCamera(
                mainCamera,
                club,
                ClubWindupAngleDeg,
                ClubSlowStartAngleDeg,
                originalCameraSize,
                originalCameraSize,
                originalCameraPos,
                originalCameraPos,
                SpinupDuration,
                lockedLaunchBody: launchBody,
                lockedLaunchPosition: moaiCenter,
                onFullRotation: PlayClubSpinWhoosh
            );

            Time.timeScale = SlowApproachTimeScale;
            Time.fixedDeltaTime = originalFixedDt * SlowApproachTimeScale;

            yield return SpinClubAndCamera(
                mainCamera,
                club,
                ClubSlowStartAngleDeg,
                ClubSlowReleaseAngleDeg,
                originalCameraSize,
                targetSize,
                originalCameraPos,
                targetCameraPos,
                SlowApproachDuration,
                lockedLaunchBody: launchBody,
                lockedLaunchPosition: moaiCenter,
                onFullRotation: PlayClubSpinWhoosh
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
                lockedLaunchPosition: moaiCenter,
                onFullRotation: PlayClubSpinWhoosh
            );

            if (club != null)
            {
                club.rotation = Quaternion.Euler(0f, 0f, ClubStrikeAngleDeg);
            }

            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = originalFixedDt;
            HoldLaunchBodyAt(launchBody, moaiCenter);
            PlayImpactHitSound();
            MoaiGolfImpactBlurOverlay.Pulse(mainCamera);
            MoaiGolfImpactParticles.Emit(moaiCenter);

            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = originalFixedDt;

            yield return SpinClubAndCamera(
                mainCamera,
                club,
                ClubStrikeAngleDeg,
                ClubFullSpinFollowThroughAngleDeg,
                targetSize,
                targetSize,
                targetCameraPos,
                targetCameraPos,
                FastFollowThroughDuration,
                lockedLaunchBody: launchBody,
                lockedLaunchPosition: moaiCenter,
                onFullRotation: PlayClubSpinWhoosh
            );

            if (club != null)
            {
                club.rotation = Quaternion.Euler(0f, 0f, ClubFullSpinFollowThroughAngleDeg);
            }

            Time.timeScale = 0f;
            Time.fixedDeltaTime = originalFixedDt;
            var launchVisual = stageView.LaunchVisual;
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

            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = originalFixedDt;
            mainCamera.orthographicSize = originalCameraSize;
            if (gameController.Phase == MoaiGolfGamePhase.LaunchAnimation)
            {
                mainCamera.transform.position = targetCameraPos;
            }

            IsPlaying = false;
        }

        private IEnumerator MoveCameraToLaunchReadyPosition(Vector2 cameraFocus)
        {
            if (mainCamera == null)
            {
                yield break;
            }

            var fromPosition = mainCamera.transform.position;
            var gameplayFocus = MoaiGolfCameraController.ResolveLaunchGameplayCameraPosition(stageView);
            var targetPosition = MoaiGolfCameraController.ClampCameraPosition(
                new Vector3(
                    cameraFocus.x,
                    Mathf.Max(fromPosition.y, gameplayFocus.y),
                    fromPosition.z
                ),
                MoaiGolfGamePhase.LaunchAnimation,
                stageView
            );

            if (Vector2.Distance(fromPosition, targetPosition) < 0.05f)
            {
                mainCamera.transform.position = targetPosition;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < LaunchReadyCameraMoveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / LaunchReadyCameraMoveDuration);
                var eased = Mathf.SmoothStep(0f, 1f, t);
                var lerped = Vector3.Lerp(fromPosition, targetPosition, eased);
                lerped.y = Mathf.Max(lerped.y, fromPosition.y);
                mainCamera.transform.position = lerped;
                yield return null;
            }

            mainCamera.transform.position = targetPosition;
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
            launchBody.rotation = 0f;
            launchBody.transform.SetPositionAndRotation(
                new Vector3(position.x, position.y, launchBody.transform.position.z),
                Quaternion.identity
            );
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
            Rigidbody2D lockedLaunchBody = null,
            Vector2 lockedLaunchPosition = default(Vector2),
            System.Action onFullRotation = null)
        {
            var completedRevolutions = 0;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                HoldLaunchBodyAt(lockedLaunchBody, lockedLaunchPosition);
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = Mathf.SmoothStep(0f, 1f, t);
                if (club != null)
                {
                    var rotZ = Mathf.LerpUnclamped(fromAngleDeg, toAngleDeg, t);
                    club.rotation = Quaternion.Euler(0f, 0f, rotZ);

                    if (onFullRotation != null)
                    {
                        var traveledDeg = Mathf.Abs(rotZ - fromAngleDeg);
                        var revolutions = Mathf.FloorToInt(traveledDeg / 360f);
                        while (completedRevolutions < revolutions)
                        {
                            completedRevolutions++;
                            onFullRotation();
                        }
                    }
                }

                if (camera != null)
                {
                    camera.orthographicSize = Mathf.Lerp(fromCameraSize, toCameraSize, eased);
                    camera.transform.position = Vector3.Lerp(fromCameraPos, toCameraPos, eased);
                }

                yield return null;
            }
        }
    }

    internal sealed class MoaiGolfImpactBlurOverlay : MonoBehaviour
    {
        private const float Duration = 0.16f;
        private const int StreakCount = 9;
        private static readonly float[] StreakY =
        {
            -0.46f, -0.34f, -0.22f, -0.1f, 0.04f, 0.17f, 0.29f, 0.4f, 0.48f
        };

        private static readonly float[] StreakHeight =
        {
            0.035f, 0.018f, 0.028f, 0.02f, 0.04f, 0.024f, 0.03f, 0.017f, 0.025f
        };

        private static readonly float[] StreakShift =
        {
            -0.06f, 0.05f, -0.035f, 0.02f, -0.05f, 0.04f, -0.025f, 0.055f, -0.045f
        };

        private static Sprite whiteSprite;

        private Camera targetCamera;
        private Transform streakRoot;
        private SpriteRenderer[] streaks;
        private float remaining;

        public static void Pulse(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            var overlay = camera.GetComponent<MoaiGolfImpactBlurOverlay>();
            if (overlay == null)
            {
                overlay = camera.gameObject.AddComponent<MoaiGolfImpactBlurOverlay>();
            }

            overlay.Play(camera);
        }

        public static void Prewarm(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            var overlay = camera.GetComponent<MoaiGolfImpactBlurOverlay>();
            if (overlay == null)
            {
                overlay = camera.gameObject.AddComponent<MoaiGolfImpactBlurOverlay>();
            }

            overlay.EnsureStreaks();
            overlay.SetVisible(false);
            overlay.enabled = false;
        }

        private void Awake()
        {
            EnsureStreaks();
            SetVisible(false);
        }

        private void LateUpdate()
        {
            if (targetCamera == null || remaining <= 0f)
            {
                SetVisible(false);
                enabled = false;
                return;
            }

            remaining -= Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(remaining / Duration);
            var alpha = Mathf.SmoothStep(0f, 1f, t) * 0.16f;
            UpdateStreakLayout(alpha);
        }

        private void Play(Camera camera)
        {
            targetCamera = camera;
            remaining = Duration;
            EnsureStreaks();
            SetVisible(true);
            enabled = true;
            UpdateStreakLayout(0.16f);
        }

        private void EnsureStreaks()
        {
            if (streaks != null)
            {
                return;
            }

            streakRoot = new GameObject("Impact Blur Overlay").transform;
            streakRoot.SetParent(transform, false);
            streakRoot.localPosition = new Vector3(0f, 0f, Mathf.Abs(transform.position.z));
            streakRoot.localRotation = Quaternion.identity;
            streakRoot.localScale = Vector3.one;

            streaks = new SpriteRenderer[StreakCount];
            for (var index = 0; index < streaks.Length; index++)
            {
                var streak = new GameObject($"Impact Blur Streak {index + 1}");
                streak.transform.SetParent(streakRoot, false);
                var renderer = streak.AddComponent<SpriteRenderer>();
                renderer.sprite = GetWhiteSprite();
                renderer.sortingOrder = 1000 + index;
                streaks[index] = renderer;
            }
        }

        private void UpdateStreakLayout(float alpha)
        {
            var cameraHeight = targetCamera.orthographicSize * 2f;
            var cameraWidth = cameraHeight * targetCamera.aspect;
            streakRoot.localPosition = new Vector3(0f, 0f, Mathf.Abs(targetCamera.transform.position.z));

            for (var index = 0; index < streaks.Length; index++)
            {
                var transformRef = streaks[index].transform;
                var width = cameraWidth * (1.08f + index * 0.018f);
                var height = cameraHeight * StreakHeight[index];
                transformRef.localScale = new Vector3(width, height, 1f);
                transformRef.localPosition = new Vector3(cameraWidth * StreakShift[index] * alpha * 7f, cameraHeight * StreakY[index], 0f);

                var warm = index % 2 == 0 ? 1f : 0.85f;
                streaks[index].color = new Color(1f, warm, 0.52f, alpha * (0.55f + index * 0.025f));
            }
        }

        private void SetVisible(bool visible)
        {
            if (streaks == null)
            {
                return;
            }

            foreach (var streak in streaks)
            {
                if (streak != null)
                {
                    streak.enabled = visible;
                }
            }
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
    }

    internal static class MoaiGolfImpactParticles
    {
        private const int ParticleCount = 96;
        private static readonly Color ParticleYellow = new Color(1f, 0.9f, 0.02f, 1f);
        private static readonly Color ParticleYellowLight = new Color(1f, 1f, 0.28f, 1f);
        private static readonly Color ParticleRed = new Color(1f, 0.06f, 0.02f, 1f);
        private static Material particleMaterial;

        public static void Emit(Vector2 position)
        {
            var particleObject = new GameObject("Launch Impact Particles");
            particleObject.transform.position = new Vector3(position.x, position.y, 0f);

            var particles = particleObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.playOnAwake = false;
            main.duration = 0.28f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.62f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
            main.startColor = ParticleYellow;
            main.gravityModifier = 0.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.useUnscaledTime = true;

            var emission = particles.emission;
            emission.enabled = false;

            var shape = particles.shape;
            shape.enabled = false;

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            var material = GetParticleMaterial();
            if (material != null)
            {
                renderer.material = material;
            }
            renderer.sortingOrder = 1100;

            for (var index = 0; index < ParticleCount; index++)
            {
                var angleRad = Random.Range(0f, Mathf.PI * 2f);
                var direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
                var speed = Random.Range(2.4f, 7.8f);
                var emitParams = new ParticleSystem.EmitParams
                {
                    position = position + direction * Random.Range(0f, 0.12f),
                    velocity = direction * speed + Random.insideUnitCircle * 0.35f,
                    startLifetime = Random.Range(0.24f, 0.66f),
                    startSize = Random.Range(0.055f, 0.17f),
                    startColor = Random.value < 0.14f ? ParticleRed : Color.Lerp(ParticleYellow, ParticleYellowLight, Random.value)
                };
                particles.Emit(emitParams, 1);
            }

            Object.Destroy(particleObject, 1.4f);
        }

        private static Material GetParticleMaterial()
        {
            if (particleMaterial != null)
            {
                return particleMaterial;
            }

            var shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            }
            if (shader == null)
            {
                return null;
            }

            particleMaterial = new Material(shader)
            {
                color = Color.white
            };
            return particleMaterial;
        }

        public static void Prewarm()
        {
            GetParticleMaterial();
        }
    }
}
