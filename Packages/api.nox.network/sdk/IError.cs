namespace Nox.Network {
	public interface IError {
		public uint   GetCode();
		public string GetMessage();
		public ushort GetStatus();
	}
}