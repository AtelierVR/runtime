#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

namespace dev.nox.development {
	public class Screenshot : EditorWindow {
		
		public enum ImageFormat {
			PNG,
			JPG,
			TGA
		}
		
		public enum ResolutionPreset {
			Free,
			HD_720p_16_9,      // 1280x720
			FHD_1080p_16_9,    // 1920x1080
			QHD_1440p_16_9,    // 2560x1440
			UHD_4K_16_9,       // 3840x2160
			SVGA_4_3,          // 800x600
			XGA_4_3,           // 1024x768
			SXGA_4_3,          // 1280x960
			UXGA_4_3,          // 1600x1200
			Square_512,        // 512x512
			Square_1024,       // 1024x1024
			Square_2048,       // 2048x2048
			Instagram_1_1,     // 1080x1080
			Portrait_9_16,     // 1080x1920
			Ultrawide_21_9,    // 2560x1080
			Cinema_2_35_1      // 2560x1088
		}
		
		private Camera _selectedCamera;
		private ImageFormat _imageFormat = ImageFormat.PNG;
		private ResolutionPreset _resolutionPreset = ResolutionPreset.Free;
		private int _resolutionMultiplier = 1;
		private int _customWidth = 1920;
		private int _customHeight = 1080;
		private string _fileName = "screenshot";
		private bool _useTimeStamp = true;
		private bool _showPreview = false;
		private Texture2D _previewTexture;
		private Vector2 _scrollPosition = Vector2.zero;
		
		[MenuItem("Nox/Tools/Screenshot Tool")]
		public static void ShowWindow() {
			GetWindow<Screenshot>("Screenshot Tool");
		}
		
		private void OnEnable() {
			// Essaie de trouver la caméra sélectionnée ou utilise celle de l'éditeur
			RefreshSelectedCamera();
		}
		
		private void OnGUI() {
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
			
			GUILayout.Label("Screenshot Tool", EditorStyles.boldLabel);
			EditorGUILayout.Space();
			
			// Sélection de la caméra
			EditorGUILayout.LabelField("Caméra", EditorStyles.boldLabel);
			_selectedCamera = (Camera)EditorGUILayout.ObjectField("Caméra source", _selectedCamera, typeof(Camera), true);
			
			if (GUILayout.Button("Actualiser caméra sélectionnée")) {
				RefreshSelectedCamera();
			}
			
			EditorGUILayout.Space();
			
			// Format d'image
			EditorGUILayout.LabelField("Format d'export", EditorStyles.boldLabel);
			_imageFormat = (ImageFormat)EditorGUILayout.EnumPopup("Format", _imageFormat);
			
			// Résolution
			EditorGUILayout.LabelField("Résolution", EditorStyles.boldLabel);
			_resolutionPreset = (ResolutionPreset)EditorGUILayout.EnumPopup("Preset", _resolutionPreset);
			
			if (_resolutionPreset == ResolutionPreset.Free) {
				EditorGUILayout.BeginHorizontal();
				_customWidth = EditorGUILayout.IntField("Largeur", _customWidth);
				_customHeight = EditorGUILayout.IntField("Hauteur", _customHeight);
				EditorGUILayout.EndHorizontal();
				
				_resolutionMultiplier = EditorGUILayout.IntSlider("Multiplicateur", _resolutionMultiplier, 1, 4);
			}
			
			Vector2 resolution = GetResolution();
			EditorGUILayout.LabelField($"Résolution finale: {resolution.x} x {resolution.y}");
			
			// Affichage du ratio d'aspect
			float aspectRatio = resolution.x / resolution.y;
			string ratioText = GetAspectRatioText(aspectRatio);
			EditorGUILayout.LabelField($"Ratio d'aspect: {ratioText}");
			
			EditorGUILayout.Space();
			
			// Nom du fichier
			EditorGUILayout.LabelField("Fichier", EditorStyles.boldLabel);
			_fileName = EditorGUILayout.TextField("Nom du fichier", _fileName);
			_useTimeStamp = EditorGUILayout.Toggle("Ajouter timestamp", _useTimeStamp);
			
			EditorGUILayout.Space();
			
			// Options de prévisualisation
			EditorGUILayout.LabelField("Prévisualisation", EditorStyles.boldLabel);
			_showPreview = EditorGUILayout.Toggle("Afficher aperçu", _showPreview);
			
			EditorGUILayout.BeginHorizontal();
			
			// Bouton de prévisualisation
			GUI.backgroundColor = Color.cyan;
			if (GUILayout.Button("Générer aperçu", GUILayout.Height(30))) {
				GeneratePreview();
			}
			
			// Bouton de capture
			GUI.backgroundColor = Color.green;
			if (GUILayout.Button("Prendre capture d'écran", GUILayout.Height(30))) {
				TakeScreenshot();
			}
			GUI.backgroundColor = Color.white;
			
			EditorGUILayout.EndHorizontal();
			
			EditorGUILayout.Space();
			
			// Affichage de la prévisualisation
			if (_showPreview && _previewTexture != null) {
				EditorGUILayout.LabelField("Aperçu:", EditorStyles.boldLabel);
				
				// Calcul de la taille d'affichage pour maintenir le ratio
				float maxPreviewSize = 300f;
				float previewWidth = _previewTexture.width;
				float previewHeight = _previewTexture.height;
				
				if (previewWidth > maxPreviewSize || previewHeight > maxPreviewSize) {
					float scale = Mathf.Min(maxPreviewSize / previewWidth, maxPreviewSize / previewHeight);
					previewWidth *= scale;
					previewHeight *= scale;
				}
				
				Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
				GUI.DrawTexture(previewRect, _previewTexture, ScaleMode.ScaleToFit);
				
				EditorGUILayout.LabelField($"Taille de l'aperçu: {_previewTexture.width}x{_previewTexture.height}");
				EditorGUILayout.Space();
			}
			// Informations
			if (_selectedCamera != null) {
				EditorGUILayout.HelpBox($"Caméra: {_selectedCamera.name}", MessageType.Info);
			} else {
				EditorGUILayout.HelpBox("Aucune caméra sélectionnée - utilisera la vue éditeur", MessageType.Warning);
			}
			
			EditorGUILayout.EndScrollView();
		}
		
