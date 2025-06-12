using System.Collections.Generic;
using Nox.Worlds;

namespace api.nox.world {
	public class WorldIdentifier : IWorldIdentifier {
		private const uint   InvalidId   = 0;
		private const string LocalServer = "::";

		private readonly uint                         _id;
		internal         string                       Server;
		private          Dictionary<string, string[]> _metadata;

		public WorldIdentifier(uint id, Dictionary<string, string[]> meta = null, string server = LocalServer) {
			_id       = id;
			Server    = server;
			_metadata = meta ?? new Dictionary<string, string[]>();
		}

		public bool IsValid()
			=> _id != InvalidId;

		public bool IsLocal()
			=> Server == LocalServer;

		public uint GetId()
			=> IsValid() ? _id : InvalidId;


		public string ToString(string fallbackServer = null)
			=> $"{_id.ToString()}{(IsLocal() ? string.IsNullOrEmpty(fallbackServer) ? "" : "@" + fallbackServer : "@" + Server)}";

		public string GetServerAddress()
			=> Server;

		public Dictionary<string, string[]> GetMetadata()
			=> _metadata;

		public static WorldIdentifier FromBase(IWorldIdentifier identifier) {
			if (identifier == null) return null;
			return new WorldIdentifier(
				identifier.GetId(),
				identifier.GetMetadata(),
				identifier.GetServerAddress()
			);
		}

		public static WorldIdentifier FromString(string identifier) {
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
				return new WorldIdentifier(id, metadata, parts[1]);

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

			return new WorldIdentifier(id, metadata, parts[1]);
		}
	}
}