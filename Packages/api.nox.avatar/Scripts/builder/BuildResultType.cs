#if UNITY_EDITOR
using System;

namespace api.nox.avatar.builder {
	[Flags]
	public enum BuildResultType {
		Success,
		AlreadyBuilding,
		EditorCompiling,
		EditorPlaying,
		UnsupportedTarget,
		InvalidTarget,
		InvalidGameObject,
		Failed = AlreadyBuilding | EditorCompiling | EditorPlaying,
	}
}
#endif