		private void OnDestroy() {
			// Nettoie la texture de prévisualisation
			if (_previewTexture != null) {
				DestroyImmediate(_previewTexture);
			}
		}
		
		private void RefreshSelectedCamera() {
			// Essaie de récupérer la caméra sélectionnée dans la hiérarchie
			GameObject selectedObject = Selection.activeGameObject;
			if (selectedObject != null) {
				Camera cam = selectedObject.GetComponent<Camera>();
				if (cam != null) {
					_selectedCamera = cam;
					return;
				}
			}
			
			// Si aucune caméra sélectionnée, prend la caméra principale
			if (_selectedCamera == null) {
				_selectedCamera = Camera.main;
			}
			
			// Si toujours aucune caméra, prend la première trouvée
			if (_selectedCamera == null) {
				Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
				if (cameras.Length > 0) {
					_selectedCamera = cameras[0];
				}
			}
		}
		
		private Vector2 GetResolution() {
			Vector2 baseResolution = GetPresetResolution();
			
			if (_resolutionPreset == ResolutionPreset.Free) {
				baseResolution = new Vector2(_customWidth * _resolutionMultiplier, _customHeight * _resolutionMultiplier);
			}
			
			return baseResolution;
		}
		
		private Vector2 GetPresetResolution() {
			switch (_resolutionPreset) {
				case ResolutionPreset.HD_720p_16_9:
					return new Vector2(1280, 720);
				case ResolutionPreset.FHD_1080p_16_9:
					return new Vector2(1920, 1080);
				case ResolutionPreset.QHD_1440p_16_9:
					return new Vector2(2560, 1440);
				case ResolutionPreset.UHD_4K_16_9:
					return new Vector2(3840, 2160);
				case ResolutionPreset.SVGA_4_3:
					return new Vector2(800, 600);
				case ResolutionPreset.XGA_4_3:
					return new Vector2(1024, 768);
				case ResolutionPreset.SXGA_4_3:
					return new Vector2(1280, 960);
				case ResolutionPreset.UXGA_4_3:
					return new Vector2(1600, 1200);
				case ResolutionPreset.Square_512:
					return new Vector2(512, 512);
				case ResolutionPreset.Square_1024:
					return new Vector2(1024, 1024);
				case ResolutionPreset.Square_2048:
					return new Vector2(2048, 2048);
				case ResolutionPreset.Instagram_1_1:
					return new Vector2(1080, 1080);
				case ResolutionPreset.Portrait_9_16:
					return new Vector2(1080, 1920);
				case ResolutionPreset.Ultrawide_21_9:
					return new Vector2(2560, 1080);
				case ResolutionPreset.Cinema_2_35_1:
					return new Vector2(2560, 1088);
				case ResolutionPreset.Free:
				default:
					return new Vector2(_customWidth, _customHeight);
			}
		}
		
