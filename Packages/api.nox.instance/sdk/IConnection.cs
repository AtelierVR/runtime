using System.Collections.Generic;

namespace Nox.Instances {
	public interface IConnection {
		public string                     GetMethod();
		public Dictionary<string, object> GetData();
	}
}