using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using UnityEngine.Rendering.Universal;
using RenderPipeline = UnityEngine.Rendering.RenderPipelineManager;
using static UnityEngine.Camera;
using System;
using Nox.CCK.Utils;

namespace Nox.CCK.Mirror {
	public class Mirror : MonoBehaviour {
		[Header("Main Settings")]
		public Renderer Renderer;
		public Material MirrorsMaterial;
		
		[Header("Camera Settings")]
		[Tooltip("Caméra à utiliser pour le rendu. Si null, utilisera Camera.main")]
		public Camera SourceCamera;

		[SerializeField]
		private LayerMask ReflectingLayers;

		public float ClipPlaneOffset = 0.001f;
		public float nearClipLimit   = 0.01f;
		public float FarClipPlane    = 25f;
		public int   XSize           = 2048;
		public int   YSize           = 2048;
		public int   depth           = 24;
		public int   Antialiasing    = 2;

		[Header("Options")]
		public bool allowXRRendering = true;
		public bool RenderPostProcessing = false;
		public bool OcclusionCulling     = false;
		public bool renderShadows        = false;

		[Header("Debug / Runtime")]
		public bool IsActive;
		public bool IsAbleToRender;
		public static bool InsideRendering;

		[Header("Cameras")]
		public Camera LeftCamera;
		public Camera RightCamera;
		public RenderTexture PortalTextureLeft;
		public RenderTexture PortalTextureRight;

		public Action OnCamerasRenderering;
		public Action OnCamerasFinished;

		private Vector3   thisPosition;
		private Vector3   normal;
		private Vector3   projectionDirection = -Vector3.forward;
		private Matrix4x4 scaledMatrix;
		private int       instanceID;

		private void OnEnable() {
			IsActive = false;
			IsAbleToRender = false;

			// Validation préliminaire
			if (!ValidateComponents()) {
				Debug.LogError("Mirror: Validation des composants échouée", this);
				return;
			}

			// Configurer les layers avant l'initialisation
			EnsureLayers();
			
			// Générer automatiquement le matériau s'il n'existe pas
			EnsureMirrorMaterial();

			// Obtenir la caméra source avec fallback robuste
			Camera targetCamera = GetTargetCameraWithFallback();
			if (targetCamera == null) {
				Debug.LogError("Mirror: Aucune caméra disponible pour l'initialisation", this);
				return;
			}

			// Initialiser le miroir
			if (!Initialize(targetCamera)) {
				Debug.LogError("Mirror: Échec de l'initialisation", this);
				return;
			}

			instanceID = gameObject.GetInstanceID();

			// S'abonner aux événements seulement si l'initialisation a réussi
			RenderPipeline.beginCameraRendering += UpdateCamera;
			
			Debug.Log($"Mirror activé avec succès - Caméra source: {targetCamera.name}", this);
		}

		private bool ValidateComponents() {
			if (Renderer == null) {
				Debug.LogError("Mirror: Aucun Renderer assigné. Veuillez assigner un Renderer dans l'inspecteur.", this);
				return false;
			}

			if (Renderer.transform == null) {
				Debug.LogError("Mirror: Le Renderer n'a pas de Transform valide.", this);
				return false;
			}

			return true;
		}

		private void EnsureMirrorMaterial() {
			if (MirrorsMaterial) {
				if (Renderer && Renderer.material != MirrorsMaterial)
					Renderer.material = MirrorsMaterial;
				return;
			}

			var shader = CreateMirrorShader();
			if (shader != null) {
				MirrorsMaterial = new Material(shader) {
					name = $"MirrorMaterial_{GetInstanceID()}"
				};

				if (Renderer)
					Renderer.material = MirrorsMaterial;
			}
		}

		private Shader CreateMirrorShader() {
			// Priorité 1: Essayer le shader URP Unlit standard
			var urpUnlitShader = Shader.Find("Universal Render Pipeline/Unlit");
			if (urpUnlitShader != null) return urpUnlitShader;

			// Priorité 2: Essayer le shader Unlit standard
			var unlitShader = Shader.Find("Unlit/Texture");
			if (unlitShader != null) return unlitShader;

			// Priorité 3: Essayer le shader sprites URP
			var spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
			if (spriteShader != null) return spriteShader;

			// Priorité 4: Shader standard comme fallback
			var standardShader = Shader.Find("Standard");
			if (standardShader != null) return standardShader;

			// Dernier recours: Built-in diffuse
			return Shader.Find("Legacy Shaders/Diffuse");
		}