		private string GetAspectRatioText(float aspectRatio) {
			// Conversion des ratios communs
			if (Mathf.Approximately(aspectRatio, 16f/9f)) return "16:9";
			if (Mathf.Approximately(aspectRatio, 4f/3f)) return "4:3";
			if (Mathf.Approximately(aspectRatio, 1f)) return "1:1";
			if (Mathf.Approximately(aspectRatio, 9f/16f)) return "9:16";
			if (Mathf.Approximately(aspectRatio, 21f/9f)) return "21:9";
			if (Mathf.Approximately(aspectRatio, 2.35f)) return "2.35:1";
			
			// Ratio personnalisé
			return $"{aspectRatio:F2}:1";
		}
		
		private void TakeScreenshot() {
			try {
				string finalFileName = GetFinalFileName();
				string projectPath = Application.dataPath;
				string fullPath = Path.Combine(projectPath, finalFileName);
				
				if (_selectedCamera != null) {
					CaptureFromCamera(fullPath);
				} else {
					CaptureFromSceneView(fullPath);
				}
				
				Debug.Log($"Capture d'écran sauvegardée: {fullPath}");
				EditorUtility.DisplayDialog("Succès", $"Capture d'écran sauvegardée:\n{fullPath}", "OK");
				
				// Rafraîchit l'asset database si le fichier est dans le projet
				if (fullPath.StartsWith(Application.dataPath)) {
					AssetDatabase.Refresh();
				}
			}
			catch (Exception e) {
				Debug.LogError($"Erreur lors de la capture d'écran: {e.Message}");
				EditorUtility.DisplayDialog("Erreur", $"Impossible de prendre la capture d'écran:\n{e.Message}", "OK");
			}
		}
		
		private void CaptureFromCamera(string path) {
			Vector2 resolution = GetResolution();
			RenderTexture renderTexture = new RenderTexture((int)resolution.x, (int)resolution.y, 24);
			_selectedCamera.targetTexture = renderTexture;
			_selectedCamera.Render();
			
			RenderTexture.active = renderTexture;
			Texture2D screenshot = new Texture2D((int)resolution.x, (int)resolution.y, TextureFormat.RGB24, false);
			screenshot.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
			screenshot.Apply();
			
			_selectedCamera.targetTexture = null;
			RenderTexture.active = null;
			DestroyImmediate(renderTexture);
			
			SaveTexture(screenshot, path);
			DestroyImmediate(screenshot);
		}
		
		private void CaptureFromSceneView(string path) {
			SceneView sceneView = SceneView.lastActiveSceneView;
			if (sceneView == null) {
				// Essaie de trouver une SceneView ouverte
				SceneView[] sceneViews = Resources.FindObjectsOfTypeAll<SceneView>();
				if (sceneViews.Length > 0) {
					sceneView = sceneViews[0];
				} else {
					throw new System.Exception("Aucune vue Scene ouverte trouvée");
				}
			}
			
			Vector2 resolution = GetResolution();
			Camera sceneCamera = sceneView.camera;
			
			// Sauvegarde les paramètres actuels
			RenderTexture originalTargetTexture = sceneCamera.targetTexture;
			
			// Crée un RenderTexture à la résolution désirée
			RenderTexture renderTexture = new RenderTexture((int)resolution.x, (int)resolution.y, 24);
			sceneCamera.targetTexture = renderTexture;
			
			// Force le rendu de la SceneView
			sceneView.camera.Render();
			
			// Capture la texture
			RenderTexture.active = renderTexture;
			Texture2D screenshot = new Texture2D((int)resolution.x, (int)resolution.y, TextureFormat.RGB24, false);
			screenshot.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
			screenshot.Apply();
			
			// Restaure les paramètres
			sceneCamera.targetTexture = originalTargetTexture;
			RenderTexture.active = null;
			DestroyImmediate(renderTexture);
			
			SaveTexture(screenshot, path);
			DestroyImmediate(screenshot);
		}
		
