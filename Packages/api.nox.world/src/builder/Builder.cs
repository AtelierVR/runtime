#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using Nox.CCK.Build;
using Nox.CCK.Utils;
using Nox.CCK.Worlds;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Nox.CCK.Utils.Logger;
using Random = System.Random;

namespace api.nox.world.builder {
	public static class Builder {
		public static bool IsBuilding;

		private static readonly Dictionary<string, string> SceneBackups = new();

		/// <summary>
		/// Creates backup copies of scenes before compilation
		/// </summary>
		/// <param name="sceneAssets">Dictionary of scene indices and their SceneAsset objects</param>
		/// <returns>True if backups were created successfully, false otherwise</returns>
		private static bool CreateSceneBackups(Dictionary<byte, SceneAsset> sceneAssets) {
			SceneBackups.Clear();

			try {
				const string backupPath = "Temp/SceneBackups/";
				if (Directory.Exists(backupPath)) {
					Directory.Delete(backupPath, true);
				}

				Directory.CreateDirectory(backupPath);

				foreach (var scene in sceneAssets) {
					var originalPath = AssetDatabase.GetAssetPath(scene.Value);
					if (string.IsNullOrEmpty(originalPath) || !File.Exists(originalPath)) {
						Logger.LogError($"Scene {scene.Value.name} is not valid or does not exist at path {originalPath}.");
						continue;
					}

					var backupFilePath = backupPath     + $"scene_{scene.Key}_backup.unity";
					var backupMetaPath = backupFilePath + ".meta";

					// Copier le fichier de scène
					File.Copy(originalPath, backupFilePath);

					// Créer un nouveau fichier .meta avec un GUID unique pour le backup
					var originalMetaContent = File.ReadAllText(originalPath + ".meta");
					var newGuid             = Guid.NewGuid().ToString("N");

					// Remplacer le GUID dans le contenu du .meta
					var lines = originalMetaContent.Split('\n');
					for (var i = 0; i < lines.Length; i++) {
						if (!lines[i].StartsWith("guid:")) continue;
						lines[i] = $"guid: {newGuid}";
						break;
					}

					var newMetaContent = string.Join("\n", lines);
					File.WriteAllText(backupMetaPath, newMetaContent);

					SceneBackups.Add(originalPath, backupFilePath);
					Logger.Log($"Created backup for scene {scene.Value.name} with unique GUID {newGuid}: {originalPath} -> {backupFilePath}");
				}

				return true;
			} catch
				(Exception e) {
				Logger.LogError($"Failed to create scene backups: {e.Message}");
				return false;
			}
		}

		/// <summary>
		/// Restores scenes from backup copies
		/// </summary>
		/// <returns>True if restoration was successful, false otherwise</returns>
		private static bool RestoreSceneBackups() {
			if (SceneBackups.Count == 0) {
				Logger.LogWarning("No scene backups found to restore.");
				return true;
			}

			try {
				foreach (var (s, value) in SceneBackups) {
					if (!File.Exists(value)) {
						Logger.LogError($"Backup file not found: {value}");
						continue;
					}

					// Restaurer seulement le fichier de scène (.unity)
					// Ne pas restaurer le .meta pour éviter les conflits de GUID
					File.Copy(value, s, true);

					Logger.Log($"Restored scene from backup: {value} -> {s}");
				}

				AssetDatabase.Refresh();
				return true;
			} catch (Exception e) {
				Logger.LogError($"Failed to restore scene backups: {e.Message}");
				return false;
			}
		}

		/// <summary>
		/// Cleans up backup files
		/// </summary>
		private static void CleanupSceneBackups() {
			try {
				const string backupPath = "Temp/SceneBackups/";
				if (Directory.Exists(backupPath)) {
					Directory.Delete(backupPath, true);
					Logger.Log("Scene backups cleaned up successfully.");
				}

				SceneBackups.Clear();
			} catch (Exception e) {
				Logger.LogWarning($"Failed to cleanup scene backups: {e.Message}");
			}
		}

