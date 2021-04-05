using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using MelonPrefManager.UI;
using MelonPrefManager.UI.Utility;
using MelonLoader;
using MelonLoader.Preferences;

namespace MelonPrefManager.UI.InteractiveValues
{
    public class InteractiveNumber : InteractiveValue
    {
        public InteractiveNumber(object value, Type valueType) : base(value, valueType) { }

        public override bool HasSubContent => false;
        public override bool SubContentWanted => false;

        public override bool SupportsType(Type type)
            => (type.IsPrimitive && type != typeof(bool)) || type == typeof(decimal);

        public override void OnValueUpdated()
        {
            base.OnValueUpdated();
        }

        public override void RefreshUIForValue()
        {
            //m_baseLabel.text = SignatureHighlighter.ParseFullSyntax(FallbackType, false);
            m_valueInput.text = Value.ToString();

            var type = Value.GetType();
            if (type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal))
            {
                m_valueInput.characterValidation = InputField.CharacterValidation.Decimal;
            }
            else
            {
                m_valueInput.characterValidation = InputField.CharacterValidation.Integer;
            }

            //if (!m_applyBtn.gameObject.activeSelf)
            //    m_applyBtn.gameObject.SetActive(true);

            if (!m_valueInput.gameObject.activeSelf)
                m_valueInput.gameObject.SetActive(true);
        }

        public MethodInfo ParseMethod => m_parseMethod ?? (m_parseMethod = Value.GetType().GetMethod("Parse", new Type[] { typeof(string) }));
        private MethodInfo m_parseMethod;

        internal void SetValueFromInput()
        {
            try
            {
                Value = ParseMethod.Invoke(null, new object[] { m_valueInput.text });
                Owner.SetValue();
                RefreshUIForValue();
            }
            catch //(Exception e)
            {
                //PrefManagerMod.LogWarning("Could not parse input! " + ReflectionUtility.ReflectionExToString(e, true));
            }
        }

        internal InputField m_valueInput;
        //internal Button m_applyBtn;

        public override void ConstructUI(GameObject parent)//, GameObject subGroup)
        {
            base.ConstructUI(parent);//, subGroup);

            //var labelLayout = m_baseLabel.gameObject.GetComponent<LayoutElement>();
            //labelLayout.minWidth = 50;
            //labelLayout.flexibleWidth = 0;

            var inputObj = UIFactory.CreateInputField(m_mainContent, "InteractiveNumberInput", "...");
            UIFactory.SetLayoutElement(inputObj, minWidth: 120, minHeight: 25, flexibleWidth: 0);

            m_valueInput = inputObj.GetComponent<InputField>();
            m_valueInput.gameObject.SetActive(false);

            m_valueInput.onValueChanged.AddListener((string val) =>
            {
                SetValueFromInput();
            });

            if (Owner.RefConfig.Validator is IValueRange range)
            {
                var sliderObj = UIFactory.CreateSlider(m_mainContent, "ValueSlider", out Slider slider);
                UIFactory.SetLayoutElement(sliderObj, minWidth: 250, minHeight: 25);
                slider.minValue = (float)range.MinValue;
                slider.maxValue = (float)range.MaxValue;

                slider.value = (float)Value;

                slider.onValueChanged.AddListener((float val) =>
                {
                    if ((float)Value == val)
                        return;

                    Value = val;
                    Owner.SetValue();
                });

                m_valueInput.onValueChanged.AddListener((string val) => 
                {
                    slider.value = (float)Value;
                });
            }

            //m_applyBtn = UIFactory.CreateButton(m_mainContent, "ApplyButton", "Apply", SetValueFromInput, new Color(0.2f, 0.2f, 0.2f));
            //UIFactory.SetLayoutElement(m_applyBtn.gameObject, minWidth: 50, minHeight: 25, flexibleWidth: 0);
        }
    }
}
