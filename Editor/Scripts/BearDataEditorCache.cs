using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CollisionBear.BearDataEditor
{
    [CreateAssetMenu(fileName = "AssetCache", menuName = "DataEditor/Asset Cache")]
    public class BearDataEditorCache : ScriptableObject
    {
        public const string CachePath = "Assets/Editor/Resources/";
        public const string FileName = "DataEditorCache.asset";
        public const string CacheFilePath = CachePath + FileName;

        public static BearDataEditorCache GetCacheIndex()
        {
            var cacheFile = AssetDatabase.LoadMainAssetAtPath(CacheFilePath) as BearDataEditorCache;
            return cacheFile;
        }

        public static BearDataEditorCache CreateCacheIndex()
        {
            var cacheIndex = CreateInstance<BearDataEditorCache>();
            cacheIndex.UpdateCache();

            if (!AssetDatabase.IsValidFolder(CachePath)) {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(CacheFilePath));
            }

            AssetDatabase.CreateAsset(cacheIndex, CacheFilePath);
            return cacheIndex;
        }

        [System.Serializable]
        public class AssetSummary
        {
            public string AssetGUID;
            public string Name;
            public List<ComponentSummary> Components = new List<ComponentSummary>();
        }

        [System.Serializable]
        public class ComponentSummary
        {
            public string Name;
            public string ScriptGuid;
            public int InstanceId;
        }

        [System.Serializable]
        public class ScriptIndexEntry
        {
            public string ScriptGuid;
            public List<string> AssetGuids = new List<string>();
        }

        public List<AssetSummary> CompleteAssetCache = new List<AssetSummary>();
        public List<ScriptIndexEntry> ScriptIndex = new List<ScriptIndexEntry>();
        public void UpdateCache()
        {
            CacheAllAssets();
            BuildScriptIndex();

            EditorUtility.SetDirty(this);
        }

        public List<string> GetPrefabsWithComponentGuid(string guid)
        {
            // TODO: Replace with binary search if slow
            var entry = ScriptIndex.FirstOrDefault(i => i.ScriptGuid == guid);
            if (entry == null) {
                return new List<string>();
            } else {
                return entry.AssetGuids;
            }
        }

        private void CacheAllAssets()
        {
            CompleteAssetCache.Clear();

            var allAssetGuids = AssetDatabase.FindAssets("t:GameObject").ToList();

            foreach (var guid in allAssetGuids) {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!assetPath.EndsWith(".prefab")) {
                    continue;
                }

                var loadedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                var assetSummary = new AssetSummary { AssetGUID = guid, Name = loadedAsset.name };

                var components = loadedAsset.GetComponents<Component>().Where(c => c is MonoBehaviour).Select(c => c as MonoBehaviour);
                if (!components.Any()) {
                    continue;
                }

                foreach (var component in components) {
                    var monoScript = MonoScript.FromMonoBehaviour(component);
                    var assetGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(monoScript));
                    assetSummary.Components.Add(new ComponentSummary { InstanceId = component.GetInstanceID(), ScriptGuid = assetGuid, Name = component.GetType().Name });
                }

                CompleteAssetCache.Add(assetSummary);
            }
        }

        private void BuildScriptIndex()
        {
            var tmpDictionary = new Dictionary<string, List<string>>();

            foreach (var asset in CompleteAssetCache) {
                foreach (var component in asset.Components) {
                    if (!tmpDictionary.ContainsKey(component.ScriptGuid)) {
                        tmpDictionary.Add(component.ScriptGuid, new List<string>());
                    }

                    tmpDictionary[component.ScriptGuid].Add(asset.AssetGUID);
                }
            }

            ScriptIndex.Clear();

            foreach (var entry in tmpDictionary.OrderBy(e => e.Key)) {
                ScriptIndex.Add(new ScriptIndexEntry { ScriptGuid = entry.Key, AssetGuids = entry.Value });
            }
        }
    }
}