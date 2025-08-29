using System.Collections.Generic;
using Nox.Avatars;

namespace api.nox.avatar {
	public class AvatarIdentifier : IAvatarIdentifier {
		private const uint   InvalidId   = 0;
		public const  string LocalServer = "::";

		private readonly uint                         _id;
		internal         string                       Server;
		private          Dictionary<string, string[]> _metadata;

		public AvatarIdentifier(uint id, Dictionary<string, string[]> meta = null, string server = LocalServer) {
			_id       = id;
			Server    = server;
			_metadata = meta ?? new Dictionary<string, string[]>();
		}

		public bool IsValid()
			=> _id != InvalidId;

		public bool IsLocal()
			=> Server == LocalServer || string.IsNullOrEmpty(Server);

		public uint GetId()
			=> IsValid() ? _id : InvalidId;


		public string ToString(string fallbackServer = null)
			=> $"{_id.ToString()}{(IsLocal() ? string.IsNullOrEmpty(fallbackServer) ? "" : "@" + fallbackServer : "@" + Server)}";

		public string GetServerAddress()
			=> Server;

		public Dictionary<string, string[]> GetMetadata()
			=> _metadata;

		public static AvatarIdentifier FromBase(IAvatarIdentifier identifier) {
			if (identifier == null) return null;
			return new AvatarIdentifier(
				identifier.GetId(),
				identifier.GetMetadata(),
				identifier.GetServerAddress()
			);
		}

		public static AvatarIdentifier FromString(string identifier) {
			if (string.IsNullOrEmpty(identifier))
				return null;

			var parts = identifier.Split('@');
			switch (parts.Length) {
				case > 2:
					return null;
				case 1:
					parts = new[] { parts[0], null };
					break;
			}

			if (string.IsNullOrEmpty(parts[1]))
				parts[1] = LocalServer;

			var split = parts[0].Split('?');
			if (split.Length > 2)
				return null;
			var idPart = split[0];
			if (!uint.TryParse(idPart, out var id))
				return null;
			var metadata = new Dictionary<string, string[]>();
			if (split.Length != 2)
				return new AvatarIdentifier(id, metadata, parts[1]);

			var metaParts = split[1].Split('&');
			foreach (var part in metaParts) {
				var metaSplit = part.Split('=');
				if (metaSplit.Length < 1)
					continue;
				var key   = metaSplit[0];
				var value = metaSplit.Length > 1 ? string.Join("=", metaSplit, 1, metaSplit.Length - 1) : null;
				if (string.IsNullOrEmpty(key))
					continue;
				if (metadata.TryGetValue(key, out var values)) {
					var newValues = new string[values.Length + 1];
					values.CopyTo(newValues, 0);
					newValues[^1] = value;
					metadata[key] = newValues;
				} else metadata[key] = new[] { value };
			}

			return new AvatarIdentifier(id, metadata, parts[1]);
		}

		public ushort GetVersion() {
			if (_metadata.TryGetValue("v", out var versions) && versions.Length > 0 && ushort.TryParse(versions[0], out var version))
				return version;
			return ushort.MaxValue; // Default value if no version is set
		}

		public void SetVersion(ushort version) {
			_metadata.Remove("v");
			if (version == ushort.MaxValue) return; // Do not set version if it's the default value
			_metadata["v"] = new[] { version.ToString() };
		}

		public bool Equals(IAvatarIdentifier other) {
			if (other == null) return false;
			if (ReferenceEquals(this, other)) return true;
			return _id == other.GetId() && Server == other.GetServerAddress();
		}
	}
}