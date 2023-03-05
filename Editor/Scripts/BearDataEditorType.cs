using UnityEngine;

namespace CollisionBear.BearDataEditor
{
    [System.Serializable]
    public class BearDataEditorType
    {
        public System.Type Type;
        public string FullClassName;
        public GUIContent DisplayName;
        public int Index;
    }
}