using BepInEx;
using Gallop;
using HarmonyLib;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace UmamusumeLocalify
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("umamusume.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            var loadResult = UmamusumeLocalify.Config.LoadConfig();

            if (UmamusumeLocalify.Config.Current.enableConsole)
            {
                WinConsole.Initialize();
            }

            Console.WriteLine($"{PluginInfo.PLUGIN_GUID} is loaded!");

            if (!loadResult)
            {
                Console.WriteLine("WARN: config.json load failed.");
            }

            var config = UmamusumeLocalify.Config.Current;

            Harmony.CreateAndPatchAll(typeof(CommonPatches));

            if (config.maxFps > -1)
            {
                Harmony.CreateAndPatchAll(typeof(FrameRateControllerPatch));
            }

            if (config.cySpringUpdateMode != null)
            {
                Harmony.CreateAndPatchAll(typeof(CySpringUpdateModePatch));
            }

            if (config.characterSystemTextCaption)
            {
                Harmony.CreateAndPatchAll(typeof(CharacterSystemTextCaptionPatch));
            }

            // Harmony.CreateAndPatchAll(typeof(ScreenPatch));
            Harmony.CreateAndPatchAll(typeof(GallopScreenPatch));
            Harmony.CreateAndPatchAll(typeof(GallopInputPatch));
            Harmony.CreateAndPatchAll(typeof(GallopFrameBufferPatch));
            Harmony.CreateAndPatchAll(typeof(LowResolutionCameraPatch));
            Harmony.CreateAndPatchAll(typeof(LowResolutionCameraFrameBufferPatch));
            Harmony.CreateAndPatchAll(typeof(UIManagerPatch));
            Harmony.CreateAndPatchAll(typeof(GraphicSettingsPatch));
            Harmony.CreateAndPatchAll(typeof(StandaloneWindowResizePatch));
        }
    }

    internal class WinConsole
    {
        public static void Initialize(bool alwaysCreateNewConsole = true)
        {
            bool consoleAttached = true;
            if (alwaysCreateNewConsole
                || (AttachConsole(ATTACH_PARRENT) == 0
                && Marshal.GetLastWin32Error() != ERROR_ACCESS_DENIED))
            {
                consoleAttached = AllocConsole() != 0;
            }

            if (consoleAttached)
            {
                SetConsoleOutputCP(65001);
                InitializeOutStream();
                InitializeInStream();
            }
        }

        private static void InitializeOutStream()
        {
            var fs = CreateFileStream("CONOUT$", GENERIC_WRITE, FILE_SHARE_WRITE, FileAccess.Write);
            if (fs != null)
            {
                var writer = new StreamWriter(fs) { AutoFlush = true };
                Console.SetOut(writer);
                Console.SetError(writer);
            }
        }

        private static void InitializeInStream()
        {
            var fs = CreateFileStream("CONIN$", GENERIC_READ, FILE_SHARE_READ, FileAccess.Read);
            if (fs != null)
            {
                Console.SetIn(new StreamReader(fs));
            }
        }

        private static FileStream CreateFileStream(string name, uint win32DesiredAccess, uint win32ShareMode, FileAccess dotNetFileAccess)
        {
            var file = new SafeFileHandle(CreateFileW(name, win32DesiredAccess, win32ShareMode, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero), true);
            if (!file.IsInvalid)
            {
                var fs = new FileStream(file, dotNetFileAccess);
                return fs;
            }
            return null;
        }

        [DllImport("kernel32.dll",
            EntryPoint = "AllocConsole",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern int AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetConsoleOutputCP(int cp);

        [DllImport("kernel32.dll",
            EntryPoint = "AttachConsole",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern UInt32 AttachConsole(UInt32 dwProcessId);

        [DllImport("kernel32.dll",
            EntryPoint = "CreateFileW",
            SetLastError = true,
            CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CreateFileW(
              string lpFileName,
              UInt32 dwDesiredAccess,
              UInt32 dwShareMode,
              IntPtr lpSecurityAttributes,
              UInt32 dwCreationDisposition,
              UInt32 dwFlagsAndAttributes,
              IntPtr hTemplateFile
            );

        private const UInt32 GENERIC_WRITE = 0x40000000;
        private const UInt32 GENERIC_READ = 0x80000000;
        private const UInt32 FILE_SHARE_READ = 0x00000001;
        private const UInt32 FILE_SHARE_WRITE = 0x00000002;
        private const UInt32 OPEN_EXISTING = 0x00000003;
        private const UInt32 FILE_ATTRIBUTE_NORMAL = 0x80;
        private const UInt32 ERROR_ACCESS_DENIED = 5;

        private const UInt32 ATTACH_PARRENT = 0xFFFFFFFF;
    }
}
