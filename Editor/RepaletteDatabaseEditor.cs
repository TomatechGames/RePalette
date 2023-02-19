using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tomatech.RePalette.Editor
{
    [CustomEditor(typeof(RePaletteDatabase))]
    public class RepaletteDatabaseEditor : UnityEditor.Editor
    {
        static RePaletteDatabase Database => RePaletteDatabase.Database;
        public override VisualElement CreateInspectorGUI()
        {
            var removeFilterButton = new Button() { text = "Remove Filter" };
            removeFilterButton.clicked += () =>
            {
                if (!Database.ThemeFilter)
                    return;
                AssetDatabase.RemoveObjectFromAsset(RePaletteDatabase.Database.ThemeFilter);
                DestroyImmediate(RePaletteDatabase.Database.ThemeFilter);
                RePaletteDatabase.Database.ThemeFilter = null;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                if(EditorWindow.HasOpenInstances<RePaletteEditorWindow>())
                    EditorWindow.GetWindow<RePaletteEditorWindow>().CreateGUIInternal();
            };
            return removeFilterButton;
        }
    }
}