		[MenuItem("Nox/Worlds/Build World")]
		public static void BuildMenu()
			=> BuildMenuAsync().Forget();

		public async static UniTask BuildMenuAsync() {
			if (!SceneDescriptorExtension.TryGetDescriptor(SceneManager.GetActiveScene(), out MainSceneDescriptor descriptor)) {
				EditorUtility.DisplayDialog("Build Failed", "No valid main scene descriptor found in the current scene.", "OK");
				return;
			}

			const string path = "Assets/Builds/";
			if (!Directory.Exists(path)) {
				try {
					Directory.CreateDirectory(path);
				} catch (Exception e) {
					EditorUtility.DisplayDialog("Build Failed", $"Failed to create output directory: {e.Message}", "OK");
					return;
				}
			}

			var data = new BuildData {
				Descriptor = descriptor,
				ShowDialog = true,
				OutputPath = path
			};

			BuildResult result;
			try {
				result = await Build(data);
			} catch (Exception e) {
				result = new BuildResult {
					Type    = BuildResultType.Failed,
					Message = $"Build failed with exception: {e.Message}"
				};
				Logger.LogError(result.Message);
			} finally {
				IsBuilding = false;
				CleanupSceneBackups();
			}

			if (result.Type == BuildResultType.Success) {
				EditorUtility.DisplayDialog("Build Success", "The world has been built successfully!", "OK");
			} else EditorUtility.DisplayDialog("Build Failed", result.Message, "OK");
		}

		public static async UniTask<BuildResult> Build(BuildData data) {
			if (data.Target == Platform.None)
				data.Target = PlatformExtensions.CurrentPlatform; // Set default filename if not provided
			if (string.IsNullOrEmpty(data.Filename))
				data.Filename = GenerateDefaultFilename(data.Descriptor.gameObject.scene.name, data.Target); // Set randomized temp path if not provided
			if (string.IsNullOrEmpty(data.TempPath))
				data.TempPath = $"Assets/Temp/{GenerateRandomHash()}/"; // Report progress: Validation
			data.ProgressCallback?.Invoke(0.05f, "Validating build prerequisites...");
			await UniTask.Yield();

			// Validation des prérequis
			var validation = ValidateBuildPrerequisites(data);
			if (validation.Type != BuildResultType.Success)
				return validation;

			IsBuilding = true;
			var rollback = EditorSceneManager.GetSceneManagerSetup();

			try {
				// Report progress: Preparation
				data.ProgressCallback?.Invoke(0.10f, "Preparing temporary directories...");
				await UniTask.Yield();

				// Préparation des répertoires temporaires
				var preparation = PrepareTemporaryDirectories(data, rollback);
				if (preparation.Type != BuildResultType.Success)
					return preparation; // Sauvegarde initiale des scènes
				if (!EditorSceneManager.SaveOpenScenes()) {
					return new BuildResult {
						Type    = BuildResultType.Failed,
						Message = "Failed to save open scenes. Please ensure all scenes are saved before building."
					};
				}

				AssetDatabase.Refresh(); // Report progress: Scene backup
				data.ProgressCallback?.Invoke(0.15f, "Creating scene backups...");
				await UniTask.Yield();

				var sceneAssets = data.Descriptor.EstimateScenes(); // Création des sauvegardes de scènes
				Logger.Log("Creating scene backups before compilation...");
				if (!CreateSceneBackups(sceneAssets)) {
					return new BuildResult {
						Type    = BuildResultType.Failed,
						Message = "Failed to create scene backups. Build aborted for safety."
					};
				} // Report progress: Loading scenes

				data.ProgressCallback?.Invoke(0.25f, "Loading required scenes...");
				await UniTask.Yield();

				// Chargement des scènes requises
				var sceneLoading = await LoadRequiredScenes(sceneAssets, rollback);
				if (sceneLoading.result.Type != BuildResultType.Success)
					return sceneLoading.result; // Report progress: Compiling scripts
				data.ProgressCallback?.Invoke(0.40f, "Compiling scripts...");
				await UniTask.Yield();

				// Compilation des scripts
				var compilation = await CompileScripts(sceneLoading.loadedScenes, rollback);
				if (compilation.Type != BuildResultType.Success)
					return compilation; // Traitement des scènes et dépendances
				// Report progress: Processing scenes
				data.ProgressCallback?.Invoke(0.60f, "Processing scenes and dependencies...");
				await UniTask.Yield();

				var processing = await ProcessScenesAndDependencies(data, sceneAssets, sceneLoading.loadedScenes, rollback);
				if (processing.Type != BuildResultType.Success)
					return processing; // Report progress: Building AssetBundle
				data.ProgressCallback?.Invoke(0.80f, "Building AssetBundle...");
				await UniTask.Yield();

				// Création de l'AssetBundle des scènes
				var assetBundleResult = await BuildScenesAssetBundle(data, rollback);
				if (assetBundleResult.Type != BuildResultType.Success)
					return assetBundleResult; // Nettoyage et restauration finale
				// Report progress: Cleanup
				data.ProgressCallback?.Invoke(0.95f, "Cleaning up...");
				await UniTask.Yield();

				// Report progress: Complete
				data.ProgressCallback?.Invoke(1.0f, "Build completed successfully!");
				await UniTask.Yield();

				return new BuildResult {
					Type = BuildResultType.Success,
				};
			} finally {
				// S'assurer que le nettoyage se fait toujours, même en cas d'erreur ou de retour anticipé
				CleanupAndRestoreState(rollback, true, data.TempPath);
			}
		}

