using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using MelonPrefManager.UI;
using MelonLoader;
using MelonLoader.Preferences;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib;

namespace MelonPrefManager.UI.InteractiveValues
{
    public class InteractiveNumber : InteractiveValue
    {
        internal InputFieldRef valueInput;

        public MethodInfo ParseMethod => parseMethod ??= Value.GetType().GetMethod("Parse", new Type[] { typeof(string) });
        private MethodInfo parseMethod;
        private Slider slider;

        public InteractiveNumber(object value, Type valueType) : base(value, valueType) { }

        public override bool SupportsType(Type type)
            => (type.IsPrimitive && type != typeof(bool)) || type == typeof(decimal);

        public override void RefreshUIForValue()
        {
            valueInput.Text = Value.ToString();

            if (!valueInput.Component.gameObject.activeSelf)
                valueInput.Component.gameObject.SetActive(true);

            if (slider)
                slider.value = (float)Convert.ChangeType(Value, typeof(float));
        }

        internal void SetValueFromInput()
        {
            try
            {
                Value = ParseMethod.Invoke(null, new object[] { valueInput.Text });

                if (Owner.RefConfig.Validator != null && !Owner.RefConfig.Validator.IsValid(Value))
                {
                    valueInput.Component.textComponent.color = Color.red;
                    return;
                }

                Owner.SetValueFromIValue();
                RefreshUIForValue();

                valueInput.Component.textComponent.color = Color.white;
            }
            catch
            {
                valueInput.Component.textComponent.color = Color.red;
            }
        }

        public override void ConstructUI(GameObject parent)
        {
            base.ConstructUI(parent);

            valueInput = UIFactory.CreateInputField(mainContent, "InteractiveNumberInput", "...");
            UIFactory.SetLayoutElement(valueInput.Component.gameObject, minWidth: 120, minHeight: 25, flexibleWidth: 0);

            valueInput.Component.gameObject.SetActive(false);

            valueInput.OnValueChanged += (string val) =>
            {
                SetValueFromInput();
            };

            //var type = Value.GetType();
            //if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            //    m_valueInput.characterValidation = InputField.CharacterValidation.Decimal;
            //else
            //    m_valueInput.characterValidation = InputField.CharacterValidation.Integer;

            if (Owner.RefConfig.Validator is IValueRange range)
            {
                Owner.mainLabel.text += $" <color=grey><i>[{range.MinValue.ToString()} - {range.MaxValue.ToString()}]</i></color>";

                var sliderObj = UIFactory.CreateSlider(mainContent, "ValueSlider", out slider);
                UIFactory.SetLayoutElement(sliderObj, minWidth: 250, minHeight: 25);

                slider.minValue = (float)Convert.ChangeType(range.MinValue, typeof(float));
                slider.maxValue = (float)Convert.ChangeType(range.MaxValue, typeof(float));

                slider.value = (float)Convert.ChangeType(Value, typeof(float));

                slider.onValueChanged.AddListener((float val) =>
                {
                    Value = Convert.ChangeType(val, FallbackType);
                    Owner.SetValueFromIValue();
                    valueInput.Text = Value.ToString();
                });

                //m_valueInput.onValueChanged.AddListener((string val) => 
                //{
                //    slider.value = (float)Convert.ChangeType(Value, typeof(float));
                //});
            }
        }
    }
}
