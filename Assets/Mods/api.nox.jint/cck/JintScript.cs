using System;
using UnityEngine;

namespace Nox.CCK.Jint {
	[Serializable, DisallowMultipleComponent]
	public class JintScript : MonoBehaviour {
		/// <summary>
		/// The Jint script asset that this script is based on.
		/// Is a .js file.
		/// </summary>
		public JintFile asset;

		/// <summary>
		/// Is a Json string that contains the exports of the Jint script.
		/// </summary>
		public string exports;

		private IJintBacking _backing;

		public IJintBacking Backing
			=> _backing ??= GetComponent<IJintBacking>();

		public void InvokeFunction(string functionName, params object[] args)
			=> Backing?.Invoke(functionName, args);

		public object CallFunction(string functionName, params object[] args)
			=> Backing?.Call(functionName, args);

		public T CallFunction<T>(string functionName, params object[] args)
			=> Backing != null
				? Backing.Call<T>(functionName, args)
				: default;

		public void Start()
			=> InvokeFunction("onStart");

		public void Update()
			=> InvokeFunction("onUpdate");

		public void FixedUpdate()
			=> InvokeFunction("onFixedUpdate");

		public void LateUpdate()
			=> InvokeFunction("onLateUpdate");

		public void OnDestroy()
			=> InvokeFunction("onDestroy");

		public void OnEnable()
			=> InvokeFunction("onEnable");

		public void OnDisable()
			=> InvokeFunction("onDisable");

		public void OnValidate()
			=> InvokeFunction("onValidate");
	}
}