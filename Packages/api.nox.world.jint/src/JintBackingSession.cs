using Jint.Native.Object;
using Nox.CCK.Jint;
using UnityEngine;

namespace api.nox.session.jint {
	public class JintBackingSession : MonoBehaviour, IJintBacking {
		public JintBackingModule module;
		public JintScript        script;
		public ObjectInstance    Context;

		public void Invoke(string method, params object[] args)
			=> module.Invoke(this, method, args);

		public object Call(string method, object[] args)
			=> module.Call(this, method, args);

		public T Call<T>(string method, object[] args)
			=> module.Call<T>(this, method, args);
	}
}