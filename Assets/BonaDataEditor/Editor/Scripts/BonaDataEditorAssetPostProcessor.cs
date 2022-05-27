using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Fyrvall.DataEditor
{
    public class BonaDataEditorAssetPostProcessor : AssetPostprocessor
    {
        private static readonly List<string> AssetFileEndings = new List<string> { ".asset", ".prefab" };

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var changedAssets = new List<string>();
            changedAssets.AddRange(importedAssets);
            changedAssets.AddRange(deletedAssets);

            var editorsNeedUpdate = changedAssets.Any(a => AssetFileEndings.Contains(Path.GetExtension(a.ToLower())));
            if (editorsNeedUpdate) {
                foreach (var window in Resources.FindObjectsOfTypeAll<BonaDataEditorWindow>()) {
                    window.RefreshObjects();
                    window.Repaint();
                }
            }
        }
    }
}