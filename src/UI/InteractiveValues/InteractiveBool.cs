using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MelonPrefManager.UI;

namespace MelonPrefManager.UI.InteractiveValues
{
    public class InteractiveBool : InteractiveValue
    {
        public InteractiveBool(object value, Type valueType) : base(value, valueType) { }

        public override bool HasSubContent => false;
        public override bool SubContentWanted => false;

        internal Toggle m_toggle;
        //internal Button m_applyBtn;

        public override void OnValueUpdated()
        {
            base.OnValueUpdated();
        }

        public override void RefreshUIForValue()
        {
            GetDefaultLabel();

            m_baseLabel.text = DefaultLabel;

            var val = (bool)Value;

            if (!m_toggle.gameObject.activeSelf)
                m_toggle.gameObject.SetActive(true);

            //if (!m_applyBtn.gameObject.activeSelf)
            //    m_applyBtn.gameObject.SetActive(true);

            if (val != m_toggle.isOn)
                m_toggle.isOn = val;

            var color = val
                ? "6bc981"  // on
                : "c96b6b"; // off

            m_baseLabel.text = $"<color=#{color}>{val}</color>";
        }

        internal void OnToggleValueChanged(bool val)
        {
            Value = val;
            RefreshUIForValue();
            Owner.SetValue();
        }

        public override void ConstructUI(GameObject parent, GameObject subGroup)
        {
            base.ConstructUI(parent, subGroup);

            var baseLayout = m_baseLabel.gameObject.GetComponent<LayoutElement>();
            baseLayout.flexibleWidth = 0;
            baseLayout.minWidth = 50;

            var toggleObj = UIFactory.CreateToggle(m_mainContent, "InteractiveBoolToggle", out m_toggle, out _, new Color(0.1f, 0.1f, 0.1f));
            UIFactory.SetLayoutElement(toggleObj, minWidth: 24);
            m_toggle.onValueChanged.AddListener(OnToggleValueChanged);

            m_baseLabel.transform.SetAsLastSibling();

            //m_applyBtn = UIFactory.CreateButton(m_mainContent,
            //    "ApplyButton",
            //    "Apply",
            //    () => { Owner.SetValue(); },
            //    new Color(0.2f, 0.2f, 0.2f));

            //UIFactory.SetLayoutElement(m_applyBtn.gameObject, minWidth: 50, minHeight: 25, flexibleWidth: 0);
        }
    }
}
