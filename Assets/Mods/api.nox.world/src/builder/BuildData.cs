using Nox.CCK.Worlds;
using UnityEditor;

#if UNITY_EDITOR
namespace api.nox.world.builder {
	public class BuildData {
		public MainSceneDescriptor Descriptor;
		public bool                ShowDialog;
		public string              OutputPath;
		public BuildTarget         Target;
	}
}
#endif