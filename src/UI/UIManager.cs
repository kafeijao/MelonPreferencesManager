using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MelonPrefManager.UI.Utility;
using MelonPrefManager.Input;

namespace MelonPrefManager.UI
{
    public static class UIManager
    {
        public static GameObject CanvasRoot { get; private set; }
        public static EventSystem EventSys { get; private set; }

        internal static Shader BackupShader { get; private set; }

        public static bool ShowMenu
        {
            get => s_showMenu;
            set 
            {
                if (s_showMenu == value || !CanvasRoot)
                    return;

                s_showMenu = value;
                CanvasRoot.SetActive(value);
                CursorUnlocker.UpdateCursorControl();
            }
        }
        public static bool s_showMenu = true;

        internal static void Init()
        {
            CreateRootCanvas();

            LoadBundle();

            UIFactory.Init();

            PreferencesEditor.Create();

            // Force refresh of anchors etc
            Canvas.ForceUpdateCanvases();

            ShowMenu = false;

            PrefManagerMod.Log("UI initialized.");
        }

        internal static void Update()
        {
            if (!CanvasRoot)
                return;

            if (InputManager.GetKeyDown(PrefManagerMod.Main_Menu_Toggle.Value))
                ShowMenu = !ShowMenu;

            if (!ShowMenu)
                return;

            if (EventSystem.current != EventSys)
                CursorUnlocker.SetEventSystem();

            SliderScrollbar.UpdateInstances();
        }

        private static void CreateRootCanvas()
        {
            CanvasRoot = new GameObject("MelonPrefManager_Canvas");
            UnityEngine.Object.DontDestroyOnLoad(CanvasRoot);
            CanvasRoot.hideFlags |= HideFlags.HideAndDontSave;
            CanvasRoot.layer = 5;
            CanvasRoot.transform.position = new Vector3(0f, 0f, 1f);

            EventSys = CanvasRoot.AddComponent<EventSystem>();
            InputManager.AddUIModule();

            Canvas canvas = CanvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.referencePixelsPerUnit = 100;
            canvas.sortingOrder = 999;

            CanvasScaler scaler = CanvasRoot.AddComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            CanvasRoot.AddComponent<GraphicRaycaster>();
        }

        private static void LoadBundle()
        {
            AssetBundle bundle;

            bundle = LoadBundle("modern");

            if (bundle == null)
                bundle = LoadBundle("legacy");

            if (bundle == null)
            {
                PrefManagerMod.LogWarning("Could not load the UI Bundle!");
                return;
            }

            BackupShader = bundle.LoadAsset<Shader>("DefaultUI");

            // Fix for games which don't ship with 'UI/Default' shader.
            if (Graphic.defaultGraphicMaterial.shader?.name != "UI/Default")
                Graphic.defaultGraphicMaterial.shader = BackupShader;

            PrefManagerMod.Log("Loaded UI AssetBundle");
        }

        private static AssetBundle LoadBundle(string version)
        {
            try
            {
                var stream = typeof(PrefManagerMod)
                    .Assembly
                    .GetManifestResourceStream($"MelonPrefManager.Resources.mpmUI.{version}.bundle");

                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    byte[] buffer = new byte[81920];
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
                        ms.Write(buffer, 0, read);
                    bytes = ms.ToArray();
                }

                return AssetBundle.LoadFromMemory(bytes);
            }
            catch
            {
                return null;
            }
        }
    }
}
