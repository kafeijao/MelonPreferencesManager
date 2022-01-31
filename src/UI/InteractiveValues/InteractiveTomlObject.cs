using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MelonLoader.Preferences;
using UniverseLib;
using UniverseLib.UI;
using UniverseLib.UI.Models;

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
        
        internal InputFieldRef valueInput;
        internal GameObject hiddenObj;
        internal Text placeholderText;

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
                    valueInput.Text = serialized;
                    placeholderText.text = valueInput.Text;
                } 
                catch 
                {
                    PrefManagerMod.Log("Trying to save TomlObject...");

                    var tomlType = TomlValue.GetType();
                    if (tomlType.Name == "TomlArray")
                        serialized = (string)_serializeArrayMethod.Invoke(TomlValue, new object[] { Owner.RefConfig.DisplayName });
                    else 
                        serialized = (string)_serializeTableMethod.Invoke(TomlValue, new object[] { null, false });

                    valueInput.Text = serialized;
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

                valueInput.Component.textComponent.color = Color.white;
            }
            catch
            {
                valueInput.Component.textComponent.color = Color.red;
            }
        }

        public override void RefreshUIForValue()
        {
            if (!hiddenObj.gameObject.activeSelf)
                hiddenObj.gameObject.SetActive(true);
        }

        public override void ConstructUI(GameObject parent)
        {
            base.ConstructUI(parent);

            hiddenObj = UIFactory.CreateLabel(mainContent, "HiddenLabel", "", TextAnchor.MiddleLeft).gameObject;
            hiddenObj.SetActive(false);
            var hiddenText = hiddenObj.GetComponent<Text>();
            hiddenText.color = Color.clear;
            hiddenText.fontSize = 14;
            hiddenText.raycastTarget = false;
            hiddenText.supportRichText = false;
            var hiddenFitter = hiddenObj.AddComponent<ContentSizeFitter>();
            hiddenFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            UIFactory.SetLayoutElement(hiddenObj, minHeight: 25, flexibleHeight: 500, minWidth: 250, flexibleWidth: 9000);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(hiddenObj, true, true, true, true);

            valueInput = UIFactory.CreateInputField(hiddenObj, "StringInputField", "...");
            UIFactory.SetLayoutElement(valueInput.Component.gameObject, minWidth: 120, minHeight: 25, flexibleWidth: 5000, flexibleHeight: 5000);

            valueInput.Component.lineType = InputField.LineType.MultiLineNewline;

            placeholderText = valueInput.Component.placeholder.GetComponent<Text>();

            placeholderText.supportRichText = false;
            valueInput.Component.textComponent.supportRichText = false;

            OnValueUpdated();

            valueInput.OnValueChanged += (string val) =>
            {
                hiddenText.text = val ?? "";
                LayoutRebuilder.ForceRebuildLayoutImmediate(Owner.ContentRect);
                SetValueFromInput();
            };
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
