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
        internal InputField m_valueInput;

        public MethodInfo ParseMethod => m_parseMethod ??= Value.GetType().GetMethod("Parse", new Type[] { typeof(string) });
        private MethodInfo m_parseMethod;

        public InteractiveNumber(object value, Type valueType) : base(value, valueType) { }

        public override bool SupportsType(Type type)
            => (type.IsPrimitive && type != typeof(bool)) || type == typeof(decimal);

        public override void RefreshUIForValue()
        {
            m_valueInput.text = Value.ToString();

            if (!m_valueInput.gameObject.activeSelf)
                m_valueInput.gameObject.SetActive(true);
        }

        internal void SetValueFromInput()
        {
            try
            {
                Value = ParseMethod.Invoke(null, new object[] { m_valueInput.text });
                Owner.SetValueFromIValue();
                RefreshUIForValue();

                m_valueInput.textComponent.color = Color.white;
            }
            catch 
            {
                m_valueInput.textComponent.color = Color.red;
            }
        }

        public override void ConstructUI(GameObject parent)
        {
            base.ConstructUI(parent);

            var inputObj = UIFactory.CreateInputField(m_mainContent, "InteractiveNumberInput", "...");
            UIFactory.SetLayoutElement(inputObj, minWidth: 120, minHeight: 25, flexibleWidth: 0);

            m_valueInput = inputObj.GetComponent<InputField>();
            m_valueInput.gameObject.SetActive(false);

            m_valueInput.onValueChanged.AddListener((string val) =>
            {
                SetValueFromInput();
            });

            var type = Value.GetType();
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                m_valueInput.characterValidation = InputField.CharacterValidation.Decimal;
            else
                m_valueInput.characterValidation = InputField.CharacterValidation.Integer;

            if (Owner.RefConfig.Validator is IValueRange range)
            {
                var sliderObj = UIFactory.CreateSlider(m_mainContent, "ValueSlider", out Slider slider);
                UIFactory.SetLayoutElement(sliderObj, minWidth: 250, minHeight: 25);

                slider.minValue = (float)Convert.ChangeType(range.MinValue, typeof(float));
                slider.maxValue = (float)Convert.ChangeType(range.MaxValue, typeof(float));

                slider.value = (float)Convert.ChangeType(Value, typeof(float));

                slider.onValueChanged.AddListener((float val) =>
                {
                    float f = (float)Convert.ChangeType(Value, typeof(float));
                    if (f == val)
                        return;

                    Value = Convert.ChangeType(val, FallbackType);
                    Owner.SetValueFromIValue();
                    m_valueInput.text = f.ToString();
                });

                m_valueInput.onValueChanged.AddListener((string val) => 
                {
                    slider.value = (float)Convert.ChangeType(Value, typeof(float));
                });
            }
        }
    }
}
