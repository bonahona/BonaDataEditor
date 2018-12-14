﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using System.IO;
using UnityEditor.IMGUI.Controls;

namespace Fyrvall.DataEditor
{
    public class BonaDataEditorWindow : EditorWindow
    {
        const string OnlineSourceUrl = "http://documentation.fyrvall.com/Projects/Details/BonaDataEditor";
        const string WindowBasePath = "Window/Bona Data Editor";
        const string DefaultTitle = "Data Editor";
        const int ListViewWidth = 300;

        [MenuItem(WindowBasePath)]
        public static void ShowWindow()
        {
            var window = CreateInstance<BonaDataEditorWindow>();
            window.Show();
        }

        public string SelectedType;
        public string FilterString;
        public UnityEngine.Object SelectedObject;

        public Editor SelectedObjectHeaderEditor;
        public List<Editor> SelectedObjectEditors;
        public List<UnityEngine.Object> FoundObjects = new List<Object>();
        public List<UnityEngine.Object> FilteredObjects = new List<Object>();

        public GUIStyle SelectedStyle;
        public GUIStyle UnselectedStyle;

        public Vector2 ListScrollViewOffset;
        public Vector2 InspectorScrollViewOffset;

        private SearchField ObjectSearchField;

        public void OpenNewEditor()
        {
            var window = CreateInstance<BonaDataEditorWindow>();
            window.Show();
        }

        public void SetupStyles()
        {
            SelectedStyle = new GUIStyle(GUI.skin.label);
            SelectedStyle.normal.textColor = Color.white;
            SelectedStyle.normal.background = CreateTexture(300, 20, new Color(0.24f, 0.48f, 0.9f));
            UnselectedStyle = new GUIStyle(GUI.skin.label);
        }

        private Texture2D CreateTexture(int width, int height, Color color)
        {
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(Enumerable.Repeat(color, width * height).ToArray());
            result.Apply();

            return result;
        }

        public void OnEnable()
        {
            ObjectSearchField = new SearchField();
            if (SelectedType == string.Empty) {
                ChangeSelectedType(GetEditorTypes().FirstOrDefault());
            } else {
                CreateEditors(SelectedObject);
            }
        }

        public void OnGUI()
        {
            SetupStyles();
            EditorGUILayout.Space();

            var types = GetEditorTypes();

            if(types.Length == 0) {
                EditorGUILayout.HelpBox(string.Format("No types to display.\n\nStart using [BonaDataEditor] attribute on classes inheriting from ScriptableObject or MonoBehaviour to expose them in the editor.\nSee the classes in the Examples folder for real uses.\n\nFor more info see {0}.", OnlineSourceUrl), MessageType.Info);
                return;
            }

            var fullTypeNames = types.Select(t => t.FullName).ToList();
            var displayNames = GetTypeNames(types);
            var currentIndex = Mathf.Max(0, fullTypeNames.GetIndexOfObject(SelectedType));

            using (new EditorGUILayout.HorizontalScope()) {
                var selectedIndex = EditorGUILayout.Popup(new GUIContent("Object category"), currentIndex, displayNames);
                var selectedTypeName = fullTypeNames[selectedIndex];
                if (selectedTypeName != SelectedType) {
                    ChangeSelectedType(types[selectedIndex]);
                }
                if(GUILayout.Button("Open new editor", GUILayout.Width(128))) {
                    OpenNewEditor();
                }
            }

            using (new EditorGUILayout.HorizontalScope()) {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box, GUILayout.Width(ListViewWidth))) {
                    DisplayObjects();
                }

