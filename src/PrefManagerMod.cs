using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MelonLoader;
using MelonLoader.Preferences;
using MelonPrefManager;
using MelonPrefManager.UI;
using UnityEngine;
using UniverseLib.Input;

[assembly: MelonGame()]
#if CPP
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]
#else
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.MONO)]
#endif
[assembly: MelonInfo(typeof(PrefManagerMod), PrefManagerMod.NAME, PrefManagerMod.VERSION, PrefManagerMod.AUTHOR)]

namespace MelonPrefManager
{
    public class PrefManagerMod : MelonMod
    {
        public const string GUID = "com.sinai.MelonPreferencesManager";
        public const string NAME = "MelonPreferencesManager";
        public const string AUTHOR = "Sinai";
        public const string VERSION = "1.0.6";

        public static PrefManagerMod Instance { get; private set; }

        // Internal config
        internal const string CTG_ID = "MelonPreferencesManager";
        internal static MelonPreferences_Category INTERNAL_CATEGORY;
        public static MelonPreferences_Entry<KeyCode> Main_Menu_Toggle;
        public static MelonPreferences_Entry<float> Startup_Delay;
        public static MelonPreferences_Entry<bool> Disable_EventSystem_Override;

        public override void OnApplicationStart()
        {
            Instance = this;
            InitConfig();

            UniverseLib.Universe.Init(Startup_Delay.Value, LateInit, LogHandler, new()
            {
                Disable_EventSystem_Override = Disable_EventSystem_Override.Value,
                Force_Unlock_Mouse = true,
                Unhollowed_Modules_Folder = Path.Combine(
                                                Path.GetDirectoryName(MelonHandler.ModsDirectory),
                                                Path.Combine("MelonLoader", "Managed"))
            });
        }

        private static void LateInit()
        {
            UIManager.Init();
        }

        public override void OnUpdate()
        {
            if (!UIManager.UIRoot)
                return;

            if (InputManager.GetKeyDown(Main_Menu_Toggle.Value))
                UIManager.ShowMenu = !UIManager.ShowMenu;
        }

        public static void InitConfig()
        {
            INTERNAL_CATEGORY = MelonPreferences.CreateCategory(CTG_ID, null);

            Main_Menu_Toggle = INTERNAL_CATEGORY.CreateEntry("Main Menu Toggle Key", KeyCode.F5);
            Startup_Delay = INTERNAL_CATEGORY.CreateEntry("Startup Delay", 1f);
            Disable_EventSystem_Override = INTERNAL_CATEGORY.CreateEntry("Disable EventSystem Override", false);
            
            //InitTest();
        }

        private void LogHandler(string log, LogType level)
        {
            switch (level)
            {
                case LogType.Log:
                    this.LoggerInstance.Msg(log);
                    return;
                case LogType.Warning:
                case LogType.Assert:
                    this.LoggerInstance.Warning(log);
                    return;
                case LogType.Exception:
                case LogType.Error:
                    this.LoggerInstance.Error(log);
                    return;
            }
        }

        //  ~~~~~~~~~~~~~~~~ TEST CONFIG ~~~~~~~~~~~~~~~~
        
        //static void InitTest()
        //{
        //    //MelonPreferences.Mapper.
        //
        //    var testCtg = MelonPreferences.CreateCategory("TestConfig");
        //
        //    testCtg.CreateEntry("This is an entry name", true, description: "Descriptions with new\r\nlines are supported");
        //    testCtg.CreateEntry("A Byte value", (byte)1, description: "What happens if an invalid value is entered?");
        //    testCtg.CreateEntry("Int slider", 32, description: "You can use sliders for any number type", validator: new ValueRange<int>(0, 100));
        //    testCtg.CreateEntry("Float slider", 666f, description: "This setting has a ValueRange of 0 to 1000", validator: new ValueRange<float>(0, 1000f));
        //    testCtg.CreateEntry("Key binding", KeyCode.Dollar, description: "KeyCodes have a special rebind helper");
        //    testCtg.CreateEntry("Enum example", CameraClearFlags.Color, description: "Enums use a dropdown");
        //    testCtg.CreateEntry("Multiline Input", (string)null, description: "Strings use a multi-line input field");
        //    testCtg.CreateEntry("My favourite color", new Color32(20, 40, 60, 255), description: "Colors have a special color picker");
        //    testCtg.CreateEntry("Float structs", Vector3.down, description: "Vector/Quaternion/etc use an editor like this");
        //    testCtg.CreateEntry("Flag toggles", BindingFlags.Public, description: "Enums with [Flags] attribute use a multi-toggle");
        //    testCtg.CreateEntry("Arrays", new[] { 0f, 1f }, description: "Arrays and other types will use the default Toml input");
        //    testCtg.CreateEntry("TestCustom", new TestConfigClass() { myString = "helloworld", myInt = 69 }, null, "Testing a custom type");
        //}
        //
        //public class TestConfigClass
        //{
        //    public string myString = "";
        //    public int myInt;
        //}
        //
        ////  ~~~~~~~~~~~~~~~~ END TEST CONFIG ~~~~~~~~~~~~~~~~

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
                    Instance.LoggerInstance.Msg(log); break;

                case LogType.Warning:
                    Instance.LoggerInstance.Warning(log); break;

                case LogType.Error:
                case LogType.Exception:
                    Instance.LoggerInstance.Error(log); break;
            }
        }

#endregion
    }
}
