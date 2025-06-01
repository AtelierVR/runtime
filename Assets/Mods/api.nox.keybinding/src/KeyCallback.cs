using System;
using UnityEngine.InputSystem;

namespace api.nox.keybinding {
	public class KeyCallback {
		public KeyCallback(Action<object> callback)
			=> Callback = callback;

		internal readonly Action<object> Callback;

		public void OnPerformed(InputAction.CallbackContext context)
			=> Callback.Invoke(context.ReadValueAsObject());

		public void OnCanceled(InputAction.CallbackContext context)
			=> Callback.Invoke(context.ReadValueAsObject());

		public void OnStarted(InputAction.CallbackContext context)
			=> Callback.Invoke(context.ReadValueAsObject());
	}
}