		private void SaveTexture(Texture2D texture, string path) {
			byte[] data;
			
			switch (_imageFormat) {
				case ImageFormat.PNG:
					data = texture.EncodeToPNG();
					break;
				case ImageFormat.JPG:
					data = texture.EncodeToJPG();
					break;
				case ImageFormat.TGA:
					data = texture.EncodeToTGA();
					break;
				default:
					data = texture.EncodeToPNG();
					break;
			}
			
			File.WriteAllBytes(path, data);
		}
		
		private string GetFinalFileName() {
			string filename = _fileName;
			if (string.IsNullOrEmpty(filename)) {
				filename = "screenshot";
			}
			
			if (_useTimeStamp) {
				string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
				filename += "_" + timestamp;
			}
			
			string extension = GetExtension();
			return filename + extension;
		}
		
		private string GetExtension() {
			switch (_imageFormat) {
				case ImageFormat.PNG:
					return ".png";
				case ImageFormat.JPG:
					return ".jpg";
				case ImageFormat.TGA:
					return ".tga";
				default:
					return ".png";
			}
		}
		
		private void GeneratePreview() {
			try {
				// Nettoie l'ancienne texture de prévisualisation
				if (_previewTexture != null) {
					DestroyImmediate(_previewTexture);
				}
				
				if (_selectedCamera != null) {
					_previewTexture = CaptureFromCameraToTexture();
				} else {
					// Pour la vue éditeur, on ne peut pas faire de vraie prévisualisation
					// On crée une texture placeholder
					_previewTexture = CreatePlaceholderTexture();
				}
				
				_showPreview = true;
				Repaint();
			}
			catch (Exception e) {
				Debug.LogError($"Erreur lors de la génération de l'aperçu: {e.Message}");
				EditorUtility.DisplayDialog("Erreur", $"Impossible de générer l'aperçu:\n{e.Message}", "OK");
			}
		}
		
		private Texture2D CaptureFromCameraToTexture() {
			Vector2 resolution = GetResolution();
			
			// Limite la résolution de la prévisualisation pour des performances optimales
			int previewWidth = Mathf.Min((int)resolution.x, 512);
			int previewHeight = Mathf.Min((int)resolution.y, 512);
			
			// Maintient le ratio d'aspect
			float aspectRatio = resolution.x / resolution.y;
			if (previewWidth / (float)previewHeight > aspectRatio) {
				previewWidth = (int)(previewHeight * aspectRatio);
			} else {
				previewHeight = (int)(previewWidth / aspectRatio);
			}
			
			RenderTexture renderTexture = new RenderTexture(previewWidth, previewHeight, 24);
			RenderTexture previousTarget = _selectedCamera.targetTexture;
			
			_selectedCamera.targetTexture = renderTexture;
			_selectedCamera.Render();
			
			RenderTexture.active = renderTexture;
			Texture2D previewTexture = new Texture2D(previewWidth, previewHeight, TextureFormat.RGB24, false);
			previewTexture.ReadPixels(new Rect(0, 0, previewWidth, previewHeight), 0, 0);
			previewTexture.Apply();
			
			_selectedCamera.targetTexture = previousTarget;
			RenderTexture.active = null;
			DestroyImmediate(renderTexture);
			
			return previewTexture;
		}
		
		private Texture2D CreatePlaceholderTexture() {
			Vector2 resolution = GetResolution();
			float aspectRatio = resolution.x / resolution.y;
			
			int width = 256;
			int height = (int)(width / aspectRatio);
			
			if (height <= 0) height = 256; // Sécurité pour éviter les hauteurs nulles
			
			Texture2D placeholder = new Texture2D(width, height, TextureFormat.RGB24, false);
			Color[] colors = new Color[width * height];
			
			// Crée un motif de damier simple avec du texte
			for (int y = 0; y < height; y++) {
				for (int x = 0; x < width; x++) {
					bool checker = ((x / 16) + (y / 16)) % 2 == 0;
					colors[y * width + x] = checker ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.9f, 0.9f, 0.9f);
				}
			}
			
			placeholder.SetPixels(colors);
			placeholder.Apply();
			
			return placeholder;
		}
	}
}
#endif // UNITY_EDITOR