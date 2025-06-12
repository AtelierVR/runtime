using Nox.CCK.Utils;
using Nox.Users;

namespace api.nox.user {
	public class UserIdentifier : INoxObject, IUserIdentifier {
		private const uint   InvalidId   = 0;
		private const string LocalServer = "::";

		private readonly string _username;
		private readonly uint   _id;
		internal         string Server;

		public UserIdentifier(uint id, string server = LocalServer) {
			_id       = id;
			Server    = server;
			_username = null;
		}

		public UserIdentifier(string username, string server = LocalServer) {
			_username = username;
			Server    = server;
			_id       = InvalidId;
		}

		public bool IsValid()
			=> IsId() || IsUsername();

		public bool IsLocal()
			=> Server == LocalServer;

		public bool IsId()
			=> _id != InvalidId;

		public bool IsUsername()
			=> !string.IsNullOrEmpty(_username);

		public string GetUsername()
			=> IsUsername() ? _username : null;


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

		public bool TryGetUsername(out string username) {
			if (IsUsername()) {
				username = _username;
				return true;
			}

			username = null;
			return false;
		}

		public string ToString(string fallbackServer = null)
			=> $"{(IsUsername() ? _username : _id.ToString())}{(IsLocal() ? string.IsNullOrEmpty(fallbackServer) ? "" : "@" + fallbackServer : "@" + Server)}";

		public string GetServerAddress()
			=> Server;

		public static UserIdentifier FromBase(IUserIdentifier identifier) {
			if (identifier == null) return null;
			if (identifier.IsId())
				return new UserIdentifier(identifier.GetId(), identifier.GetServerAddress());
			return identifier.IsUsername()
				? new UserIdentifier(identifier.GetUsername(), identifier.GetServerAddress())
				: null;
		}

		public static UserIdentifier FromString(string identifier) {
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

			if (uint.TryParse(parts[0], out var id))
				return new UserIdentifier(id, parts[1]);
			return !string.IsNullOrEmpty(parts[0])
				? new UserIdentifier(parts[0], parts[1])
				: null;
		}
	}
}