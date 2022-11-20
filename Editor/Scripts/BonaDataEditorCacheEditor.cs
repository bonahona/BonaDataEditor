using UnityEditor;
using UnityEngine;

namespace Fyrvall.DataEditor
{
    [CustomEditor(typeof(BonaDataEditorCache))]
    public class BonaDataEditorCacheEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            if (GUILayout.Button("Rebuild index")) {
                var cache = target as BonaDataEditorCache;
                cache.UpdateCache();
            }
        }
    }
}