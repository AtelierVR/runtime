using System;
using Nox.Servers;

namespace api.nox.server {
	// ReSharper disable InconsistentNaming
	[Serializable]
	public class Server : IServer {
		public string   id;
		public string   title;
		public string   description;
		public string   address;
		public Gateways gateways;
		public string[] features;
		public string   icon;
		public string   version;
		public ulong    ready_at;
		public string   certificate;

		public string GetId()
			=> id;

		public string GetTitle()
			=> title;

		public string GetDescription()
			=> description;

		public string GetAddress()
			=> address;

		public IGateways GetGateways()
			=> gateways;

		public string[] GetFeatures()
			=> features;

		public string GetIconUrl()
			=> icon;

		public Version GetVersion()
			=> Version.Parse(version);

		public DateTime GetReadyAt()
			=> DateTimeOffset.FromUnixTimeSeconds((long)ready_at).UtcDateTime;

		public string GetCertificate() {
			if (string.IsNullOrEmpty(certificate)) return null;
			if (certificate.StartsWith("-----BEGIN CERTIFICATE-----\n"))
				return certificate;

			var lines = new string[certificate.Length / 64 + 1];
			for (var i = 0; i < lines.Length; i++) {
				var start  = i * 64;
				var length = Math.Min(64, certificate.Length - start);
				lines[i] = certificate.Substring(start, length);
			}

			return "-----BEGIN CERTIFICATE-----\n"
				+ string.Join("\n", lines)
				+ "\n-----END CERTIFICATE-----";
		}

		public override string ToString()
			=> $"{GetType().Name}[id={id}, title={title}, address={address}, version={version}]";
	}
}