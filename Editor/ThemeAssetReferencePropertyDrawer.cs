using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static Codice.Client.BaseCommands.WkStatus.Printers.StatusChangeInfo;

namespace Tomatech.RePalette.Editor
{
    [CustomPropertyDrawer(typeof(ThemeAssetReference<>))]
    public class ThemeAssetReferencePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty keyProp = property.FindPropertyRelative("addressableKey");
            SerializedProperty subAssetProp = property.FindPropertyRelative("subAssetKey");
            ThemeAssetEntry keyEntry = null;

            if (RePaletteDatabase.Database)
                keyEntry = RePaletteDatabase.Database.GetAssetEntryFromAddressableKey(keyProp.stringValue);
            else
                Debug.LogWarning("RePalette has not been set up. Please set it up using \"Window/Tomatech/Repalette Theme Manager\"");

            var rootElement = new VisualElement();
            rootElement.AddToClassList("unity-base-field");
            rootElement.AddToClassList("unity-base-popup-field");
            rootElement.AddToClassList("unity-popup-field");
            rootElement.AddToClassList("unity-base-field__inspector-field");

            //TODO: sort this out
            //rootElement.AddToClassList(BaseField<string>.alignedFieldUssClassName);

            var propertyDisplayLabel = new Label(property.displayName) { style = { width = new Length(40, LengthUnit.Percent) } };
            propertyDisplayLabel.AddToClassList("unity-text-element");
            propertyDisplayLabel.AddToClassList("unity-label");
            propertyDisplayLabel.AddToClassList("unity-base-field__label");
            propertyDisplayLabel.AddToClassList("unity-base-popup-field__label");
            propertyDisplayLabel.AddToClassList("unity-popup-field__label");
            propertyDisplayLabel.AddToClassList("unity-property-field__label");

            var buttonParent = new VisualElement() { style = { flexGrow = 1, flexDirection = FlexDirection.Row, marginRight = 5 } };

            var addressSelectorButton = CreateDropdownButton();
            var addressSelectorLabel = addressSelectorButton.Q<Label>();
            addressSelectorLabel.text = keyEntry != null ? keyEntry.humanKey : "No Asset Selected";
            addressSelectorLabel.style.unityFontStyleAndWeight = keyEntry != null ? FontStyle.Normal : FontStyle.Italic;

            var subAssetSelectorField = new DropdownField();
            subAssetSelectorField.style.flexGrow = 0.2f;
            subAssetSelectorField.style.display = keyEntry != null && keyEntry.enforcedSubAssets.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            void BindSubAssetSelector()
            {
                if (keyEntry != null)
                {
                    var choiceList = keyEntry.enforcedSubAssets.ToList();
                    choiceList.Insert(0, "");
                    subAssetSelectorField.choices = choiceList;
                    if (choiceList.Contains(subAssetProp.stringValue))
                        subAssetSelectorField.index = choiceList.IndexOf(subAssetProp.stringValue);
                    else
                        subAssetSelectorField.index = 0;
                    choiceList[0] = "(none)";
                    subAssetSelectorField.value = choiceList[subAssetSelectorField.index];
                }
            }
            BindSubAssetSelector();


