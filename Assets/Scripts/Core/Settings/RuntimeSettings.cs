using UnityEngine;

namespace VoidRogues
{
    public class RuntimeSettings
    {
        // CONSTANTS

        public const string KEY_MUSIC_VOLUME = "MusicVolume";
        public const string KEY_EFFECTS_VOLUME = "EffectsVolume";
        public const string KEY_WINDOWED = "Windowed";
        public const string KEY_RESOLUTION = "Resolution";
        public const string KEY_GRAPHICS_QUALITY = "GraphicsQuality";
        public const string KEY_LIMIT_FPS = "LimitFPS";
        public const string KEY_TARGET_FPS = "TargetFPS";
        public const int KEY_TARGET_FPS_FALLBACK = 90;
        public const string KEY_REGION = "Region";
        public const string KEY_REGION_FALLBACK = "us";
        public const string KEY_SENSITIVITY = "Sensitivity";
        public const string KEY_AIM_SENSITIVITY = "AimSensitivity";
        public const string KEY_VSYNC = "VSync";

        // PUBLIC MEMBERS

        public Options Options
        {
            get
            {
                if (_options == null)
                {
                    _options = new Options();
                    _options.Initialize(Global.Settings.DefaultOptions, true, "Options.V1.");
                    //_options.SaveChanges();
                }
                return _options;
            }
        }

        public float MusicVolume { get { return Options.GetFloat(KEY_MUSIC_VOLUME); } set { Options.Set(KEY_MUSIC_VOLUME, value, false); } }
        public float EffectsVolume { get { return Options.GetFloat(KEY_EFFECTS_VOLUME); } set { Options.Set(KEY_EFFECTS_VOLUME, value, false); } }

        public bool Windowed { get { return Options.GetBool(KEY_WINDOWED); } set { Options.Set(KEY_WINDOWED, value, false); } }
        public int Resolution { get { return Options.GetInt(KEY_RESOLUTION); } set { Options.Set(KEY_RESOLUTION, value, false); } }
        public int GraphicsQuality { get { return Options.GetInt(KEY_GRAPHICS_QUALITY); } set { Options.Set(KEY_GRAPHICS_QUALITY, value, false); } }
        public bool VSync 
        { 
            get 
            { 
                return Options.GetBool(KEY_VSYNC, false); 
            } 
            set { Options.Set(KEY_VSYNC, value, false); 
            } 
        }
        public bool LimitFPS { get { return Options.GetBool(KEY_LIMIT_FPS, false); } set { Options.Set(KEY_LIMIT_FPS, value, false); } }
        public int TargetFPS { get { return Options.GetInt(KEY_TARGET_FPS); } set { Options.Set(KEY_TARGET_FPS, value, false); } }
        public float Sensitivity { get { return Options.GetFloat(KEY_SENSITIVITY); } set { Options.Set(KEY_SENSITIVITY, value, false); } }
        public float AimSensitivity { get { return Options.GetFloat(KEY_AIM_SENSITIVITY); } set { Options.Set(KEY_AIM_SENSITIVITY, value, false); } }

        public string Region 
        { 
            get 
            {
                return Options.GetString(KEY_REGION, KEY_REGION_FALLBACK);
            } 
            set 
            { 
                Options.Set(KEY_REGION, value, true); 
            } 
        }

        // PRIVATE MEMBERS
        private Options _options = null;

        // PUBLIC METHODS

        public void Initialize(GlobalSettings settings)
        {
            Windowed = Screen.fullScreen == false;
            GraphicsQuality = QualitySettings.GetQualityLevel();
            Resolution = GetCurrentResolutionIndex();

            QualitySettings.vSyncCount = VSync == true ? 1 : 0;
            Application.targetFrameRate = LimitFPS == true ? TargetFPS : -1;
        }

        // PRIVATE MEMBERS

        private int GetCurrentResolutionIndex()
        {
            var resolutions = Screen.resolutions;
            if (resolutions == null || resolutions.Length == 0)
                return -1;

            int currentWidth = Mathf.RoundToInt(Screen.width);
            int currentHeight = Mathf.RoundToInt(Screen.height);

            // Get the current refresh rate as a float (this is the recommended way)
            float currentRefreshRate = (float)Screen.currentResolution.refreshRateRatio.value;

            for (int i = 0; i < resolutions.Length; i++)
            {
                var resolution = resolutions[i];

                if (resolution.width == currentWidth &&
                    resolution.height == currentHeight &&
                    Mathf.Approximately((float)resolution.refreshRateRatio.value, currentRefreshRate))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}