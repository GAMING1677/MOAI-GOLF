using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MoaiGolf
{
    public sealed class MoaiGolfTitleView : MonoBehaviour
    {
        public const string StageSceneName = "MoaiGolfStage";
        public const string TitleCanvasObjectName = "MoaiGolfTitleCanvas";

        [SerializeField] private Camera mainCamera;
        [SerializeField] private MoaiGolfTitleLogoDrop logoDrop;
        [SerializeField] private MoaiGolfBgmController bgmController;
        [SerializeField] private Vector2 logoLandingCenter = new Vector2(9.6f, 3.8f);

        [SerializeField] private Button startButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private GameObject optionsDialogRoot;
        [SerializeField] private Button optionsCloseButton;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider seVolumeSlider;
        [SerializeField] private Text bgmVolumeLabel;
        [SerializeField] private Text seVolumeLabel;

        private bool isOptionsOpen;
        private bool uiListenersRegistered;

        private void Awake()
        {
            ApplyScreenBaseline();
            ApplyCameraBaseline();
            ResolveUiReferencesFromHierarchy();

            if (bgmController != null)
            {
                bgmController.SetVolume(MoaiGolfAudioSettingsStore.BgmVolume);
            }

            RegisterUiListeners();
            InitializeVolumeSliders();
            SetOptionsOpen(false);
            SetMenuButtonsVisible(true);
        }

        private void Update()
        {
            RefreshVolumeLabels();
            bgmController?.SetMenuDucked(isOptionsOpen);
        }

        public void ResolveUiReferencesFromHierarchy()
        {
            var canvas = transform.Find(TitleCanvasObjectName);
            if (canvas == null)
            {
                return;
            }

            startButton ??= canvas.Find("MainMenu/StartButton")?.GetComponent<Button>();
            optionsButton ??= canvas.Find("MainMenu/OptionsButton")?.GetComponent<Button>();
            optionsDialogRoot ??= canvas.Find("OptionsDialog")?.gameObject;

            var optionsPanel = canvas.Find("OptionsDialog/Panel");
            optionsCloseButton ??= optionsPanel?.Find("CloseButton")?.GetComponent<Button>();

            var bgmRoot = optionsPanel?.Find("BgmVolume");
            if (bgmRoot != null)
            {
                bgmVolumeSlider ??= bgmRoot.GetComponentInChildren<Slider>(true);
                bgmVolumeLabel ??= bgmRoot.Find("ValueLabel")?.GetComponent<Text>();
            }

            var seRoot = optionsPanel?.Find("SeVolume");
            if (seRoot != null)
            {
                seVolumeSlider ??= seRoot.GetComponentInChildren<Slider>(true);
                seVolumeLabel ??= seRoot.Find("ValueLabel")?.GetComponent<Text>();
            }
        }

        private void ApplyScreenBaseline()
        {
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.SetResolution(
                MoaiGolfWorldSettings.ViewportWidthPixels,
                MoaiGolfWorldSettings.ViewportHeightPixels,
                FullScreenMode.FullScreenWindow
            );
        }

        private void ApplyCameraBaseline()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                return;
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = MoaiGolfWorldSettings.CameraOrthographicSize;
            mainCamera.transform.position = new Vector3(
                MoaiGolfWorldSettings.CameraCenterX,
                MoaiGolfWorldSettings.CameraCenterY,
                MoaiGolfWorldSettings.CameraZ
            );
            mainCamera.backgroundColor = new Color(0.19f, 0.3f, 0.47f);
        }

        private void RegisterUiListeners()
        {
            if (uiListenersRegistered)
            {
                return;
            }

            BindButton(startButton, LoadStageScene);
            BindButton(optionsButton, OpenOptions);
            BindButton(optionsCloseButton, CloseOptions);

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.AddListener(value => bgmController?.SetVolume(value));
            }

            if (seVolumeSlider != null)
            {
                seVolumeSlider.onValueChanged.AddListener(value => MoaiGolfAudioSettingsStore.SeVolume = value);
            }

            uiListenersRegistered = true;
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void InitializeVolumeSliders()
        {
            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.SetValueWithoutNotify(MoaiGolfAudioSettingsStore.BgmVolume);
            }

            if (seVolumeSlider != null)
            {
                seVolumeSlider.SetValueWithoutNotify(MoaiGolfAudioSettingsStore.SeVolume);
            }

            RefreshVolumeLabels();
        }

        private void RefreshVolumeLabels()
        {
            if (bgmVolumeLabel != null && bgmVolumeSlider != null)
            {
                bgmVolumeLabel.text = $"{Mathf.RoundToInt(bgmVolumeSlider.value * 100f)}%";
            }

            if (seVolumeLabel != null && seVolumeSlider != null)
            {
                seVolumeLabel.text = $"{Mathf.RoundToInt(seVolumeSlider.value * 100f)}%";
            }
        }

        private void OpenOptions()
        {
            SetOptionsOpen(true);
        }

        private void CloseOptions()
        {
            SetOptionsOpen(false);
        }

        private void SetOptionsOpen(bool open)
        {
            isOptionsOpen = open;
            if (optionsDialogRoot != null)
            {
                optionsDialogRoot.SetActive(open);
            }
        }

        private void SetMenuButtonsVisible(bool visible)
        {
            if (startButton != null)
            {
                startButton.gameObject.SetActive(visible);
            }

            if (optionsButton != null)
            {
                optionsButton.gameObject.SetActive(visible);
            }
        }

        private void LoadStageScene()
        {
            SceneManager.LoadScene(StageSceneName);
        }
    }
}
