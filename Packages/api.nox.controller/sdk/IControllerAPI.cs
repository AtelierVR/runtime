using Cysharp.Threading.Tasks;
using UnityEngine.Events;

namespace Nox.Controllers {
	public interface IControllerAPI {
		/// <summary>
		/// Gets the current controller.
		/// </summary>
		/// <returns></returns>
		public IController Current { get; }

		/// <summary>
		/// Invoked when the current controller changes.
		/// </summary>
		public UnityEvent<IController> OnCurrentChanged { get; }

		/// <summary>
		/// Sets the current controller.
		/// </summary>
		/// <param name="controller"></param>
		/// <returns>True if the controller was set successfully, false otherwise.</returns>
		public UniTask<bool> SetCurrent(IController controller);
	}
}