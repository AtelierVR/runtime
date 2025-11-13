#if UNITY_EDITOR
using Nox.CCK.YantraJS;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace api.nox.yantra {
	// [ScriptedImporter(1, "js")]
	// public class FileImporter : ScriptedImporter {
	// 	public override void OnImportAsset(AssetImportContext ctx) {
	// 		Nox.CCK.Utils.Logger.LogDebug($"Importing YantraJS file: {ctx.assetPath}");
	// 		var asset = ScriptableObject.CreateInstance<YantraFile>();
	// 		asset.text = System.IO.File.ReadAllText(ctx.assetPath);
	// 		ctx.AddObjectToAsset("main", asset);
	// 		ctx.SetMainObject(asset);
	// 		ctx.DependsOnSourceAsset(ctx.assetPath);

	// 		// update all references to this asset
	// 		for (var i = 0; i < SceneManager.sceneCount; i++) {
	// 			var scene = SceneManager.GetSceneAt(i);
	// 			if (!scene.isLoaded) continue;
	// 			var objs = FindObjectsByType<YantraScript>(FindObjectsSortMode.None);
	// 			foreach (var obj in objs) {
	// 				Nox.CCK.Utils.Logger.LogDebug($"Checking YantraScript {obj.name} in scene {scene.name}");
	// 				if (!obj.asset || AssetDatabase.GetAssetPath(obj.asset) != ctx.assetPath) continue;
	// 				EditorUtility.SetDirty(obj);
	// 				obj.OnValidate();
	// 			}
	// 		}

	// 		// mark the asset as dirty
	// 	}
	// }
}
#endif

