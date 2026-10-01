using UnityEngine;

namespace OCC.Combat.Presentation
{
    public sealed class PcDisplayPreferences
    {
        public const string Prefix = "OCC.FirstExperiencePrototype.";
        public static readonly int[] FrameRates = { 30, 60, 120, 144, 240, -1 };
        public int ResolutionIndex = 2;
        public int ModeIndex;
        public bool VSync;
        public int FrameRateIndex = 1;
        public FullScreenMode Mode => ModeIndex == 1 ? FullScreenMode.FullScreenWindow :
            ModeIndex == 2 ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
        public string ModeLabel => ModeIndex == 1 ? "无边框全屏" : ModeIndex == 2 ? "全屏" : "窗口";
        public string FrameRateLabel => VSync ? "跟随显示器刷新率" :
            FrameRates[FrameRateIndex] < 0 ? "不限" : FrameRates[FrameRateIndex] + " FPS";
        public PcDisplayPreferences Copy() => (PcDisplayPreferences)MemberwiseClone();

        public static PcDisplayPreferences Load(int resolutionCount, string prefix = Prefix)
        {
            return new PcDisplayPreferences
            {
                ResolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt(prefix + "resolution.v1", 2), 0, resolutionCount - 1),
                ModeIndex = Mathf.Clamp(PlayerPrefs.GetInt(prefix + "displayMode.v2",
                    PlayerPrefs.GetInt(prefix + "fullscreen.v1", 0) != 0 ? 1 : 0), 0, 2),
                VSync = PlayerPrefs.GetInt(prefix + "vSync.v1", 0) != 0,
                FrameRateIndex = Mathf.Clamp(PlayerPrefs.GetInt(prefix + "frameRate.v1", 1), 0, FrameRates.Length - 1)
            };
        }

        public void Save(string prefix = Prefix)
        {
            PlayerPrefs.SetInt(prefix + "resolution.v1", ResolutionIndex);
            PlayerPrefs.SetInt(prefix + "displayMode.v2", ModeIndex);
            PlayerPrefs.SetInt(prefix + "fullscreen.v1", ModeIndex == 0 ? 0 : 1);
            PlayerPrefs.SetInt(prefix + "vSync.v1", VSync ? 1 : 0);
            PlayerPrefs.SetInt(prefix + "frameRate.v1", FrameRateIndex);
            PlayerPrefs.Save();
        }

        public void Apply(Vector2Int resolution)
        {
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            Application.targetFrameRate = VSync ? -1 : FrameRates[FrameRateIndex];
            // Game View sizes are managed by the editor; native modes are player-only.
            if (!Application.isEditor) Screen.SetResolution(resolution.x, resolution.y, Mode);
        }
    }
}
