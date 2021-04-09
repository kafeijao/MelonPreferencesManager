using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MelonLoader;
using MelonLoader.Preferences;
using MelonLoader.Tomlyn.Model;
using MelonPrefManager;
using MelonPrefManager.Input;
using MelonPrefManager.Runtime;
using MelonPrefManager.UI;
using UnityEngine;

[assembly: MelonGame()]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.UNIVERSAL)]
[assembly: MelonInfo(typeof(PrefManagerMod), PrefManagerMod.NAME, PrefManagerMod.VERSION, PrefManagerMod.AUTHOR)]

namespace MelonPrefManager
{
    public class PrefManagerMod : MelonMod
    {
        public const string NAME = "MelonPreferencesManager";
        public const string AUTHOR = "Sinai";
        public const string VERSION = "0.4.8";

        public static PrefManagerMod Instance { get; private set; }

        // Internal config
        internal const string CTG_ID = "MelonPreferencesManager";
        internal static MelonPreferences_Category INTERNAL_CATEGORY;
        public static MelonPreferences_Entry<KeyCode> Main_Menu_Toggle;

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
            InputManager.Update();
        }

        public static void InitConfig()
        {
            INTERNAL_CATEGORY = MelonPreferences.CreateCategory(CTG_ID, null);

            Main_Menu_Toggle = INTERNAL_CATEGORY.CreateEntry("Main Menu Toggle Key", KeyCode.F5);

            // InitTest();
        }

        ////  ~~~~~~~~~~~~~~~~ TEST CONFIG ~~~~~~~~~~~~~~~~

        //static void InitTest()
        //{
        //    MelonPreferences.Mapper.RegisterMapper(TestReader, TestWriter);

        //    var testCtg = MelonPreferences.CreateCategory("TestConfig");

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

        //public class TestConfigClass
        //{
        //    public string myString = "";
        //    public int myInt;
        //}

        //public static TomlObject TestWriter(TestConfigClass testConfig)
        //{
        //    if (testConfig == null)
        //        return null;

        //    string[] arr = new[] { testConfig.myString, testConfig.myInt.ToString() };
        //    return MelonPreferences.Mapper.WriteArray(arr);
        //}

        //public static TestConfigClass TestReader(TomlObject value)
        //{
        //    try
        //    {
        //        string[] arr = MelonPreferences.Mapper.ReadArray<string>(value as TomlArray);
        //        return new TestConfigClass
        //        {
        //            myString = arr[0],
        //            myInt = int.Parse(arr[1]),
        //        };
        //    }
        //    catch
        //    {
        //        return default;
        //    }
        //}

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
                    MelonLogger.Msg(log); break;

                case LogType.Warning:
                    MelonLogger.Warning(log); break;

                case LogType.Error:
                case LogType.Exception:
                    MelonLogger.Error(log); break;
            }
        }

        #endregion
    }
}
