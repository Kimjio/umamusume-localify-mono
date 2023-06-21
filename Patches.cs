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
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR;
using static WindowsAPI;

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
        private static bool TitleViewController_InitializeView(TitleViewController __instance, ref IEnumerator __result)
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


            IntPtr activeWindow = GetActiveWindow();
            long num = GetWindowLong(activeWindow, StandaloneWindowResize.WINAPI_GWL_STYLE);
            num |= StandaloneWindowResize.WINAPI_WS_MAXIMIZEBOX;

            SetWindowLongPtr(activeWindow, StandaloneWindowResize.WINAPI_GWL_STYLE, (IntPtr)num);

            if (!Config.Current.freeFormWindow)
            {
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
            }
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

    [HarmonyPatch(typeof(Gallop.Screen))]
    internal class GallopScreenPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Gallop.Screen.Width), MethodType.Getter)]
        private static bool get_Width(ref int __result)
        {
            __result = UnityEngine.Screen.width;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Gallop.Screen.Height), MethodType.Getter)]
        private static bool get_Height(ref int __result)
        {
            __result = UnityEngine.Screen.height;
            return false;
        }

        /*[HarmonyPrefix]
        [HarmonyPatch(nameof(Gallop.Screen.OriginalScreenWidth), MethodType.Getter)]
        private static bool get_OriginalScreenWidth(ref int __result)
        {
            __result = UnityEngine.Screen.width;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Gallop.Screen.OriginalScreenHeight), MethodType.Getter)]
        private static bool get_OriginalScreenHeight(ref int __result)
        {
            __result = UnityEngine.Screen.height;
            return false;
        }*/

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Gallop.Screen.SetResolution))]
        private static bool SetResolution(ref int w, ref int h, ref bool fullscreen, ref bool forceUpdate)
        {
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Gallop.Screen.IsCurrentOrientation))]
        private static bool IsCurrentOrientation(ref bool __result)
        {
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(GraphicSettings))]
    internal class GraphicSettingsPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GraphicSettings.GetVirtualResolution))]
        private static bool GetVirtualResolution(ref Vector2Int __result)
        {
            __result = new Vector2Int(UnityEngine.Screen.width, UnityEngine.Screen.height);
            return false;
        }
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GraphicSettings.GetVirtualResolution3D))]
        private static bool GetVirtualResolution3D(ref bool isForcedWideAspect, ref Vector2Int __result)
        {
            __result = new Vector2Int(UnityEngine.Screen.width, UnityEngine.Screen.height);
            return false;
        }
    }

    [HarmonyPatch(typeof(LowResolutionCamera))]
    internal class LowResolutionCameraPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(LowResolutionCamera.UpdateDirection))]
        private static bool UpdateDirection(LowResolutionCamera __instance)
        {
            if (!__instance._isInitialized)
            {
                return false;
            }
            __instance._resolution3d = MonoSingleton<GraphicSettings>.Instance.GetVirtualResolution3D(false);
            __instance._resolution2d = MonoSingleton<GraphicSettings>.Instance.GetVirtualResolution();
            __instance._fovFactor = 1f;
            if (__instance.gameObject.activeInHierarchy)
            {
                __instance.CreateRenderTexture(ref __instance._resolution3d);
                __instance.RemakeCommandBuffer();
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(LowResolutionCameraFrameBuffer))]
    internal class LowResolutionCameraFrameBufferPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(LowResolutionCameraFrameBuffer.UpdateDirection))]
        private static bool UpdateDirection(LowResolutionCameraFrameBuffer __instance)
        {
            if (!__instance._isInitialized)
            {
                return false;
            }
            __instance._resolution3d = MonoSingleton<GraphicSettings>.Instance.GetVirtualResolution3D(false);
            __instance._resolution2d = MonoSingleton<GraphicSettings>.Instance.GetVirtualResolution();
            __instance._fixScaleX = 1f;
            __instance._fixScaleY = 1f;
            __instance._offsetX = 0f;
            __instance._offsetY = 0f;
            __instance._fovFactor = 1f;
            if (__instance._frameBuffer.Material != null)
            {
                Material material = __instance._frameBuffer.Material;
                material.SetFloat(ShaderManager.GetPropertyId(ShaderManager.PropertyId._lowResFixScaleX), __instance._fixScaleX);
                material.SetFloat(ShaderManager.GetPropertyId(ShaderManager.PropertyId._lowResFixScaleY), __instance._fixScaleY);
                material.SetFloat(ShaderManager.GetPropertyId(ShaderManager.PropertyId._lowResOffsetX), __instance._offsetX);
                material.SetFloat(ShaderManager.GetPropertyId(ShaderManager.PropertyId._lowResOffsetY), __instance._offsetY);
            }
            if (__instance.NeedUpdateCommandBuffer())
            {
                __instance.RemakeCommandBuffer();
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(UIManager))]
    internal class UIManagerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIManager.GetCameraSizeByOrientation))]
        private static bool GetCameraSizeByOrientation(ScreenOrientation orientation, ref float __result)
        {
            __result = 5f;
            /*if (orientation == ScreenOrientation.LandscapeLeft)
            {
                float num;
                float num2;
                if (Gallop.Screen.IsVertical)
                {
                    num = UnityEngine.Screen.height;
                    num2 = UnityEngine.Screen.width;
                }
                else
                {
                    num = UnityEngine.Screen.width;
                    num2 = UnityEngine.Screen.height;
                }
                __result = 5f / (num / num2);
            }*/

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIManager.DefaultResolution), MethodType.Getter)]
        private static bool get_DefaultResolution(ref Vector2 __result)
        {
            __result = new Vector2(UnityEngine.Screen.width, UnityEngine.Screen.height);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(UIManager.WaitBootSetup))]
        private static bool WaitBootSetup(UIManager __instance, ref IEnumerator __result)
        {
            __result = WaitBootSetup(__instance);
            return false;
        }

        private static IEnumerator WaitBootSetup(UIManager __instance)
        {
            Vector2 defaultResolution = UIManager.DefaultResolution;
            CanvasScaler[] canvasScalerList = __instance.GetCanvasScalerList();
            for (int i = 0; i < canvasScalerList.Length; i++)
            {
                canvasScalerList[i].referenceResolution = defaultResolution;
                if (defaultResolution.x < defaultResolution.y)
                {

                    canvasScalerList[i].scaleFactor = System.Math.Min(1, System.Math.Max(1, defaultResolution.y / 1080) * Config.Current.freeFormUiScalePortrait / (defaultResolution.y / 1080));
                }
                else
                {
                    canvasScalerList[i].scaleFactor = System.Math.Min(1, System.Math.Max(1, defaultResolution.x / 1920) * Config.Current.freeFormUiScaleLandscape / (defaultResolution.x / 1920));
                }
            }
            UIManager.Instance._bgCamera.backgroundColor = Color.clear;
            __instance.AdjustSafeArea();
            yield return UIManager.WaitForEndOfFrame;
            yield return UIManager.WaitForEndOfFrame;
            __instance.CreateRenderTextureFromScreen();
            yield break;
        }
    }

    [HarmonyPatch(typeof(GallopInput))]
    internal class GallopInputPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GallopInput.mousePosition))]
        private static bool mousePosition(ref Vector3 __result)
        {
            __result = Input.mousePosition;
            return false;
        }
    }

    [HarmonyPatch(typeof(GallopFrameBuffer))]
    internal class GallopFrameBufferPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GallopFrameBuffer.Initialize))]
        private static void Initialize(GallopFrameBuffer __instance)
        {
            if (!GallopFrameBuffer._frameBufferList.Contains(__instance))
            {
                GallopFrameBuffer._frameBufferList.Add(__instance);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(GallopFrameBuffer.Release))]
        private static void Release(GallopFrameBuffer __instance)
        {
            if (GallopFrameBuffer._frameBufferList.Contains(__instance))
            {
                GallopFrameBuffer._frameBufferList.Remove(__instance);
            }
        }
    }

    [HarmonyBefore(nameof(GallopScreenPatch))]
    [HarmonyPatch(typeof(StandaloneWindowResize))]
    internal class StandaloneWindowResizePatch
    {
        const uint WM_SIZE = 5;
        const uint WM_CLOSE = 16;
        const uint WM_WINDOWPOSCHANGED = 71;
        const uint WM_SIZING = 532;
        const uint WM_SYSCOMMAND = 274;
        static readonly IntPtr SC_MAXIMIZE = new(61490);
        static readonly IntPtr SC_MINIMIZE = new(61472);
        static readonly IntPtr SC_RESTORE = new(61728);
        static readonly IntPtr SC_RESTORE_1 = new(61730);

        struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int Width;
            public int Height;
            public uint flags;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct Size
        {
            public Size(IntPtr raw)
            {
                this.raw = raw;
            }

            [FieldOffset(0)]
            public IntPtr raw;

            [FieldOffset(0)]
            public ushort Low;

            [FieldOffset(2)]
            public ushort High;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(StandaloneWindowResize.ReshapeAspectRatio))]
        private static bool ReshapeAspectRatio()
        {
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(StandaloneWindowResize.KeepAspectRatio))]
        private static bool KeepAspectRatio(ref float currentWidth, ref float currentHeight)
        {
            return false;
        }

        private static IEnumerator ResizeStartCoroutine(Vector2 vector)
        {
            StandaloneWindowResize.IsPreventReShape = true;
            StandaloneWindowResize.DisableWindowHitTest();

            bool isVert = vector.x < vector.y;

            yield return new WaitForEndOfFrame();
            if (MonoSingleton<UIManager>.HasInstance())
            {
                Gallop.Screen.InitializeChangeScaleForPC(isVert, out Gallop.Screen._bgCameraSettings);
                UIManager.Instance._bgCamera.backgroundColor = Color.clear;
            }

            AnMonoSingleton<AnRootManager>.Instance.ScreenRate = StandaloneWindowResize._aspectRatio;

            // UnityEngine.Screen.SetResolution((int)vector.x, (int)vector.y, false, 0);
            // StandaloneWindowResize.KeepAspectRatio();
            Gallop.Screen.UpdateForPC();

            if (MonoSingleton<UIManager>.HasInstance())
            {
                MonoSingleton<UIManager>.Instance.ChangeResizeUIForPC((int)vector.x, (int)vector.y);

                UnityEngine.Screen.orientation = ScreenOrientation.AutoRotation;

                MonoSingleton<UIManager>.Instance.gameObject.transform.localScale = new Vector3(1f, 1f, 1f);
                MonoSingleton<UIManager>.Instance.EndOrientation(ref Gallop.Screen._bgCameraSettings);
                MonoSingleton<TapEffectController>.Instance.Enable();
                Vector2 resolution = vector;
                //res.x /= (max(1.0f, res.x / 1920.f) * g_force_landscape_ui_scale);
                // res.y /= (max(1.0f, res.y / 1080.f) * g_force_landscape_ui_scale);
                CanvasScaler[] canvasScalerList = MonoSingleton<UIManager>.Instance.GetCanvasScalerList();
                CanvasScaler[] array = canvasScalerList;
                for (int i = 0; i < array.Length; i++)
                {
                    CanvasScaler canvasScaler = array[i];
                    bool keepActive = canvasScaler.gameObject.activeSelf;
                    canvasScaler.gameObject.SetActive(true);
                    canvasScaler.referenceResolution = resolution;
                    canvasScaler.gameObject.SetActive(keepActive);
                    if (isVert)
                    {

                        canvasScaler.scaleFactor = System.Math.Min(1, System.Math.Max(1, vector.y / 1080) * Config.Current.freeFormUiScalePortrait / (vector.y / 1080));
                    }
                    else
                    {
                        canvasScaler.scaleFactor = System.Math.Min(1, System.Math.Max(1, vector.x / 1920) * Config.Current.freeFormUiScaleLandscape / (vector.x / 1920));

                    }
                }

                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                MonoSingleton<UIManager>.Instance.AdjustSafeArea();
                MonoSingleton<UIManager>.Instance._bgManager.OnChangeResolutionByGraphicsSettings();
                yield return UIManager.WaitForEndOfFrame;
                yield return UIManager.WaitForEndOfFrame;

                yield return UIManager.WaitForEndOfFrame;
                yield return UIManager.WaitForEndOfFrame;
                MonoSingleton<UIManager>.Instance.CheckUIToFrameBufferBlitInstance();
                MonoSingleton<UIManager>.Instance.ReleaseRenderTexture();

                // int width = Gallop.Screen.Width;
                // int height = Gallop.Screen.Height;
                int width = UnityEngine.Screen.width;
                int height = UnityEngine.Screen.height;
                // width *= Display.main.systemWidth / 1920;
                // height *= Display.main.systemHeight / 1080;
                MonoSingleton<UIManager>.Instance._uiTexture = new RenderTexture(width, height, 24)
                {
                    autoGenerateMips = false,
                    useMipMap = false,
                    antiAliasing = 1
                };
                if (!MonoSingleton<UIManager>.Instance._uiTexture.Create())
                {
                    MonoSingleton<UIManager>.Instance.ReleaseRenderTexture();
                }

                MonoSingleton<UIManager>.Instance._uiCommandBuffer.Blit(MonoSingleton<UIManager>.Instance._uiTexture, BuiltinRenderTextureType.CurrentActive, MonoSingleton<UIManager>.Instance._blitToFrameMaterial);
                MonoSingleton<UIManager>.Instance._uiCamera.targetTexture = MonoSingleton<UIManager>.Instance._uiTexture;
                MonoSingleton<UIManager>.Instance._bgCamera.targetTexture = MonoSingleton<UIManager>.Instance._uiTexture;
                MonoSingleton<UIManager>.Instance._noImageEffectUICamera.targetTexture = MonoSingleton<UIManager>.Instance._uiTexture;
                if (MonoSingleton<UIManager>.Instance._uiToFrameBufferBlitCamera != null)
                {
                    MonoSingleton<UIManager>.Instance._uiToFrameBufferBlitCamera.enabled = true;
                }
            }

            GraphicSettings.Instance.Update3DRenderTexture();

            if (MonoSingleton<TapEffectController>.HasInstance())
            {
                MonoSingleton<TapEffectController>.Instance.RefreshAll();
            }
            if (MonoSingleton<UIManager>.HasInstance())
            {
                MonoSingleton<UIManager>.Instance.AdjustMissionClearContentsRootRect();
                MonoSingleton<UIManager>.Instance.AdjustSafeAreaToAnnounceRect();
                UIManager.Instance._bgCamera.backgroundColor = Color.clear;
            }

            if (RaceManager.HasInstance())
            {
                if (RaceManager.Instance.State == RaceDefine.RaceState.WinningCircle || RaceManager.Instance.State == RaceDefine.RaceState.End)
                {
                    RaceManager.Instance.StartCoroutine(
                        RaceManager.Instance.RaceMainView._resultList.MakeResultCuttCapture(RaceCameraManager.Instance.CurrentCamera, RaceCameraManager.Instance.FrameBuffer._renderBuffer, 
                        delegate {
                            RaceCameraManager.Instance.SetCourseCameraEnable(true);
                            MonoSingleton<RaceManager>.Instance.InitRaceResultCapture();
                        },
                        delegate
                        {
                            RaceCameraManager.Instance.SetCourseCameraEnable(false);
                            MonoSingleton<RaceManager>.Instance.ReleaseRaceResultScene(false);
                        }));
                }
            }

            StandaloneWindowResize.EnableWindowHitTest();
            StandaloneWindowResize.IsPreventReShape = false;
            yield break;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(StandaloneWindowResize.WndProc))]
        private static bool WndProc(IntPtr _hWnd, uint _msg, IntPtr _wParam, IntPtr lParam, ref IntPtr __result)
        {

            if (_msg == WM_CLOSE)
            {
                Application.Quit();
                __result = IntPtr.Zero;
                return false;
            }

            if (_msg == WM_SIZE)
            {
                IntPtr activeWindow = GetActiveWindow();
                long num = GetWindowLong(activeWindow, StandaloneWindowResize.WINAPI_GWL_STYLE);
                num |= StandaloneWindowResize.WINAPI_WS_MAXIMIZEBOX;

                SetWindowLongPtr(activeWindow, StandaloneWindowResize.WINAPI_GWL_STYLE, (IntPtr)num);

                var size = new Size(lParam);
                if (StandaloneWindowResize.windowLastWidth != size.Low || StandaloneWindowResize.windowLastHeight != size.High)
                {
                    Vector2 windowFrameSize = GetWindowFrameSize();
                    int contentWidth = size.Low - (int)windowFrameSize.x;
                    int contentHeight = size.High - (int)windowFrameSize.y;
                    Vector3 vector = new Vector3(contentWidth, contentHeight, 1f);
                    StandaloneWindowResize.windowLastWidth = size.Low;
                    StandaloneWindowResize.windowLastHeight = size.High;
                    StandaloneWindowResize.SaveChangedWidth(UnityEngine.Screen.width, UnityEngine.Screen.height);
                    StandaloneWindowResize._aspectRatio = vector.x / vector.y;

                    UIManager.Instance.StartCoroutine(ResizeStartCoroutine(vector));
                }
                __result = CallWindowProc(StandaloneWindowResize.oldWndProcPtr, _hWnd, _msg, _wParam, lParam);
                return false;
            }

            if (_msg == WM_SIZING)
            {
                WinAPIRect structure = (WinAPIRect)Marshal.PtrToStructure(lParam, typeof(WinAPIRect));
                if (StandaloneWindowResize.windowLastWidth != structure.Width || StandaloneWindowResize.windowLastHeight != structure.Height)
                {
                    Vector2 windowFrameSize = GetWindowFrameSize();
                    int num = (int)_wParam;
                    bool flag = num == 7 || num == 1 || num == 4;
                    bool flag2 = num == 3 || num == 4 || num == 5;
                    int contentWidth = structure.Width - (int)windowFrameSize.x;
                    int contentHeight = structure.Height - (int)windowFrameSize.y;
                    Vector3 vector = new Vector3(contentWidth, contentHeight, 1f);
                    int num8 = (int)vector.x + (int)windowFrameSize.x;
                    int num9 = (int)vector.y + (int)windowFrameSize.y;
                    if (flag)
                    {
                        structure.Left = structure.Right - num8;
                    }
                    else
                    {
                        structure.Right = structure.Left + num8;
                    }
                    if (flag2)
                    {
                        structure.Top = structure.Bottom - num9;
                    }
                    else
                    {
                        structure.Bottom = structure.Top + num9;
                    }
                    Marshal.StructureToPtr(structure, lParam, true);
                    StandaloneWindowResize.windowLastWidth = structure.Width;
                    StandaloneWindowResize.windowLastHeight = structure.Height;
                    StandaloneWindowResize.SaveChangedWidth(UnityEngine.Screen.width, UnityEngine.Screen.height);
                    StandaloneWindowResize._aspectRatio = vector.x / vector.y;
                }
                __result = CallWindowProc(StandaloneWindowResize.oldWndProcPtr, _hWnd, _msg, _wParam, lParam);
                return false;
            }
            return true;
        }

        [HarmonyPatch(nameof(StandaloneWindowResize.DisableMaximizebox))]
        private static bool DisableMaximizebox()
        {
            Console.WriteLine("DisableMaximizebox");
            return false;
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
