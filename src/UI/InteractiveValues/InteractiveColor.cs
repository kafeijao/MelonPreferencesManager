using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MelonPrefManager.UI.InteractiveValues
{
    public class InteractiveColor : InteractiveValue
    {
        //~~~~~~~~~ Instance ~~~~~~~~~~

        public InteractiveColor(object value, Type valueType) : base(value, valueType) { }

        //public override bool HasSubContent => true;
        //public override bool SubContentWanted => true;

        public override bool SupportsType(Type type)
            => type == typeof(Color);

        public override void RefreshUIForValue()
        {
            base.RefreshUIForValue();

            RefreshColorUI();
        }

        private void RefreshColorUI()
        {
            var color = (Color)this.Value;

            m_inputs[0].text = color.r.ToString();
            m_inputs[1].text = color.g.ToString();
            m_inputs[2].text = color.b.ToString();
            m_inputs[3].text = color.a.ToString();

            if (m_colorImage)
                m_colorImage.color = color;
        }

        internal override void OnToggleSubcontent(bool toggle)
        {
            base.OnToggleSubcontent(toggle);

            RefreshColorUI();
        }

        #region UI CONSTRUCTION

        private Image m_colorImage;

        private readonly InputField[] m_inputs = new InputField[4];
        private readonly Slider[] m_sliders = new Slider[4];

        public override void ConstructUI(GameObject parent)//, GameObject subGroup)
        {
            base.ConstructUI(parent);

            // todo: 
            // - move UI to here
            // - make smaller, 2 columns (1 for editors, 1 for color), editor column has 2 rows, color img on right side

            // hori group

            var baseHoriGroup = UIFactory.CreateHorizontalGroup(m_mainContent, "ColorEditor", false, false, true, true, 5,
                default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);

            // vert group (editors)

            var editorGroup = UIFactory.CreateVerticalGroup(baseHoriGroup, "EditorsGroup", false, false, true, true, 3, new Vector4(3, 3, 3, 3),
                new Color(1,1,1,0));

            var grid = UIFactory.CreateGridGroup(editorGroup, "Grid", new Vector2(290, 25), new Vector2(2, 2), new Color(1, 1, 1, 0));
            UIFactory.SetLayoutElement(grid, minWidth: 580, minHeight: 60, flexibleWidth: 0);

            for (int i = 0; i < 4; i++)
                AddEditorRow(i, grid);

            var imgHolder = UIFactory.CreateVerticalGroup(baseHoriGroup, "ImgHolder", true, true, true, true, 0, new Vector4(1, 1, 1, 1),
                new Color(0.08f, 0.08f, 0.08f));
            UIFactory.SetLayoutElement(imgHolder, minWidth: 50, minHeight: 60, flexibleWidth: 999, flexibleHeight: 0);

            var imgObj = UIFactory.CreateUIObject("ColorImageHelper", imgHolder, new Vector2(100, 25));
            m_colorImage = imgObj.AddComponent<Image>();
            m_colorImage.color = (Color)this.Value;

            RefreshUIForValue();
        }

        private static readonly string[] s_fieldNames = new[] { "R", "G", "B", "A" };

        internal void AddEditorRow(int index, GameObject groupObj)
        {
            var row = UIFactory.CreateHorizontalGroup(groupObj, "EditorRow_" + s_fieldNames[index], 
                false, true, true, true, 5, default, new Color(1, 1, 1, 0));

            var label = UIFactory.CreateLabel(row, "RowLabel", $"{s_fieldNames[index]}:", TextAnchor.MiddleRight, Color.cyan);
            UIFactory.SetLayoutElement(label.gameObject, minWidth: 20, flexibleWidth: 0, minHeight: 25);

            var inputFieldObj = UIFactory.CreateInputField(row, "InputField", "...", 14, 3, 1);
            UIFactory.SetLayoutElement(inputFieldObj, minWidth: 70, minHeight: 25, flexibleWidth: 0);

            var inputField = inputFieldObj.GetComponent<InputField>();
            m_inputs[index] = inputField;
            inputField.characterValidation = InputField.CharacterValidation.Decimal;

            inputField.onValueChanged.AddListener((string value) => 
            {
                float val = float.Parse(value);
                SetValueToColor(val);
                m_sliders[index].value = val;
            });

            var sliderObj = UIFactory.CreateSlider(row, "Slider", out Slider slider);
            m_sliders[index] = slider;
            UIFactory.SetLayoutElement(sliderObj, minHeight: 25, flexibleWidth: 999, flexibleHeight: 0);
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = GetValueFromColor();

            slider.onValueChanged.AddListener((float value) =>
            {
                inputField.text = value.ToString();
                SetValueToColor(value);
                m_inputs[index].text = value.ToString();
            });

            // methods for writing to the color for this field

            void SetValueToColor(float floatValue)
            {
                Color _color = (Color)Value;
                switch (index)
                {
                    case 0: _color.r = floatValue; break;
                    case 1: _color.g = floatValue; break;
                    case 2: _color.b = floatValue; break;
                    case 3: _color.a = floatValue; break;
                }
                Value = _color;
                m_colorImage.color = _color;
                Owner.SetValue();
            }

            float GetValueFromColor()
            {
                Color _color = (Color)Value;
                switch (index)
                {
                    case 0: return _color.r;
                    case 1: return _color.g;
                    case 2: return _color.b;
                    case 3: return _color.a;
                    default: throw new NotImplementedException();
                }
            }
        }

        #endregion
    }
}
