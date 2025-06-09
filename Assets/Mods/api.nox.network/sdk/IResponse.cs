namespace Nox.Network {
	public interface IResponse<out T> {
		public IError GetError();
		public bool   HasError();
		public bool   HasData();
		public T      GetData();
	}
}