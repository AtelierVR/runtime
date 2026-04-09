using System;
using System.Collections.Generic;
using Nox.CCK.Network;
using Nox.Servers;

namespace api.nox.server
{
	[Serializable]
	public class ServerEndpoints : Dictionary<string, string>, IServerEndpoints
	{
		private ServerEndpoints(IDictionary<string, string> dictionary) : base(dictionary) { }

		public string WellKnown
			=> TryGetValue("wellknown", out var v)
				? v
				: null;

		public string Webfinger
			=> TryGetValue("webfinger", out var v)
				? v
				: null;

		public string Nodeinfo
			=> TryGetValue("nodeinfo", out var v)
				? v
				: null;

		public static ServerEndpoints From(Dictionary<string, string> dict)
			=> dict == null ? null : new ServerEndpoints(dict);
	}
}
