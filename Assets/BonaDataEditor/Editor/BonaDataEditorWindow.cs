using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using System.IO;

public class BonaDataEditorWindow : EditorWindow
{
    const string WindowBasePath = "Window/Bona Data Editor";
    const string DefaultTitle = "Data Editor";
    const int ListViewWidth = 300;

    [MenuItem(WindowBasePath)]
    public static void ShowWindow()
    {
        var window = CreateInstance<BonaDataEditorWindow>();
        window.titleContent = new GUIContent(DefaultTitle);
        window.UpdateAvailableTypes();
        window.minSize = new Vector2(800, 600);
        window.maxSize = new Vector2(float.MaxValue, float.MaxValue);
        window.Show();
    }

    public System.Type[] AvailableTypes;
    public System.Type SelectedType;

    public string FilterString;
    public UnityEngine.Object SelectedObject;
    public Editor[] SelectedObjectEditors;
    public UnityEngine.Object[] FoundObjects;
    public UnityEngine.Object[] FilteredObjects;

    public GUIStyle SelectedStyle;
    public GUIStyle UnselectedStyle;

    public Vector2 ListScrollViewOffset;
    public Vector2 InspectorScrollViewOffset;

    public void SetupStyles()
    {
        SelectedStyle = new GUIStyle(GUI.skin.textField);
        SelectedStyle.fontStyle = FontStyle.Bold;
        UnselectedStyle = new GUIStyle(GUI.skin.textField);
    }

