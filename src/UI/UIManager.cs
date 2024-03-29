﻿using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.Panels;

namespace MelonPrefManager.UI
{
    // helper classes for managing category and entry representations

    internal class CategoryInfo
    {
        public MelonPreferences_Category RefCategory;

        internal List<EntryInfo> Prefs = new();

        internal bool isCompletelyHidden;
        internal ButtonRef listButton;
        internal GameObject contentObj;

        internal IEnumerable<GameObject> HiddenEntries
            => Prefs.Where(it => it.IsHidden).Select(it => it.content);
    }

    internal class EntryInfo
    {
        public MelonPreferences_Entry RefEntry;
        public bool IsHidden => RefEntry.IsHidden;

        internal GameObject content;
    }

    public class UIManager : PanelBase
    {
        public static UIManager Instance { get; private set; }

        internal static UIBase uiBase;

        public override string Name => $"<b>{PrefManagerMod.NAME}</b> <i>{PrefManagerMod.VERSION}</i>";

        public override int MinWidth => 750;
        public override int MinHeight => 750;
        public override Vector2 DefaultAnchorMin => new(0.2f, 0.02f);
        public override Vector2 DefaultAnchorMax => new(0.8f, 0.98f);

        public static bool ShowMenu
        {
            get => uiBase != null && uiBase.Enabled;
            set
            {
                if (uiBase == null || !uiBase.RootObject || uiBase.Enabled == value)
                    return;

                UniversalUI.SetUIActive(PrefManagerMod.GUID, value);
                Instance.SetActive(value);
            }
        }

        public static bool ShowHiddenConfigs { get; internal set; }

        //internal static GameObject MainPanel;
        internal static GameObject CategoryListContent;
        internal static GameObject ConfigEditorContent;

        internal static string Filter => currentFilter ?? "";

        private static string currentFilter;

        private static readonly HashSet<CachedConfigEntry> editingEntries = new();
        private static ButtonRef saveButton;

        private static readonly Dictionary<string, CategoryInfo> _categoryInfos = new();
        private static CategoryInfo _currentCategory;

        private static Color _normalDisabledColor = new(0.17f, 0.25f, 0.17f);
        private static Color _normalActiveColor = new(0, 0.45f, 0.05f);

        public UIManager(UIBase owner) : base(owner)
        {
            Instance = this;
        }

        internal static void Init()
        {
            uiBase = UniversalUI.RegisterUI(PrefManagerMod.GUID, null);

            CreateMenu();

            // Force refresh of anchors etc
            Canvas.ForceUpdateCanvases();

            ShowMenu = false;

            PrefManagerMod.Log("UI initialized.");
        }

        public static void OnEntryEdit(CachedConfigEntry entry)
        {
            if (!editingEntries.Contains(entry))
                editingEntries.Add(entry);

            if (!saveButton.Component.interactable)
                saveButton.Component.interactable = true;
        }

        public static void OnEntryUndo(CachedConfigEntry entry)
        {
            if (editingEntries.Contains(entry))
                editingEntries.Remove(entry);

            if (!editingEntries.Any())
                saveButton.Component.interactable = false;
        }

        public static void SavePreferences()
        {
            try
            {
                MelonPreferences.Save();
            }
            catch (Exception ex)
            {
                PrefManagerMod.LogWarning(ex);
            }

            for (int i = editingEntries.Count - 1; i >= 0; i--)
                editingEntries.ElementAt(i).OnSaveOrUndo();

            editingEntries.Clear();
            saveButton.Component.interactable = false;
        }

        // called by UIManager.Init
        internal static void CreateMenu()
        {
            if (Instance != null)
            {
                PrefManagerMod.LogWarning("An instance of PreferencesEditor already exists, cannot create another!");
                return;
            }

            new UIManager(uiBase);

            MelonCoroutines.Start(SetupCategories());
        }

