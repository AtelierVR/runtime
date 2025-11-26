using System;
using UnityEngine;

namespace FFmpeg.Unity.Helpers {
	public abstract class BaseWorker : MonoBehaviour {
		public virtual void Pause()  { }
		public virtual void Resume() { }
		public virtual void Seek()   { }

		public long pts;
	}
}