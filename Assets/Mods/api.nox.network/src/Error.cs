using System;
using Nox.Network;

namespace api.nox.network {
	[Serializable]
	public class Error : IError {
		public string message;
		public uint   code;
		public ushort status;

		public uint GetCode()
			=> code;

		public string GetMessage()
			=> message;

		public ushort GetStatus()
			=> status;

		public override string ToString()
			=> $"{GetType().Name}[status={status}, code={code}, message={message}]";
	}
}