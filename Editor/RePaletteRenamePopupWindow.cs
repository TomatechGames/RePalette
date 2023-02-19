using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tomatech.RePalette.Editor
{
    public class RePaletteRenamePopupWindow : PopupWindowContent
    {
        Action<string> renameAction;
        string currentRenameValue;
        float width;
        public RePaletteRenamePopupWindow(Action<string> renameAction, string existingName = "", float width = 300)
        {
            this.renameAction = renameAction;
            this.width = width;
            currentRenameValue = existingName;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(width, 22);
        }

        public override void OnGUI(Rect rect)
        {
            GUI.SetNextControlName("textField");
            currentRenameValue = EditorGUILayout.TextField(currentRenameValue);
            GUI.FocusControl("textField");
            if (Event.current.keyCode == KeyCode.Return)
            {
                if (renameAction is not null)
                    renameAction(currentRenameValue);
                editorWindow.Close();
            }
        }
    }
}