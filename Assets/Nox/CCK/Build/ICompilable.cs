using Cysharp.Threading.Tasks;

namespace Nox.CCK.Build {
	public interface ICompilable {
		int CompileOrder
			=> 1000;

		void Compile() { }

		UniTask CompileAsync()
			=> UniTask.CompletedTask;
	}
}