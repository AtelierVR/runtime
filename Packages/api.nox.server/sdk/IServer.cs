using System;

namespace Nox.Servers {
	public interface IServer {
		/// <summary>
		/// Identifier to distinguish the sub-server for an address.
		/// </summary>
		/// <returns></returns>
		public string GetId();

		/// <summary>
		/// Get the title.
		/// </summary>
		/// <returns></returns>
		public string GetTitle();

		/// <summary>
		/// Get the description.
		/// </summary>
		/// <returns></returns>
		public string GetDescription();

		/// <summary>
		/// Get list of useful urls.
		/// </summary>
		/// <returns></returns>
		public IGateways GetGateways();

		/// <summary>
		/// List of features supported by the server.
		/// </summary>
		/// <returns></returns>
		public string[] GetFeatures();

		/// <summary>
		/// Get the version.
		/// </summary>
		/// <returns></returns>
		public Version GetVersion();

		/// <summary>
		/// Get the time when the server is ready to requests.
		/// </summary>
		/// <returns></returns>
		public DateTime GetReadyAt();

		/// <summary>
		/// Get the icon URL.
		/// </summary>
		/// <returns></returns>
		public string GetIconUrl();

		/// <summary>
		/// Get the rsa certificate.
		/// </summary>
		/// <returns></returns>
		public string GetCertificate();
	}
}