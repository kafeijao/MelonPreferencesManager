using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MelonLoader;
using MelonPrefManager.UI.InteractiveValues;
using MelonPrefManager.UI.Utility;
using System.Reflection;

namespace MelonPrefManager.UI
{
    public class CachedConfigEntry
    {
        public MelonPreferences_Entry RefConfig { get; }
        public InteractiveValue IValue;

        // UI
        public bool UIConstructed;
        public GameObject parentContent;
        public GameObject ContentGroup;
        public RectTransform ContentRect;
        public GameObject SubContentGroup;

        internal GameObject m_UIroot;
        internal GameObject m_undoButton;

        public Type FallbackType => RefConfig.GetReflectedType();

        public CachedConfigEntry(MelonPreferences_Entry config, GameObject parent)
        {
            RefConfig = config;
            parentContent = parent;

            EnsureConfigValid();

            config.OnValueChangedUntyped += () => { UpdateValue(); };

            CreateIValue(config.BoxedValue, FallbackType);
        }

        public void CreateIValue(object value, Type fallbackType)
        {
            IValue = InteractiveValue.Create(value, fallbackType);
            IValue.Owner = this;
            IValue.m_mainContentParent = ContentGroup;
            IValue.m_subContentParent = this.SubContentGroup;
        }

        private void EnsureConfigValid()
        {
            // MelonLoader does not support null config values. Ensure valid.
            if (RefConfig.BoxedValue == null)
            {
                if (FallbackType == typeof(string))
                    RefConfig.BoxedValue = "";
                else if (FallbackType.IsArray)
                    RefConfig.BoxedValue = Array.CreateInstance(FallbackType.GetElementType(), 0);
                else
                    RefConfig.BoxedValue = Activator.CreateInstance(FallbackType);

                RefConfig.BoxedEditedValue = RefConfig.BoxedValue;
            }
        }

        public void UpdateValue()
        {
            EnsureConfigValid();
            IValue.Value = RefConfig.BoxedEditedValue;

            IValue.OnValueUpdated();
            IValue.RefreshSubContentState();
        }

        public void SetValueFromIValue()
        {
            if (RefConfig.Validator != null)
                IValue.Value = RefConfig.Validator.EnsureValid(IValue.Value);

            var edited = RefConfig.BoxedEditedValue;
            if ((edited == null && IValue.Value == null) || (edited != null && edited.Equals(IValue.Value)))
                return;

            RefConfig.BoxedEditedValue = IValue.Value;
            PreferencesEditor.OnEntryEdit(this);
            m_undoButton.SetActive(true);
        }

        public void UndoEdits()
        {
            RefConfig.BoxedEditedValue = RefConfig.BoxedValue;
            IValue.Value = RefConfig.BoxedValue;
            IValue.OnValueUpdated();

            OnSaveOrUndo();
            PreferencesEditor.OnEntryUndo(this);
        }

        public void RevertToDefault()
        {
            RefConfig.ResetToDefault();
            RefConfig.BoxedEditedValue = RefConfig.BoxedValue;
            UpdateValue();
            OnSaveOrUndo();
        }

        internal void OnSaveOrUndo()
        {
            m_undoButton.SetActive(false);
        }

        public void Enable()
        {
            if (!UIConstructed)
            {
                ConstructUI();
                UpdateValue();
            }

            m_UIroot.SetActive(true);
            m_UIroot.transform.SetAsLastSibling();
        }

        public void Disable()
        {
            if (m_UIroot)
                m_UIroot.SetActive(false);
        }

        public void Destroy()
        {
            if (this.m_UIroot)
                GameObject.Destroy(this.m_UIroot);
        }

        internal void ConstructUI()
        {
            UIConstructed = true;

            m_UIroot = UIFactory.CreateVerticalGroup(parentContent, "CacheObjectBase.MainContent", true, false, true, true, 0, 
                default, new Color(1,1,1,0));
            ContentRect = m_UIroot.GetComponent<RectTransform>();
            ContentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25);
            UIFactory.SetLayoutElement(m_UIroot, minHeight: 25, flexibleHeight: 9999, minWidth: 200, flexibleWidth: 5000);

            ContentGroup = UIFactory.CreateVerticalGroup(m_UIroot, "ConfigHolder", true, false, true, true, 5, new Vector4(2, 2, 5, 5),
                new Color(0.12f, 0.12f, 0.12f));

            var horiGroup = UIFactory.CreateHorizontalGroup(ContentGroup, "ConfigEntryHolder", false, false, true, true,
                5, default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(horiGroup, minHeight: 30, flexibleHeight: 0);

            // config entry label

            var configLabel = UIFactory.CreateLabel(horiGroup, "ConfigLabel", this.RefConfig.DisplayName, TextAnchor.MiddleLeft, 
                new Color(0.7f, 1, 0.7f));
            configLabel.text += $" <i>({SignatureHighlighter.ParseFullSyntax(RefConfig.GetReflectedType(), false)})</i>";
            UIFactory.SetLayoutElement(configLabel.gameObject, minWidth: 200, minHeight: 22, flexibleWidth: 9999, flexibleHeight: 0);

            // Undo button

            var undoButton = UIFactory.CreateButton(horiGroup, "UndoButton", "Undo", UndoEdits, new Color(0.3f, 0.3f, 0.3f));
            m_undoButton = undoButton.gameObject;
            m_undoButton.SetActive(false);
            UIFactory.SetLayoutElement(m_undoButton, minWidth: 80, minHeight: 22, flexibleWidth: 0);

            // Default button

            var defaultButton = UIFactory.CreateButton(horiGroup, "DefaultButton", "Default", RevertToDefault, new Color(0.3f, 0.3f, 0.3f));
            UIFactory.SetLayoutElement(defaultButton.gameObject, minWidth: 80, minHeight: 22, flexibleWidth: 0);

            // Description label

            if (RefConfig.Description != null)
            {
                var desc = UIFactory.CreateLabel(ContentGroup, "Description", $"<i>{RefConfig.Description}</i>", TextAnchor.MiddleLeft, Color.grey);
                UIFactory.SetLayoutElement(desc.gameObject, minWidth: 250, minHeight: 18, flexibleWidth: 9999, flexibleHeight: 0);
            }

            // subcontent

            SubContentGroup = UIFactory.CreateVerticalGroup(ContentGroup, "CacheObjectBase.SubContent", true, false, true, true, 0, default,
                new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(SubContentGroup, minHeight: 30, flexibleHeight: 9999, minWidth: 125, flexibleWidth: 9000);

            SubContentGroup.SetActive(false);

            // setup IValue references

            if (IValue != null)
            {
                IValue.m_mainContentParent = ContentGroup;
                IValue.m_subContentParent = this.SubContentGroup;
            }
        }
    }
}