		/// <summary>
		/// Validates build prerequisites and parameters
		/// </summary>
		private static BuildResult ValidateBuildPrerequisites(BuildData data) {
			if (IsBuilding)
				return new BuildResult {
					Type    = BuildResultType.AlreadyBuilding,
					Message = "A build is already in progress."
				};

			if (EditorApplication.isCompiling)
				return new BuildResult {
					Type    = BuildResultType.EditorCompiling,
					Message = "Unity is currently compiling scripts. Please wait until the compilation is complete."
				};

			if (EditorApplication.isPlaying)
				return new BuildResult {
					Type    = BuildResultType.EditorPlaying,
					Message = "Unity is currently in play mode. Please stop playing before building."
				};

			if (data.Target == Platform.None)
				return new BuildResult {
					Type    = BuildResultType.InvalidTarget,
					Message = "No build target specified. Please select a valid target platform."
				};

			if (!data.Target.IsSupported())
				return new BuildResult {
					Type    = BuildResultType.UnsupportedTarget,
					Message = $"The build target {data.Target} is not supported."
				};

			return new BuildResult { Type = BuildResultType.Success };
		}

		/// <summary>
		/// Prepares temporary directories and validates scene
		/// </summary>
		private static BuildResult PrepareTemporaryDirectories(BuildData data, SceneSetup[] rollback) {
			var mainScene = data.Descriptor.gameObject.scene;
			if (!mainScene.IsValid()) {
				return new BuildResult {
					Type    = BuildResultType.InvalidScene,
					Message = "The scene is not valid. Please ensure the scene is properly set up."
				};
			}

			var tempPath = data.TempPath;
			var depPath  = tempPath + "Dependencies/";
			if (Directory.Exists(tempPath))
				try {
					Directory.Delete(tempPath, true);
				} catch (Exception e) {
					return new BuildResult {
						Type    = BuildResultType.Failed,
						Message = $"Failed to delete temporary directory: {e.Message}"
					};
				}

			Directory.CreateDirectory(tempPath);
			if (!Directory.Exists(depPath))
				Directory.CreateDirectory(depPath);

			return new BuildResult { Type = BuildResultType.Success };
		}