    public void OnGUI()
    {
        SetupStyles();

        EditorGUILayout.Space();
        var types = GetEditorTypes();
        var displayNames = GetTypeNames(types);
        var currentIndex = types.GetIndexOfObject(SelectedType);

        EditorGUILayout.BeginHorizontal();
        var selectedIndex = EditorGUILayout.Popup(new GUIContent("Object category"), currentIndex, displayNames);
        if(selectedIndex != currentIndex) {
            ChangeSelectedType(types[selectedIndex]);
        }
        if (GUILayout.Button("Update")) {
            UpdateAvailableTypes();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        EditorGUILayout.BeginVertical(GUILayout.Width(ListViewWidth));
        DisplayObjects();
        EditorGUILayout.EndVertical();
        EditorGUILayout.BeginVertical();
        DisplaySelectedObject();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    public void DisplayObjects()
    {
        if(FoundObjects == null) {
            return;
        }

        EditorGUILayout.LabelField("Search", EditorStyles.boldLabel);
        var tmpFilterString = EditorGUILayout.TextField(FilterString);
        if(tmpFilterString != FilterString) {
            FilteredObjects = FilterObjects(FoundObjects, tmpFilterString);
            FilterString = tmpFilterString;
        }

        EditorGUILayout.LabelField("Found " + FilteredObjects.Count());
        ListScrollViewOffset = EditorGUILayout.BeginScrollView(ListScrollViewOffset);
        foreach(var foundObject in FilteredObjects) {
            if(GUILayout.Button(foundObject.name, GetGuIStyle(foundObject))) {
                ChangeSelectedObject(foundObject);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    public GUIStyle GetGuIStyle(UnityEngine.Object o)
    {
        if(SelectedObject == o) {
            return SelectedStyle;
        } else {
            return UnselectedStyle;
        }
    }

    public void DisplaySelectedObject()
    {
        var inspectorWidth = this.position.width - ListViewWidth - 50;

        if (SelectedObject != null) {
            EditorGUILayout.LabelField(SelectedObject.name, EditorStyles.boldLabel);
            EditorGUILayout.Space();
        }

        if (SelectedObjectEditors != null) {
            InspectorScrollViewOffset = EditorGUILayout.BeginScrollView(InspectorScrollViewOffset);
            foreach (var selectedEditor in SelectedObjectEditors) {
                if (selectedEditor != null) {
                    EditorGUILayout.BeginHorizontal(GUILayout.Width(inspectorWidth));
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    selectedEditor.DrawDefaultInspector();
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }

    public void ChangeSelectedType(System.Type type)
    {
        SelectedType = type;
        FoundObjects = FindAssetsOfType(type);
        titleContent = new GUIContent(GetTypeName(type));
        FilteredObjects = FoundObjects;
        FilterString = string.Empty;
        ClearSelectedEditor();
    }

    public UnityEngine.Object[] FindAssetsOfType(System.Type type)
    {
        var result = new List<Object>();
        foreach(var asset in AssetDatabase.GetAllAssetPaths().Where(asset => IsAllowedLoadingAsset(asset))) {
            var loadedAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset);

            if(IsCorrectComponent(type, loadedAsset)) {
                result.Add(loadedAsset);
            }else if (IsCorrentScriptableObject(type, loadedAsset, loadedAsset.name)) {
                result.Add(loadedAsset);
            }
        }

        return result.OrderBy(o => o.name).ToArray();
    }

    private bool IsAllowedLoadingAsset(string assetPath)
    {
        if (!assetPath.StartsWith("Assets/")) {
            return false;
        }

        if(!Path.GetExtension(assetPath).In(".asset", ".prefab")){
            return false;
        }

        return true;
    }

    private bool IsCorrectComponent(System.Type type, UnityEngine.Object asset)
    {
        if (typeof(Component).IsAssignableFrom(type)) {
            if (asset is GameObject) {
                var component = asset.To<GameObject>().GetComponent(type);
                if (component != null) {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsCorrentScriptableObject(System.Type type, UnityEngine.Object asset, string name)
    {
        if (typeof(ScriptableObject).IsAssignableFrom(type) || type.IsInterface) {
            if (asset.IsInstanceOf(type)) {
                return true;
            }
        }

        return false;
    }

    public UnityEngine.Object[] FilterObjects(UnityEngine.Object[] startCollection, string filter)
    {
        if(filter == string.Empty) {
            return startCollection;
        }

        return startCollection.Where(o => o.name.ToLower().Contains(filter.ToLower())).ToArray();
    }

    public void ChangeSelectedObject(UnityEngine.Object selectedObject)
    {
        if(selectedObject == null) {
            return;
        }

        if(selectedObject == SelectedObject) {
            return;
        }

        ClearSelectedEditor();

        SelectedObject = selectedObject;
        if (SelectedObject is GameObject) {
            var gameObject = SelectedObject.To<GameObject>();
            var components = gameObject.GetComponents<Component>();
            SelectedObjectEditors = new Editor[components.Length];
            for(int i = 0; i < components.Length; i ++) {
                SelectedObjectEditors[i] = Editor.CreateEditor(components[i]);
            }
        } else {
            SelectedObjectEditors = new Editor[] { Editor.CreateEditor(selectedObject) };
        }
    }

    public void ClearSelectedEditor()
    {
        if (SelectedObjectEditors != null) {
            foreach(var editor in SelectedObjectEditors) {
                if(editor != null) {
                    DestroyImmediate(editor);
                }
            }
        }
    }

    public void UpdateAvailableTypes()
    {
        AvailableTypes = GetEditorTypes();
    }

    public System.Type[] GetEditorTypes()
    {
        return Assembly.GetAssembly(typeof(CharacterBase)).GetTypes().Where(t => IsObjectEditorType(t)).ToArray();
    }

    public bool IsObjectEditorType(System.Type type)
    {
        return type.GetCustomAttributes(typeof(BonaDataEditorAttribute), false).FirstOrDefault() != null;
    }

    public GUIContent[] GetTypeNames(System.Type[] types)
    {
        return types.Map(t => new GUIContent(GetTypeName(t))).ToArray();
    }

    public string GetTypeName(System.Type type)
    {
        var attribute = type.GetCustomAttributes(typeof(BonaDataEditorAttribute), false).FirstOrDefault().To<BonaDataEditorAttribute>();
        if(attribute.DisplayName == string.Empty) {
            return type.Name;
        } else {
            return attribute.DisplayName;
        }
    }
}
