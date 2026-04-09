using System.Collections;
using System.Collections.Generic;
using Nox.Servers;

namespace api.nox.server
{
	public class ServerGateway : Dictionary<string, string>, IServerGateway
	{
		private ServerGateway(Dictionary<string, string> dict) : base(dict) { }

		public static ServerGateway From(Dictionary<string, string> dict)
			=> dict == null ? null : new ServerGateway(dict);

		public string Api 
			=> TryGetValue("api", out var v) 
				? v 
				: null;

		public string Web 
			=> TryGetValue("web", out var v) 
				? v 
				: null;

		public string Ws 
			=> TryGetValue("ws", out var v) 
				? v 
				: null;

		public string Wellknown 
			=> TryGetValue("wellknown", out var v) 
				? v 
				: null;
	}
}