		private Camera GetTargetCameraWithFallback() {
			// Priorité 1: Caméra assignée explicitement
			if (SourceCamera != null) return SourceCamera;

			// Priorité 3: Camera.main par défaut
			if (Camera.main != null) return Camera.main;

			// Priorité 4: Chercher n'importe quelle caméra active dans la scène
			Camera[] cameras = FindObjectsOfType<Camera>();
			foreach (Camera cam in cameras) {
				if (cam.enabled && cam.gameObject.activeInHierarchy) {
					return cam;
				}
			}

			return null;
		}

		private void OnDisable() {
			CleanUp();
		}

		private void OnDestroy() {
			RenderPipeline.beginCameraRendering -= UpdateCamera;
		}

		private void CleanUp() {
			if (PortalTextureLeft) DestroyImmediate(PortalTextureLeft);
			if (PortalTextureRight) DestroyImmediate(PortalTextureRight);
			if (LeftCamera) Destroy(LeftCamera.gameObject);
			if (RightCamera) Destroy(RightCamera.gameObject);
		}

		private void EnsureLayers() {
			if (ReflectingLayers != 0) return;

			int defaultLayer = LayerMask.NameToLayer("Default");
			//
			// if (remoteLayer < 0 || localLayer < 0 || defaultLayer < 0) {
			// 	Debug.LogError("One or more required layers are missing.");
			// } else {
			// 	ReflectingLayers = (1 << remoteLayer) | (1 << localLayer) | (1 << defaultLayer);
			// }
		}

		private bool Initialize(Camera sourceCamera) {
			if (sourceCamera == null) {
				Debug.LogError("Mirror: Aucune caméra source disponible pour l'initialisation", this);
				return false;
			}

			if (Renderer == null) {
				Debug.LogError("Mirror: Aucun renderer assigné au miroir", this);
				return false;
			}

			scaledMatrix = Matrix4x4.Scale(new Vector3(-1, 1, 1));

			CreatePortalCamera(sourceCamera, StereoscopicEye.Left, ref LeftCamera, ref PortalTextureLeft);
			CreatePortalCamera(sourceCamera, StereoscopicEye.Right, ref RightCamera, ref PortalTextureRight);

			// Vérifier que les caméras ont été créées correctement
			if (LeftCamera == null || RightCamera == null) {
				Debug.LogError("Mirror: Échec de la création des caméras portal", this);
				IsActive = false;
				IsAbleToRender = false;
				return false;
			}

			// Vérifier que les RenderTextures ont été créées
			if (PortalTextureLeft == null || PortalTextureRight == null) {
				Debug.LogError("Mirror: Échec de la création des RenderTextures", this);
				IsActive = false;
				IsAbleToRender = false;
				return false;
			}

			// Assigner les textures au matériau maintenant que tout est créé
			AssignTexturesToMaterial();

			// Le renderer n'a pas besoin d'être visible au moment de l'initialisation
			IsAbleToRender = true;
			IsActive = true;
			InsideRendering = false;
			
			Debug.Log($"Mirror initialisé avec succès - Caméras: {LeftCamera.name}, {RightCamera.name}", this);
			return true;
		}

		private void UpdateCamera(ScriptableRenderContext context, Camera camera) {
			if (!IsAbleToRender || !IsActive) return;
			if (!IsCameraAble(camera)) return;

			OnCamerasRenderering?.Invoke();

			thisPosition = Renderer.transform.position;
			normal       = Renderer.transform.TransformDirection(projectionDirection);

			UpdateCameraState(context, camera);

			OnCamerasFinished?.Invoke();
		}

