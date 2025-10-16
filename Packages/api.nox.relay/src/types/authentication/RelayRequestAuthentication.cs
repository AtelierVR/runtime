using Nox.CCK.Utils;
using Nox.Users;

namespace api.nox.relay.types.Authentication {
	public class RelayRequestAuthentication : RelayRequest {
		public AuthenticationAction Action;

		public byte[] PublicKey; // Client's public key (for RequestChallenge)
		public byte[] Signature; // Signature of the challenge (for ResolveChallenge)
		public uint   UserId;    // User ID (for ResolveChallenge)
		public string Server;    // Server address (for ResolveChallenge)

		public static RelayRequestAuthentication CreateRequest()
			=> new() {
				Action = AuthenticationAction.RequestChallenge,
			};

		public static RelayRequestAuthentication CreateResponse(byte[] publicKey, byte[] signature, uint userId, string server)
			=> new() {
				Action    = AuthenticationAction.ResolveChallenge,
				PublicKey = publicKey,
				Signature = signature,
				UserId    = userId,
				Server    = server,
			};

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			buffer.Write(Action);
			if (Action != AuthenticationAction.ResolveChallenge)
				return buffer;
			buffer.Write((ushort)PublicKey.Length);
			buffer.Write(PublicKey);
			buffer.Write((ushort)Signature.Length);
			buffer.Write(Signature);
			buffer.Write(UserId);
			buffer.Write(Server);
			return buffer;
		}
	}
}