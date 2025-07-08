using System;
using Nox.CCK.Utils;
using Nox.Network;

namespace api.nox.network {
	[Serializable]
	public class Response<T> : INoxObject, IResponse<T> {
		public Error error;
		public T     data;

		public IError GetError()
			=> error;

		public bool HasError()
			=> error is { status: > 0 } or { code: > 0 };

		public bool HasData()
			=> data != null;

		public T GetData()
			=> data;
	}
}