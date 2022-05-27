using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Fyrvall.DataEditor
{
    public class BonaDataEditorWindow : EditorWindow
    {
        const string OnlineSourceUrl = "https://github.com/bonahona/bonadataeditor";
        const string WindowBasePath = "Window/Bona Data Editor";
        const int ListViewWidth = 300;
        const int IconSize = 33;

        private static GUIContent ShowInProjectContent;
        private static GUIContent ObjectCategoryContent;

        private static BonaDataEditorType[] AvailableEditorTypes;
        private static GUIContent[] DisplayNames;

        private static List<List<BonaDataEditorPreviewButton>> IconGroups;
        private static Dictionary<KeyCode, BonaDataEditorType> HotKeyMappings;

        private static GUIStyle IconButton;

        static BonaDataEditorWindow()
        {
            GetEditorTypes();
        }

        private static void GetEditorTypes()
        {
            var typeAttributes = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Select(t => (Type: t, Attribute: ObjectEditorAttribute(t)))
                .Where(t => t.Attribute != null)
                .ToList();

            AvailableEditorTypes = typeAttributes.Select(t => new BonaDataEditorType {
                Type = t.Type,
                Index = typeAttributes.IndexOf(t),
                FullClassName = t.Type.FullName,
                DisplayName = new GUIContent(GetTypeName(t.Type))
            }).ToArray();
            DisplayNames = AvailableEditorTypes.Select(t => t.DisplayName).ToArray();

            IconGroups = new List<List<BonaDataEditorPreviewButton>>();
            HotKeyMappings = new Dictionary<KeyCode, BonaDataEditorType>();

            if (!typeAttributes.Any(t => t.Attribute.UseIcon)) {
                return;
            }

            var groupsRequired = typeAttributes.Select(t => t.Attribute.IconGroupIndex).Max() +1;
            for(int i = 0; i < groupsRequired; i ++) {
                IconGroups.Add(new List<BonaDataEditorPreviewButton>());
            }

            foreach(var type in typeAttributes.Where(t => t.Attribute.UseIcon)) {
                var editorType = AvailableEditorTypes.First(t => t.Type == type.Type);
                IconGroups[type.Attribute.IconGroupIndex].Add(new BonaDataEditorPreviewButton(
                    type.Type,
                    editorType,
                    type.Attribute));

                if(type.Attribute.HotKey != KeyCode.None) {
                    if (HotKeyMappings.ContainsKey(type.Attribute.HotKey)) {
                        Debug.LogWarning("BonaDataEditor - Multiple types uses the same Hot key " + type.Attribute.HotKey);
                        continue;
                    }

                    HotKeyMappings.Add(type.Attribute.HotKey, editorType);
                }
            } 
        }

        private static BonaDataEditorAttribute ObjectEditorAttribute(System.Type type)
        {
            return type.GetCustomAttributes(typeof(BonaDataEditorAttribute), false).FirstOrDefault() as BonaDataEditorAttribute;
        }

        private static string GetTypeName(System.Type type)
        {
            var attribute = (BonaDataEditorAttribute)type.GetCustomAttributes(typeof(BonaDataEditorAttribute), false).FirstOrDefault();
            if (attribute.DisplayName == string.Empty) {
                return AddSpacesToSentence(type.Name);
            } else {
                return attribute.DisplayName;
            }
        }

        private static string AddSpacesToSentence(string text)
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

        private static Texture2D CreateTexture(int width, int height, Color32 color)
        {
            Texture2D result = new Texture2D(width, height);
            result.SetPixels32(Enumerable.Repeat(color, width * height).ToArray(), 0);
            result.Apply();

            return result;
        }

        [MenuItem(WindowBasePath + " #b")]
        public static void ShowWindow()
        {
            var window = CreateInstance<BonaDataEditorWindow>();
            window.Show();
        }

        private BonaDataEditorType SelectedType;

        [SerializeField]
        private BonaDataEditorCache CacheIndex;
        [SerializeField]
        private string SelectedTypeName;

        private int SelectedObjectIndex;

        private BonaDataEditorAsset SelectedObject = new BonaDataEditorAsset();
        private Object PrefabInstance;

        private List<Editor> AllEditors;
        private Editor SelectedObjectHeaderEditor;
        private List<Editor> SelectedObjectEditors;

        private List<BonaDataEditorAsset> FoundObjects = new List<BonaDataEditorAsset>();
        private List<BonaDataEditorAsset> FilteredObjects = new List<BonaDataEditorAsset>();

        private Dictionary<BonaDataEditorType, List<BonaDataEditorAsset>> CachedObjects = new Dictionary<BonaDataEditorType, List<BonaDataEditorAsset>>();

        private string FilterString;
        private SearchField ObjectSearchField;
        private GUIStyle SelectedStyle;
        private GUIStyle UnselectedStyle;

        private Vector2 ListScrollViewOffset;
        private Vector2 InspectorScrollViewOffset;

        private void OpenNewEditor()
        {
            var window = CreateInstance<BonaDataEditorWindow>();
            window.Show();
        }

        private void SetupStyles()
        {
            SelectedStyle = new GUIStyle(GUI.skin.label);
            SelectedStyle.normal.textColor = Color.white;
            SelectedStyle.normal.background = CreateTexture(300, 20, new Color(0.24f, 0.48f, 0.9f));

            UnselectedStyle = new GUIStyle(GUI.skin.label);
            IconButton = new GUIStyle(GUI.skin.button) {
                padding = new RectOffset(1, 1, 1, 1)
            };

        }

        public void OnDisable()
        {
            ClearAllEditors();
        }

        public void OnEnable()
        {
            if (CacheIndex == null) {
                CacheIndex = LoadCacheIndex();
            }

            foreach (var group in IconGroups) {
                foreach(var item in group) {
                    if(item.Icon == null && item.IconPath != string.Empty) {
                        item.LoadIcon();
                    }
                }
            }

            ShowInProjectContent = new GUIContent(Resources.Load<Texture>("ShowInProjectIcon"), "Open in project view");
            ObjectCategoryContent = new GUIContent("Object category");

            ObjectSearchField = new SearchField();
            if (SelectedTypeName == string.Empty || !AvailableEditorTypes.Select(t => t.FullClassName).Contains(SelectedTypeName)) {
                ChangeSelectedType(AvailableEditorTypes.FirstOrDefault());
            } else {
#if UNITY_2018_3_OR_NEWER
                UpdateSelectedObjectIndex(SelectedType.Index);
                if (SelectedObject?.Object is GameObject) {
                    CreateEditors(PrefabInstance);
                } else {
                    CreateEditors(SelectedObject?.Object);
                }
#else
                CreateEditors(SelectedObject);
#endif
            }
        }

        private BonaDataEditorCache LoadCacheIndex()
        {
            var result = BonaDataEditorCache.GetCacheIndex();
            if (result == null) {
                if (EditorUtility.DisplayDialog("No Cache index found", "Must create a cache index before the editor will work. This will take a few minutes", "Ok")) {
                    result = BonaDataEditorCache.CreateCacheIndex();
                }
            }

            return result;
        }

        private void ClearAllEditors()
        {
            try {
                if (AllEditors == null) {
                    AllEditors = new List<Editor>();
                } else {
                    foreach (var editor in AllEditors) {
                        if (editor != null) {
                            editor.ResetTarget();
                            GameObject.DestroyImmediate(editor);
                        }
                    }
                    AllEditors.Clear();
                }

                if (SelectedObjectEditors == null) {
                    SelectedObjectEditors = new List<Editor>();
                } else {
                    SelectedObjectEditors.Clear();
                }
                SelectedObjectHeaderEditor = null;
            } catch (System.Exception e) {
                Debug.LogWarning("DataEdior error on cleanup: " + e.Message);

            }
        }

        public void OnGUI()
        {
            SetupStyles();
            EditorGUILayout.Space();

            if (AvailableEditorTypes.Length == 0) {
                EditorGUILayout.HelpBox(string.Format("No types to display.\n\nStart using [BonaDataEditor] attribute on classes inheriting from ScriptableObject or MonoBehaviour to expose them in the editor.\nSee the classes in the Examples folder for real uses.\n\nFor more info see {0}.", OnlineSourceUrl), MessageType.Info);
                return;
            }

            HandleKeyboardInput();
            DisplayTypeSelection();
            DisplayObjectSelection();
        }

        private void DisplayTypeSelection()
        {
            using (new EditorGUILayout.HorizontalScope()) {
                var selectedTypeIndex = EditorGUILayout.Popup(ObjectCategoryContent, SelectedType.Index, DisplayNames);
                var tmpSelectedType = AvailableEditorTypes[selectedTypeIndex];
                if (tmpSelectedType != SelectedType) {
                    ChangeSelectedType(AvailableEditorTypes[selectedTypeIndex]);
                }
                if (GUILayout.Button("Open new editor", GUILayout.Width(128))) {
                    OpenNewEditor();
                }
            }

            using(new EditorGUILayout.HorizontalScope()) {
                GUILayout.Label(GUIContent.none, EditorStyles.label, GUILayout.Width(6), GUILayout.Height(IconSize));
                foreach (var group in IconGroups) {
                    foreach(var item in group) {
                        SetGuiColorState(item.EditorType == SelectedType);
                        if (GUILayout.Button(item.GetContent(), IconButton, GUILayout.Width(IconSize), GUILayout.Height(IconSize))) {
                            if (AvailableEditorTypes[item.EditorType.Index] != SelectedType) {
                                ChangeSelectedType(AvailableEditorTypes[item.EditorType.Index]);
                            }
                        }
                    }

                    GUILayout.Label(GUIContent.none, EditorStyles.label, GUILayout.Width(12), GUILayout.Height(IconSize));
                }

                SetGuiColorState(false);
            }
        }

        private void SetGuiColorState(bool state)
        {
            if (state) {
                GUI.color = Color.green;
            } else {
                GUI.color = Color.white;
            }
        }

        private void DisplayObjectSelection()
        {
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

        private void HandleKeyboardInput()
        {
            if (Event.current.type == EventType.KeyDown) {
                if (Event.current.keyCode == KeyCode.DownArrow) {
                    UpdateSelectedObjectIndex(SelectedObjectIndex + 1);
                    Event.current.Use();
                } else if (Event.current.keyCode == KeyCode.UpArrow) {
                    UpdateSelectedObjectIndex(SelectedObjectIndex + -1);
                    Event.current.Use();
                } else if(HotKeyMappings.ContainsKey(Event.current.keyCode)) {
                    ChangeSelectedType(HotKeyMappings[Event.current.keyCode]);
                }
            }
        }

        private void UpdateSelectedObjectIndex(int newIndex)
        {
            if (SelectedObject == null || FilteredObjects.Count == 0) {
                return;
            }

            SelectedObjectIndex = FilteredObjects.IndexOf(SelectedObject);
            SelectedObjectIndex = Mathf.Clamp(newIndex, 0, FilteredObjects.Count - 1);
            ChangeSelectedObject(FilteredObjects[SelectedObjectIndex]);
        }

        private void DisplayObjects()
        {
            using (new EditorGUILayout.HorizontalScope()) {
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
                var count = 0;
                foreach (var foundObject in FilteredObjects.ToList()) {
                    if (foundObject == null) {
                        FilteredObjects.Remove(foundObject);
                    } else {
                        using (new EditorGUILayout.HorizontalScope()) {
                            GUI.DrawTexture(GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16)), foundObject.GetPreview());
                            if (GUILayout.Button(foundObject.Name, GetGUIStyle(foundObject))) {
                                ChangeSelectedObject(foundObject);
                            }

                            if (GUILayout.Button(ShowInProjectContent, EditorStyles.label, GUILayout.MaxWidth(18))) {
                                var assetObject = foundObject.GetObject();
                                ProjectWindowUtil.ShowCreatedAsset(assetObject);
                                EditorGUIUtility.PingObject(assetObject);
                            }
                        }
                    }

                    count++;
                }
            }
        }

        public void RefreshObjects()
        {
            if (SelectedType != null && FilterString != null) {
                UpdateFoundObjects(SelectedType, force: true);
                UpdateFilter(FilterString);
            }
        }

        private void DisplaySearchField()
        {
            var searchRect = GUILayoutUtility.GetRect(100, 32);
            var tmpFilterString = ObjectSearchField.OnGUI(searchRect, FilterString);

            if (tmpFilterString != FilterString) {
                UpdateFilter(tmpFilterString);
                FilterString = tmpFilterString;
            }
        }

        private void UpdateFilter(string filterString)
        {
            FilteredObjects = FilterObjects(FoundObjects, filterString);
        }

        private GUIStyle GetGUIStyle(BonaDataEditorAsset o)
        {
            if (SelectedObject == o) {
                return SelectedStyle;
            } else {
                return UnselectedStyle;
            }
        }

        private void DisplaySelectedObject()
        {
            if (SelectedObject?.Object == null) {
                return;
            }

            if (SelectedObjectEditors == null) {
                return;
            }

            EditorGUI.BeginChangeCheck();

            var inspectorWidth = position.width - (ListViewWidth + 70);

            // For some reason these values will be needed to be set several times when drawing the different editors and I don't know why.
            var labelWidth = GetLabelWidth(inspectorWidth, 200);
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

                            EditorGUI.BeginChangeCheck();
                            if (selectedEditor.target is MonoBehaviour || selectedEditor.target is ScriptableObject) {
                                EditorGUIUtility.labelWidth = labelWidth;
                                EditorGUIUtility.fieldWidth = fieldWidth;
                                selectedEditor.OnInspectorGUI();
                            } else {
                                EditorGUIUtility.labelWidth = labelWidth;
                                EditorGUIUtility.fieldWidth = fieldWidth;
#if UNITY_2020_1_OR_NEWER
                                selectedEditor.OnInspectorGUI();
                                //selectedEditor.DrawDefaultInspector();
#else
                                selectedEditor.DrawDefaultInspector();
#endif
                            }

                            if (EditorGUI.EndChangeCheck()) {
                                string assetPath = AssetDatabase.GetAssetPath(SelectedObject.Object);
                                if (PrefabInstance != null) {
                                    PrefabUtility.SaveAsPrefabAsset(PrefabInstance as GameObject, assetPath);
                                }
                            }

                            DrawUILine(Color.gray);
                            EditorGUILayout.Space();
                        }
                    }
                }
            }

            EditorGUI.EndChangeCheck();
        }

        private float GetLabelWidth(float totalWidth, float minWidth)
        {
            if (totalWidth <= minWidth) {
                return totalWidth;
            }

            return Mathf.Min(minWidth, totalWidth);
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

        public void DrawComponentPreview(Object unityObject)
        {
            var drawRect = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
            var previewTexture = AssetPreview.GetMiniThumbnail(unityObject);

            if (previewTexture != null) {
                GUI.DrawTexture(drawRect, previewTexture);
            }
        }

        public void ChangeSelectedType(BonaDataEditorType editorType)
        {
            if (editorType == null) {
                titleContent = new GUIContent("Data Editor", Resources.Load<Texture>("DataEditorIcon"));
                return;
            }

            SelectedType = editorType;
            SelectedTypeName = editorType.FullClassName;

            UpdateFoundObjects(editorType);
            titleContent = new GUIContent(editorType.DisplayName.text.Substring(0, Mathf.Min(name.Length, 10)), Resources.Load<Texture>("DataEditorIcon"));
            FilteredObjects = FoundObjects;
            FilterString = string.Empty;
            ClearAllEditors();
            SelectedObject = null;

            Repaint();
        }

        public void UpdateFoundObjects(BonaDataEditorType editorType, bool force = false)
        {
            if(CachedObjects.ContainsKey(editorType) && !force) {
                FoundObjects = CachedObjects[editorType];
            } else {
                FoundObjects = FindAssetsOfType(editorType.Type).OrderBy(a => a.Name).ToList();
                CachedObjects.Add(editorType, FoundObjects);
            }
        }

        public List<BonaDataEditorAsset> FindAssetsOfType(System.Type type)
        {
            var result = new List<BonaDataEditorAsset>();
            if (typeof(ScriptableObject).IsAssignableFrom(type)) {
                result = FindScriptableObjectOfType(type);
            } else if (typeof(Component).IsAssignableFrom(type)) {
                result = FindPrefabsWithComponentType(type);
            }

            return result;
        }

        public List<BonaDataEditorAsset> FindScriptableObjectOfType(System.Type type)
        {
            var allValidAsset = AssetDatabase.FindAssets(string.Format("t:{0}", type.FullName));
            return allValidAsset
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                .Select(p => new BonaDataEditorAsset(p))
                .ToList();
        }

        private List<BonaDataEditorAsset> FindPrefabsWithComponentType(System.Type type)
        {
            var allGameObjectAssetPaths = AssetDatabase.FindAssets("t:GameObject")
                .Select(g => AssetDatabase.GUIDToAssetPath(g))
                .Where(p => p.EndsWith(".prefab"))
                .ToList();

            return allGameObjectAssetPaths
                .Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p))
                .Where(a => HasComponent(a, type))
                .Select(a => new BonaDataEditorAsset(a))
                .ToList();
        }

        private bool HasComponent(GameObject gameObject, System.Type type)
        {
            return gameObject.GetComponents<Component>()
                .Where(t => type.IsInstanceOfType(t))
                .Any();
        }

        public List<BonaDataEditorAsset> FilterObjects(List<BonaDataEditorAsset> startCollection, string filter)
        {
            if (filter == string.Empty) {
                return startCollection;
            }

            return startCollection.Where(o => o.Name.ToLower().Contains(filter.ToLower())).ToList();
        }

        public void ChangeSelectedObject(BonaDataEditorAsset selectedObject)
        {
            if (selectedObject == null) {
                return;
            }

            if (selectedObject == SelectedObject) {
                return;
            }

            SelectedObjectIndex = FilteredObjects.IndexOf(selectedObject);

#if UNITY_2018_3_OR_NEWER
            if (selectedObject.Object is GameObject) {
                // Unload previous
                if (PrefabInstance != null) {
                    PrefabUtility.UnloadPrefabContents(PrefabInstance as GameObject);
                }
                PrefabInstance = PrefabUtility.LoadPrefabContents(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(selectedObject.GetObject()));
                SelectedObject = selectedObject;
                CreateEditors(PrefabInstance);
            } else {
                PrefabInstance = null;
                SelectedObject = selectedObject;
                CreateEditors(SelectedObject.GetObject());
            }
#else
            SelectedObject = selectedObject;
            PrefabInstance = null;
            CreateEditors(SelectedObject);
#endif
            GUI.FocusControl(null);
        }

        public Editor GetOrCreateEditorFortarget(Object target)
        {
            if (target == null) {
                throw new System.ArgumentNullException("Tried to create editor for object or component that is null");
            }

            var result = Editor.CreateEditor(target);
            AllEditors.Add(result);
            return result;
        }

        public void CreateEditors(Object selectedObject)
        {
            if (selectedObject == null) {
                SelectedObject = null;
                return;
            }

            SelectedObjectHeaderEditor = Editor.CreateEditor(selectedObject);
            AllEditors.Add(SelectedObjectHeaderEditor);
            if (selectedObject is GameObject) {
                var gameObject = selectedObject as GameObject;
                var components = gameObject.GetComponents<Component>();
                SelectedObjectEditors = new List<Editor>();
                for (int i = 0; i < components.Length; i++) {
                    var editor = GetOrCreateEditorFortarget(components[i]);
                    SelectedObjectEditors.Add(editor);
                }
            } else {
                SelectedObjectEditors = new List<Editor> { Editor.CreateEditor(selectedObject) };
            }
        }
    }
}