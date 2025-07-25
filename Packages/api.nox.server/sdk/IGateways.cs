namespace Nox.Servers {
	public interface IGateways {
		/// <summary>
		/// Get the HTTP gateway URL (origin).
		/// </summary>
		/// <returns></returns>
		public string GetHttp();

		/// <summary>
		/// Get the WebSocket gateway URL.
		/// </summary>
		/// <returns></returns>
		public string GetWs();

		/// <summary>
		/// Home page URL of the frontend (origin).
		/// </summary>
		/// <returns></returns>
		public string GetWeb();
	}
}