            addressSelectorButton.clicked += () =>
            {
                SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(addressSelectorButton.worldBound.center)),
                    ScriptableObject.CreateInstance<ThemeAssetSearchProvider>()
                    .SetActionOnSelect(e =>
                    {
                        Debug.Log(e.addressableKey);
                        keyProp.stringValue = e.addressableKey;
                        keyEntry = e;
                        property.serializedObject.ApplyModifiedProperties();

                        addressSelectorButton.Q<Label>().text = e.humanKey;
                        addressSelectorLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
                        if(keyEntry.enforcedSubAssets!=null)
                            subAssetSelectorField.choices = keyEntry.enforcedSubAssets;
                        subAssetSelectorField.index = 0;
                        subAssetSelectorField.style.display = (keyEntry.enforcedSubAssets != null && keyEntry.enforcedSubAssets.Count > 0) ? DisplayStyle.Flex : DisplayStyle.None;
                        BindSubAssetSelector();
                        //show or hide sub-asset button depending on if the entry has enforced subassets
                    }));
            };

            subAssetSelectorField.RegisterValueChangedCallback(e =>
            {
                if (keyEntry == null)
                    return;
                if (subAssetSelectorField.index == 0)
                    subAssetProp.stringValue = "";
                else
                    subAssetProp.stringValue = subAssetSelectorField.value;
                property.serializedObject.ApplyModifiedProperties();
            });

            buttonParent.Add(addressSelectorButton);
            buttonParent.Add(subAssetSelectorField);

            rootElement.Add(propertyDisplayLabel);
            rootElement.Add(buttonParent);

            return rootElement;
        }

        Button CreateDropdownButton()
        {
            var dropdownButton = new Button() { style = { flexGrow = 1f, flexShrink = 0 } };
            dropdownButton.AddToClassList("unity-base-field__input");
            dropdownButton.AddToClassList("unity-base-popup-field__input");
            dropdownButton.AddToClassList("unity-popup-field__input");

            var buttonSelectorLabel = new Label();
            buttonSelectorLabel.AddToClassList("unity-text-element");
            buttonSelectorLabel.AddToClassList("unity-base-popup-field__text");

            var buttonSelectorIcon = new VisualElement() { style = { backgroundImage = EditorGUIUtility.FindTexture("Search Icon") } };
            buttonSelectorIcon.AddToClassList("unity-base-popup-field__arrow");

            dropdownButton.Add(buttonSelectorLabel);
            dropdownButton.Add(buttonSelectorIcon);

            return dropdownButton;
        }


        //class CachedPropData
        //{
        //    public SerializedProperty prop;
        //    public SerializedProperty keyProp;
        //    public ThemeAssetEntry keyEntryResult;
        //    public SerializedProperty subAssetProp;
        //    public List<string> SelectableSubAssets { get
        //        {
        //            var initialList = keyEntryResult != null ? keyEntryResult.enforcedSubAssets.ToList() : new();
        //            initialList.Insert(0,"");
        //            return initialList;
        //        } 
        //    }
        //}
        //static Dictionary<string, CachedPropData> cachedPropDataDict = new();

        //public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        //{
        //    CachedPropData data;
        //    if (!cachedPropDataDict.ContainsKey(property.propertyPath))
        //    {
        //        Debug.Log("noCache");
        //        data = new();
        //        data.prop = property;
        //        data.keyProp = property.FindPropertyRelative("addressableKey");
        //        data.subAssetProp = property.FindPropertyRelative("subAssetKey");
        //        data.keyEntryResult = ThemeEditorDatabase.Database.GetAssetEntryFromAddressableKey(data.keyProp.stringValue);
        //        cachedPropDataDict.Add(property.propertyPath, data);
        //    }
        //    else
        //        data = cachedPropDataDict[property.propertyPath];

        //    EditorGUI.BeginProperty(position, label, property);
        //    EditorGUI.BeginChangeCheck();
        //    //base.OnGUI(position, property, label);
        //    var fieldPosition = EditorGUI.PrefixLabel(position, label);

        //    bool displaySubAssetSelector = data.SelectableSubAssets.Count > 1;

        //    Rect addressRect = fieldPosition;
        //    Rect subAssetRect = fieldPosition;

        //    if (displaySubAssetSelector)
        //    {
        //        addressRect.width *= 0.8f;
        //        subAssetRect.width *= 0.2f;
        //        subAssetRect.x += addressRect.width;
        //    }

        //    bool addressClicked = GUI.Button(addressRect, data.keyEntryResult?.humanKey, EditorStyles.popup);
        //    if (addressClicked)
        //        SearchWindow.Open(new SearchWindowContext(GUIUtility.GUIToScreenPoint(addressRect.center)), 
        //            ScriptableObject.CreateInstance<ThemeAssetReferenceSearchProvider>()
        //            .SetActionOnSelect(e=> {
        //                Debug.Log(e.addressableKey);
        //                property.FindPropertyRelative("addressableKey").stringValue = e.addressableKey;
        //                data.keyEntryResult = e;
        //                data.prop.serializedObject.ApplyModifiedProperties();
        //            }));

        //    if(displaySubAssetSelector)
        //    {
        //        int subAssetIndex = 0;
        //        if(data.SelectableSubAssets.Contains(data.subAssetProp.stringValue))
        //            subAssetIndex = data.SelectableSubAssets.IndexOf(data.subAssetProp.stringValue);
        //        subAssetIndex = EditorGUI.Popup(subAssetRect, 0, data.SelectableSubAssets.Select(s => new GUIContent(s)).ToArray());
        //        data.subAssetProp.stringValue = data.SelectableSubAssets[subAssetIndex];
        //    }

        //    if(EditorGUI.EndChangeCheck())
        //        property.serializedObject.ApplyModifiedProperties();
        //    EditorGUI.EndProperty();
        //}
    }
}
