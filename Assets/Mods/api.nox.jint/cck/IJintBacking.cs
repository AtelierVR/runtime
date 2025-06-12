using UnityEngine;

namespace Nox.CCK.Jint {
	public interface IJintBacking {
		void Invoke(string functionName, JintScript script, params object[] args);

		object Call(string functionName, JintScript script, object[] args);

		T Call<T>(string functionName, JintScript script, object[] args);
	}
}