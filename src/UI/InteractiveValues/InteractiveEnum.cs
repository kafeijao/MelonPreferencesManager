using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using MelonPrefManager.UI;

namespace MelonPrefManager.UI.InteractiveValues
{
    public class InteractiveEnum : InteractiveValue
    {
        internal static Dictionary<Type, KeyValuePair<int,string>[]> s_enumNamesCache = new Dictionary<Type, KeyValuePair<int, string>[]>();

        public InteractiveEnum(object value, Type valueType) : base(value, valueType)
        {
            GetNames();
        }

        public override bool HasSubContent => false;
        public override bool SubContentWanted => false;

        public override bool SupportsType(Type type)
            => type.IsEnum;

        internal KeyValuePair<int,string>[] m_values = new KeyValuePair<int, string>[0];
        internal Dictionary<string, Dropdown.OptionData> m_dropdownOptions = new Dictionary<string, Dropdown.OptionData>();

        internal void GetNames()
        {
            var type = Value?.GetType() ?? FallbackType;

            if (!s_enumNamesCache.ContainsKey(type))
            {
                // using GetValues not GetNames, to catch instances of weird enums (eg CameraClearFlags)
                var values = Enum.GetValues(type);

                var list = new List<KeyValuePair<int, string>>();
                var set = new HashSet<string>();

                foreach (var value in values)
                {
                    var name = value.ToString();

                    if (set.Contains(name)) 
                        continue;

                    set.Add(name);

                    var backingType = Enum.GetUnderlyingType(type);
                    int intValue;
                    try
                    {
                        // this approach is necessary, a simple '(int)value' is not sufficient.

                        var unbox = Convert.ChangeType(value, backingType);

                        intValue = (int)Convert.ChangeType(unbox, typeof(int));
                    }
                    catch (Exception ex)
                    {
                        PrefManagerMod.LogWarning("[InteractiveEnum] Could not Unbox underlying type " + backingType.Name + " from " + type.FullName);
                        PrefManagerMod.Log(ex.ToString());
                        continue;
                    }

                    list.Add(new KeyValuePair<int, string>(intValue, name));
                }

                s_enumNamesCache.Add(type, list.ToArray());
            }

            m_values = s_enumNamesCache[type];
        }

        public override void OnValueUpdated()
        {
            GetNames();

            base.OnValueUpdated();
        }

        public override void RefreshUIForValue()
        {
            base.RefreshUIForValue();

            //m_baseLabel.text = "";

            string key = Value.ToString();
            if (m_dropdownOptions.ContainsKey(key))
                m_dropdown.value = m_dropdown.options.IndexOf(m_dropdownOptions[key]);
        }

        private void SetValueFromDropdown()
        {
            var type = Value?.GetType() ?? FallbackType;
            var index = m_dropdown.value;

            var value = Enum.Parse(type, s_enumNamesCache[type][index].Value);

            if (value != null)
            {
                Value = value;
                Owner.SetValue();
                RefreshUIForValue();
            }
        }

        internal Dropdown m_dropdown;
        //internal Text m_dropdownText;

        public override void ConstructUI(GameObject parent)//, GameObject subGroup)
        {
            base.ConstructUI(parent);//, subGroup);

            //UIFactory.SetLayoutElement(m_baseLabel.gameObject, minWidth: 0, flexibleWidth: 0);

            // dropdown

            var dropdownObj = UIFactory.CreateDropdown(m_mainContent, out m_dropdown, "", 14, null);
            UIFactory.SetLayoutElement(dropdownObj, minWidth: 400, minHeight: 25);

            foreach (var kvp in m_values)
            {
                var opt = new Dropdown.OptionData
                {
                    text = kvp.Value
                };
                m_dropdown.options.Add(opt);
                m_dropdownOptions.Add(kvp.Value, opt);
            }

            m_dropdown.onValueChanged.AddListener((int val) =>
            {
                SetValueFromDropdown();
            });

            //m_dropdownText = m_dropdown.transform.Find("Label").GetComponent<Text>();

            //// apply button

            //var apply = UIFactory.CreateButton(m_mainContent, "ApplyButton", "Apply", SetValueFromDropdown, new Color(0.3f, 0.3f, 0.3f));
            //UIFactory.SetLayoutElement(apply.gameObject, minHeight: 25, minWidth: 50);

            //RefreshUIForValue();
        }
    }
}
