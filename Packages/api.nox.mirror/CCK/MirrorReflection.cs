using UnityEngine;

namespace Nox.CCK.Mirror {
	// Exemple de script simplifié
	public class MirrorReflection : MonoBehaviour {
		public Camera        referenceCamera;  // caméra à refléter
		public Camera        reflectionCamera; // caméra utilisée pour le render
		public RenderTexture renderTexture;

		void Start() {
			reflectionCamera.targetTexture = renderTexture;
		}

		void LateUpdate() {
			// Plan du miroir : défini par la normale (ici supposée up) et la position de l'objet
			Vector3 normal = transform.up;
			Vector3 pos    = transform.position;

			// Calculer position réfléchie
			Vector3 camPos   = referenceCamera.transform.position;
			float   distance = Vector3.Dot(normal, camPos - pos);
			Vector3 reflPos  = camPos - 2f * distance * normal;
			reflectionCamera.transform.position = reflPos;

			// Calculer orientation réfléchie (on inverse l'axe de roulis par rapport à la normale)
			Vector3 camEuler = referenceCamera.transform.eulerAngles;
			reflectionCamera.transform.eulerAngles = new Vector3(-camEuler.x, camEuler.y, camEuler.z);

			// Mettre à jour matrices (montrer l'idée, peut aussi utiliser CalculateObliqueMatrix)
			// Pour couper l'espace derrière le miroir :
			Vector4   clipPlane  = new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, pos));
			Matrix4x4 projection = referenceCamera.CalculateObliqueMatrix(clipPlane);
			reflectionCamera.projectionMatrix = projection;

			// Inverser le culling pour ce rendu
			GL.invertCulling = true;
			reflectionCamera.Render();
			GL.invertCulling = false;

			// Appliquer la RenderTexture au matériau du miroir
			GetComponent<Renderer>().sharedMaterial.mainTexture = renderTexture;
		}
	}
}