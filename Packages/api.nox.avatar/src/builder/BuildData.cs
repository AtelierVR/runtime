using System;
using Nox.CCK.Avatars;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using UnityEditor;

#if UNITY_EDITOR
namespace api.nox.avatar.builder {
	public class BuildData {
		public AvatarDescriptor      Descriptor;
		public bool                  ShowDialog;
		public string                OutputPath;
		public Platform              Target;
		public string                Filename;
		public string                TempPath;
		public Action<float, string> ProgressCallback;
	}
}
#endif