		/// <summary>
		/// Loads all required scenes for the build
		/// </summary>
		private static async UniTask<(BuildResult result, List<Scene> loadedScenes)> LoadRequiredScenes(
			Dictionary<byte, SceneAsset> sceneAssets, SceneSetup[] rollback) {
			var loadedScenes = new List<Scene> {
				EditorSceneManager.OpenScene(
					AssetDatabase.GetAssetPath(sceneAssets.First().Value),
					OpenSceneMode.Single
				)
			};

			foreach (var scene in sceneAssets.Skip(1)) {
				await UniTask.Yield();
				var loadedScene = EditorSceneManager.OpenScene(
					AssetDatabase.GetAssetPath(scene.Value),
					OpenSceneMode.Additive
				);
				if (loadedScene.IsValid())
					loadedScenes.Add(loadedScene);
			}

			if (loadedScenes.Count == 0) {
				return (new BuildResult {
					Type    = BuildResultType.InvalidScene,
					Message = "No valid scenes found to build. Please ensure the main scene and sub-scenes are set up correctly."
				}, null);
			}

			return (new BuildResult { Type = BuildResultType.Success }, loadedScenes);
		}

		/// <summary>
		/// Compiles all ICompilable scripts in the loaded scenes
		/// </summary>
		private static async UniTask<BuildResult> CompileScripts(List<Scene> loadedScenes, SceneSetup[] rollback) {
			var compilableScripts = loadedScenes
				.SelectMany(scene => scene.GetRootGameObjects())
				.SelectMany(rootObject => rootObject.GetComponentsInChildren<ICompilable>(true))
				.OrderBy(script => script.CompileOrder)
				.ToList();
			if (compilableScripts.Count == 0)
				Logger.Log("No compilable scripts found in the loaded scenes.");

			var compilationFailed = false;
			foreach (var script in compilableScripts)
				try {
					Logger.Log($"Compiling script: {script.GetType().Name} (Order: {script.CompileOrder})");
					script.Compile();
					await script.CompileAsync();
				} catch (Exception e) {
					Logger.LogError($"Failed to compile script {script.GetType().Name}: {e.Message}");
					compilationFailed = true;
					break;
				}

			if (compilationFailed) {
				return new BuildResult {
					Type    = BuildResultType.Failed,
					Message = "Script compilation failed. Original scenes have been restored from backup."
				};
			}

			if (!EditorSceneManager.SaveOpenScenes()) {
				Logger.LogError("Failed to save scenes after compilation. Restoring backups...");
				return new BuildResult {
					Type    = BuildResultType.Failed,
					Message = "Failed to save open scenes after compilation. Original scenes have been restored from backup."
				};
			}

			// Force asset database refresh to ensure files are written to disk
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			// Wait a frame to ensure file operations are complete
			await UniTask.Yield();

			return new BuildResult { Type = BuildResultType.Success };
		}

