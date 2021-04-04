using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MelonPrefManager.UI;
using MelonPrefManager.UI.Utility;
using MelonPrefManager.Runtime;

namespace MelonPrefManager.UI.InteractiveValues
{
    // TODO: Support arbitrary types through TomlMapper (if they dont have a specific ivalue)
    // Just use string editor and use the mapper to set/parse the value.

    // TODO: Support registering custom IValue handlers, need to redesign the GetIValueForType method to support that.

    public class InteractiveValue
    {
        public static Type GetIValueForType(Type type)
        {
            if (type == typeof(bool))
                return typeof(InteractiveBool);
            else if (type.IsPrimitive || typeof(decimal).IsAssignableFrom(type))
                return typeof(InteractiveNumber);
            else if (type == typeof(string))
                return typeof(InteractiveString);
            else if (typeof(Enum).IsAssignableFrom(type))
            {
                if (type.GetCustomAttributes(typeof(FlagsAttribute), true) is object[] fa && fa.Any())
                    return typeof(InteractiveFlags);
                else
                    return typeof(InteractiveEnum);
            }
            else if (typeof(Color).IsAssignableFrom(type))
                return typeof(InteractiveColor);
            else if (InteractiveUnityStruct.SupportsType(type))
                return typeof(InteractiveUnityStruct);
            else
                return typeof(InteractiveValue);
        }

        public static InteractiveValue Create(object value, Type fallbackType)
        {
            var type = ReflectionUtility.GetActualType(value) ?? fallbackType;
            var iType = GetIValueForType(type);

            return (InteractiveValue)Activator.CreateInstance(iType, new object[] { value, type });
        }

        // ~~~~~~~~~ Instance ~~~~~~~~~

        public InteractiveValue(object value, Type valueType)
        {
            this.Value = value;
            this.FallbackType = valueType;
        }

        public CachedConfigEntry Owner;

        public object Value;
        public readonly Type FallbackType;

        public virtual bool HasSubContent => false;
        public virtual bool SubContentWanted => false;

        public string DefaultLabel => m_defaultLabel ?? GetDefaultLabel();
        internal string m_defaultLabel;
        internal string m_richValueType;

        public bool m_UIConstructed;

        public virtual void OnValueUpdated()
        {
            if (!m_UIConstructed)
                ConstructUI(m_mainContentParent, m_subContentParent);

            RefreshUIForValue();
        }

        public virtual void RefreshUIForValue()
        {
            GetDefaultLabel();
            m_baseLabel.text = DefaultLabel;
        }

        public void RefreshElementsAfterUpdate()
        {
            if (HasSubContent)
            {
                if (m_subExpandBtn.gameObject.activeSelf != SubContentWanted)
                    m_subExpandBtn.gameObject.SetActive(SubContentWanted);

                if (!SubContentWanted && m_subContentParent.activeSelf)
                    ToggleSubcontent();
            }
        }

        public virtual void ConstructSubcontent()
        {
            m_subContentConstructed = true;
        }

        public virtual void DestroySubContent()
        {
            if (this.m_subContentParent && HasSubContent)
            {
                for (int i = 0; i < this.m_subContentParent.transform.childCount; i++)
                {
                    var child = m_subContentParent.transform.GetChild(i);
                    if (child)
                        GameObject.Destroy(child.gameObject);
                }
            }

            m_subContentConstructed = false;
        }

        public void ToggleSubcontent()
        {
            if (!this.m_subContentParent.activeSelf)
            {
                this.m_subContentParent.SetActive(true);
                this.m_subContentParent.transform.SetAsLastSibling();
                m_subExpandBtn.GetComponentInChildren<Text>().text = "▼";
            }
            else
            {
                this.m_subContentParent.SetActive(false);
                m_subExpandBtn.GetComponentInChildren<Text>().text = "▲";
            }

            OnToggleSubcontent(m_subContentParent.activeSelf);

            RefreshElementsAfterUpdate();
        }

