using MelonLoader;
using MelonPrefManager.UI.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MelonLoader.Preferences;

namespace MelonPrefManager.UI.InteractiveValues
{
    public class InteractiveTomlObject : InteractiveValue
    {
        static InteractiveTomlObject()
        {
            var t_TomlMain = ReflectionUtility.GetTypeByName("Tomlet.TomletMain");
            var t_TomlValue = ReflectionUtility.GetTypeByName("Tomlet.Models.TomlValue");

            _toTomlValue = t_TomlMain.GetMethod("ValueFrom", new Type[] { typeof(Type), typeof(object) });
            _fromTomlValue = t_TomlMain.GetMethod("To", new Type[] { typeof(Type), t_TomlValue });

            _serializedValueProperty = t_TomlValue.GetProperty("SerializedValue");

            var t_TomlTable = ReflectionUtility.GetTypeByName("Tomlet.Models.TomlTable");
            _serializeTableMethod = t_TomlTable.GetMethod("SerializeNonInlineTable");

            var t_TomlArray = ReflectionUtility.GetTypeByName("Tomlet.Models.TomlArray");
            _serializeArrayMethod = t_TomlArray.GetMethod("SerializeTableArray");
        }

        private static readonly MethodInfo _toTomlValue;
        private static readonly MethodInfo _fromTomlValue;
        private static readonly PropertyInfo _serializedValueProperty;
        private static readonly MethodInfo _serializeTableMethod;
        private static readonly MethodInfo _serializeArrayMethod;

        public InteractiveTomlObject(object value, Type valueType) : base(value, valueType) { }

        // Default handler for any type without a specific handler.
        public override bool SupportsType(Type type) => true;

        public object TomlValue;
        
        internal InputField m_valueInput;
        internal GameObject m_hiddenObj;
        internal Text m_placeholderText;

        public override void OnValueUpdated()
        {
            base.OnValueUpdated();

            try
            {
                TomlValue = _toTomlValue.Invoke(null, new object[] { Value.GetActualType(), Value });

                string serialized;
                try 
                {
                    serialized = (string)_serializedValueProperty.GetValue(TomlValue, null); 
                    m_valueInput.text = serialized;
                    m_placeholderText.text = m_valueInput.text;
                } 
                catch 
                {
                    PrefManagerMod.Log("Trying to save TomlObject...");

                    var tomlType = TomlValue.GetType();
                    if (tomlType.Name == "TomlArray")
                        serialized = (string)_serializeArrayMethod.Invoke(TomlValue, new object[] { Owner.RefConfig.DisplayName });
                    else 
                        serialized = (string)_serializeTableMethod.Invoke(TomlValue, new object[] { null, false });

                    m_valueInput.text = serialized;
                    PrefManagerMod.Log("Done");
                }

                
            }
            catch (Exception ex)
            {
                PrefManagerMod.LogWarning($"Unable to edit config '{Owner.RefConfig.DisplayName}' due to an error with the Mapper!" +
                    $"\r\n{ex}");
            }
        }

        internal void SetValueFromInput()
        {
            try
            {

                Value = _fromTomlValue.Invoke(null, new object[] { Value.GetActualType(), TomlValue });

                Owner.SetValueFromIValue();

                m_valueInput.textComponent.color = Color.white;
            }
            catch
            {
                m_valueInput.textComponent.color = Color.red;
            }
        }

        public override void RefreshUIForValue()
        {
            if (!m_hiddenObj.gameObject.activeSelf)
                m_hiddenObj.gameObject.SetActive(true);
        }

        public override void ConstructUI(GameObject parent)
        {
            base.ConstructUI(parent);

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

            OnValueUpdated();

            m_valueInput.onValueChanged.AddListener((string val) =>
            {
                hiddenText.text = val ?? "";
                LayoutRebuilder.ForceRebuildLayoutImmediate(Owner.ContentRect);
                SetValueFromInput();
            });
        }

        //// Borrowed from MelonPreferences/API.cs
        //
        //private static ValueSyntax CreateValueSyntax(TomlObject obj)
        //{
        //    return obj.Kind switch
        //    {
        //        ObjectKind.Boolean => new BooleanValueSyntax(((TomlBoolean)obj).Value),
        //        ObjectKind.String => new StringValueSyntax(((TomlString)obj).Value),
        //        ObjectKind.Float => new FloatValueSyntax(((TomlFloat)obj).Value),
        //        ObjectKind.Integer => new IntegerValueSyntax(((TomlInteger)obj).Value),
        //        ObjectKind.Array => CreateArraySyntaxFromTomlArray((TomlArray)obj),
        //        _ => null
        //    };
        //}
        //
        //private static ArraySyntax CreateArraySyntaxFromTomlArray(TomlArray arr)
        //{
        //    var newSyntax = new ArraySyntax
        //    {
        //        OpenBracket = SyntaxFactory.Token(TokenKind.OpenBracket),
        //        CloseBracket = SyntaxFactory.Token(TokenKind.CloseBracket)
        //    };
        //    for (var i = 0; i < arr.Count; i++)
        //    {
        //        var item = new ArrayItemSyntax { Value = CreateValueSyntax(arr.GetTomlObject(i)) };
        //        if (i + 1 < arr.Count)
        //        {
        //            item.Comma = SyntaxFactory.Token(TokenKind.Comma);
        //            item.Comma.AddTrailingWhitespace();
        //        }
        //        newSyntax.Items.Add(item);
        //    }
        //
        //    return newSyntax;
        //}
    }
}
