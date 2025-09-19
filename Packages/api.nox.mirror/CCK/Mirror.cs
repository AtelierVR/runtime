using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.CCK.Mirror {
	//http://wiki.unity3d.com/index.php?title=MirrorReflection4
	// Based on the script of Slaynash (MetrixVR)
	public class Mirror : MonoBehaviour {
		public bool      m_DisablePixelLights = true;
		public int       m_TextureSize        = 1024;
		public float     m_ClipPlaneOffset    = 0.00f;
		public LayerMask m_ReflectLayers      = -1;
		public bool      invertCulling        = false;

		//Mirror lock
		private static bool isRenderingMirror;

		//Updated once
		private Camera mirrorCam;
		private Skybox mirrorSkybox;

		//Updated every render
		private Matrix4x4  parentTMat;
		private Quaternion parentRotation;

		private Dictionary<Camera, RenderData> m_Reflections = new Dictionary<Camera, RenderData>();
		private RenderData[]                   reflectionDatas;
		private int[]                          textureShaderId = new int[2];

		private void Start() {
			Renderer component      = base.GetComponent<Renderer>();
			Material sharedMaterial = component.sharedMaterial;
			sharedMaterial.shader = Shader.Find("Nox/MirrorShader");
			textureShaderId[0]    = Shader.PropertyToID("_LeftEyeTexture");
			textureShaderId[1]    = Shader.PropertyToID("_RightEyeTexture");
		}

		private void Update() {
			// Force update du miroir même quand seule la vue Game est active
			var rend = GetComponent<Renderer>();
			if (!enabled || !rend || !rend.enabled)
				return;

			// Priorité à la caméra principale (joueur) plutôt qu'à Camera.current
			var cam       = Camera.main;
			if (!cam) cam = Camera.current;

			if (!cam || cam == mirrorCam)
				return;

			if (isRenderingMirror)
				return;

			// Vérifier si la caméra est visible dans le frustum du miroir
			if (!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(cam), rend.bounds))
				return;

			RenderMirror(cam, rend);
		}

		private void RenderMirror(Camera cam, Renderer rend) {
			isRenderingMirror = true;
			if (!mirrorCam) InitCamera(cam);
			UpdateCameraConfig(cam);
			UpdateParentTransform(cam);
			var rdatas = GetRenderDatas(cam);

			if (cam.stereoEnabled) {
				if (cam.stereoTargetEye == StereoTargetEyeMask.Left || cam.stereoTargetEye == StereoTargetEyeMask.Both) {
					Vector3    worldEyePos         = GetWorldEyePos(cam, XRNode.LeftEye);
					Quaternion worldEyeRot         = GetWorldEyeRot(cam, XRNode.LeftEye);
					Matrix4x4  eyeProjectionMatrix = GetEyeProjectionMatrix(cam, XRNode.LeftEye);
					UpdateAndRenderCamera(cam, rdatas.textures[0], worldEyePos, worldEyeRot, eyeProjectionMatrix);
				}

				if (cam.stereoTargetEye == StereoTargetEyeMask.Right || cam.stereoTargetEye == StereoTargetEyeMask.Both) {
					Vector3    worldEyePos         = GetWorldEyePos(cam, XRNode.RightEye);
					Quaternion worldEyeRot         = GetWorldEyeRot(cam, XRNode.RightEye);
					Matrix4x4  eyeProjectionMatrix = GetEyeProjectionMatrix(cam, XRNode.RightEye);
					UpdateAndRenderCamera(cam, rdatas.textures[1], worldEyePos, worldEyeRot, eyeProjectionMatrix);
				}
			} else {
				UpdateAndRenderCamera(cam, rdatas.textures[0], cam.transform.position, cam.transform.rotation, cam.projectionMatrix);
			}

			rend.SetPropertyBlock(rdatas.propertyBlock);
			isRenderingMirror = false;
		}

		private void OnWillRenderObject() {
			var rend = GetComponent<Renderer>();
			if (!enabled || !rend || !rend.enabled)
				return;

			// Priorité à la caméra principale (joueur) plutôt qu'à Camera.current
			var cam       = Camera.main;
			if (!cam) cam = Camera.current;

			if (!cam || cam == mirrorCam)
				return;

			if (isRenderingMirror)
				return;

			RenderMirror(cam, rend);
		}

		private void InitCamera(Camera src) {
			GameObject gameObject = new GameObject(
				"CameraMirror (" + base.gameObject.name + ")", new Type[] {
					typeof(Camera),
					typeof(Skybox),
					typeof(FlareLayer)
				}
			);
			gameObject.hideFlags = HideFlags.DontSave;
			mirrorSkybox         = gameObject.GetComponent<Skybox>();
			mirrorCam            = gameObject.GetComponent<Camera>();
			mirrorCam.enabled    = false;
		}

		private void UpdateCameraConfig(Camera src) {
			mirrorCam.clearFlags      = src.clearFlags;
			mirrorCam.backgroundColor = src.backgroundColor;
			if (src.clearFlags == CameraClearFlags.Skybox) {
				Skybox sky = src.GetComponent(typeof(Skybox)) as Skybox;
				if (!sky || !sky.material) {
					mirrorSkybox.enabled = false;
				} else {
					mirrorSkybox.enabled  = true;
					mirrorSkybox.material = sky.material;
				}
			}

			mirrorCam.farClipPlane        = src.farClipPlane;
			mirrorCam.nearClipPlane       = src.nearClipPlane;
			mirrorCam.orthographic        = src.orthographic;
			mirrorCam.fieldOfView         = src.fieldOfView;
			mirrorCam.aspect              = src.aspect;
			mirrorCam.orthographicSize    = src.orthographicSize;
			mirrorCam.useOcclusionCulling = true;
		}

		private void UpdateParentTransform(Camera cam) {
			if (cam.transform.parent != null) {
				parentTMat     = cam.transform.parent.localToWorldMatrix;
				parentRotation = cam.transform.parent.rotation;
			} else {
				Quaternion localRotation = InputTracking.GetLocalRotation(XRNode.Head);
				Matrix4x4  matrix4x      = Matrix4x4.TRS(InputTracking.GetLocalPosition(XRNode.Head), localRotation, Vector3.one);
				parentTMat     = cam.transform.localToWorldMatrix * matrix4x.inverse;
				parentRotation = cam.transform.rotation           * Quaternion.Inverse(localRotation);
			}
		}

		private RenderData GetRenderDatas(Camera cam) {
			RenderData renderData = null;
			if (!this.m_Reflections.TryGetValue(cam, out renderData)) {
				renderData               = new RenderData();
				renderData.propertyBlock = new MaterialPropertyBlock();
				m_Reflections[cam]       = renderData;
			}

			for (int i = 0; i < 2; i++) {
				if (i > 0 && !cam.stereoEnabled) {
					break;
				}

				int antialiasing = QualitySettings.antiAliasing > 0 ? QualitySettings.antiAliasing : 1;
				if (!renderData.textures[i] || renderData.textures[i].width != cam.pixelWidth || renderData.textures[i].height != cam.pixelHeight || renderData.textures[i].antiAliasing != antialiasing) {
					if (renderData.textures[i]) {
						DestroyImmediate(renderData.textures[i]);
					}

					renderData.textures[i]              = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 24);
					renderData.textures[i].antiAliasing = antialiasing;
					renderData.textures[i].hideFlags    = HideFlags.DontSave;
					renderData.propertyBlock.SetTexture(textureShaderId[i], renderData.textures[i]);
				}
			}

			return renderData;
		}

		private void UpdateAndRenderCamera(Camera cam, RenderTexture renderTarget, Vector3 cpos, Quaternion crot, Matrix4x4 pMatrix) {
			mirrorCam.ResetWorldToCameraMatrix();
			Vector3 pos    = transform.position;
			Vector3 normal = transform.up;

			float   d               = -Vector3.Dot(normal, pos) - m_ClipPlaneOffset;
			Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

			Matrix4x4 reflection = Matrix4x4.zero;
			CalculateReflectionMatrix(ref reflection, reflectionPlane);

			mirrorCam.transform.position = cpos;
			mirrorCam.transform.rotation = crot;
			mirrorCam.projectionMatrix   = pMatrix;

			mirrorCam.worldToCameraMatrix *= reflection;

			// Setup oblique projection matrix so that near plane is our reflection
			// plane. This way we clip everything below/above it for free.
			Vector4 clipPlane = CameraSpacePlane(mirrorCam, pos, normal, 1.0f);
			mirrorCam.projectionMatrix   = mirrorCam.CalculateObliqueMatrix(clipPlane);
			mirrorCam.transform.position = GetPosition(mirrorCam.cameraToWorldMatrix);
			mirrorCam.transform.rotation = GetRotation(mirrorCam.cameraToWorldMatrix);

			mirrorCam.cullingMask   = ~(1 << 4) & ~(1 << LayerMask.NameToLayer("MainCameraOnly")) & m_ReflectLayers.value; // never render water layer
			mirrorCam.targetTexture = renderTarget;

			if (invertCulling)
				GL.invertCulling = true;

			// Use RenderPipeline.SubmitRenderRequest instead of Camera.Render() to avoid URP context conflicts
			var renderRequest = new RenderPipeline.StandardRequest {
				destination = renderTarget
			};

			// Try the new approach first, fallback to old method if not available
			try {
				RenderPipeline.SubmitRenderRequest(mirrorCam, renderRequest);
			} catch (System.Exception) {
				// Fallback: Reset any existing render pipeline data before rendering
				mirrorCam.ResetWorldToCameraMatrix();
				mirrorCam.ResetProjectionMatrix();
				mirrorCam.projectionMatrix = mirrorCam.CalculateObliqueMatrix(clipPlane);
				mirrorCam.Render();
			}

			if (invertCulling)
				GL.invertCulling = false;
		}


		// Calculates reflection matrix around the given plane
		private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane) {
			reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
			reflectionMat.m01 = (-2F * plane[0] * plane[1]);
			reflectionMat.m02 = (-2F * plane[0] * plane[2]);
			reflectionMat.m03 = (-2F * plane[3] * plane[0]);

			reflectionMat.m10 = (-2F * plane[1] * plane[0]);
			reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
			reflectionMat.m12 = (-2F * plane[1] * plane[2]);
			reflectionMat.m13 = (-2F * plane[3] * plane[1]);

			reflectionMat.m20 = (-2F * plane[2] * plane[0]);
			reflectionMat.m21 = (-2F * plane[2] * plane[1]);
			reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
			reflectionMat.m23 = (-2F * plane[3] * plane[2]);

			reflectionMat.m30 = 0F;
			reflectionMat.m31 = 0F;
			reflectionMat.m32 = 0F;
			reflectionMat.m33 = 1F;
		}

		// Given position/normal of the plane, calculates plane in camera space.
		private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign) {
			Vector3   offsetPos = pos + normal * m_ClipPlaneOffset;
			Matrix4x4 m         = cam.worldToCameraMatrix;
			Vector3   cpos      = m.MultiplyPoint(offsetPos);
			Vector3   cnormal   = m.MultiplyVector(normal).normalized * sideSign;
			return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
		}


		private Vector3 GetWorldEyePos(Camera cam, XRNode eye) {
			Vector3 localPosition = InputTracking.GetLocalPosition(eye);
			return parentTMat.MultiplyPoint3x4(localPosition);
		}

		private Quaternion GetWorldEyeRot(Camera cam, XRNode eye) {
			Quaternion localRotation = InputTracking.GetLocalRotation(eye);
			return this.parentRotation * localRotation;
		}

		private Matrix4x4 GetEyeProjectionMatrix(Camera cam, XRNode eye) {
			return cam.GetStereoProjectionMatrix((eye != XRNode.RightEye) ? Camera.StereoscopicEye.Left : Camera.StereoscopicEye.Right);
		}


		private static Quaternion GetRotation(Matrix4x4 matrix) {
			Quaternion result = default(Quaternion);
			result.w = Mathf.Sqrt(Mathf.Max(0f, 1f                                        + matrix.m00 + matrix.m11 + matrix.m22)) / 2f;
			result.x = Mathf.Sqrt(Mathf.Max(0f, 1f              + matrix.m00              - matrix.m11 - matrix.m22))              / 2f;
			result.y = Mathf.Sqrt(Mathf.Max(0f, 1f - matrix.m00 + matrix.m11              - matrix.m22))                           / 2f;
			result.z = Mathf.Sqrt(Mathf.Max(0f, 1f              - matrix.m00 - matrix.m11 + matrix.m22))                           / 2f;
			result.x = _copysign(result.x, matrix.m21 - matrix.m12);
			result.y = _copysign(result.y, matrix.m02 - matrix.m20);
			result.z = _copysign(result.z, matrix.m10 - matrix.m01);
			return result;
		}

		private static Vector3 GetPosition(Matrix4x4 matrix) {
			float m  = matrix.m03;
			float m2 = matrix.m13;
			float m3 = matrix.m23;
			return new Vector3(m, m2, m3);
		}


		private static float _copysign(float sizeval, float signval)
			=> !Mathf.Approximately(Mathf.Sign(signval), 1f) ? -Mathf.Abs(sizeval) : Mathf.Abs(sizeval);

		private void OnDestroy() {
			if (mirrorCam) {
				DestroyImmediate(mirrorCam.gameObject);
				mirrorCam = null;
			}

			foreach (var rdata in m_Reflections.Select(kv => kv.Value))
				for (var i = 0; i < 2; i++) {
					if (!rdata.textures[i]) continue;
					DestroyImmediate(rdata.textures[i]);
					rdata.textures[i] = null;
				}

			m_Reflections.Clear();
		}


		private class RenderData {
			public   RenderTexture[]       textures = new RenderTexture[2];
			public   MaterialPropertyBlock matPBs;
			internal MaterialPropertyBlock propertyBlock;
		}
	}
}