        // wait for end of chainloader setup. mods that set up preferences after this aren't compatible atm.
        private static IEnumerator SetupCategories()
        {
            yield return null;

            foreach (var ctg in MelonPreferences.Categories.OrderBy(it => it.DisplayName))
            {
                if (_categoryInfos.ContainsKey(ctg.Identifier))
                    continue;

                try
                {
                    var info = new CategoryInfo()
                    {
                        RefCategory = ctg,
                    };

                    // List button

                    var btn = UIFactory.CreateButton(CategoryListContent, "BUTTON_" + ctg.Identifier, ctg.DisplayName);
                    btn.OnClick += () => { SetActiveCategory(ctg.Identifier); };
                    UIFactory.SetLayoutElement(btn.Component.gameObject, flexibleWidth: 9999, minHeight: 30, flexibleHeight: 0);
                    RuntimeHelper.SetColorBlock(btn.Component, _normalDisabledColor, new Color(0.7f, 1f, 0.7f),
                        new Color(0, 0.25f, 0));

                    info.listButton = btn;

                    // hide buttons for completely-hidden categories.
                    if (!ctg.Entries.Any(it => !it.IsHidden))
                    {
                        btn.Component.gameObject.SetActive(false);
                        info.isCompletelyHidden = true;
                    }

                    // Editor content

                    var content = UIFactory.CreateVerticalGroup(ConfigEditorContent, "CATEGORY_" + ctg.Identifier, true, false, true, true, 4,
                        default, new Color(0.05f, 0.05f, 0.05f));

                    // Actual config entry editors
                    foreach (var pref in ctg.Entries)
                    {
                        var cache = new CachedConfigEntry(pref, content);
                        cache.Enable();

                        var obj = cache.UIroot;

                        info.Prefs.Add(new EntryInfo()
                        {
                            RefEntry = pref,
                            content = obj
                        });

                        if (pref.IsHidden)
                            obj.SetActive(false);
                    }

                    content.SetActive(false);

                    info.contentObj = content;

                    _categoryInfos.Add(ctg.Identifier, info);
                }
                catch (Exception ex)
                {
                    PrefManagerMod.LogWarning($"Exception setting up category '{ctg.DisplayName}'!\r\n{ex}");
                }
            }
        }

        public static void SetHiddenConfigVisibility(bool show)
        {
            if (ShowHiddenConfigs == show)
                return;

            ShowHiddenConfigs = show;

            foreach (var entry in _categoryInfos)
            {
                var info = entry.Value;

                if (info.isCompletelyHidden)
                    info.listButton.Component.gameObject.SetActive(ShowHiddenConfigs);
            }

            if (_currentCategory != null && !ShowHiddenConfigs && _currentCategory.isCompletelyHidden)
                UnsetActiveCategory();

            RefreshFilter();
        }

        public static void FilterConfigs(string search)
        {
            currentFilter = search.ToLower();
            RefreshFilter();
        }

        internal static void RefreshFilter()
        {
            if (_currentCategory == null)
                return;

            foreach (var entry in _currentCategory.Prefs)
            {
                bool val = (string.IsNullOrEmpty(currentFilter)
                                || entry.RefEntry.DisplayName.ToLower().Contains(currentFilter)
                                || (entry.RefEntry.Description?.ToLower().Contains(currentFilter) ?? false))
                           && (!entry.IsHidden || ShowHiddenConfigs);

                entry.content.SetActive(val);
            }
        }

        public static void SetActiveCategory(string categoryIdentifier)
        {
            if (!_categoryInfos.ContainsKey(categoryIdentifier))
                return;

            UnsetActiveCategory();

            var info = _categoryInfos[categoryIdentifier];

            _currentCategory = info;

            var obj = info.contentObj;
            obj.SetActive(true);

            var btn = info.listButton;
            RuntimeHelper.SetColorBlock(btn.Component, _normalActiveColor);

            RefreshFilter();
        }

        internal static void UnsetActiveCategory()
        {
            if (_currentCategory == null)
                return;

            RuntimeHelper.SetColorBlock(_currentCategory.listButton.Component, _normalDisabledColor);
            _currentCategory.contentObj.SetActive(false);

            _currentCategory = null;
        }

