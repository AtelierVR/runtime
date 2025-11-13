namespace Nox.YantraJS {
	public interface IYantraBacking {
		public void Invoke(string method, params object[] args);

		public object Call(string method, object[] args);

		public T Call<T>(string method, object[] args);
	}
}

