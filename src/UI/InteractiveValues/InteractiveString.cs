using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using MelonPrefManager.UI;
using MelonPrefManager.UI.Utility;

namespace MelonPrefManager.UI.InteractiveValues
{
    public class InteractiveString : InteractiveValue
    {
        public InteractiveString(object value, Type valueType) : base(value, valueType) { }

        public override bool HasSubContent => false;
        public override bool SubContentWanted => false;

        public override bool SupportsType(Type type)
            => type == typeof(string);

        public override void OnValueUpdated()
        {
            base.OnValueUpdated();
        }

        public override void RefreshUIForValue()
        {
            GetDefaultLabel(false);

            m_baseLabel.text = m_richValueType;

            if (!m_hiddenObj.gameObject.activeSelf)
                m_hiddenObj.gameObject.SetActive(true);

            if (!string.IsNullOrEmpty((string)Value))
            {
                var toString = (string)Value;
                if (toString.Length > 15000)
                    toString = toString.Substring(0, 15000);

                m_valueInput.text = toString;
                m_placeholderText.text = toString;
            }
            else
            {
                string s = Value == null 
                            ? "null" 
                            : "empty";

                m_valueInput.text = "";
                m_placeholderText.text = s;
            }

            m_labelLayout.minWidth = 50;
            m_labelLayout.flexibleWidth = 0;
        }

        internal void SetValueFromInput()
        {
            Value = m_valueInput.text;
            Owner.SetValue();
            //RefreshUIForValue();
        }

        // for the default label
        internal LayoutElement m_labelLayout;

        // for input
        internal InputField m_valueInput;
        internal GameObject m_hiddenObj;
        internal Text m_placeholderText;

        public override void ConstructUI(GameObject parent, GameObject subGroup)
        {
            base.ConstructUI(parent, subGroup);

            GetDefaultLabel(false);
            m_richValueType = SignatureHighlighter.ParseFullSyntax(FallbackType, false);

            m_labelLayout = m_baseLabel.gameObject.GetComponent<LayoutElement>();

            m_hiddenObj = UIFactory.CreateLabel(m_mainContent, "HiddenLabel", "", TextAnchor.MiddleLeft).gameObject;
            m_hiddenObj.SetActive(false);
            var hiddenText = m_hiddenObj.GetComponent<Text>();
            hiddenText.color = Color.clear;
            hiddenText.fontSize = 14;
            hiddenText.raycastTarget = false;
            hiddenText.supportRichText = false;
            var hiddenFitter = m_hiddenObj.AddComponent<ContentSizeFitter>();
            hiddenFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIFactory.SetLayoutElement(m_hiddenObj, minHeight: 25, flexibleHeight: 500, minWidth: 250, flexibleWidth: 9000);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(m_hiddenObj, true, true, true, true);

            var inputObj = UIFactory.CreateInputField(m_hiddenObj, "StringInputField", "...", 14, 3);
            UIFactory.SetLayoutElement(inputObj, minWidth: 120, minHeight: 25, flexibleWidth: 5000, flexibleHeight: 5000);

            m_valueInput = inputObj.GetComponent<InputField>();
            m_valueInput.lineType = InputField.LineType.MultiLineNewline;

            m_placeholderText = m_valueInput.placeholder.GetComponent<Text>();

            m_placeholderText.supportRichText = false;
            m_valueInput.textComponent.supportRichText = false;

            m_valueInput.onValueChanged.AddListener((string val) =>
            {
                hiddenText.text = val ?? "";
                LayoutRebuilder.ForceRebuildLayoutImmediate(Owner.m_mainRect);
                SetValueFromInput();
            });

            //var apply = UIFactory.CreateButton(m_mainContent, "ApplyButton", "Apply", SetValueFromInput, new Color(0.2f, 0.2f, 0.2f));
            //UIFactory.SetLayoutElement(apply.gameObject, minWidth: 50, minHeight: 25, flexibleWidth: 0);

            RefreshUIForValue();
        }
    }
}