                if (SelectedObject != null) {
                    using (new EditorGUILayout.VerticalScope(GUI.skin.box)) {
                        DisplaySelectedObject();
                    }
                }
            }
        }

        public void DisplayObjects()
        {
            using(new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField("Found " + FilteredObjects.Count());
                if (GUILayout.Button("Refresh", GUILayout.Width(64))) {
                    RefreshObjects();
                }
            }


            DisplaySearchField();

            if (FoundObjects == null) {
                return;
            }

            using (var scrollScope = new EditorGUILayout.ScrollViewScope(ListScrollViewOffset)) {
                ListScrollViewOffset = scrollScope.scrollPosition;
                foreach (var foundObject in FilteredObjects.ToList()) {
                    if (foundObject == null) {
                        FilteredObjects.Remove(foundObject);
                    } else {
                        using (new EditorGUILayout.HorizontalScope()) {
                            GUI.DrawTexture(GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16)), GetPreviewTexture(foundObject));
                            if (GUILayout.Button(foundObject.name, GetGuIStyle(foundObject))) {
                                ChangeSelectedObject(foundObject);
                            }
                            if (GUILayout.Button(new GUIContent(Resources.Load<Texture>("ShowInProjectIcon")), EditorStyles.label, GUILayout.MaxWidth(22))) {
                                EditorGUIUtility.PingObject(foundObject);
                            }
                        }
                    }
                }
            }
        }

        public void RefreshObjects()
        {
            var types = GetEditorTypes();
            if(types.Length == 0) {
                return;
            }

            var type = types.Where(t => t.FullName == SelectedType).FirstOrDefault();
            if (type != null) {
                FoundObjects = FindAssetsOfType(type);
                UpdateFilter(FilterString);
            }
        }

        public Texture2D GetPreviewTexture(Object asset)
        {
            var result = AssetPreview.GetAssetPreview(asset);

            if(result == null) {
                result = AssetPreview.GetMiniThumbnail(asset);
            }

            return result;
        }

        public void DisplaySearchField()
        {
            var searchRect = GUILayoutUtility.GetRect(100, 32);
            var tmpFilterString = ObjectSearchField.OnGUI(searchRect, FilterString);

            if (tmpFilterString != FilterString) {
                UpdateFilter(tmpFilterString);
                FilterString = tmpFilterString;
            }
        }

        public void UpdateFilter(string filterString)
        {
            FilteredObjects = FilterObjects(FoundObjects, filterString);
        }

        public GUIStyle GetGuIStyle(UnityEngine.Object o)
        {
            if (SelectedObject == o) {
                return SelectedStyle;
            } else {
                return UnselectedStyle;
            }
        }

        public void DisplaySelectedObject()
        {
            EditorGUI.BeginChangeCheck();
            if (SelectedObject == null) {
                return;
            }

            if (SelectedObjectEditors == null) {
                return;
            }

            var inspectorWidth = this.position.width - (ListViewWidth + 70);

            // For some reason these values will be needed to be set several times when drawing the different editors and I don't know why.
            var labelWidth = GetLabelWidth(inspectorWidth, 150);
            var fieldWidth = GetLabelWidth(inspectorWidth - labelWidth, float.MaxValue);

            using (var scrollScope = new EditorGUILayout.ScrollViewScope(InspectorScrollViewOffset)) {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(inspectorWidth))) {
                    InspectorScrollViewOffset = scrollScope.scrollPosition;

                    SelectedObjectHeaderEditor.DrawHeader();
                    foreach (var selectedEditor in SelectedObjectEditors) {
                        if (selectedEditor != null) {
                            using (new EditorGUILayout.HorizontalScope()) {
                                DrawComponentPreview(selectedEditor.target);
                                EditorGUILayout.LabelField(selectedEditor.target.GetType().Name, EditorStyles.boldLabel);
                            }

                            if (selectedEditor.target is MonoBehaviour || selectedEditor.target is ScriptableObject) {
                                EditorGUIUtility.labelWidth = labelWidth;
                                EditorGUIUtility.fieldWidth = fieldWidth;
                                selectedEditor.OnInspectorGUI();
                            } else {
                                EditorGUIUtility.labelWidth = labelWidth;
                                EditorGUIUtility.fieldWidth = fieldWidth;
                                selectedEditor.DrawDefaultInspector();
                            }
                            DrawUILine(Color.gray);
                            EditorGUILayout.Space();
                        }
                    }
                }
            }

            EditorGUI.EndChangeCheck();
        }

        public float GetLabelWidth(float totalWidth, float minWidth)
        {
            if(totalWidth <= minWidth) {
                return totalWidth;
            }

            return Mathf.Min(minWidth,totalWidth);
        }

        public void DrawUILine(Color color, int thickness = 1, int padding = 0)
        {
            Rect lineRect = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            lineRect.height = thickness;
            lineRect.y += padding / 2;
            lineRect.x -= 20;
            lineRect.width += 20;
            EditorGUI.DrawRect(lineRect, color);
        }

        public void DrawComponentPreview(UnityEngine.Object unityObject)
        {
            var drawRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
            var previewTexture = AssetPreview.GetMiniThumbnail(unityObject);

            if (previewTexture != null) {
                GUI.DrawTexture(drawRect, previewTexture);
            }
        }

        public void ChangeSelectedType(System.Type type)
        {
            if(type == null) {
                titleContent = new GUIContent("Data Editor", Resources.Load<Texture>("DataEditorIcon"));
                return;
            }

            SelectedType = type.FullName;
            FoundObjects = FindAssetsOfType(type);
            var name = GetTypeName(type);
            titleContent = new GUIContent(GetTypeName(type).Substring(0, Mathf.Min(name.Length, 10)), Resources.Load<Texture>("DataEditorIcon"));
            FilteredObjects = FoundObjects;
            FilterString = string.Empty;
            ClearSelectedEditor();
        }

        public List<UnityEngine.Object> FindAssetsOfType(System.Type type)
        {
            if (typeof(ScriptableObject).IsAssignableFrom(type)) {
                return FindScriptableObjectOfType(type);
            }else if (typeof(Component).IsAssignableFrom(type)) {
                return FindPrefabsWithComponentType(type);
            } else {
                return new List<Object>();
            }
        }

        public List<UnityEngine.Object> FindScriptableObjectOfType(System.Type type)
        {
            return AssetDatabase.FindAssets(string.Format("t:{0}", type))
                .Select(g => AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(g)))
                .OrderBy(o => o.name)
                .ToList();
        }

        private List<UnityEngine.Object> FindPrefabsWithComponentType(System.Type type)
        {
            return AssetDatabase.GetAllAssetPaths()
                .Where(p => p.Contains(".prefab"))
                .Select(p => AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(p))
                .Where(a => HasComponent(a, type))
                .Map(a => a.To<UnityEngine.Object>())
                .ToList();
        }

        private bool HasComponent(GameObject gameObject, System.Type type)
        {
            return gameObject.GetComponents<Component>()
                .Where(t => type.IsInstanceOfType(t))
                .Any();
        }

        public List<UnityEngine.Object> FilterObjects(List<UnityEngine.Object> startCollection, string filter)
        {
            if (filter == string.Empty) {
                return startCollection;
            }

            return startCollection.Where(o => o.name.ToLower().Contains(filter.ToLower())).ToList();
        }

        public void ChangeSelectedObject(UnityEngine.Object selectedObject)
        {
            if (selectedObject == null) {
                return;
            }

            if (selectedObject == SelectedObject) {
                return;
            }

            SelectedObject = selectedObject;
            CreateEditors(selectedObject);
            GUI.FocusControl(null);
        }

        public void CreateEditors(UnityEngine.Object selectedObject)
        {
            SelectedObjectHeaderEditor = Editor.CreateEditor(SelectedObject);
            if (SelectedObject is GameObject) {
                var gameObject = SelectedObject.To<GameObject>();
                var components = gameObject.GetComponents<Component>();
                SelectedObjectEditors = new List<Editor>();
                for (int i = 0; i < components.Length; i++) {
                    SelectedObjectEditors.Add(Editor.CreateEditor(components[i]));
                }
            } else {
                SelectedObjectEditors = new List<Editor> { Editor.CreateEditor(selectedObject) };
            }
        }

        public void ClearSelectedEditor()
        {
            if (SelectedObjectEditors != null) {
                foreach (var editor in SelectedObjectEditors) {
                    if (editor != null) {
                        DestroyImmediate(editor);
                    }
                }
            }
            
            if(SelectedObjectHeaderEditor != null) {
                DestroyImmediate(SelectedObjectHeaderEditor);
            }

            SelectedObjectHeaderEditor = null;
            SelectedObject = null;
        }

        public System.Type[] GetEditorTypes()
        {
            return Assembly.GetAssembly(typeof(DummyClass)).GetTypes().Where(t => IsObjectEditorType(t)).ToArray();
        }

        public bool IsObjectEditorType(System.Type type)
        {
            return type.GetCustomAttributes(typeof(BonaDataEditorAttribute), false).FirstOrDefault() != null;
        }

        public GUIContent[] GetTypeNames(System.Type[] types)
        {
            return types.Select(t => new GUIContent(GetTypeName(t))).ToArray();
        }

        public string GetTypeName(System.Type type)
        {
            var attribute = type.GetCustomAttributes(typeof(BonaDataEditorAttribute), false).FirstOrDefault().To<BonaDataEditorAttribute>();
            if (attribute.DisplayName == string.Empty) {
                return AddSpacesToSentence(type.Name);
            } else {
                return attribute.DisplayName;
            }
        }

        private string AddSpacesToSentence(string text)
        {
            System.Text.StringBuilder newText = new System.Text.StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++) {
                if (char.IsUpper(text[i]) && text[i - 1] != ' ')
                    newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }
    }
}