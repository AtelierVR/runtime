using System.Collections.Generic;
using UnityEditor;

namespace Nox.CCK.Worlds {
	public class MainSceneDescriptor : BaseSceneDescriptor {
		public string[] scenes;
		
		#if UNITY_EDITOR
		public List<SceneAsset> sceneAssets;
		#endif
	}
}