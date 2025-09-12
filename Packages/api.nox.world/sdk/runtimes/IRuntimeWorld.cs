using Cysharp.Threading.Tasks;
using Nox.CCK.Worlds;

namespace Nox.Worlds {
	/// <summary>
	/// Represents the world in which all scenes are loaded.
	/// </summary>
	public interface IRuntimeWorld {
		/// <summary>
		/// Returns the world identifier.
		/// </summary>
		/// <returns></returns>
		public IWorldIdentifier GetIdentifier();

		/// <summary>
		/// Sets the world identifier.
		/// </summary>
		/// <param name="identifier"></param>
		public void SetIdentifier(IWorldIdentifier identifier);
		
		/// <summary>
		/// Returns all loaded scenes in the world.
		/// </summary>
		/// <returns></returns>
		public IRuntimeWorldInstance[] GetInstances();
		
		/// <summary>
		/// Returns the scene at the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public IRuntimeWorldInstance GetInstance(int index);

		/// <summary>
		/// Returns the number of scenes in the world.
		/// </summary>
		/// <returns></returns>
		public int GetInstanceCount();

		/// <summary>
		/// Makes the world current.
		/// </summary>
		public void SetCurrent();

		/// <summary>
		/// Checks if the world is current.
		/// </summary>
		/// <returns></returns>
		public bool IsCurrent();

		/// <summary>
		/// Disposes the world and unloads all scenes.
		/// </summary>
		/// <returns></returns>
		public UniTask Dispose();
	}
}