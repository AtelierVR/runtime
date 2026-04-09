using System.Collections;
using System.Collections.Generic;
using Nox.Servers;

namespace api.nox.server
{
	public class ServerVersions : Dictionary<string, string>, IServerVersions
	{
		private ServerVersions(Dictionary<string, string> dict) : base(dict) { }

		public static ServerVersions From(Dictionary<string, string> dict)
			=> dict == null ? null : new ServerVersions(dict);

		public string Node
			=> TryGetValue("node", out var v) ? v : null;
	}
}
