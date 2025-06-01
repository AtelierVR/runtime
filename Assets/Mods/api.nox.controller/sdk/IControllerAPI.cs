namespace Nox.Controllers {
	public interface IControllerAPI {
		/// <summary>
		/// Gets the current controller.
		/// </summary>
		/// <returns></returns>
		public IController GetCurrent();

		/// <summary>
		/// Sets the current controller.
		/// </summary>
		/// <param name="controller"></param>
		/// <returns>True if the controller was set successfully, false otherwise.</returns>
		public bool SetCurrent(IController controller);
	}
}