		/// <summary>
		/// Saves compiled scenes and copies dependencies to temporary directory
		/// </summary>
		private static async UniTask<BuildResult> ProcessScenesAndDependencies(
			BuildData data, Dictionary<byte, SceneAsset> sceneAssets, List<Scene> loadedScenes, SceneSetup[] rollback) {
			var tempPath = data.TempPath;
			var depPath  = tempPath + "Dependencies/";

			// Ensure the Dependencies directory exists
			Directory.CreateDirectory(depPath);

			var assets       = new List<string>();
			var scenes       = new List<string>();
			var initialGUIDs = new Dictionary<string, string>();
			var endGUIDs     = new Dictionary<string, string>();

			foreach (var scene in sceneAssets) {
				var sceneIndex  = scene.Key;
				var sceneAsset  = scene.Value;
				var path        = AssetDatabase.GetAssetPath(sceneAsset);
				var destination = tempPath + sceneIndex + ".unity";

				if (string.IsNullOrEmpty(path) || !File.Exists(path)) {
					CleanupAndRestoreState(rollback);
					return new BuildResult {
						Type    = BuildResultType.InvalidScene,
						Message = $"Scene {sceneAsset.name} is not valid or does not exist at path {path}."
					};
				} // Trouver la scène chargée correspondante et la sauvegarder

				var loadedScene = loadedScenes.FirstOrDefault(s => s.path == path);
				if (loadedScene.IsValid()) {
					// Sauvegarder la scène compilée dans son chemin original (pour préserver les modifications)
					EditorSceneManager.SaveScene(loadedScene);

					// Copier le fichier de scène compilé vers la destination sans l'ouvrir
					File.Copy(path, destination, true);
					File.Copy(path + ".meta", destination + ".meta", true);
					Logger.Log($"Saved and copied compiled scene {sceneAsset.name} from {path} -> {destination}");
				} else {
					// Fallback : copier le fichier si la scène n'est pas trouvée dans les scènes chargées
					File.Copy(path, destination);
					File.Copy(path + ".meta", destination + ".meta");
					Logger.Log($"Copied scene {sceneAsset.name} from {path} -> {destination}");
				}

				assets.Add(destination);
				scenes.Add(destination);
				initialGUIDs.Add(destination, AssetDatabase.AssetPathToGUID(path));
				endGUIDs.Add(destination, Guid.NewGuid().ToString("N"));

				// Copy dependencies
				foreach (var dependency in AssetDatabase.GetDependencies(path)) {
					var gui             = Guid.NewGuid().ToString("N");
					var destinationPath = depPath + gui + Path.GetExtension(dependency);
					if (assets.Contains(destinationPath)) continue;
					switch (Path.GetExtension(dependency)) {
						case ".cs":
						case ".dll":
						case ".meta":
						case ".unity":
							continue;
					}

					File.Copy(dependency, destinationPath);
					File.Copy(dependency + ".meta", destinationPath + ".meta");
					Logger.Log("Copied " + dependency               + " to " + destinationPath);
					assets.Add(destinationPath);
					initialGUIDs.Add(destinationPath, AssetDatabase.AssetPathToGUID(dependency));
					endGUIDs.Add(destinationPath, gui);
				}
			}

			// Process GUID replacements
			await ProcessGuidReplacements(assets, initialGUIDs, endGUIDs);

			return new BuildResult { Type = BuildResultType.Success };
		}

		/// <summary>
		/// Processes GUID replacements in assets
		/// </summary>
		private static async UniTask ProcessGuidReplacements(List<string> assets, Dictionary<string, string> initialGUIDs, Dictionary<string, string> endGUIDs) {
			foreach (var asset in assets) {
				if (new[] {
					".unity", ".prefab", ".asset",
					".mat", ".anim", ".controller"
				}.Contains(Path.GetExtension(asset))) {
					var text = await File.ReadAllTextAsync(asset);
					text = initialGUIDs.Aggregate(
						text,
						(current, guid) => new Regex(guid.Value).Replace(current, endGUIDs[guid.Key])
					);
					await File.WriteAllTextAsync(asset, text);
				}

				var meta = await File.ReadAllTextAsync(asset + ".meta");
				meta = initialGUIDs.Aggregate(
					meta,
					(current, guid) => new Regex(guid.Value).Replace(current, endGUIDs[guid.Key])
				);
				await File.WriteAllTextAsync(asset + ".meta", meta);
			}

			AssetDatabase.Refresh();

			// Get all new UIDs
			var newGUIDs = new Dictionary<string, string>();
			foreach (var asset in assets)
				if (asset.StartsWith("Assets/Temp/Dependencies/")) {
					var fileName = Path.GetFileNameWithoutExtension(asset);
					newGUIDs.Add(fileName, AssetDatabase.AssetPathToGUID(asset));
					Logger.Log("Dependency: " + fileName + " -> " + newGUIDs[fileName]);
				}

			// Set updated UIDs
			foreach (var asset in assets) {
				if (!new[] {
					".unity", ".prefab", ".asset",
					".mat", ".anim", ".controller"
				}.Contains(Path.GetExtension(asset))) continue;
				var text = await File.ReadAllTextAsync(asset);
				text = newGUIDs.Aggregate(text, (current, guid) => new Regex(guid.Key).Replace(current, guid.Value));
				await File.WriteAllTextAsync(asset, text);
			}

			AssetDatabase.Refresh();
		}