		private bool IsCameraAble(Camera camera) {
			// Vérifier si c'est la caméra source assignée ou Camera.main
			Camera targetCamera = GetTargetCameraWithFallback();
			return targetCamera != null && camera.GetInstanceID() == targetCamera.GetInstanceID();
		}

		private void UpdateCameraState(ScriptableRenderContext context, Camera camera) {
			if (InsideRendering) return;
			InsideRendering = true;

			camera.transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

			if (camera.stereoEnabled) {
				RenderCamera(camera, MonoOrStereoscopicEye.Left, context, position, rotation);
				RenderCamera(camera, MonoOrStereoscopicEye.Right, context, position, rotation);
			} else {
				RenderCamera(camera, MonoOrStereoscopicEye.Mono, context, position, rotation);
			}

			InsideRendering = false;
		}

		private void RenderCamera(Camera sourceCamera, MonoOrStereoscopicEye eye, ScriptableRenderContext context, Vector3 srcPosition, Quaternion srcRotation) {
			Camera portalCamera = (eye == MonoOrStereoscopicEye.Right) ? RightCamera : LeftCamera;
			if (!portalCamera) return;

			Vector3 eyeOffset;
			Matrix4x4 projMatrix;

			if (eye == MonoOrStereoscopicEye.Mono) {
				eyeOffset = srcPosition;
				projMatrix = sourceCamera.projectionMatrix;
			} else {
				StereoscopicEye Eye = (StereoscopicEye)eye;
				eyeOffset = sourceCamera.GetStereoViewMatrix(Eye).inverse.MultiplyPoint(Vector3.zero);
				projMatrix = sourceCamera.GetStereoProjectionMatrix(Eye);
			}

			transform.GetPositionAndRotation(out Vector3 TransformPosition, out Quaternion Rotation);

			Vector3 localEyeOffset = InverseTransformPointCustom(TransformPosition, Rotation, eyeOffset);
			Vector3 reflectedForward = Vector3.Reflect(InverseTransformDirectionCustom(Rotation, srcRotation * Vector3.forward), Vector3.forward);
			Vector3 reflectedUp = Vector3.Reflect(InverseTransformDirectionCustom(Rotation, srcRotation * Vector3.up), Vector3.forward);
			Vector3 reflectedPos = Vector3.Reflect(localEyeOffset, Vector3.forward);

			Quaternion reflectedRotation = Quaternion.LookRotation(reflectedForward, reflectedUp);
			portalCamera.transform.SetLocalPositionAndRotation(reflectedPos, reflectedRotation);

			Vector4 clipPlane = CameraHelper.SpacePlane(portalCamera.worldToCameraMatrix, thisPosition, normal, ClipPlaneOffset);
			clipPlane.x *= -1;

			CalculateObliqueMatrix(ref projMatrix, clipPlane);
			portalCamera.projectionMatrix = scaledMatrix * projMatrix * scaledMatrix;

			#pragma warning disable CS0618
			UniversalRenderPipeline.RenderSingleCamera(context, portalCamera);
			#pragma warning restore CS0618
		}

		private Vector3 InverseTransformDirectionCustom(Quaternion rotation, Vector3 direction) {
			// Inverse transform the direction by the rotation only (ignore position)
			return Quaternion.Inverse(rotation) * direction;
		}

		private Vector3 InverseTransformPointCustom(Vector3 position, Quaternion rotation, Vector3 point) {
			// Subtract the position, then remove rotation
			return Quaternion.Inverse(rotation) * (point - position);
		}

		/// <summary>
		/// Calculates an oblique projection matrix
		/// </summary>
		public static void CalculateObliqueMatrix(ref Matrix4x4 projection, float4 clipPlane) {
			// Compute the clip-space corner point opposite the clipping plane
			float4 q = projection.inverse * new float4(math.sign(clipPlane.x), math.sign(clipPlane.y), 1.0f, 1.0f);
			// Calculate the scaled plane vector
			float dot = math.dot(clipPlane, q);
			if (dot == 0.0f) {
				return; // avoid divide-by-zero just in case
			}

			float4 c = clipPlane * (2.0f / dot);
			// Replace the third row of the projection matrix
			projection[2]  = c.x - projection[3];
			projection[6]  = c.y - projection[7];
			projection[10] = c.z - projection[11];
			projection[14] = c.w - projection[15];
		}

