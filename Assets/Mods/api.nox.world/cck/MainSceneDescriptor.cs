using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Nox.CCK.Worlds {
	public class MainSceneDescriptor : BaseSceneDescriptor {
		#region Scene Assets

		public string[] scenes;

		#if UNITY_EDITOR
		public List<SceneAsset> sceneAssets;

		public string[] GetScenes()
			=> GetSceneAssets()
				.ToList()
				.ConvertAll(AssetDatabase.GetAssetPath)
				.ToArray();

		public SceneAsset[] GetSceneAssets()
			=> sceneAssets?.ToArray()
				?? Array.Empty<SceneAsset>();
		#else
		public string[] GetScenes() 
			=> scenes 
				?? Array.Empty<string>();
		#endif

		#endregion
	}
}