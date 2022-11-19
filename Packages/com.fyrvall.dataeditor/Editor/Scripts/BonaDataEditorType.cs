using UnityEngine;

namespace Fyrvall.DataEditor
{
    [System.Serializable]
    public class BonaDataEditorType
    {
        public System.Type Type;
        public string FullClassName;
        public GUIContent DisplayName;
        public int Index;
    }
}