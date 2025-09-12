using System;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using UnityEditor;

#if UNITY_EDITOR
namespace api.nox.world.builder {
	public class BuildData {
		public WorldDescriptor       Descriptor;
		public bool                  ShowDialog;
		public string                OutputPath;
		public Platform              Target;
		public string                Filename;
		public string                TempPath;
		public Action<float, string> ProgressCallback;
	}
}
#endif