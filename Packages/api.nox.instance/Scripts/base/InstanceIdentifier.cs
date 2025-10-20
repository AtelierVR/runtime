using System.Collections.Generic;
using Nox.CCK.Utils;
using Nox.Instances;

namespace api.nox.instance {
	public class InstanceIdentifier : INoxObject, IInstanceIdentifier {
		private const uint   InvalidId   = 0;
		private const string LocalServer = "::";

		private readonly string                       _name;
		private readonly uint                         _id;
		private readonly Dictionary<string, string[]> _metadata;
		internal         string                       Server;

		public InstanceIdentifier(uint id, Dictionary<string, string[]> meta = null, string server = LocalServer) {
			_id       = id;
			Server    = server;
			_metadata = meta ?? new Dictionary<string, string[]>();
			_name     = null;
		}

		public InstanceIdentifier(string name, Dictionary<string, string[]> meta = null, string server = LocalServer) {
			_name     = name;
			Server    = server;
			_metadata = meta ?? new Dictionary<string, string[]>();
			_id       = InvalidId;
		}

		public Dictionary<string, string[]> GetMetadata()
			=> _metadata;

		public bool IsValid()
			=> IsId() || IsName();

		public bool IsLocal()
			=> Server == LocalServer;

		public bool IsId()
			=> _id != InvalidId;

		public bool IsName()
			=> !string.IsNullOrEmpty(_name);

		public string GetName()
			=> IsName() ? _name : null;


		public uint GetId()
			=> IsId() ? _id : InvalidId;

		public bool TryGetId(out uint id) {
			if (IsId()) {
				id = _id;
				return true;
			}

			id = InvalidId;
			return false;
		}

		public bool TryGetName(out string name) {
			if (IsName()) {
				name = _name;
				return true;
			}

			name = null;
			return false;
		}

		public string ToString(string fallbackServer = null)
			=> $"{(IsName() ? _name : _id.ToString())}{(IsLocal() ? string.IsNullOrEmpty(fallbackServer) ? "" : "@" + fallbackServer : "@" + Server)}";

		public string GetServerAddress()
			=> Server;

		public bool Equals(IInstanceIdentifier other) {
			var sameServer = other.IsLocal() && IsLocal()
				|| !other.IsLocal()          && !IsLocal() && other.GetServerAddress() == GetServerAddress();
			if (!sameServer) return false;
			if (IsId() && other.IsId())
				return GetId() == other.GetId();
			if (IsName() && other.IsName())
				return GetName() == other.GetName();
			return false;
		}

		public bool Equals(string other)
			=> Equals(FromString(other));

		public static InstanceIdentifier FromBase(IInstanceIdentifier identifier) {
			if (identifier == null) return null;
			if (identifier.IsId())
				return new InstanceIdentifier(identifier.GetId(), identifier.GetMetadata(), identifier.GetServerAddress());
			return identifier.IsName()
				? new InstanceIdentifier(identifier.GetName(), identifier.GetMetadata(), identifier.GetServerAddress())
				: null;
		}

		public static InstanceIdentifier FromString(string identifier) {
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

			var split = parts[0].Split('?');
			if (split.Length > 2)
				return null;
			var metadata = new Dictionary<string, string[]>();

			if (split.Length > 1) {
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
			}

			var idPart = split[0];
			return !uint.TryParse(idPart, out var id)
				? new InstanceIdentifier(idPart, metadata, parts[1])
				: new InstanceIdentifier(id, metadata, parts[1]);
		}
	}
}