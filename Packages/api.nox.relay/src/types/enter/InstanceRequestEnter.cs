using System;
using api.nox.relay.types.Instance;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Enter {
	public class InstanceRequestEnter : RelayInstanceRequest {

		public EnterFlags Flags;
		public string     Display;
		public string     Password;

		public override Buffer ToBuffer() {
			var buffer = new Buffer();
			buffer.Write(InternalId);

			if (string.IsNullOrEmpty(Display))
				Flags  &= ~EnterFlags.UsePseudonyme;
			else Flags |= EnterFlags.UsePseudonyme;

			if (string.IsNullOrEmpty(Password))
				Flags  &= ~EnterFlags.UsePassword;
			else Flags |= EnterFlags.UsePassword;

			buffer.Write(Flags);

			if (Flags.HasFlag(EnterFlags.UsePseudonyme))
				buffer.Write(Display);

			if (Flags.HasFlag(EnterFlags.UsePassword))
				buffer.Write(Password);
			
			return buffer;
		}
	}
}