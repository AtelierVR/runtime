using System.Collections.Generic;

namespace Nox.YantraJS {
	public interface IYantraScript {
		string GetContent();

		Dictionary<string, object> GetExports();

		#if UNITY_EDITOR
		void SetExports(Dictionary<string, object> exports);
		#endif
	}
}

