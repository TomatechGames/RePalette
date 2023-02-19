using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tomatech.RePalette.Editor
{
    [CustomPropertyDrawer(typeof(BasicThemeFilter))]
    public class BasicThemeFilterPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var keyProp = property.FindPropertyRelative("themeKey");
            var keyField = new TextField(keyProp.displayName);
            keyField.BindProperty(keyProp);
            keyField.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            return keyField;
        }
    }
}
