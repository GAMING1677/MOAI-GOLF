using UnityEngine;

namespace MoaiGolf
{
    public static class MoaiGolfAudioSettingsStore
    {
        private const string BgmVolumeKey = "MoaiGolf.BgmVolume";
        private const string SeVolumeKey = "MoaiGolf.SeVolume";

        public static float BgmVolume
        {
            get => PlayerPrefs.GetFloat(BgmVolumeKey, MoaiGolfBgmController.DefaultVolume);
            set
            {
                PlayerPrefs.SetFloat(BgmVolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        public static float SeVolume
        {
            get => PlayerPrefs.GetFloat(SeVolumeKey, MoaiGolfSeController.DefaultVolume);
            set
            {
                PlayerPrefs.SetFloat(SeVolumeKey, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }
    }
}
