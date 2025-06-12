using UnityEditor.AssetImporters;
using UnityEngine;

namespace Nox.CCK.Worlds {
	[ScriptedImporter(1, "js")]
	public class JintImporter : ScriptedImporter {
		public override void OnImportAsset(AssetImportContext ctx) {
			var asset = ScriptableObject.CreateInstance<JintFile>();
			asset.text = System.IO.File.ReadAllText(ctx.assetPath);
			ctx.AddObjectToAsset("main", asset);
			ctx.SetMainObject(asset);
			ctx.DependsOnSourceAsset(ctx.assetPath);
		}
	}
}