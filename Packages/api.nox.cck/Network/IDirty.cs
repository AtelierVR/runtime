namespace Nox.CCK.Network {
	public interface IDirty {
		/// <summary>
		/// Check if the object is dirty (has been modified).
		/// </summary>
		/// <returns></returns>
		public DirtyBy GetDirty();

		/// <summary>
		/// Mark the object as dirty or clean.
		/// </summary>
		/// <param name="dirty"></param>
		public void SetDirty(DirtyBy dirty);
	}
}