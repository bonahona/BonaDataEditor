using UnityEngine;

namespace CollisionBear.BearDataEditor
{
    [System.Serializable]
    public class BearDataEditorPreviewButton
    {
        public string FullClassName;
        public string IconPath = string.Empty;
        public GUIContent Icon;
        public KeyCode HotKey;
        public string Tooltip;
        public BearDataEditorType EditorType;

        public BearDataEditorPreviewButton(System.Type type, BearDataEditorType editorType, BearDataEditorAttribute attribute)
        {
            FullClassName = type.FullName;
            EditorType = editorType;
            IconPath = attribute.IconPath;
            HotKey = attribute.HotKey;
            Tooltip = GetTooltip(editorType, attribute);
        }

        public void LoadIcon()
        {
            Icon = new GUIContent(Resources.Load<Texture2D>(IconPath), Tooltip);
        }

        public GUIContent GetContent()
        {
            if(Icon == null) {
                return EditorType.DisplayName;
            } else {
                return Icon;
            }
        }

        private string GetTooltip(BearDataEditorType editorType, BearDataEditorAttribute attribute)
        {
            if (attribute.HotKey == KeyCode.None) {
                return editorType.DisplayName.text;
            } else {
                return editorType.DisplayName + "\t" + attribute.HotKey;
            }
        }
    }
}