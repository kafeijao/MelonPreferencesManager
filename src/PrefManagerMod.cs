using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using MelonLoader;
using MelonLoader.Preferences;
using MelonLoader.Tomlyn.Model;
using MelonPrefManager;
using MelonPrefManager.Input;
using MelonPrefManager.Runtime;
using MelonPrefManager.UI;
using UnityEngine;

[assembly: MelonInfo(typeof(PrefManagerMod), PrefManagerMod.NAME, PrefManagerMod.VERSION, PrefManagerMod.AUTHOR)]
[assembly: MelonGame(null, null)]

namespace MelonPrefManager
{
    public class PrefManagerMod : MelonMod
    {
        public const string GUID = "com.sinai.melonprefmanager";
        public const string NAME = "MelonPreferencesManager";
        public const string AUTHOR = "Sinai";
        public const string VERSION = "0.1.0";

        public static PrefManagerMod Instance { get; private set; }

        public override void OnApplicationStart()
        {
            Instance = this;

            RuntimeProvider.Init();
            InputManager.Init();
            InitConfig();
            UIFactory.Init();
            UIManager.Init();
        }

        public override void OnUpdate()
        {
            UIManager.Update();
        }

        #region INTERNAL CONFIG

        internal const string CTG_ID = "MelonPreferencesManager";
        internal static MelonPreferences_Category INTERNAL_CATEGORY;

        public static MelonPreferences_Entry<KeyCode> Main_Menu_Toggle;

        public static void InitConfig()
        {
            INTERNAL_CATEGORY = MelonPreferences.CreateCategory(CTG_ID, null);

            Main_Menu_Toggle = INTERNAL_CATEGORY.CreateEntry("Main Menu Toggle Key", KeyCode.F5);

            //  ~~~~~~~~~~~~~~~~ TEST CONFIG ~~~~~~~~~~~~~~~~

            var testCtg = MelonPreferences.CreateCategory("TestConfig");

            testCtg.CreateEntry("Bool", false, description: "Descriptions are supported");
            testCtg.CreateEntry("Byte", (byte)0xD, description: "Descriptions with new\r\nlines are supported.");
            testCtg.CreateEntry("Int", 32, description: "Hello world!");
            testCtg.CreateEntry("Float", 666f, description: "Example of a float value range", validator: new ValueRange<float>(0, 1000f));
            testCtg.CreateEntry("KeyCode", KeyCode.Dollar, description: "Dropdown example");
            testCtg.CreateEntry("String", "Hello, world!", description: "String example");
            testCtg.CreateEntry("Color", Color.magenta, description: "Color example");
            testCtg.CreateEntry("Vector3", Vector3.down, description: "Vector3 example");
        }

        #endregion

        #region LOGGING HELPERS

        public static void Log(object message)
            => Log(message, LogType.Log);

        public static void LogWarning(object message)
            => Log(message, LogType.Warning);

        public static void LogError(object message)
            => Log(message, LogType.Error);

        internal static void Log(object message, LogType logType)
        {
            string log = message?.ToString() ?? "";

            switch (logType)
            {
                case LogType.Log:
                case LogType.Assert:
                    MelonLogger.Msg(log); break;

                case LogType.Warning:
                    MelonLogger.Warning(log); break;

                case LogType.Error:
                case LogType.Exception:
                    MelonLogger.Error(log); break;
            }
        }

        #endregion

        #region CONSOLE EXIT CALLBACK

        internal static void InitConsoleCallback()
        {
            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler, true);
        }

        static bool ConsoleEventCallback(int eventType)
        {
            // 2 is Console Quit
            if (eventType == 2)
                MelonPreferences.Save();

            return false;
        }

        static ConsoleEventDelegate handler;
        private delegate bool ConsoleEventDelegate(int eventType);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

        #endregion
    }

    #region MELONPREFERENCES EXTENSIONS

    // Temporary, will PR these into MelonLoader repo if still necessary at release.

    //public static class MelonPrefExtensions
    //{
    //    public static object BoxedValue(this MelonPreferences_Entry entry)
    //    {
    //        return entry.GetType()
    //                    .GetField("myValue", BindingFlags.NonPublic | BindingFlags.Instance)
    //                    .GetValue(entry);
    //    }

    //    public static void SetValue(this MelonPreferences_Entry entry, object value)
    //    {
    //        entry.GetType()
    //             .GetProperty("Value")
    //             .SetValue(entry, value, null);
    //    }
    //}

    #endregion
}
