#if UNITY_EDITOR
using System;

namespace api.nox.world.builder {
	[Flags]
	public enum BuildResultType {
		Success,
		AlreadyBuilding,
		EditorCompiling,
		EditorPlaying,
		UnsupportedTarget,
		InvalidTarget,
		InvalidScene,
		Failed = AlreadyBuilding | EditorCompiling | EditorPlaying,
	}
}
#endif