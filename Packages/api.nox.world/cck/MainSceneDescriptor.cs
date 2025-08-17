using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Utils;
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
			=> scenes ?? Array.Empty<string>();
		#endif

		#endregion

		#region Publisher

		#if UNITY_EDITOR
		public Platform target;
		public uint     publishId;
		public string   publishServer;
		public uint     publishVersion;
		#endif

		#endregion

		#region Build

		#if UNITY_EDITOR
		
		int CompileOrder
			=> 9999;
		
		public override void Compile() {
			if (target == Platform.None)
				target = PlatformExtensions.CurrentPlatform;
			sceneAssets = this.EstimateScenes().Values.ToList();
			sceneAssets.RemoveAt(0);
			scenes = GetScenes().ToArray();
			base.Compile();
		}
		#endif

		#endregion
	}
}