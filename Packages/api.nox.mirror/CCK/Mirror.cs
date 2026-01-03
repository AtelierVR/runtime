using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace Nox.CCK.Mirror
{
    /// <summary>
    /// A high-quality planar mirror for URP with XR (VR/AR) support.
    /// The mirror uses transform.up as the reflection normal.
    /// For a vertical mirror, rotate the object so Y (green arrow) points toward the viewer.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class Mirror : MonoBehaviour
    {
        [Header("Mirror Settings")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _resolutionScale = 1f;
        
        [SerializeField] private int _maxResolution = 2048;
        
        [SerializeField] private LayerMask _reflectionLayers = -1;
        
        [Header("Performance")]
        [SerializeField] private bool _disablePixelLights = true;
        
        [Header("Rendering")]
        [SerializeField] private float _clipPlaneOffset = 0.07f;
        
        // Private fields
        private Camera _mirrorCamera;
        private RenderTexture _reflectionTextureLeft;
        private RenderTexture _reflectionTextureRight;
        private Renderer _renderer;
        private Material _mirrorMaterial;
        private MaterialPropertyBlock _propertyBlock;
        
        private bool _texturesAssigned;
        private int _lastTextureWidth;
        private int _lastTextureHeight;
        
        private static readonly int LeftEyeTextureId = Shader.PropertyToID("_LeftEyeTexture");
        private static readonly int RightEyeTextureId = Shader.PropertyToID("_RightEyeTexture");
        
        private static bool s_IsRendering;
        private const string MirrorShaderName = "Nox/MirrorShader";

        private void OnEnable()
        {
            _renderer = GetComponent<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();
            
            EnsureMirrorMaterial();
            
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            Cleanup();
        }

        private void EnsureMirrorMaterial()
        {
            if (_renderer == null) return;
            
            var currentMat = _renderer.sharedMaterial;
            if (currentMat != null && currentMat.shader != null && 
                currentMat.shader.name == MirrorShaderName)
                return;
            
            var shader = Shader.Find(MirrorShaderName);
            if (shader == null)
            {
                Debug.LogError($"[Mirror] Shader '{MirrorShaderName}' not found!");
                return;
            }
            
            _mirrorMaterial = new Material(shader)
            {
                name = "Mirror Material",
                hideFlags = HideFlags.DontSave
            };
            
            _renderer.sharedMaterial = _mirrorMaterial;
        }

        private void EnsureMirrorCamera()
        {
            if (_mirrorCamera != null) return;
            
            var go = new GameObject("Mirror Camera", typeof(Camera));
            go.hideFlags = HideFlags.HideAndDontSave;
            
            _mirrorCamera = go.GetComponent<Camera>();
            _mirrorCamera.enabled = false;
            
            // Add URP camera data
            var urpData = go.AddComponent<UniversalAdditionalCameraData>();
            urpData.renderShadows = false;
            urpData.requiresColorOption = CameraOverrideOption.Off;
            urpData.requiresDepthOption = CameraOverrideOption.Off;
        }

        private void EnsureRenderTextures(Camera cam)
        {
            int w = Mathf.Clamp((int)(cam.pixelWidth * _resolutionScale), 64, _maxResolution);
            int h = Mathf.Clamp((int)(cam.pixelHeight * _resolutionScale), 64, _maxResolution);
            
            // Check if we need to update textures
            bool sizeChanged = (_lastTextureWidth != w || _lastTextureHeight != h);
            
            if (_reflectionTextureLeft == null || sizeChanged)
            {
                if (_reflectionTextureLeft != null)
                {
                    _reflectionTextureLeft.Release();
                    DestroyImmediate(_reflectionTextureLeft);
                }
                
                _reflectionTextureLeft = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
                {
                    name = "Mirror Left",
                    hideFlags = HideFlags.DontSave
                };
                
                _texturesAssigned = false;
                _lastTextureWidth = w;
                _lastTextureHeight = h;
            }
            
            if (cam.stereoEnabled)
            {
                if (_reflectionTextureRight == null || sizeChanged)
                {
                    if (_reflectionTextureRight != null)
                    {
                        _reflectionTextureRight.Release();
                        DestroyImmediate(_reflectionTextureRight);
                    }
                    
                    _reflectionTextureRight = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
                    {
                        name = "Mirror Right",
                        hideFlags = HideFlags.DontSave
                    };
                    
                    _texturesAssigned = false;
                }
            }
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            // Only render for Game/SceneView cameras
            if (cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView)
                return;
            
            // Skip our own camera
            if (_mirrorCamera != null && cam == _mirrorCamera)
                return;
            
            // Prevent recursion
            if (s_IsRendering)
                return;
            
            // Check if enabled and visible
            if (!enabled || _renderer == null || !_renderer.enabled)
                return;
            
            // Check visibility (is the mirror on screen?)
            if (!_renderer.isVisible)
                return;
            
            s_IsRendering = true;
            
            try
            {
                RenderMirror(context, cam);
            }
            finally
            {
                s_IsRendering = false;
            }
        }

        private void RenderMirror(ScriptableRenderContext context, Camera cam)
        {
            EnsureMirrorCamera();
            EnsureRenderTextures(cam);
            
            // Copy camera settings
            _mirrorCamera.CopyFrom(cam);
            _mirrorCamera.enabled = false;
            
            int pixelLightCount = QualitySettings.pixelLightCount;
            if (_disablePixelLights)
                QualitySettings.pixelLightCount = 0;
            
            try
            {
                if (cam.stereoEnabled && XRSettings.enabled)
                {
                    // VR mode - render both eyes
                    RenderEye(context, cam, Camera.StereoscopicEye.Left, _reflectionTextureLeft);
                    RenderEye(context, cam, Camera.StereoscopicEye.Right, _reflectionTextureRight);
                    
                    if (!_texturesAssigned)
                    {
                        _propertyBlock.SetTexture(LeftEyeTextureId, _reflectionTextureLeft);
                        _propertyBlock.SetTexture(RightEyeTextureId, _reflectionTextureRight);
                        _renderer.SetPropertyBlock(_propertyBlock);
                        _texturesAssigned = true;
                    }
                }
                else
                {
                    // Non-VR mode
                    RenderEye(context, cam, Camera.StereoscopicEye.Left, _reflectionTextureLeft);
                    
                    if (!_texturesAssigned)
                    {
                        _propertyBlock.SetTexture(LeftEyeTextureId, _reflectionTextureLeft);
                        _propertyBlock.SetTexture(RightEyeTextureId, _reflectionTextureLeft);
                        _renderer.SetPropertyBlock(_propertyBlock);
                        _texturesAssigned = true;
                    }
                }
            }
            finally
            {
                if (_disablePixelLights)
                    QualitySettings.pixelLightCount = pixelLightCount;
            }
        }

        private void RenderEye(ScriptableRenderContext context, Camera sourceCam, Camera.StereoscopicEye eye, RenderTexture target)
        {
            Vector3 camPos;
            Matrix4x4 projMatrix;
            
            if (sourceCam.stereoEnabled && XRSettings.enabled)
            {
                camPos = sourceCam.transform.TransformPoint(XRSettings.eyeTextureWidth > 0 
                    ? (eye == Camera.StereoscopicEye.Left ? new Vector3(-0.032f, 0, 0) : new Vector3(0.032f, 0, 0))
                    : Vector3.zero);
                projMatrix = sourceCam.GetStereoProjectionMatrix(eye);
            }
            else
            {
                camPos = sourceCam.transform.position;
                projMatrix = sourceCam.projectionMatrix;
            }
            
            Vector3 mirrorPos = transform.position;
            Vector3 mirrorNormal = transform.up;
            
            // Calculate reflection plane
            float d = -Vector3.Dot(mirrorNormal, mirrorPos) - _clipPlaneOffset;
            Vector4 reflectionPlane = new Vector4(mirrorNormal.x, mirrorNormal.y, mirrorNormal.z, d);
            
            // Calculate reflection matrix
            Matrix4x4 reflectionMatrix = CalculateReflectionMatrix(reflectionPlane);
            
            // Reflect camera position
            Vector3 reflectedPos = reflectionMatrix.MultiplyPoint(camPos);
            
            // Setup mirror camera
            _mirrorCamera.transform.position = reflectedPos;
            _mirrorCamera.transform.rotation = sourceCam.transform.rotation;
            _mirrorCamera.projectionMatrix = projMatrix;
            _mirrorCamera.worldToCameraMatrix = sourceCam.worldToCameraMatrix * reflectionMatrix;
            
            // Oblique projection for near clipping at mirror plane
            Vector4 clipPlane = CameraSpacePlane(_mirrorCamera, mirrorPos, mirrorNormal, 1.0f);
            _mirrorCamera.projectionMatrix = _mirrorCamera.CalculateObliqueMatrix(clipPlane);
            
            // Render settings
            _mirrorCamera.cullingMask = _reflectionLayers & ~(1 << 4); // Exclude Water layer
            _mirrorCamera.targetTexture = target;
            
            // Render with inverted culling
            GL.invertCulling = true;
            UniversalRenderPipeline.RenderSingleCamera(context, _mirrorCamera);
            GL.invertCulling = false;
        }

        private static Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
        {
            Matrix4x4 m = Matrix4x4.identity;
            
            m.m00 = 1f - 2f * plane[0] * plane[0];
            m.m01 = -2f * plane[0] * plane[1];
            m.m02 = -2f * plane[0] * plane[2];
            m.m03 = -2f * plane[3] * plane[0];

            m.m10 = -2f * plane[1] * plane[0];
            m.m11 = 1f - 2f * plane[1] * plane[1];
            m.m12 = -2f * plane[1] * plane[2];
            m.m13 = -2f * plane[3] * plane[1];

            m.m20 = -2f * plane[2] * plane[0];
            m.m21 = -2f * plane[2] * plane[1];
            m.m22 = 1f - 2f * plane[2] * plane[2];
            m.m23 = -2f * plane[3] * plane[2];

            m.m30 = 0f;
            m.m31 = 0f;
            m.m32 = 0f;
            m.m33 = 1f;
            
            return m;
        }

        private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
        {
            Vector3 offsetPos = pos + normal * _clipPlaneOffset;
            Matrix4x4 worldToCam = cam.worldToCameraMatrix;
            Vector3 cpos = worldToCam.MultiplyPoint(offsetPos);
            Vector3 cnormal = worldToCam.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }

        private void Cleanup()
        {
            if (_mirrorCamera != null)
            {
                DestroyImmediate(_mirrorCamera.gameObject);
                _mirrorCamera = null;
            }
            
            if (_reflectionTextureLeft != null)
            {
                _reflectionTextureLeft.Release();
                DestroyImmediate(_reflectionTextureLeft);
                _reflectionTextureLeft = null;
            }
            
            if (_reflectionTextureRight != null)
            {
                _reflectionTextureRight.Release();
                DestroyImmediate(_reflectionTextureRight);
                _reflectionTextureRight = null;
            }
        }

#if UNITY_EDITOR
        private void Reset()
        {
            _renderer = GetComponent<Renderer>();
            EnsureMirrorMaterial();
        }

        private void OnValidate()
        {
            if (_renderer == null)
                _renderer = GetComponent<Renderer>();
        }

        private void OnDrawGizmosSelected()
        {
            // Draw mirror plane
            Gizmos.color = Color.cyan;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(1f, 0.01f, 1f));
            
            // Draw UP direction (mirror normal)
            Gizmos.color = Color.green;
            Gizmos.DrawLine(Vector3.zero, Vector3.up * 0.5f);
            Gizmos.DrawSphere(Vector3.up * 0.5f, 0.02f);
        }
#endif
    }
}
