using Nox.Users;

namespace Nox.CCK.Users {
	public readonly struct UserIdentifier : IUserIdentifier {
		public static UserIdentifier Invalid
			=> new(InvalidId);

		private const uint InvalidId = 0;
		private const string LocalServer = "::";

		private readonly string _username;
		private readonly uint _id;
		private readonly string _server;

		public UserIdentifier(uint id, string server = LocalServer) {
			_id = id;
			_server = server;
			_username = null;
		}

		public UserIdentifier(string username, string server = LocalServer) {
			_username = username;
			_server = server;
			_id = InvalidId;
		}

		public bool IsValid()
			=> IsId() || IsUsername();

		public bool IsLocal()
			=> _server == LocalServer;

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

		public string ToString(string fallbackServer)
			=> $"{(IsUsername() ? _username : _id.ToString())}{(IsLocal() ? string.IsNullOrEmpty(fallbackServer) ? "" : "@" + fallbackServer : "@" + _server)}";

		public string GetServer()
			=> _server;

		public bool Equals(IUserIdentifier other) {
			var sameServer = other.IsLocal() && IsLocal()
				|| !other.IsLocal() && !IsLocal() && other.GetServer() == GetServer();
			if (!sameServer) return false;
			if (IsId() && other.IsId())
				return GetId() == other.GetId();
			if (IsUsername() && other.IsUsername())
				return GetUsername() == other.GetUsername();
			return false;
		}

		public bool Equals(string other)
			=> Equals(From(other));

		private bool Equals(UserIdentifier identifier)
			=> Equals((IUserIdentifier)identifier);

		public static UserIdentifier FromBase(IUserIdentifier identifier) {
			if (identifier == null) return null;
			if (identifier.IsId())
				return new UserIdentifier(identifier.GetId(), identifier.GetServer());
			return identifier.IsUsername()
				? new UserIdentifier(identifier.GetUsername(), identifier.GetServer())
				: null;
		}

		public static UserIdentifier From(string identifier) {
			if (string.IsNullOrEmpty(identifier))
				return Invalid;

			var parts = identifier.Split('@');
			switch (parts.Length) {
				case > 2:
					return Invalid;
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
				: Invalid;
		}

		/// <summary>
		/// Implicit conversion to string
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns></returns>
		public static implicit operator string(UserIdentifier identifier)
			=> identifier.ToString();

		/// <summary>
		/// Implicit conversion from string
		/// </summary>
		/// <param name="identifier"></param>
		/// <returns></returns>
		public static implicit operator UserIdentifier(string identifier)
			=> From(identifier);

		public override string ToString()
			=> ToString(null);
	}
}