		/// <summary>
		/// Builds an AssetBundle containing the compiled scenes
		/// </summary>
		/// <param name="data">Build data containing target platform and descriptor info</param>
		/// <param name="rollback">Scene manager setup for rollback in case of failure</param>
		/// <returns>BuildResult indicating success or failure</returns>
		private static async UniTask<BuildResult> BuildScenesAssetBundle(BuildData data, SceneSetup[] rollback) {
			try {
				// Validate input data
				if (data == null) {
					return new BuildResult {
						Type    = BuildResultType.Failed,
						Message = "BuildData is null."
					};
				}

				if (string.IsNullOrEmpty(data.TempPath)) {
					return new BuildResult {
						Type    = BuildResultType.Failed,
						Message = "Temporary path is null or empty."
					};
				}

				if (string.IsNullOrEmpty(data.OutputPath)) {
					return new BuildResult {
						Type    = BuildResultType.Failed,
						Message = "Output path is null or empty."
					};
				}

				if (string.IsNullOrEmpty(data.Filename)) {
					return new BuildResult {
						Type    = BuildResultType.Failed,
						Message = "Filename is null or empty."
					};
				}

				var tempPath = data.TempPath;
				Logger.Log("Building AssetBundle for compiled scenes...");

				// Report progress: Collecting scenes
				data.ProgressCallback?.Invoke(0.82f, "Collecting scene files...");
				await UniTask.Yield();

				// Collecter toutes les scènes dans le dossier Temp
				var sceneFiles = Directory.GetFiles(tempPath, "*.unity", SearchOption.TopDirectoryOnly)
					.Where(f => !f.EndsWith(".meta"))
					.ToArray();

				if (sceneFiles.Length == 0) {
					return new BuildResult {
						Type    = BuildResultType.Failed,
						Message = "No compiled scenes found to bundle."
					};
				} // Report progress: Preparing AssetBundle

				data.ProgressCallback?.Invoke(0.84f, "Preparing AssetBundle build...");
				await UniTask.Yield();
				Logger.Log($"Found {sceneFiles.Length} scene files to bundle:");
				foreach (var sceneFile in sceneFiles) {
					Logger.Log($"  - {sceneFile}");
				}

				// Validate all scene files exist and convert to relative paths for Unity
				var validSceneFiles = new List<string>();
				foreach (var sceneFile in sceneFiles) {
					if (File.Exists(sceneFile)) {
						// Convert absolute path to relative path for Unity AssetDatabase
						var relativePath = sceneFile.Replace('\\', '/');
						if (relativePath.StartsWith(Application.dataPath.Replace('\\', '/')))
							relativePath = "Assets" + relativePath[Application.dataPath.Length..];
						validSceneFiles.Add(relativePath);
						Logger.Log($"  Valid scene: {relativePath}");
					} else {
						Logger.LogWarning($"Scene file does not exist: {sceneFile}");
					}
				}

				if (validSceneFiles.Count == 0) {
					return new BuildResult {
						Type    = BuildResultType.Failed,
						Message = "No valid scene files found for bundling."
					};
				}

				// Créer les AssetBundleBuild
				var assetBundleBuilds = new AssetBundleBuild[1];
				assetBundleBuilds[0] = new AssetBundleBuild {
					assetBundleName  = data.Filename,
					assetNames       = validSceneFiles.ToArray(),
					addressableNames = validSceneFiles.Select(Path.GetFileNameWithoutExtension).ToArray()
				};

				Logger.Log($"Created AssetBundle build: {data.Filename} with {assetBundleBuilds[0].assetNames.Length} assets");
				foreach (var assetName in assetBundleBuilds[0].assetNames)
					Logger.Log($"  - {assetName}");
				foreach (var addressableName in assetBundleBuilds[0].addressableNames)
					Logger.Log($"  - Addressable: {addressableName}");

				// Validate the AssetBundleBuild
				if (assetBundleBuilds[0].assetNames.Length == 0) {
					return new BuildResult {
						Type    = BuildResultType.Failed,
						Message = "No valid assets found for AssetBundle build."
					};
				}

				if (string.IsNullOrEmpty(assetBundleBuilds[0].assetBundleName)) {
					return new BuildResult {
						Type    = BuildResultType.Failed,
						Message = "AssetBundle name is null or empty."
					};
				}

				if (assetBundleBuilds[0].addressableNames.Length == 0) {
					return new BuildResult {
						Type    = BuildResultType.Failed,
						Message = "No addressable names found for AssetBundle build."
					};
				}

				Logger.Log($"AssetBundle build prepared with {assetBundleBuilds[0].assetNames.Length} assets and {assetBundleBuilds[0].addressableNames.Length} addressables.");


				// Report progress: Creating output directory
				data.ProgressCallback?.Invoke(0.86f, "Creating output directory...");
				await UniTask.Yield();

				// Créer le dossier de sortie
				var outputPath = data.OutputPath;
				Directory.CreateDirectory(outputPath); // Options de build
				var options = BuildAssetBundleOptions.None | BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode;

				// Report progress: Building AssetBundle (this is the long operation)
				data.ProgressCallback?.Invoke(0.88f, "Building AssetBundle (this may take a while)...");
				await UniTask.Yield();

				// Validate build target
				var buildTarget = data.Target.GetBuildTarget();
				Logger.Log($"Building AssetBundle with target: {buildTarget}");

				// Additional validation
				if (assetBundleBuilds.Length == 0) {
					return new BuildResult {
						Type    = BuildResultType.Failed,
						Message = "AssetBundle builds array is null or empty."
					};
				}

				if (string.IsNullOrEmpty(outputPath)) {
					return new BuildResult {
						Type    = BuildResultType.Failed,
						Message = "Output path is null or empty."
					};
				}

				// Construire l'AssetBundle
				var buildSuccess = BuildAssetBundleInternal(
					outputPath,
					assetBundleBuilds,
					options,
					buildTarget
				);

				data.ProgressCallback?.Invoke(0.92f, "Finalizing AssetBundle...");


				if (!buildSuccess) {
					Logger.LogError("AssetBundle build failed. Check console for details.");
					return new BuildResult {
						Type    = BuildResultType.Failed,
						Message = "Failed to build AssetBundle. Check console for details."
					};
				}

				Logger.Log($"AssetBundle '{data.Filename}' built successfully at: {outputPath}");
				Logger.Log($"Scenes included: {string.Join(", ", sceneFiles.Select(Path.GetFileName))}");
				return new BuildResult { Type = BuildResultType.Success };
			} catch (Exception e) {
				Logger.LogError($"AssetBundle build failed: {e.Message}");
				return new BuildResult {
					Type    = BuildResultType.Failed,
					Message = $"AssetBundle build failed: {e.Message}"
				};
			}
		}

