using Nox.Sessions;

namespace api.nox.offline {
	public class OfflineState : IAdapterState {
		public OfflineState(bool isReady, string message = "", float progress = 1f) {
			_isReady  = isReady;
			_message  = message;
			_progress = progress;
		}

		private readonly string _message;
		private readonly float  _progress;
		private readonly bool   _isReady;

		public bool IsReady()
			=> _isReady;

		public string GetMessage()
			=> _message;

		public float GetProgress()
			=> _progress;

		public override string ToString()
			=> $"{GetType().Name}[IsReady={_isReady}, Message=\"{_message}\", Progress={_progress}]";
	}
}