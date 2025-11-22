#if UNITY_EDITOR
namespace api.nox.avatar.builder {
	public class BuildResult {
		public BuildResultType Type;
		public string          Message;
		public string          Output;

		public bool IsFailed
			=> Type.HasFlag(BuildResultType.Failed);
	}
}
#endif