		/// <summary>
		/// Builds an AssetBundle and returns whether the operation was successful
		/// Thanks for "https://light11.hatenadiary.com/entry/2021/03/30/201333" to fix the issue with BuildPipeline.BuildAssetBundles and UniTask
		/// </summary>
		/// <param name="outputPath">The output path for the AssetBundle</param>
		/// <param name="assetBundleBuilds">The AssetBundle builds to create</param>
		/// <param name="options">Build options</param>
		/// <param name="buildTarget">Target platform</param>
		/// <returns>True if the build was successful, false otherwise</returns>
		private static bool BuildAssetBundleInternal(string outputPath, AssetBundleBuild[] assetBundleBuilds, BuildAssetBundleOptions options, BuildTarget buildTarget) {
			try {
				return CompatibilityBuildPipeline.BuildAssetBundles(
					outputPath,
					assetBundleBuilds,
					options,
					buildTarget
				);
			} catch (Exception e) {
				Logger.LogError($"AssetBundle build failed: {e.Message}");
				return false;
			}
		}

		/// <summary>
		/// Helper method to cleanup and restore state before returning a build result
		/// </summary>
		/// <param name="rollback">The scene manager setup to restore</param>
		/// <param name="restoreScenes">Whether to restore scenes from backup</param>
		/// <param name="tempPath">The temporary path to cleanup (optional)</param>
		private static void CleanupAndRestoreState(SceneSetup[] rollback, bool restoreScenes = true, string tempPath = null) {
			Logger.LogWarning("Restoring scene backups...");
			if (restoreScenes && !RestoreSceneBackups())
				Logger.LogError("Failed to restore scene backups. Please check the backup files.");
			CleanupSceneBackups(); // Nettoyer le répertoire temporaire spécifique si fourni
			if (!string.IsNullOrEmpty(tempPath) && Directory.Exists(tempPath)) {
				try {
					Directory.Delete(tempPath, true);
					Logger.Log($"Cleaned up temporary directory: {tempPath}");
				} catch (Exception e) {
					Logger.LogWarning($"Failed to cleanup temporary directory {tempPath}: {e.Message}");
				}
			} // Nettoyer aussi le répertoire Assets/Temp/ de base pour éviter l'accumulation

			try {
				const string baseTempPath = "Assets/Temp/";
				if (Directory.Exists(baseTempPath)) {
					// Nettoyer seulement les dossiers plus anciens que 1 heure pour éviter les conflits avec des builds concurrents
					var directories = Directory.GetDirectories(baseTempPath);
					foreach (var dir in directories) {
						var dirInfo = new DirectoryInfo(dir);
						if (DateTime.Now - dirInfo.CreationTime > TimeSpan.FromHours(1)) {
							try {
								Directory.Delete(dir, true);
								Logger.Log($"Cleaned up old temporary directory: {dir}");
							} catch {
								// Ignore errors for individual directory cleanup
							}
						}
					}
				}
			} catch (Exception e) {
				Logger.LogWarning($"Failed to cleanup base temporary directory: {e.Message}");
			}

			IsBuilding = false;
			EditorSceneManager.RestoreSceneManagerSetup(rollback);
		}

		/// <summary>
		/// Generates a default filename for the asset bundle based on date, random int, and main scene name
		/// </summary>
		/// <param name="mainSceneName">The name of the main scene</param>
		/// <param name="platform"></param>
		/// <returns>A filename in the format: date-sceneName.nw</returns>
		private static string GenerateDefaultFilename(string mainSceneName, Platform platform) {
			var date      = DateTime.Now.ToString("yyyy-MM-dd-HHmm");
			var sceneName = mainSceneName.ToLowerInvariant();

			// Remove any invalid filename characters from scene name
			sceneName = Regex.Replace(sceneName, @"[^a-z0-9\-_]", "");

			return $"{date}-{sceneName}-{platform.GetPlatformName()}.noxw";
		}

		/// <summary>
		/// Generates a random hash for temporary directory
		/// </summary>
		/// <returns>A random hash string</returns>
		private static string GenerateRandomHash() {
			var random = new Random();
			var bytes  = new byte[16];
			random.NextBytes(bytes);
			return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
		}
	}
}
#endif