using MelonLoader;
using MelonPrefManager.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MelonPrefManager.UI
{
    public class PreferencesEditor
    {
        // helper classes for managing category and entry representations

        internal class CategoryInfo
        {
            public MelonPreferences_Category RefCategory;

            internal List<EntryInfo> Prefs = new List<EntryInfo>();

            internal bool isCompletelyHidden;
            internal Button listButton;
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

        public static PreferencesEditor Instance { get; internal set; }

        public static bool ShowHiddenConfigs { get; internal set; }

        internal static GameObject MainPanel;
        internal static GameObject CategoryListViewport;
        internal static GameObject ConfigEditorViewport;

        internal static string Filter => currentFilter ?? "";
        private static string currentFilter;

        private static readonly HashSet<CachedConfigEntry> editingEntries = new HashSet<CachedConfigEntry>();
        private static Button saveButton;

        public static void OnEntryEdit(CachedConfigEntry entry)
        {
            if (!editingEntries.Contains(entry))
                editingEntries.Add(entry);

            if (!saveButton.interactable)
                saveButton.interactable = true;
        }

        public static void OnEntryUndo(CachedConfigEntry entry)
        {
            if (editingEntries.Contains(entry))
                editingEntries.Remove(entry);

            if (!editingEntries.Any())
                saveButton.interactable = false;
        }

        public static void SavePreferences()
        {
            MelonPreferences.Save();

            foreach (var entry in editingEntries)
                entry.OnSaveOrUndo();

            editingEntries.Clear();
            saveButton.interactable = false;
        }

        // called by UIManager.Init
        internal static void Create()
        {
            if (Instance != null)
            {
                PrefManagerMod.LogWarning("An instance of PreferencesEditor already exists, cannot create another!");
                return;
            }

            Instance = new PreferencesEditor();
            Instance.ConstructMenu();

            MelonCoroutines.Start(SetupCategories());
        }

        private static readonly Dictionary<string, CategoryInfo> _categoryInfos = new Dictionary<string, CategoryInfo>();
        private static CategoryInfo _currentCategory;

        private static Color _normalDisabledColor = new Color(0.17f, 0.25f, 0.17f);
        private static Color _normalActiveColor = new Color(0, 0.45f, 0.05f);

        public static void SetHiddenConfigVisibility(bool show)
        {
            if (ShowHiddenConfigs == show)
                return;

            ShowHiddenConfigs = show;

            foreach (var entry in _categoryInfos)
            {
                var info = entry.Value;

                if (info.isCompletelyHidden)
                    info.listButton.gameObject.SetActive(ShowHiddenConfigs);
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
            btn.colors = RuntimeProvider.Instance.SetColorBlock(btn.colors, _normalActiveColor);

            RefreshFilter();
        }

        internal static void UnsetActiveCategory()
        {
            if (_currentCategory == null)
                return;

            var colors = _currentCategory.listButton.colors;
            colors = RuntimeProvider.Instance.SetColorBlock(colors, _normalDisabledColor);
            _currentCategory.listButton.colors = colors;
            _currentCategory.contentObj.SetActive(false);

            _currentCategory = null;
        }

        #region UI Construction

        private void ConstructMenu()
        {
            MainPanel = UIFactory.CreatePanel("MainMenu", out GameObject mainContent);

            var rect = MainPanel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.02f);
            rect.anchorMax = new Vector2(0.8f, 0.98f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 1000);

            ConstructTitleBar(mainContent);

            ConstructSaveButton(mainContent);

            ConstructToolbar(mainContent);

            ConstructEditorViewport(mainContent);
        }

        private void ConstructTitleBar(GameObject content)
        {
            // Core title bar holder

            GameObject titleBar = UIFactory.CreateHorizontalGroup(content, "MainTitleBar", true, true, true, true, 0, new Vector4(3, 3, 15, 3));
            UIFactory.SetLayoutElement(titleBar, minWidth: 25, minHeight: 30, flexibleHeight: 0);

            // Main title label

            var text = UIFactory.CreateLabel(titleBar, "TitleLabel", $"<b><color=#4cd43d>MelonPreferencesManager</color></b> " +
                $"<i><color=#ff3030>v{PrefManagerMod.VERSION}</color></i>", 
                TextAnchor.MiddleLeft, default, true, 15);
            UIFactory.SetLayoutElement(text.gameObject, flexibleWidth: 5000);

            // Hide button

            ColorBlock colorBlock = new ColorBlock();
            colorBlock = RuntimeProvider.Instance.SetColorBlock(colorBlock, new Color(1, 0.2f, 0.2f),
                new Color(1, 0.6f, 0.6f), new Color(0.3f, 0.1f, 0.1f));

            var hideButton = UIFactory.CreateButton(titleBar,
                "HideButton",
                $"X",
                () => { UIManager.ShowMenu = false; },
                colorBlock);
            UIFactory.SetLayoutElement(hideButton.gameObject, minWidth: 25, flexibleWidth: 0);

            Text hideText = hideButton.GetComponentInChildren<Text>();
            hideText.color = Color.white;
            hideText.resizeTextForBestFit = true;
            hideText.resizeTextMinSize = 8;
            hideText.resizeTextMaxSize = 14;
        }

        private void ConstructSaveButton(GameObject mainContent)
        {
            saveButton = UIFactory.CreateButton(mainContent, "SaveButton", "Save Preferences", SavePreferences);
            UIFactory.SetLayoutElement(saveButton.gameObject, minHeight: 35, flexibleWidth: 9999);
            var colors = new ColorBlock() { colorMultiplier = 1 };
            saveButton.colors = RuntimeProvider.Instance.SetColorBlock(colors, new Color(0.1f, 0.3f, 0.1f),
                new Color(0.2f, 0.5f, 0.2f), new Color(0.1f, 0.2f, 0.1f), new Color(0.2f, 0.2f, 0.2f));

            saveButton.interactable = false;
        }

        private void ConstructToolbar(GameObject parent)
        {
            var toolbarGroup = UIFactory.CreateHorizontalGroup(parent, "Toolbar", false, true, true, true, 4, new Vector4(3, 3, 3, 3),
                new Color(0.1f, 0.1f, 0.1f));

            var toggleObj = UIFactory.CreateToggle(toolbarGroup, "HiddenConfigsToggle", out Toggle toggle, out Text toggleText);
            toggle.isOn = false;
            toggle.onValueChanged.AddListener((bool val) =>
            {
                SetHiddenConfigVisibility(val);
            });
            toggleText.text = "Show Advanced Settings";
            UIFactory.SetLayoutElement(toggleObj, minWidth: 280, minHeight: 25, flexibleHeight: 0, flexibleWidth: 0);

            var inputField = UIFactory.CreateInputField(toolbarGroup, "FilterInput", "Search...", 14);
            UIFactory.SetLayoutElement(inputField, flexibleWidth: 9999);
            var input = inputField.GetComponent<InputField>();
            input.onValueChanged.AddListener(FilterConfigs);
        }

        private void ConstructEditorViewport(GameObject mainContent)
        {
            var horiGroup = UIFactory.CreateHorizontalGroup(mainContent, "Main", true, true, true, true, 2, default, new Color(0.08f, 0.08f, 0.08f));

            var ctgList = UIFactory.CreateScrollView(horiGroup, "CategoryList", out GameObject ctgViewport, out _, new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(ctgList, minWidth: 300, flexibleWidth: 0);
            CategoryListViewport = ctgViewport;

            var editor = UIFactory.CreateScrollView(horiGroup, "ConfigEditor", out GameObject editorViewport, out _, new Color(0.05f, 0.05f, 0.05f));
            UIFactory.SetLayoutElement(editor, flexibleWidth: 9999);
            ConfigEditorViewport = editorViewport;
        }

        // wait for end of chainloader setup. mods that set up preferences after this aren't compatible atm.
        private static IEnumerator SetupCategories()
        {
            yield return null;

            ColorBlock btnColors = new ColorBlock();
            btnColors = RuntimeProvider.Instance.SetColorBlock(btnColors, _normalDisabledColor, new Color(0.7f, 1f, 0.7f),
                new Color(0, 0.25f, 0));

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

                    var btn = UIFactory.CreateButton(CategoryListViewport,
                        "BUTTON_" + ctg.Identifier,
                        ctg.DisplayName,
                        () => { SetActiveCategory(ctg.Identifier); },
                        btnColors);
                    UIFactory.SetLayoutElement(btn.gameObject, flexibleWidth: 9999, minHeight: 30, flexibleHeight: 0);

                    info.listButton = btn;

                    // hide buttons for completely-hidden categories.
                    if (!ctg.Entries.Any(it => !it.IsHidden))
                    {
                        btn.gameObject.SetActive(false);
                        info.isCompletelyHidden = true;
                    }

                    // Editor content

                    var content = UIFactory.CreateVerticalGroup(ConfigEditorViewport, "CATEGORY_" + ctg.Identifier, true, false, true, true, 4,
                        default, new Color(0.05f, 0.05f, 0.05f));

                    // Actual config entry editors
                    foreach (var pref in ctg.Entries)
                    {
                        var cache = new CachedConfigEntry(pref, content);
                        cache.Enable();

                        var obj = cache.m_UIroot;

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

        #endregion
    }
}
