#if UNITY_EDITOR
namespace api.nox.world.builder {
	public class BuildResult {
		public BuildResultType Type;
		public string          Message;

		public bool IsFailed
			=> Type.HasFlag(BuildResultType.Failed);
	}
}
#endif