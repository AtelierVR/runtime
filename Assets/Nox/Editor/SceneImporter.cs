// detect scene unity file

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace Nox.Editor
{
    public class SceneImporter : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var asset in importedAssets)
                if (asset.EndsWith(".unity"))
                {
                    Debug.Log("Scene imported: " + asset);
                    var scene = SceneManager.GetSceneByPath(asset);
                    List<EditorBuildSettingsScene> originalScenes = EditorBuildSettings.scenes.ToList();
                    if (originalScenes.All(s => s.path != asset))
                    {
                        originalScenes.Add(new EditorBuildSettingsScene(asset, true));
                        EditorBuildSettings.scenes = originalScenes.ToArray();
                    }
                }

            foreach (var asset in deletedAssets)
                if (asset.EndsWith(".unity"))
                {
                    Debug.Log("Scene deleted: " + asset);
                    List<EditorBuildSettingsScene> originalScenes = EditorBuildSettings.scenes.ToList();
                    originalScenes.RemoveAll(s => s.path == asset);
                    EditorBuildSettings.scenes = originalScenes.ToArray();
                }

            foreach (var asset in movedAssets)
                if (asset.EndsWith(".unity"))
                {
                    Debug.Log("Scene moved: " + asset);
                    var scene = SceneManager.GetSceneByPath(asset);
                    List<EditorBuildSettingsScene> originalScenes = EditorBuildSettings.scenes.ToList();
                    originalScenes.RemoveAll(s => s.path == asset);
                    originalScenes.Add(new EditorBuildSettingsScene(asset, true));
                    EditorBuildSettings.scenes = originalScenes.ToArray();
                }

            foreach (var asset in movedFromAssetPaths)
                if (asset.EndsWith(".unity"))
                {
                    Debug.Log("Scene moved from: " + asset);
                    List<EditorBuildSettingsScene> originalScenes = EditorBuildSettings.scenes.ToList();
                    originalScenes.RemoveAll(s => s.path == asset);
                    EditorBuildSettings.scenes = originalScenes.ToArray();
                }

            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Distinct().ToArray();
            AssetDatabase.SaveAssets();
        }
    }
}