using UnityEngine;
using UnityEngine.Serialization;

namespace Hactazia.FFPlay {
	[RequireComponent(typeof(MeshRenderer))]
	public class VideoForMeshRenderer : MonoBehaviour {
		public VideoWorker worker;

		private MeshRenderer          _render;
		private MaterialPropertyBlock _block;

		public int    index    = -1;
		public string property = "_EmissionMap";

		public bool updateGI = true;

		private void Start() {
			worker ??= GetComponentInParent<VideoWorker>(true);

			if (!worker) {
				Debug.LogWarning("No FFTexturePlayer found.");
			}

			_block  = new MaterialPropertyBlock();
			_render = GetComponent<MeshRenderer>();
			worker.OnDisplay.AddListener(OnDisplay);
		}

		private void OnDestroy()
			=> worker?.OnDisplay.RemoveListener(OnDisplay);

		private void OnDisplay(Texture2D texture) {
			if (texture)
				_block.SetTexture(property, texture);

			if (!_render) return;

			if (index == -1)
				_render.SetPropertyBlock(_block);
			else _render.SetPropertyBlock(_block, index);

			if (updateGI)
				_render.UpdateGIMaterials();
		}
	}
}