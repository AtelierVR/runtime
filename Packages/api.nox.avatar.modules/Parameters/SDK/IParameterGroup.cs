namespace Nox.Avatars.Parameters {
	public interface IParameterGroup {
		public IParameter[] GetParameters();

		public IParameter GetParameter(string name);

		public IParameter GetParameter(int hash);
	}
}