        internal virtual void OnToggleSubcontent(bool toggle)
        {
            if (!m_subContentConstructed)
                ConstructSubcontent();
        }

        internal MethodInfo m_toStringMethod;
        internal MethodInfo m_toStringFormatMethod;
        internal bool m_gotToStringMethods;

        public string GetDefaultLabel(bool updateType = true)
        {
            var valueType = Value?.GetType() ?? this.FallbackType;
            if (updateType)
                m_richValueType = SignatureHighlighter.ParseFullSyntax(valueType, true);

            if (Value.IsNullOrDestroyed())
                return m_defaultLabel = $"<color=grey>null</color> ({m_richValueType})";

            string label;

            if (!m_gotToStringMethods)
            {
                m_gotToStringMethods = true;

                m_toStringMethod = valueType.GetMethod("ToString", new Type[0]);
                m_toStringFormatMethod = valueType.GetMethod("ToString", new Type[] { typeof(string) });

                // test format method actually works
                try
                {
                    m_toStringFormatMethod.Invoke(Value, new object[] { "F3" });
                }
                catch
                {
                    m_toStringFormatMethod = null;
                }
            }

            string toString;
            if (m_toStringFormatMethod != null)
                toString = (string)m_toStringFormatMethod.Invoke(Value, new object[] { "F3" });
            else
                toString = (string)m_toStringMethod.Invoke(Value, new object[0]);

            toString = toString ?? "";

            string typeName = valueType.FullName;
            if (typeName.StartsWith("Il2CppSystem."))
                typeName = typeName.Substring(6, typeName.Length - 6);

            toString = ReflectionProvider.Instance.ProcessTypeNameInString(valueType, toString, ref typeName);

            // If the ToString is just the type name, use our syntax highlighted type name instead.
            if (toString == typeName)
            {
                label = m_richValueType;
            }
            else // Otherwise, parse the result and put our highlighted name in.
            {
                if (toString.Length > 200)
                    toString = toString.Substring(0, 200) + "...";

                label = toString;

                var unityType = $"({valueType.FullName})";
                if (Value is UnityEngine.Object && label.Contains(unityType))
                    label = label.Replace(unityType, $"({m_richValueType})");
                else
                    label += $" ({m_richValueType})";
            }

            return m_defaultLabel = label;
        }

        #region UI CONSTRUCTION

        internal GameObject m_mainContentParent;
        internal GameObject m_subContentParent;

        internal GameObject m_mainContent;
        internal Text m_baseLabel;

        internal Button m_subExpandBtn;
        internal bool m_subContentConstructed;

        public virtual void ConstructUI(GameObject parent, GameObject subGroup)
        {
            m_UIConstructed = true;

            m_mainContent = UIFactory.CreateHorizontalGroup(parent, $"InteractiveValue_{this.GetType().Name}", false, false, true, true, 4, default, 
                new Color(1, 1, 1, 0), TextAnchor.UpperLeft);

            var mainRect = m_mainContent.GetComponent<RectTransform>();
            mainRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 25);

            UIFactory.SetLayoutElement(m_mainContent, flexibleWidth: 9000, minWidth: 175, minHeight: 25, flexibleHeight: 0);

            // subcontent expand button
            if (HasSubContent)
            {
                m_subExpandBtn = UIFactory.CreateButton(m_mainContent, "ExpandSubcontentButton", "▲", ToggleSubcontent, new Color(0.3f, 0.3f, 0.3f));
                UIFactory.SetLayoutElement(m_subExpandBtn.gameObject, minHeight: 25, minWidth: 25, flexibleWidth: 0, flexibleHeight: 0);
            }

            // value label

            m_baseLabel = UIFactory.CreateLabel(m_mainContent, "ValueLabel", "", TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(m_baseLabel.gameObject, flexibleWidth: 9000, minHeight: 25);

            m_subContentParent = subGroup;
        }

#endregion
    }
}
