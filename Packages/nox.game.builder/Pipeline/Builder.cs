using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nox.CCK.Mods;
using Nox.CCK.Mods.Metadata;
using Nox.CCK.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.GameBuilder.Pipeline {
	public static class Builder {
		public static bool IsBuilding;

		public static readonly UnityEvent<float, string> OnBuildProgress = new();
		public static readonly UnityEvent<BuildResult>   OnBuildFinished = new();
		public static readonly UnityEvent<BuildData>     OnBuildStarted  = new();

		public static async UniTask<BuildResult> Build(BuildData data) {
			// Wrap user progress callback to also emit UnityEvent
			var userProgress = data.ProgressCallback;
			data.ProgressCallback = (p, m) => {
				try {
					OnBuildProgress.Invoke(p, m);
				} catch {
					/* ignore listener errors */
				}

				try {
					userProgress?.Invoke(p, m);
				} catch {
					/* ignore user callback errors */
				}
			};

			// Notify build start
			try {
				OnBuildStarted.Invoke(data);
			} catch {
				/* ignore listener errors */
			}

			BuildResult Finish(BuildResult r) {
				try {
					OnBuildFinished.Invoke(r);
				} catch {
					/* ignore listener errors */
				}

				return r;
			}

			if (IsBuilding) {
				return Finish(
					new BuildResult {
						Type    = BuildResultType.AlreadyBuilding,
						Message = "A build is already in progress."
					}
				);
			}

			if (EditorApplication.isCompiling) {
				return Finish(
					new BuildResult {
						Type    = BuildResultType.EditorCompiling,
						Message = "The editor is currently compiling scripts."
					}
				);
			}

			if (EditorApplication.isPlaying) {
				return Finish(
					new BuildResult {
						Type    = BuildResultType.EditorPlaying,
						Message = "The editor is currently in play mode."
					}
				);
			}

			IsBuilding = true;

			try {
				// Use default build name if not provided
				if (string.IsNullOrEmpty(data.BuildName))
					data.BuildName = Application.productName;

				// Use current platform if not provided or None
				if (data.Target == Platform.None)
					data.Target = PlatformExtensions.CurrentPlatform;

				// Filter mods to only include kernel mods
				data.Mods = GetKernelMods(data.Mods);

				if (!data.Target.IsSupported())
					return Finish(
						new BuildResult {
							Type    = BuildResultType.UnsupportedTarget,
							Message = $"The platform {data.Target.GetPlatformName()} is not supported on this editor version."
						}
					);

				data.ProgressCallback(0.1f, "Preparing build...");
				await UniTask.Delay(100); // Give UI time to update

				if (File.Exists(data.OutputPath))
					File.Delete(data.OutputPath);

				if (!Directory.Exists(data.OutputPath))
					Directory.CreateDirectory(data.OutputPath);

				var scenes = GetScenesToBuild(data.Mods);

				data.ProgressCallback(0.2f, $"Building {scenes.Length} scenes...");

				var buildPlayerOptions = new BuildPlayerOptions {
					scenes           = scenes,
					locationPathName = Path.Combine(data.OutputPath, data.BuildName + (data.Target == Platform.Windows ? ".exe" : "")),
					options          = data.BuildOptions,
					target           = data.Target.GetBuildTarget()
				};

				// We can't really get progress from BuildPipeline.BuildPlayer as it is blocking in main thread usually, 
				// but we can wrap it in a task to at least not freeze the UI completely if it was async (it's not really async in Editor API)
				// However, since we are in Editor, we have to run it on main thread.

				var report  = BuildPipeline.BuildPlayer(buildPlayerOptions);
				var summary = report.summary;

				if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
					return Finish(
						new BuildResult {
							Type    = BuildResultType.Failed,
							Message = $"Build failed with {summary.totalErrors} errors."
						}
					);

				// Build Mods and Game Data
				if (data.Mods != null && data.Mods.Length > 0) {
					data.ProgressCallback(0.5f, "Building mods...");

					var outputData = Path.Combine(data.OutputPath, data.BuildName + "_Data", "Nox");
					var gameData   = Path.Combine(outputData, "game_data.json");

					if (!Directory.Exists(outputData))
						Directory.CreateDirectory(outputData);

					var resultAssets = BuildAssets.BuildAsAssetBundles(data.Mods, data.Target, outputData);

					if (resultAssets == null)
						return Finish(
							new BuildResult {
								Type    = BuildResultType.Failed,
								Message = "Failed to build asset bundles."
							}
						);

					var metadatas = new JArray();

					for (var i = 0; i < data.Mods.Length; i++) {
						var mod  = data.Mods[i];
						var meta = mod.GetMetadata();

						data.ProgressCallback(0.5f + (0.4f * ((float)i / data.Mods.Length)), $"Processing mod {meta.GetId()}...");

						var obj = meta.ToObject();

						obj["kernel"] = new JObject() {
							["active"]      = meta.GetCustom("kernel", false),
							["base_path"]   = GetPath(mod.GetData<string>("folder")),
							["assets_path"] = GetPath(mod.GetData<string>("assets")),
							["definition"]  = GetPath(mod.GetData<string>("definition")),
							["manifest"]    = GetPath(mod.GetData<string>("manifest"))
						};

						var assetResult = resultAssets.FirstOrDefault(r => r.mod.GetMetadata().Match(meta.GetId()));

						JArray assetObj = new();
						if (assetResult != null)
							foreach (var asset in assetResult.outputs)
								if (File.Exists(asset)) {
									var bundle = AssetBundle.LoadFromFile(asset);
									if (bundle == null) continue;
									assetObj.Add(
										new JObject() {
											["name"] = Path.GetFileName(asset),
											["file"] = Path.Combine(Path.GetRelativePath(outputData, asset))
												.Replace("\\", "/")
												.ToLower(),
											["assets"] = new JArray(bundle.GetAllAssetNames().Select(a => a.ToLower())),
											["scenes"] = new JArray(bundle.GetAllScenePaths().Select(a => a.ToLower()))
										}
									);
									bundle.Unload(true);
								}

						obj["kernel"]["assets"] = assetObj;

						metadatas.Add(obj);
					}

					data.ProgressCallback(0.95f, "Saving game data...");

					await File.WriteAllTextAsync(
						gameData, new JObject {
							["mods"] = metadatas,
							["engine"] = new JObject {
								["name"]    = EngineExtensions.CurrentEngine.ToString(),
								["version"] = EngineExtensions.CurrentVersion.ToString()
							},
							["platform"] = data.Target.GetPlatformName()
						}.ToString()
					);
				}

				data.ProgressCallback(1f, "Build completed successfully.");
				return Finish(
					new BuildResult {
						Type    = BuildResultType.Success,
						Output  = summary.outputPath,
						Message = "Build completed successfully."
					}
				);
			} catch (Exception e) {
				Logger.LogError($"Build failed: {e}");
				return Finish(
					new BuildResult {
						Type    = BuildResultType.Failed,
						Message = $"Build failed with exception: {e.Message}"
					}
				);
			} finally {
				IsBuilding = false;
			}
		}

		public static string[] GetScenesToBuild(IMod[] mods) {
			var list = new List<string>();

			try {
				if (mods != null) {
					foreach (var mod in mods) {
						var path = mod.GetData("folder", "");
						if (string.IsNullOrEmpty(path)) continue;
						var t = Path.Combine(path, "buildscenes");
						if (!Directory.Exists(t)) continue;
						var files = Directory.GetFiles(t, "*.unity", SearchOption.AllDirectories)
							.Select(
								s => "Assets/"
									+ Path.GetRelativePath(Application.dataPath, s)
										.Replace("\\", "/")
							)
							.ToArray();
						list.AddRange(files);
					}
				}

				list.Sort((a, b) => a.EndsWith("main.unity") ? -1 : 1);
			} catch (Exception ex) {
				Logger.LogWarning(new Exception("Error getting build scenes from mods", ex));
			}

			return Array.Empty<string>();
		}

		public static IMod[] GetKernelMods(IMod[] mods)
			=> mods
				.Where(m => m.GetMetadata().IsKernel())
				.ToArray();

		private static string GetPath(string path)
			=> string.IsNullOrEmpty(path)
				? null
				: Path.Combine("Assets", Path.GetRelativePath(Application.dataPath, path))
					.Replace("\\", "/")
					.ToLower()
					.Replace("assets/../", "");
	}
}