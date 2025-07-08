using System.Collections.Generic;
using Nox.Instances;

namespace api.nox.instance {
	public class Connection : IConnection {
		private const    string                     Method = "relay";
		private readonly Dictionary<string, object> _data;

		public string GetMethod()
			=> Method;

		public Connection(string address)
			=> _data = new Dictionary<string, object> {
				{ "address", address },
				{ "connections", new[] { "udp://" + address, "tcp://" + address } }
			};


		public Dictionary<string, object> GetData()
			=> _data;
	}
}