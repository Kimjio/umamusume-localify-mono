using Gallop;
using Newtonsoft.Json;
using System;
using System.IO;

namespace UmamusumeLocalify
{
    internal class Config
    {
        public bool dumpEntries;
        public bool enableConsole;
        public bool enableLogger;
        public bool staticEntriesUseHash;
        public bool staticEntriesUseTextIdName;
        public int maxFps;
        public bool unlockSize;
        public float uiScale;
        public bool freeFormWindow;
        public float freeFormUiScalePortrait = 0.6f;
        public float freeFormUiScaleLandscape = 0.5f;
        public float uiAnimationScale;
        public float resolution3dScale;
        public bool replaceToBuiltinFont;
        public bool replaceToCustomFont;
        public string fontAssetBundlePath;
        public string fontAssetName;
        public string tmproFontAssetName;
        public bool autoFullscreen;
        public GraphicSettings.GraphicsQuality? graphicsQuality;
        public int antiAliasing;
        public bool forceLandscape;
        public float forceLandscapeUiScale;
        public bool uiLoadingShowOrientationGuide;
        public string customTitleName;
        public string replaceAssetsPath;
        public string replaceAssetBundleFilePath;
        public string[] replaceAssetBundleFilePaths;
        public string replaceTextDBPath;
        public bool characterSystemTextCaption;
        public CySpringController.SpringUpdateMode? cySpringUpdateMode;
        public bool hideNowLoading;
        public string textIdDict;
        public bool discordRichPresence;
        public string[] dicts;


        private static Config currentConfig = new();

        public static Config Current
        {
            get { return currentConfig; }
        }

        public static bool LoadConfig()
        {
            try
            {
                var json = File.ReadAllText("./config.json");
                currentConfig = JsonConvert.DeserializeObject<Config>(json);
            } catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }
    }
}
