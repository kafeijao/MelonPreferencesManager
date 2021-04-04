using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MelonLoader;
using MelonPrefManager.UI.InteractiveValues;

namespace MelonPrefManager.UI
{
    public class CachedConfigEntry
    {
        public MelonPreferences_Entry RefConfig { get; }
        public InteractiveValue IValue;

        public Type FallbackType => RefConfig.GetReflectedType();

        public CachedConfigEntry(MelonPreferences_Entry config, GameObject parent)
        {
            RefConfig = config;

            m_parentContent = parent;

            config.OnValueChangedUntyped += () => { UpdateValue(); };

            CreateIValue(config.BoxedValue, FallbackType);
        }

        public void CreateIValue(object value, Type fallbackType)
        {
            IValue = InteractiveValue.Create(value, fallbackType);
            IValue.Owner = this;
            IValue.m_mainContentParent = m_mainGroup;
            IValue.m_subContentParent = this.m_subContent;
        }

        public void SetValue()
        {
            if (RefConfig.Validator != null)
                IValue.Value = RefConfig.Validator.EnsureValid(IValue.Value);

            RefConfig.BoxedValue = IValue.Value;
        }

        public void Enable()
        {
            if (!m_constructedUI)
            {
                ConstructUI();
                UpdateValue();
            }

            m_mainContent.SetActive(true);
            m_mainContent.transform.SetAsLastSibling();
        }

        public void Disable()
        {
            if (m_mainContent)
                m_mainContent.SetActive(false);
        }

        public void Destroy()
        {
            if (this.m_mainContent)
                GameObject.Destroy(this.m_mainContent);
        }

        public void UpdateValue()
        {
            var value = RefConfig.BoxedValue;
            IValue.Value = value;

            IValue.OnValueUpdated();
            IValue.RefreshElementsAfterUpdate();
        }

        #region UI CONSTRUCTION

        internal bool m_constructedUI;
        internal GameObject m_parentContent;
        internal RectTransform m_mainRect;
        internal GameObject m_mainContent;
        internal GameObject m_subContent;

        internal GameObject m_mainGroup;

        internal void ConstructUI()
        {
            m_constructedUI = true;

            m_mainContent = UIFactory.CreateVerticalGroup(m_parentContent, "CacheObjectBase.MainContent", true, false, true, true, 0, 
                default, new Color(1,1,1,0));
            m_mainRect = m_mainContent.GetComponent<RectTransform>();
            m_mainRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25);
            UIFactory.SetLayoutElement(m_mainContent, minHeight: 25, flexibleHeight: 9999, minWidth: 200, flexibleWidth: 5000);

            // subcontent

            m_subContent = UIFactory.CreateVerticalGroup(m_mainContent, "CacheObjectBase.SubContent", true, false, true, true, 0, default,
                new Color(1,1,1,0));
            UIFactory.SetLayoutElement(m_subContent, minHeight: 30, flexibleHeight: 9999, minWidth: 125, flexibleWidth: 9000);

            m_subContent.SetActive(false);

            IValue.m_subContentParent = m_subContent;

            m_mainGroup = UIFactory.CreateVerticalGroup(m_mainContent, "ConfigHolder", true, false, true, true, 5, new Vector4(2, 2, 5, 5),
                new Color(0.12f, 0.12f, 0.12f));

            var horiGroup = UIFactory.CreateHorizontalGroup(m_mainGroup, "ConfigEntryHolder", false, false, true, true,
                0, default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(horiGroup, minHeight: 30, flexibleHeight: 0);

            // config entry label

            var configLabel = UIFactory.CreateLabel(horiGroup, "ConfigLabel", this.RefConfig.DisplayName, TextAnchor.MiddleLeft, 
                new Color(0.7f, 1, 0.7f));
            var leftRect = configLabel.GetComponent<RectTransform>();
            leftRect.anchorMin = Vector2.zero;
            leftRect.anchorMax = Vector2.one;
            leftRect.offsetMin = Vector2.zero;
            leftRect.offsetMax = Vector2.zero;
            leftRect.sizeDelta = Vector2.zero;
            UIFactory.SetLayoutElement(configLabel.gameObject, minWidth: 250, minHeight: 22, flexibleWidth: 9999, flexibleHeight: 0);

            // Default button

            var defaultButton = UIFactory.CreateButton(horiGroup,
                "RevertDefaultButton",
                "Default",
                () => { RefConfig.ResetToDefault(); },
                new Color(0.3f, 0.3f, 0.3f));
            UIFactory.SetLayoutElement(defaultButton.gameObject, minWidth: 80, minHeight: 22, flexibleWidth: 0);

            // Description label

            if (RefConfig.Description != null)
            {
                var desc = UIFactory.CreateLabel(m_mainGroup, "Description", $"<i>{RefConfig.Description}</i>", TextAnchor.MiddleLeft, Color.grey);
                UIFactory.SetLayoutElement(desc.gameObject, minWidth: 250, minHeight: 18, flexibleWidth: 9999, flexibleHeight: 0);
            }

            // IValue

            if (IValue != null)
            {
                IValue.m_mainContentParent = m_mainGroup;
                IValue.m_subContentParent = this.m_subContent;
            }

            m_subContent.transform.SetParent(m_mainGroup.transform, false);
        }
       
        #endregion
    }
}
