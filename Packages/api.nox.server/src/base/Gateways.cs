using System;
using Nox.Servers;

namespace api.nox.server {
	[Serializable]
	public class Gateways : IGateways {
		public string ws;
		public string http;
		public string web;

		public string GetHttp()
			=> http;

		public string GetWeb()
			=> web;

		public string GetWs()
			=> ws;
	}
}