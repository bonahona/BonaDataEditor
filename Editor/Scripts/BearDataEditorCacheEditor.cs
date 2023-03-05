using UnityEditor;
using UnityEngine;

namespace CollisionBear.BearDataEditor
{
    [CustomEditor(typeof(BearDataEditorCache))]
    public class BearDataEditorCacheEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            if (GUILayout.Button("Rebuild index")) {
                var cache = target as BearDataEditorCache;
                cache.UpdateCache();
            }
        }
    }
}