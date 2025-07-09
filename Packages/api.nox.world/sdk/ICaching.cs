using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine.Events;

namespace Nox.Worlds {
	public interface ICaching {
		public void    Cancel();
		public UniTask Start();
		public bool    IsRunning();
		UniTask        Wait();

		public UnityEvent<float> GetProgressEvent();

		public float GetProgress();
	}
}