        #region UI Construction

        public override void SetDefaultSizeAndPosition()
        {
            base.SetDefaultSizeAndPosition();

            Rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1000);
        }


        protected override void ConstructPanelContent()
        {
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(ContentRoot, true, false, true, true);

            ConstructTitleBar();

            ConstructSaveButton();

            ConstructToolbar();

            ConstructEditorViewport();
        }

        protected override void OnClosePanelClicked()
        {
            base.OnClosePanelClicked();
            ShowMenu = false;
        }

        private void ConstructTitleBar()
        {
            Text titleText = TitleBar.transform.GetChild(0).GetComponent<Text>();
            titleText.text = $"<b><color=#4cd43d>MelonPreferencesManager</color></b> <i><color=#ff3030>v{PrefManagerMod.VERSION}</color></i>";

            Button closeButton = TitleBar.GetComponentInChildren<Button>();
            RuntimeHelper.SetColorBlock(closeButton, new(1, 0.2f, 0.2f), new(1, 0.6f, 0.6f), new(0.3f, 0.1f, 0.1f));

            Text hideText = closeButton.GetComponentInChildren<Text>();
            hideText.color = Color.white;
            hideText.resizeTextForBestFit = true;
            hideText.resizeTextMinSize = 8;
            hideText.resizeTextMaxSize = 14;
        }

        private void ConstructSaveButton()
        {
            saveButton = UIFactory.CreateButton(ContentRoot, "SaveButton", "Save Preferences");
            saveButton.OnClick += SavePreferences;
            UIFactory.SetLayoutElement(saveButton.Component.gameObject, minHeight: 35, flexibleWidth: 9999);
            RuntimeHelper.SetColorBlock(saveButton.Component, new Color(0.1f, 0.3f, 0.1f),
                new Color(0.2f, 0.5f, 0.2f), new Color(0.1f, 0.2f, 0.1f), new Color(0.2f, 0.2f, 0.2f));

            saveButton.Component.interactable = false;
        }

        private void ConstructToolbar()
        {
            var toolbarGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "Toolbar", false, false, true, true, 4, new Vector4(3, 3, 3, 3),
                new Color(0.1f, 0.1f, 0.1f));

            var toggleObj = UIFactory.CreateToggle(toolbarGroup, "HiddenConfigsToggle", out Toggle toggle, out Text toggleText);
            toggle.isOn = false;
            toggle.onValueChanged.AddListener((bool val) =>
            {
                SetHiddenConfigVisibility(val);
            });
            toggleText.text = "Show Advanced Settings";
            UIFactory.SetLayoutElement(toggleObj, minWidth: 280, minHeight: 25, flexibleHeight: 0, flexibleWidth: 0);

            var inputField = UIFactory.CreateInputField(toolbarGroup, "FilterInput", "Search...");
            UIFactory.SetLayoutElement(inputField.Component.gameObject, flexibleWidth: 9999, minHeight: 25);
            inputField.OnValueChanged += FilterConfigs;
        }

        private void ConstructEditorViewport()
        {
            var horiGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "Main", true, true, true, true, 2, default, new Color(0.08f, 0.08f, 0.08f));

            var ctgList = UIFactory.CreateScrollView(horiGroup, "CategoryList", out GameObject ctgContent, out _, new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(ctgList, minWidth: 300, flexibleWidth: 0);
            CategoryListContent = ctgContent;
            UIFactory.SetLayoutGroup<VerticalLayoutGroup>(ctgContent, spacing: 3);

            var editor = UIFactory.CreateScrollView(horiGroup, "ConfigEditor", out GameObject editorContent, out _, new Color(0.05f, 0.05f, 0.05f));
            UIFactory.SetLayoutElement(editor, flexibleWidth: 9999);
            ConfigEditorContent = editorContent;
        }

        #endregion
    }
}
