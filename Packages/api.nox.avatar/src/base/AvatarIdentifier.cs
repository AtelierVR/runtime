using System;
using System.Text.RegularExpressions;
using Nox.Avatars;
using Nox.CCK.Utils;
using UnityEngine;

namespace api.nox.avatar.network {
	[Serializable]
	public class AvatarIdentifier : IAvatarIdentifier, INoxObject {
		private const string IDENTIFIER_PATTERN = @"^(?:([^@]+)@)?(\d+)$";

		public string Server { get; set; } = "";
		public uint   Id     { get; set; } = 0;

		public AvatarIdentifier() { }

		public AvatarIdentifier(string server, uint id) {
			Server = server ?? "";
			Id     = id;
		}

		public AvatarIdentifier(uint id) {
			Server = "";
			Id     = id;
		}

		public static AvatarIdentifier FromString(string str) {
			if (string.IsNullOrEmpty(str))
				return null;

			var match = Regex.Match(str, IDENTIFIER_PATTERN);
			if (!match.Success)
				return null;

			var serverGroup = match.Groups[1];
			var idGroup     = match.Groups[2];

			if (!uint.TryParse(idGroup.Value, out var id))
				return null;

			var server = serverGroup.Success ? serverGroup.Value : "";
			return new AvatarIdentifier(server, id);
		}

		public bool IsValid() {
			return Id > 0;
		}

		public bool IsLocal() {
			return string.IsNullOrEmpty(Server) || Server == "::" || Server == "localhost";
		}

		public string GetServerAddress()
			=> Server;

		public override string ToString() {
			if (IsLocal())
				return Id.ToString();
			return $"{Server}@{Id}";
		}

		public override bool Equals(object obj) {
			if (obj is AvatarIdentifier other) {
				return Server == other.Server && Id == other.Id;
			}

			return false;
		}

		public override int GetHashCode() {
			return HashCode.Combine(Server, Id);
		}

		public string ToJson() {
			return JsonUtility.ToJson(this);
		}
	}
}