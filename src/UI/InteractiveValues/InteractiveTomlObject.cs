using MelonLoader;
using MelonLoader.Tomlyn;
using MelonLoader.Tomlyn.Model;
using MelonLoader.Tomlyn.Syntax;
using MelonPrefManager.UI.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MelonPrefManager.UI.InteractiveValues
{
    public class InteractiveTomlObject : InteractiveValue
    {
        public InteractiveTomlObject(object value, Type valueType) : base(value, valueType)
        {
        }

        public override bool SupportsType(Type type)
            => true;

        public override bool HasSubContent => false;
        public override bool SubContentWanted => false;

        public TomlObject RefTomlObject;
        public DocumentSyntax tomlDoc;
        private MethodInfo _toTomlMethod;
        private MethodInfo _fromTomlMethod;

        public override void OnValueUpdated()
        {
            base.OnValueUpdated();

            try
            {
                if (_toTomlMethod == null)
                {
                    var type = Value.GetActualType();
                    _toTomlMethod = typeof(TomlMapper).GetMethod("ToToml").MakeGenericMethod(type);
                }

                RefTomlObject = _toTomlMethod.Invoke(MelonPreferences.Mapper, new object[] { Value }) as TomlObject;

                var valueSyntax = CreateValueSyntax(RefTomlObject);

                m_valueInput.text = valueSyntax.ToString();
                m_placeholderText.text = m_valueInput.text;
            }
            catch
            {
                PrefManagerMod.LogWarning($"Unable to edit config '{Owner.RefConfig.DisplayName}' due to an error with the Mapper!");
            }
        }

        private static ValueSyntax CreateValueSyntax(TomlObject obj)
        {
            return obj.Kind switch
            {
                ObjectKind.Boolean => new BooleanValueSyntax(((TomlBoolean)obj).Value),
                ObjectKind.String => new StringValueSyntax(((TomlString)obj).Value),
                ObjectKind.Float => new FloatValueSyntax(((TomlFloat)obj).Value),
                ObjectKind.Integer => new IntegerValueSyntax(((TomlInteger)obj).Value),
                ObjectKind.Array => CreateArraySyntaxFromTomlArray((TomlArray)obj),
                _ => null
            };
        }

        private static ArraySyntax CreateArraySyntaxFromTomlArray(TomlArray arr)
        {
            var newSyntax = new ArraySyntax
            {
                OpenBracket = SyntaxFactory.Token(TokenKind.OpenBracket),
                CloseBracket = SyntaxFactory.Token(TokenKind.CloseBracket)
            };
            for (var i = 0; i < arr.Count; i++)
            {
                var item = new ArrayItemSyntax { Value = CreateValueSyntax(arr.GetTomlObject(i)) };
                if (i + 1 < arr.Count)
                {
                    item.Comma = SyntaxFactory.Token(TokenKind.Comma);
                    item.Comma.AddTrailingWhitespace();
                }
                newSyntax.Items.Add(item);
            }

            return newSyntax;
        }

        internal void SetValueFromInput()
        {
            try
            {
                string docTxt = $"[null]\r\nvalue = {m_valueInput.text}";

                var tomlDoc = Toml.Parse(docTxt).ToModel();

                foreach (KeyValuePair<string, object> keypair in tomlDoc)
                {
                    string category = keypair.Key;
                    if (string.IsNullOrEmpty(category))
                        continue;
                    TomlTable tbl = (TomlTable)keypair.Value;
                    if (tbl.Count <= 0)
                        continue;
                    foreach (KeyValuePair<string, object> tblkeypair in tbl)
                    {
                        var value = tblkeypair.Value as TomlObject;

                        if (_fromTomlMethod == null)
                        {
                            var type = Value.GetActualType();
                            _fromTomlMethod = typeof(TomlMapper).GetMethod("FromToml").MakeGenericMethod(type);
                        }

                        Value = _fromTomlMethod.Invoke(MelonPreferences.Mapper, new object[] { value });

                        break;
                    }
                }

                Owner.SetValueFromIValue();

                m_valueInput.textComponent.color = Color.white;
            }
            catch //(Exception ex)
            {
                m_valueInput.textComponent.color = Color.red;
                //PrefManagerMod.LogWarning($"Unable to parse input! {ex}");
            }
        }

        public override void RefreshUIForValue()
        {
            if (!m_hiddenObj.gameObject.activeSelf)
                m_hiddenObj.gameObject.SetActive(true);
        }

        // for the default label
        //internal LayoutElement m_labelLayout;

        // for input
        internal InputField m_valueInput;
        internal GameObject m_hiddenObj;
        internal Text m_placeholderText;

        public override void ConstructUI(GameObject parent)//, GameObject subGroup)
        {
            base.ConstructUI(parent);//, subGroup);

            //GetDefaultLabel(false);
            //m_richValueType = SignatureHighlighter.ParseFullSyntax(FallbackType, false);

            //m_labelLayout = m_baseLabel.gameObject.GetComponent<LayoutElement>();
            //m_labelLayout.minWidth = 0;
            //m_labelLayout.flexibleWidth = 0;

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

            //var apply = UIFactory.CreateButton(m_mainContent, "ApplyButton", "Apply", SetValueFromInput, new Color(0.2f, 0.2f, 0.2f));
            //UIFactory.SetLayoutElement(apply.gameObject, minWidth: 50, minHeight: 25, flexibleWidth: 0);

            OnValueUpdated();

            m_valueInput.onValueChanged.AddListener((string val) =>
            {
                hiddenText.text = val ?? "";
                LayoutRebuilder.ForceRebuildLayoutImmediate(Owner.ContentRect);
                SetValueFromInput();
            });
        }
    }
}
