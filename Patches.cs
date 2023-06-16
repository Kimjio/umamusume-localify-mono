using AnimateToUnity;
using CriWare;
using Cute.Core;
using Gallop;
using Gallop.FirebasePlugin;
using Gallop.Model.Component;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UmamusumeLocalify
{
    internal class CommonPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SplashViewController), nameof(SplashViewController.KakaoStart))]
        private static bool SplashViewController_KakaoStart(SplashViewController __instance)
        {
            __instance.wait = true;
            KakaoManager.Instance.StartKakaoManager(delegate (bool loginSuccess, string message)
            {
                Console.WriteLine($"loginSuccess: {loginSuccess}, message: {message}");
                if (!loginSuccess)
                {
                    if (MonoSingleton<SaveDataManager>.Instance.SaveLoader.IsKakaoLoginClear)
                    {
                        // MonoSingleton<SaveDataManager>.Instance.RemoveSaveDataAndSaveOption();
                        MonoSingleton<SaveDataManager>.Instance.SaveLoader.IsSkippableTutorial = true;
                        MonoSingleton<SaveDataManager>.Instance.SaveLoader.IsKakaoLoginClear = true;
                        MonoSingleton<SaveDataManager>.Instance.Save();
                    }
                    else
                    {
                        // MonoSingleton<SaveDataManager>.Instance.RemoveSaveDataAndSaveOption();
                    }
                }
                __instance.wait = false;
            }, __instance.KakaoNetConnectRetry);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TitleViewController), nameof(TitleViewController.InitializeView))]
        private static bool TitleViewController_InitializeView(TitleViewController __instance, out IEnumerator __result)
        {
            __result = InitializeView(__instance);
            return false;
        }

        private static IEnumerator InitializeView(TitleViewController __instance)
        {
            __instance._view.StartButton.SetOnClick(new UnityAction(__instance.OnClickPushStart));
            __instance._view.MenuButton.SetOnClick(new UnityAction(TitleViewController.OnClickMenu));
            __instance._view.KaKaoLoginTap.SetOnClick(new UnityAction(__instance.OnClickKaKaoLogin));
            __instance._view.KaKaoLoginTap.InitializeCollision(true);
            __instance._view.GuestLoginTap.SetOnClick(new UnityAction(__instance.OnClickGuestLogin));
            __instance._view.GuestLoginTap.InitializeCollision(true);
            __instance._view.VersionText.text = string.Format(__instance.VERSION_TEXT, DeviceHelper.GetAppVersionName());
            __instance._view.ViewerIdButton.SetOnClick(new UnityAction(__instance.OnClickViewerIdButton));
            __instance.SetViewerIdText();
            FirebaseInitializer.SetUserId(Certification.ViewerId);
            if (KakaoManager.Instance.SdkStart)
            {
                if (KakaoManager.Instance.KakaoAutoLogin)
                {
                    __instance._view.StartButton.enabled = true;
                    __instance._view.KaKaoLoginTap.SetActiveWithCheck(false);
                    __instance._view.GuestLoginTap.SetActiveWithCheck(false);
                    __instance._view.StartTapObiect.SetActiveWithCheck(true);
                }
                else
                {
                    __instance._view.StartButton.enabled = false;
                    __instance._view.StartTapObiect.SetActiveWithCheck(false);
                    __instance._view.KaKaoLoginTap.SetActiveWithCheck(true);
                    __instance._view.GuestLoginTap.SetActiveWithCheck(true);
                }
            }
            // __instance._view.GuestLoginTap.SetActiveWithCheck(false);
            __instance._view.ProgressRootObject.SetActiveWithCheck(false);
            __instance._gaugeSize.x = 0f;
            __instance._gaugeSize.y = __instance._view.MaskTransform.rect.height;
            __instance._gaugeWidth = __instance._view.MaskTransform.rect.width;
            ApplicationSettingSaveLoader saveLoader = MonoSingleton<SaveDataManager>.Instance.SaveLoader;
            if (saveLoader.CampaignTitleLogoChangeId > 0)
            {
                string campaginTitleLogo = ResourcePath.GetCampaginTitleLogo(saveLoader.CampaignTitleLogoChangeId);
                if (AssetManager.LocalFile.Exists(campaginTitleLogo))
                {
                    if (Gallop.TimeUtil.ToUnixTime(DateTime.UtcNow) < saveLoader.CampaignTitleLogoChangeEndTime)
                    {
                        __instance._view.TitleLogoImage.texture = ResourceManager.LoadOnView<Texture2D>(campaginTitleLogo, SceneDefine.ViewId.None);
                        __instance._view.TitleLogoTransform.anchoredPosition = new Vector2(-14f, 610f);
                        __instance._view.TitleLogoTransform.sizeDelta = new Vector2(744f, 632f);
                    }
                    else
                    {
                        saveLoader.ResetCampaignTitleLogoData();
                    }
                }
            }
            MonoSingleton<Gallop.WebViewManager>.Instance.SetCustomFont(WebViewDefine.FontNameDefine.JP_DYNAMIC01);
            StandaloneWindowResize.CalcScreenSizeRatio();
            MonoSingleton<UIManager>.Instance.ChangeResizeUIForPC((int)UIManager.DefaultResolution.x, (int)UIManager.DefaultResolution.y);
            AnMonoSingleton<AnRootManager>.Instance.ScreenRate = Gallop.Screen.Width / (float)UnityEngine.Screen.width;
            TitleSceneController sceneController = __instance.GetSceneController<TitleSceneController>();
            yield return sceneController.PlayMovieAsync();
            yield break;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UncheaterInit), nameof(UncheaterInit.OnApplicationPause))]
        private static bool UncheaterInit_OnApplicationPause(UncheaterInit __instance, bool value)
        {
            // Allow direct launch
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BootSystem), nameof(BootSystem.Awake))]
        private static bool BootSystem_Awake(BootSystem __instance)
        {
            // Allow direct launch
            Perf.Time instance = Perf.Time.Instance;
            __instance.StartCoroutine(__instance.BootCoroutine());

            // TODO: Force landscape
            Vector2 changedSize = new((UnityEngine.Screen.width > UnityEngine.Screen.height) ? UnityEngine.Screen.height : UnityEngine.Screen.width, (UnityEngine.Screen.width > UnityEngine.Screen.height) ? UnityEngine.Screen.width : UnityEngine.Screen.height);
            changedSize = StandaloneWindowResize.GetChangedSize(changedSize.x, changedSize.y, true);
            if (StandaloneWindowResize.CheckOverScreenSize(changedSize.x, changedSize.y))
            {
                StandaloneWindowResize.IsPreventReShape = false;
                StandaloneWindowResize.KeepAspectRatio(changedSize.x, changedSize.y);
                StandaloneWindowResize.IsPreventReShape = true;
                return false;
            }
            Gallop.Screen.SetResolution((int)changedSize.x, (int)changedSize.y, false, false);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PartsEpisodeList), nameof(PartsEpisodeList.SetupStoryExtraEpisodeList))]
        private static void PartsEpisodeList_SetupStoryExtraEpisodeList(PartsEpisodeList __instance,
            EpisodeDefine.ExtraSubCategory extraSubCategory, List<EpisodePartData> partDataList, EpisodePartData partData, Action<EpisodePartData, EpisodeStoryData> onClick)
        {
            __instance._voiceButton._playVoiceButton.SetOnClick(() =>
            {
                var bannerList = MasterDataManager.Instance.masterBannerData.GetListWithGroupId(7);
                int announceId = -1;
                foreach (var item in bannerList)
                {
                    if (item.Type == 7 && item.ConditionValue == partData.Id)
                    {
                        announceId = item.Transition;
                    }
                }

                if (announceId == -1 && partData.Id < 1005)
                {
                    announceId = partData.Id - 1002;
                }

                DialogAnnounceEvent.Open(announceId, () => { }, () => { });
            });
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DialogCircleItemDonate), nameof(DialogCircleItemDonate.Initialize))]
        private static void DialogCircleItemDonate_Initialize(DialogCircleItemDonate __instance, DialogCommon dialog, WorkCircleChatData.ItemRequestInfo itemRequestInfo)
        {
            __instance._donateCount = __instance.CalcDonateItemMax();
            __instance.ValidateDonateItemCount();
            __instance.ApplyDonateItemCountText();
            __instance.OnClickPlusButton();
        }
    }

    [HarmonyPatch(typeof(FrameRateController))]
    internal class FrameRateControllerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(FrameRateController.OverrideByNormalFrameRate))]
        private static bool OverrideByNormalFrameRate(ref FrameRateController.FrameRateOverrideLayer layer)
        {
            layer = FrameRateController.FrameRateOverrideLayer.SystemValue;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(FrameRateController.OverrideByMaxFrameRate))]
        private static bool OverrideByMaxFrameRate(ref FrameRateController.FrameRateOverrideLayer layer)
        {
            layer = FrameRateController.FrameRateOverrideLayer.SystemValue;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(FrameRateController.ResetOverride))]
        private static bool ResetOverride(ref FrameRateController.FrameRateOverrideLayer layer)
        {
            layer = FrameRateController.FrameRateOverrideLayer.SystemValue;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(FrameRateController.ReflectionFrameRate))]
        private static bool ReflectionFrameRate()
        {
            Application.targetFrameRate = Config.Current.maxFps;
            return false;
        }
    }

    [HarmonyPatch(typeof(CySpringUpdater))]
    internal class CySpringUpdateModePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(CySpringUpdater.SpringUpdateMode), MethodType.Setter)]
        private static bool set_SpringUpdateMode(CySpringUpdater __instance, ref CySpringController.SpringUpdateMode value)
        {
            value = Config.Current.cySpringUpdateMode.Value;
            return true;
        }


        [HarmonyPrefix]
        [HarmonyPatch(nameof(CySpringUpdater.SpringUpdateMode), MethodType.Getter)]
        private static bool get_SpringUpdateMode(CySpringUpdater __instance, ref CySpringController.SpringUpdateMode __result)
        {
            __instance.SpringUpdateMode = Config.Current.cySpringUpdateMode.Value;
            return true;
        }
    }

    internal class CharacterSystemTextCaptionPatch
    {
        static IntPtr currentPlayerHandle = IntPtr.Zero;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Cute.Cri.AtomSourceEx), nameof(Cute.Cri.AtomSourceEx.SetParameter))]
        private static void AtomSourceEx_SetParameter(Cute.Cri.AtomSourceEx __instance)
        {
            var r = new Regex(@"(\d{4})(?:\d{2})");
            var matches = r.Match(__instance.cueSheet).Groups;
            if (matches.Count > 1)
            {
                var texts = MasterCharacterSystemText.GetByCharaId(int.Parse(matches[1].Value));
                var text = texts.Find(text =>
                {
                    if (__instance.cueSheet == text.CueSheet && __instance.CueId == text.CueId)
                    {
                        return true;
                    }
                    return false;
                });
                if (text != null)
                {
                    var handle = __instance.player.nativeHandle;
                    if (!text.CueSheet.Contains("_home_") &&
                        !text.CueSheet.Contains("_kakao_") &&
                        !text.CueSheet.Contains("_tc_") &&
                        !text.CueSheet.Contains("_title_") &&
                        !text.CueSheet.Contains("_gacha_") &&
                        text.VoiceId != 95001 &&
                        (text.CharacterId < 9000 || text.VoiceId == 70000))
                    {
                        var displayTime = UIManager.Instance._notification._displayTime;
                        UIManager.Instance._notification._displayTime = AudioManager.Instance.GetCueLength(text.CueSheet, text.CueId);
                        currentPlayerHandle = __instance.player.handle;
                        UIManager.Instance.ShowNotification(GallopUtil.LineHeadWrap(text.Text.Replace('\n', ' '), 32));
                        UIManager.Instance._notification._displayTime = displayTime;
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CriAtomExPlayer), nameof(CriAtomExPlayer.criAtomExPlayer_Stop))]
        private static void CriAtomExPlayer_criAtomExPlayer_Stop(IntPtr player)
        {
            if (player == currentPlayerHandle)
            {
                currentPlayerHandle = IntPtr.Zero;
                UIManager.Instance.HideNotification();
            }
        }
    }

    internal class ScreenPatch
    {
        private static float aspectRatio = 16 / 9;

        static int lastDisplayWidth = GetResolution().width;
        static int lastDisplayHeight = GetResolution().height;
        static int lastVirtWindowWidth = GetResolution().width - 400;
        static int lastVirtWindowHeight = (int)(lastVirtWindowWidth / aspectRatio);
        static int lastHrizWindowWidth = GetResolution().width - 400;
        static int lastHrizWindowHeight = (int)(lastHrizWindowWidth / aspectRatio);

        static bool fullScreenFl = Config.Current.forceLandscape && Config.Current.autoFullscreen;
        static bool fullScreenFlOverride = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UnityEngine.Screen), nameof(UnityEngine.Screen.SetResolution), typeof(int), typeof(int), typeof(FullScreenMode), typeof(int))]
        private static void Screen_SetResolution(ref int width, ref int height, ref FullScreenMode fullscreenMode, int preferredRefreshRate)
        {
            if (Config.Current.forceLandscape && !Config.Current.autoFullscreen)
            {
                fullScreenFl = false;
                if (width < height)
                {
                    (width, height) = (height, width);
                    fullscreenMode = FullScreenMode.Windowed;
                    return;
                }
            }

            bool reqVirt = width < height;

            if (StandaloneWindowResize.IsVirt && fullScreenFl)
            {
                fullScreenFl = false;
                fullScreenFlOverride = false;
                width = lastVirtWindowWidth;
                height = lastVirtWindowHeight;
                return;
            }

            var display = Display.main;

            if (reqVirt && (display.renderingWidth > display.renderingHeight))
            {
                fullScreenFl = false;
                fullScreenFlOverride = false;
                if (lastVirtWindowWidth < lastVirtWindowHeight && Config.Current.forceLandscape)
                {
                    width = lastVirtWindowHeight;
                    height = lastVirtWindowWidth;
                    return;
                }
                width = lastVirtWindowWidth;
                height = lastVirtWindowHeight;
                return;
            }

            bool needFullScreen = false;

            var r = GetResolution();

            if (Config.Current.autoFullscreen)
            {
                if (StandaloneWindowResize.IsVirt && r.width / r.height == (9 / 16))
                {
                    needFullScreen = true;
                }
                else if (!StandaloneWindowResize.IsVirt && r.width / r.height == (16 / 9))
                {
                    needFullScreen = true;
                }
            }

            if (!fullScreenFl && !Config.Current.forceLandscape)
            {
                if (!(display.renderingWidth > display.renderingHeight))
                {
                    lastVirtWindowWidth = display.renderingWidth;
                    lastVirtWindowHeight = display.renderingHeight;
                    if (needFullScreen && (lastHrizWindowWidth == 0 || lastHrizWindowHeight == 0))
                    {
                        float newRatio = r.width / r.height;

                        lastHrizWindowWidth = r.width - 400;
                        lastHrizWindowHeight = (int)(lastHrizWindowWidth / newRatio);
                    }
                }
                else
                {
                    lastHrizWindowWidth = display.renderingWidth;
                    lastHrizWindowHeight = display.renderingHeight;
                }
            }

            if (!fullScreenFlOverride)
            {
                fullScreenFl = needFullScreen;
            }

            if (!reqVirt && !fullScreenFl && lastHrizWindowWidth > 0 && lastHrizWindowHeight > 0)
            {
                width = lastHrizWindowWidth;
                height = lastHrizWindowHeight;
            }
            width = fullScreenFl ? r.width : width;
            height = fullScreenFl ? r.height : height;
            fullscreenMode = fullScreenFl ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StandaloneWindowResize), nameof(StandaloneWindowResize.getOptimizedWindowSizeVirt))]
        private static void StandaloneWindowResize_getOptimizedWindowSizeVirt(ref int _width, ref int _height, ref Vector3 __result)
        {
            _height = (int)(_width * aspectRatio);

            __result.x = _width;
            __result.y = _height;
            __result.z = aspectRatio;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StandaloneWindowResize), nameof(StandaloneWindowResize.getOptimizedWindowSizeHori))]
        private static void StandaloneWindowResize_getOptimizedWindowSizeHori(ref int _width, ref int _height, ref Vector3 __result)
        {
            _width = (int)(_height * aspectRatio);

            __result.x = _width;
            __result.y = _height;
            __result.z = aspectRatio;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Gallop.Screen), nameof(Gallop.Screen.Height), MethodType.Getter)]
        private static bool Screen_get_Height(ref int __result)
        {
            int w = System.Math.Max(lastDisplayWidth, lastDisplayHeight);
            int h = System.Math.Min(lastDisplayWidth, lastDisplayHeight);

            __result = StandaloneWindowResize.IsVirt ? w : h;
            // __result = 2560;

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Gallop.Screen), nameof(Gallop.Screen.Width), MethodType.Getter)]
        private static bool Screen_get_Width(ref int __result)
        {
            int w = System.Math.Max(lastDisplayWidth, lastDisplayHeight);
            int h = System.Math.Min(lastDisplayWidth, lastDisplayHeight);

            __result = StandaloneWindowResize.IsVirt ? h : w;

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CanvasScaler), nameof(CanvasScaler.referenceResolution), MethodType.Setter)]
        private static bool CanvasScaler_set_referenceResolution(ref Vector2 value, CanvasScaler __instance)
        {
            var r = GetResolution();

            if (Config.Current.forceLandscape)
            {
                value.x /= (System.Math.Max(1, r.width / 1920) * Config.Current.forceLandscapeUiScale);
                value.y /= (System.Math.Max(1, r.height / 1080) * Config.Current.forceLandscapeUiScale);
            }
            else
            {
                value.x = r.width;
                value.y = r.height;
            }

            try
            {

                __instance.SetScaleFactor(System.Math.Max(1, r.width / 1920) * (Config.Current.forceLandscape ? Config.Current.forceLandscapeUiScale : Config.Current.uiScale));
            }
            catch
            {
            }


            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BootSystem), nameof(BootSystem.Awake))]
        private static bool BootSystem_Awake(BootSystem __instance)
        {
            var r = GetResolution();
            lastDisplayWidth = r.width;
            lastDisplayHeight = r.height;
            return true;
        }

        private static Resolution GetResolution()
        {
            Resolution res;
            UnityEngine.Screen.get_currentResolution_Injected(out res);

            int width = (int)(System.Math.Min(res.height, res.width) * aspectRatio);
            if (res.width > res.height)
            {
                int temp = res.width;
                res.width = width;
                res.height = temp;
            }
            else
            {
                int temp = res.height;
                res.height = width;
                res.width = temp;
            }

            return res;
        }

    }
}