		private void CreatePortalCamera(Camera sourceCamera, StereoscopicEye eye, ref Camera portalCamera, ref RenderTexture portalTexture) {
			portalTexture = new RenderTexture(XSize, YSize, 0, RenderTextureFormat.Default) {
				name         = $"__MirrorReflection{eye}{GetInstanceID()}",
				isPowerOfTwo = true,
				antiAliasing = 1,
				depth        = depth
			};
			
			// Créer la RenderTexture avec les bonnes propriétés
			portalTexture.Create();

			CreateNewCamera(sourceCamera, out portalCamera);
			if (portalCamera == null) {
				Debug.LogError($"Mirror: Échec de la création de la caméra {eye}", this);
				return;
			}
			
			portalCamera.targetTexture = portalTexture;

			Debug.Log($"Mirror: RenderTexture créée - {portalTexture.name} ({portalTexture.width}x{portalTexture.height})", this);
		}

		private void AssignTexturesToMaterial() {
			if (!Renderer || !MirrorsMaterial) {
				Debug.LogWarning("Mirror: Impossible d'assigner les textures - Renderer ou MirrorsMaterial manquant", this);
				return;
			}

			// Assigner les textures selon le type de shader
			if (MirrorsMaterial.shader.name == "Nox/Mirror") {
				// Shader personnalisé avec support stéréo
				if (PortalTextureLeft != null) {
					MirrorsMaterial.SetTexture("_ReflectionTexLeft", PortalTextureLeft);
					Debug.Log("Mirror: Texture Left assignée au shader Nox/Mirror", this);
				}
				if (PortalTextureRight != null) {
					MirrorsMaterial.SetTexture("_ReflectionTexRight", PortalTextureRight);
					Debug.Log("Mirror: Texture Right assignée au shader Nox/Mirror", this);
				}
				if (PortalTextureLeft != null) {
					MirrorsMaterial.SetTexture("_ReflectionTexMono", PortalTextureLeft);
					Debug.Log("Mirror: Texture Mono assignée au shader Nox/Mirror", this);
				}
			} else {
				// Shader standard - utiliser la texture Left comme texture principale
				if (PortalTextureLeft != null) {
					MirrorsMaterial.SetTexture("_MainTex", PortalTextureLeft);
					Debug.Log($"Mirror: Texture Left assignée à _MainTex du shader {MirrorsMaterial.shader.name}", this);
				}
				
				// Essayer d'autres propriétés communes
				if (PortalTextureLeft != null && MirrorsMaterial.HasProperty("_BaseMap")) {
					MirrorsMaterial.SetTexture("_BaseMap", PortalTextureLeft);
					Debug.Log("Mirror: Texture assignée à _BaseMap (URP)", this);
				}
			}

			// S'assurer que le matériau est assigné au renderer
			if (Renderer.material != MirrorsMaterial) {
				Renderer.material = MirrorsMaterial;
				Debug.Log("Mirror: Matériau assigné au renderer", this);
			}
		}

		private void CreateNewCamera(Camera sourceCamera, out Camera newCamera) {
			GameObject camObj = new GameObject($"MirrorCam_{GetInstanceID()}_{sourceCamera.GetInstanceID()}", typeof(Camera));

			camObj.transform.SetParent(transform);
			newCamera = camObj.GetComponent<Camera>();
			newCamera.enabled = false;
			newCamera.CopyFrom(sourceCamera);
			newCamera.depth = 2;
			newCamera.farClipPlane = FarClipPlane;
			newCamera.cullingMask = ReflectingLayers;
			newCamera.useOcclusionCulling = OcclusionCulling;

			if (newCamera.TryGetComponent(out UniversalAdditionalCameraData cameraData)) {
				cameraData.allowXRRendering = allowXRRendering;
				cameraData.renderPostProcessing = RenderPostProcessing;
				cameraData.renderShadows = renderShadows;
			}
		}

		private void VisibilityFlag(bool isVisible) {
			IsAbleToRender = isVisible;
		}
	}
}
