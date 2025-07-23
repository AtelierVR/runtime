using Nox.Worlds;

namespace Nox.Sessions {
	public interface IDimension {
		/// <summary>
		/// Get the id of the dimension.
		/// The default is "default", for the first/main scene added, use "default" as id.
		/// </summary>
		/// <returns></returns>
		public string GetName();

		/// <summary>
		/// Index of the instance scene for the session.
		/// If is 0, the dimension not have own instance scene.
		/// </summary>
		/// <returns></returns>
		public int GetMainIndex();

		/// <summary>
		/// Get the scene of the dimension.
		/// </summary>
		/// <returns></returns>
		public IScene GetScene();
		
		/// <summary>
		/// Indicate if the dimension is active.
		/// </summary>
		/// <returns></returns>
		public bool   IsActive();
	}
}