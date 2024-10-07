#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Nox.CCK;
using Nox.CCK.Worlds;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Nox.Editor.Worlds
{
    [CustomEditor(typeof(MainDescriptor)), CanEditMultipleObjects]
    public class MainDescriptorEditor : UnityEditor.Editor
    {
        public override bool UseDefaultMargins() => false;

        public override VisualElement CreateInspectorGUI()
        {
            var root = Resources.Load<VisualTreeAsset>("api.nox.cck.world.maindescriptor").CloneTree();
            var descriptor = target as MainDescriptor;
            var comp = root.Q<VisualElement>("compiled-message");
            if (descriptor.IsCompiled)
            {
                comp.style.display = DisplayStyle.Flex;
                comp.Q<Image>("icon").image = Resources.Load<Texture2D>("Warning");
            }
            else
            {
                comp.style.display = DisplayStyle.None;
            }

            var spawns = root.Q<ListView>("spawns");
            spawns.showAddRemoveFooter = !descriptor.IsCompiled;
            spawns.allowAdd = !descriptor.IsCompiled;
            spawns.allowRemove = !descriptor.IsCompiled;
            spawns.showBoundCollectionSize = !descriptor.IsCompiled;
            spawns.makeItem = () =>
            {
                var item = new VisualElement();
                item.style.paddingLeft = 2;
                item.style.paddingRight = 8;
                var obj = new ObjectField() { objectType = typeof(GameObject), label = "#-", focusable = !descriptor.IsCompiled };
                if (!descriptor.IsCompiled)
                    obj.RegisterValueChangedCallback(evt =>
                    {
                        if (item.userData is int i && i >= 0 && i < descriptor.GetSpawns().Count)
                            descriptor.Spawns[i] = evt.newValue as GameObject;
                    });
                else obj.SetEnabled(false);
                item.Add(obj);
                return item;
            };
            spawns.bindItem = (e, i) =>
            {
                e.Q<ObjectField>().label = "#" + i;
                e.Q<ObjectField>().value = descriptor.GetSpawns()[i];
                e.userData = i;
            };
            spawns.itemsSource = descriptor.GetSpawns();

            if (!descriptor.IsCompiled)
                root.Q<Button>("spawns-normalize").clicked += () =>
                {
                    var o = descriptor.EstimateSpawns();
                    descriptor.Spawns = o.Values.ToList();
                    spawns.itemsSource = descriptor.GetSpawns();
                };
            else root.Q<Button>("spawns-normalize").SetEnabled(false);

            var networkObjects = root.Q<ListView>("network-objects");
            networkObjects.showAddRemoveFooter = !descriptor.IsCompiled;
            networkObjects.allowAdd = !descriptor.IsCompiled;
            networkObjects.allowRemove = !descriptor.IsCompiled;
            networkObjects.showBoundCollectionSize = !descriptor.IsCompiled;
            networkObjects.makeItem = () =>
            {
                var item = new VisualElement();
                item.style.paddingLeft = 2;
                item.style.paddingRight = 8;
                var obj = new ObjectField() { objectType = typeof(NetworkObject), label = "#-", focusable = !descriptor.IsCompiled };
                if (!descriptor.IsCompiled)
                    obj.RegisterValueChangedCallback(evt =>
                    {
                        if (item.userData is int i && i >= 0 && i < descriptor.GetNetworkObjects().Count)
                            descriptor.GetNetworkObjects()[i] = evt.newValue as NetworkObject;
                    });
                else obj.SetEnabled(false);
                item.Add(obj);
                return item;
            };
            networkObjects.bindItem = (e, i) =>
            {
                e.Q<ObjectField>().label = "#" + descriptor.GetNetworkObjects()[i]?.networkId;
                e.Q<ObjectField>().value = descriptor.GetNetworkObjects()[i];
                e.userData = i;
            };
            networkObjects.itemsSource = descriptor.GetNetworkObjects();

            if (!descriptor.IsCompiled)
            {
                root.Q<Button>("network-objects-normalize").clicked += () =>
                {
                    var o = descriptor.EstimateNetworkObjects();
                    foreach (var obj in o)
                        obj.Value.networkId = obj.Key;
                    descriptor.NetworkObjects = o.Values.ToList();
                    networkObjects.itemsSource = descriptor.GetNetworkObjects();
                };

                root.Q<Button>("network-objects-detect").clicked += () =>
                {
                    var o = new List<NetworkObject>();
                    foreach (var ro in descriptor.gameObject.scene.GetRootGameObjects())
                        o.AddRange(ro.GetComponentsInChildren<NetworkObject>());
                    descriptor.NetworkObjects = o;
                    networkObjects.itemsSource = descriptor.GetNetworkObjects();
                };
            }
            else
            {
                root.Q<Button>("network-objects-normalize").SetEnabled(false);
                root.Q<Button>("network-objects-detect").SetEnabled(false);
            }

            var spawnType = root.Q<EnumField>("spawn-type");
            spawnType.Init(descriptor.SpawnType);
            if (!descriptor.IsCompiled)
                spawnType.RegisterValueChangedCallback(evt => descriptor.SpawnType = (SpawnType)evt.newValue);
            else spawnType.SetEnabled(false);

            var sc = AssetDatabase.LoadAssetAtPath<SceneAsset>(descriptor.gameObject.scene.path);
            root.Q<ObjectField>("current-scene").value = sc;
            if (!descriptor.IsCompiled)
                root.Q<ObjectField>("current-scene").RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue == sc) return;
                    root.Q<ObjectField>("current-scene").value = sc;
                });

            else root.Q<ObjectField>("current-scene").SetEnabled(false);

            var respawnHeight = root.Q<DoubleField>("respawn-height");
            respawnHeight.value = descriptor.RespawnHeight;
            if (!descriptor.IsCompiled)
                respawnHeight.RegisterValueChangedCallback(evt => descriptor.RespawnHeight = evt.newValue);
            else respawnHeight.SetEnabled(false);

            var scenes = root.Q<ListView>("scenes");
            scenes.showAddRemoveFooter = !descriptor.IsCompiled;
            scenes.allowAdd = !descriptor.IsCompiled;
            scenes.allowRemove = !descriptor.IsCompiled;
            scenes.showBoundCollectionSize = !descriptor.IsCompiled;

            scenes.makeItem = () =>
            {
                var item = new VisualElement();
                item.style.paddingLeft = 2;
                item.style.paddingRight = 8;
                var obj = new ObjectField() { objectType = typeof(SceneAsset), label = "#-", focusable = !descriptor.IsCompiled };
                if (!descriptor.IsCompiled)
                    obj.RegisterValueChangedCallback(evt =>
                    {
                        if (item.userData is int i && i >= 0 && i < descriptor.Scenes.Count)
                            descriptor.Scenes[i] = evt.newValue as SceneAsset;
                    });
                else obj.SetEnabled(false);
                item.Add(obj);
                return item;
            };

            scenes.bindItem = (e, i) =>
            {
                e.Q<ObjectField>().label = "#" + i;
                var scenes = descriptor.GetScenes();
                var scene = scenes.Count > i ? scenes[i] : null;
                e.Q<ObjectField>().value = scene != null ? AssetDatabase.LoadAssetAtPath<SceneAsset>(scene) : null;
                e.userData = i;
            };

            scenes.itemsAdded += e =>
            {
                if (descriptor.IsCompiled) return;
                descriptor.Scenes.Add(null);
                scenes.itemsSource = descriptor.GetScenes();
            };

            scenes.itemsRemoved += e =>
            {
                if (descriptor.IsCompiled) return;
                descriptor.Scenes = descriptor.Scenes.Where((_, i) => !e.Contains(i)).ToList();
                scenes.itemsSource = descriptor.GetScenes();
            };

            scenes.itemIndexChanged += (i, j) =>
            {
                if (descriptor.IsCompiled) return;
                var sc = descriptor.Scenes;
                if (i < 0 || i >= sc.Count) return;
                if (j < 0 || j >= sc.Count) return;
                (sc[j], sc[i]) = (sc[i], sc[j]);
                descriptor.Scenes = sc;
                scenes.itemsSource = descriptor.GetScenes();
            };

            scenes.itemsSource = descriptor.GetScenes();
            if (!descriptor.IsCompiled)
                root.Q<Button>("scenes-normalize").clicked += () =>
                {
                    var o = descriptor.EstimateScenes();
                    descriptor.Scenes = o.Values.Skip(1).ToList();
                    scenes.itemsSource = descriptor.GetScenes();
                };
            else root.Q<Button>("scenes-normalize").SetEnabled(false);

            root.Q<Button>("open-scenes").clicked += () =>
            {
                foreach (var scene in descriptor.GetScenes())
                    EditorSceneManager.OpenScene(scene, OpenSceneMode.Additive);
            };

            if (!descriptor.IsCompiled)
                root.Q<Button>("detect-scenes").clicked += () =>
                {
                    var descriptors = BaseDescriptor.GetDescriptors();
                    Debug.Log("Detected " + descriptors.Length + " descriptors");
                    var sc = new List<SceneAsset>();
                    foreach (var desc in descriptors)
                        if (desc is SubDescriptor mD)
                        {
                            var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(mD.gameObject.scene.path);
                            if (scene != null && !sc.Contains(scene))
                                sc.Add(scene);
                        }
                    descriptor.Scenes = sc;
                    scenes.itemsSource = descriptor.GetScenes();
                };
            else root.Q<Button>("detect-scenes").SetEnabled(false);

            var features = root.Q<ListView>("features");
            features.showAddRemoveFooter = !descriptor.IsCompiled;
            features.allowAdd = !descriptor.IsCompiled;
            features.allowRemove = !descriptor.IsCompiled;
            features.showBoundCollectionSize = !descriptor.IsCompiled;
            features.makeItem = () =>
            {
                var item = new VisualElement();
                item.style.paddingLeft = 2;
                item.style.paddingRight = 8;
                var obj = new TextField() { label = "#-", focusable = !descriptor.IsCompiled };
                if (!descriptor.IsCompiled)
                    obj.RegisterValueChangedCallback(evt =>
                    {
                        if (item.userData is int i && i >= 0 && i < descriptor.Features.Count)
                            descriptor.Features[i] = evt.newValue;
                    });
                else obj.SetEnabled(false);
                item.Add(obj);
                return item;
            };
            features.bindItem = (e, i) =>
            {
                e.Q<TextField>().label = "#" + i;
                e.Q<TextField>().value = descriptor.Features[i];
                e.userData = i;
            };

            features.itemsSource = descriptor.GetFeatures();

            if (!descriptor.IsCompiled)
                root.Q<Button>("features-normalize").clicked += () =>
                {
                    var o = descriptor.EstimateFeatures();
                    descriptor.Features = o.Values.ToList();
                    features.itemsSource = descriptor.GetFeatures();
                };
            else root.Q<Button>("features-normalize").SetEnabled(false);

            var mods = root.Q<ListView>("mods");
            mods.showAddRemoveFooter = !descriptor.IsCompiled;
            mods.allowAdd = !descriptor.IsCompiled;
            mods.allowRemove = !descriptor.IsCompiled;
            mods.showBoundCollectionSize = !descriptor.IsCompiled;

            mods.makeItem = () =>
            {
                var item = new VisualElement();
                item.style.paddingLeft = 2;
                item.style.paddingRight = 8;
                var obj = new TextField() { label = "#-", focusable = !descriptor.IsCompiled };
                if (!descriptor.IsCompiled)
                    obj.RegisterValueChangedCallback(evt =>
                    {
                        if (item.userData is int i && i >= 0 && i < descriptor.ModRequirements.Count)
                        {
                            Debug.Log("Changed mod" + item.userData + " (value) to " + evt.newValue);
                            descriptor.ModRequirements[i].Id = evt.newValue;
                        }
                    });
                else obj.SetEnabled(false);
                item.Add(obj);
                var e = new EnumField() { label = "Flags", focusable = !descriptor.IsCompiled };
                if (!descriptor.IsCompiled)
                    e.RegisterValueChangedCallback(evt =>
                    {
                        if (item.userData is int i && i >= 0 && i < descriptor.ModRequirements.Count)
                        {
                            Debug.Log("Changed mod " + item.userData + " (flag) to " + evt.newValue);
                            descriptor.ModRequirements[i].Flags = (ModRequirmentFlags)evt.newValue;
                        }
                    });
                else e.SetEnabled(false);
                item.Add(e);
                return item;
            };

            mods.bindItem = (e, i) =>
            {
                var mod = descriptor.GetRequirementMods()[i];
                e.Q<TextField>().label = "#" + i;
                e.Q<TextField>().value = mod?.Id ?? "";
                if (mod != null) e.Q<EnumField>().Init(mod.Flags);
                else e.Q<EnumField>().Init(ModRequirmentFlags.None);
                e.userData = i;
            };

            mods.itemsAdded += e =>
            {
                if (descriptor.IsCompiled) return;
                descriptor.ModRequirements.Add(new ModRequirement()
                {
                    Id = "",
                    Flags = ModRequirmentFlags.None
                });
                mods.itemsSource = descriptor.GetRequirementMods();
            };

            mods.itemsRemoved += e =>
            {
                if (descriptor.IsCompiled) return;
                descriptor.ModRequirements = descriptor.ModRequirements.Where((_, i) => !e.Contains(i)).ToList();
                mods.itemsSource = descriptor.GetRequirementMods();
            };

            mods.itemIndexChanged += (i, j) =>
            {
                if (descriptor.IsCompiled) return;
                var m = descriptor.ModRequirements;
                if (i < 0 || i >= m.Count) return;
                if (j < 0 || j >= m.Count) return;
                (m[j], m[i]) = (m[i], m[j]);
                mods.itemsSource = descriptor.GetRequirementMods();
            };

            if (!descriptor.IsCompiled)
                root.Q<Button>("mods-normalize").clicked += () =>
                {
                    var o = descriptor.EstimateMods();
                    descriptor.ModRequirements = o.Values.ToList();
                    mods.itemsSource = descriptor.GetRequirementMods();
                };
            else root.Q<Button>("mods-normalize").SetEnabled(false);

            mods.itemsSource = descriptor.GetRequirementMods();

            return root;
        }


        private static SupportBuildTarget _buildTarget = SupportBuildTarget.NoTarget;
        private static bool _building = false;
        public static bool IsBuilding => _building;

        private static BuildResult EndBuild(string path, string message, bool dialog, SceneSetup[] currentScenes)
        {
            _building = false;
            if (dialog && !string.IsNullOrEmpty(message))
                EditorUtility.DisplayDialog("Nox Build Error", message, "OK");
            if (currentScenes.Length > 0)
                EditorSceneManager.RestoreSceneManagerSetup(currentScenes);
            return new BuildResult
            {
                Success = string.IsNullOrEmpty(message),
                ErrorMessage = message,
                path = path
            };
        }

        [MenuItem("Nox/CCK/World/Build")]
        public static void BuildWorld()
        {
            BuildWorld(null, SupportBuildTarget.NoTarget, true);
        }

        public static BuildResult BuildWorld(MainDescriptor descriptor = null, SupportBuildTarget buildTarget = SupportBuildTarget.NoTarget, bool dialog = true)
        {
            _building = true;
            if (EditorApplication.isCompiling)
                return EndBuild(null, "Cannot build while compiling", dialog, new SceneSetup[0]);
            if (EditorApplication.isPlaying)
                return EndBuild(null, "Cannot build while playing", dialog, new SceneSetup[0]);

            if (buildTarget == SupportBuildTarget.NoTarget)
                buildTarget = _buildTarget;
            if (buildTarget == SupportBuildTarget.NoTarget)
                return EndBuild(null, "No build target selected", dialog, new SceneSetup[0]);

            var currentScene = SceneManager.GetActiveScene();
            if (descriptor == null) descriptor = GetWorldDescriptors(dialog).FirstOrDefault();
            if (descriptor == null) return EndBuild(null, "No World Descriptor Found", dialog, new SceneSetup[0]);

            if (!EditorSceneManager.SaveScene(currentScene))
                return EndBuild(null, "Failed to save current scene", dialog, new SceneSetup[0]);

            string assetBundleDirectory = "Assets/Output/";
            if (!Directory.Exists(assetBundleDirectory))
                Directory.CreateDirectory(assetBundleDirectory);
            string buildBundleDirectory = "Assets/BuildOutput/";
            if (!Directory.Exists(buildBundleDirectory))
                Directory.CreateDirectory(buildBundleDirectory);

            string buildId = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + '-' + currentScene.name.ToLower();
            var pathBuild = buildBundleDirectory + buildId + "/";
            if (!Directory.Exists(pathBuild))
                Directory.CreateDirectory(pathBuild);
            var dependencyPath = pathBuild + "Dependencies/";
            if (!Directory.Exists(dependencyPath))
                Directory.CreateDirectory(dependencyPath);

            if (!EditorSceneManager.SaveOpenScenes())
                return EndBuild(null, "Failed to save all scenes", dialog, new SceneSetup[0]);

            var currentScenes = EditorSceneManager.GetSceneManagerSetup();

            var scenesSet = descriptor.EstimateScenes();
            var assets = new List<string>();
            var scenes = new List<string>();
            var initialGUIDs = new Dictionary<string, string>();
            var endGUIDs = new Dictionary<string, string>();

            foreach (var scene in scenesSet)
            {
                var path = AssetDatabase.GetAssetPath(scene.Value);
                var destination = pathBuild + scene.Key + ".unity";
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return EndBuild(null, "Scene not found: " + scene.Value.name, dialog, currentScenes);
                File.Copy(path, destination);
                File.Copy(path + ".meta", destination + ".meta");
                Debug.Log("Copied " + path + " to " + destination);
                assets.Add(destination);
                scenes.Add(destination);
                initialGUIDs.Add(destination, AssetDatabase.AssetPathToGUID(path));
                endGUIDs.Add(destination, Guid.NewGuid().ToString("N"));
                foreach (string dependency in AssetDatabase.GetDependencies(path))
                {
                    var gui = Guid.NewGuid().ToString("N");
                    var destinationPath = pathBuild + "Dependencies/" + gui + Path.GetExtension(dependency);
                    if (assets.Contains(destinationPath)) continue;
                    if (Path.GetExtension(dependency) == ".cs") continue;
                    if (Path.GetExtension(dependency) == ".dll") continue;
                    if (Path.GetExtension(dependency) == ".meta") continue;
                    if (Path.GetExtension(dependency) == ".unity") continue;
                    File.Copy(dependency, destinationPath);
                    File.Copy(dependency + ".meta", destinationPath + ".meta");
                    Debug.Log("Copied " + dependency + " to " + destinationPath);
                    assets.Add(destinationPath);
                    initialGUIDs.Add(destinationPath, AssetDatabase.AssetPathToGUID(dependency));
                    endGUIDs.Add(destinationPath, gui);
                }
            }

            foreach (var asset in assets)
            {
                if (new[] {
                    ".unity", ".prefab",  ".asset",
                    ".mat", ".anim", ".controller"
                }.Contains(Path.GetExtension(asset)))
                {
                    string text = File.ReadAllText(asset);
                    foreach (var guid in initialGUIDs)
                        text = new Regex(guid.Value).Replace(text, endGUIDs[guid.Key]);
                    File.WriteAllText(asset, text);
                }
                string meta = File.ReadAllText(asset + ".meta");
                foreach (var guid in initialGUIDs)
                    meta = new Regex(guid.Value).Replace(meta, endGUIDs[guid.Key]);
                File.WriteAllText(asset + ".meta", meta);
            }

            AssetDatabase.Refresh();

            // get all new uids
            var newGUIDs = new Dictionary<string, string>();
            foreach (var asset in assets)
                if (asset.StartsWith(dependencyPath))
                {
                    var fileName = Path.GetFileNameWithoutExtension(asset);
                    newGUIDs.Add(fileName, AssetDatabase.AssetPathToGUID(asset));
                    Debug.Log("Dependency: " + fileName + " -> " + newGUIDs[fileName]);
                }

            // set updated uids
            foreach (var asset in assets)
            {
                if (!new[] {
                    ".unity", ".prefab",  ".asset",
                    ".mat", ".anim", ".controller"
                }.Contains(Path.GetExtension(asset))) continue;
                string text = File.ReadAllText(asset);
                foreach (var guid in newGUIDs)
                    text = new Regex(guid.Key).Replace(text, guid.Value);
                File.WriteAllText(asset, text);
            }

            AssetDatabase.Refresh();

            var loadedScenes = new List<Scene>() { EditorSceneManager.OpenScene(scenes[0], OpenSceneMode.Single) };
            foreach (var scene in scenes.Skip(1))
                loadedScenes.Add(EditorSceneManager.OpenScene(scene, OpenSceneMode.Additive));

            MainDescriptor mD = null;
            foreach (var root in loadedScenes[0].GetRootGameObjects())
            {
                mD = root.GetComponentInChildren<MainDescriptor>();
                if (mD != null) break;
            }

            List<SubDescriptor> sDs = new();
            foreach (var scene in loadedScenes.Skip(1))
            {
                SubDescriptor sD = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    sD = root.GetComponentInChildren<SubDescriptor>();
                    if (sD != null) break;
                }
                if (sD != null) sDs.Add(sD);
                else return EndBuild(null, "Sub Descriptor not found in scene: " + scene.name, dialog, currentScenes);
            }

            // compile all descriptors
            foreach (var desc in new BaseDescriptor[] { mD }.Concat(sDs))
            {
                Debug.Log("Compiling " + desc.name + " in " + desc.gameObject.scene.name);
                desc.Compile();
            }
            // save all scenes and await
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.Refresh();

            foreach (var scene in loadedScenes)
                EditorSceneManager.CloseScene(scene, false);

            _lastbuild_scene = scenes.ToArray();
            _lastbuild_id = buildId;
            _lastbuild_directory = pathBuild;
            _lastbuild_target = (BuildTarget)buildTarget;

            if (!Compile(scenes.ToArray(), buildId, assetBundleDirectory, (BuildTarget)buildTarget))
                return EndBuild(assetBundleDirectory + buildId + ".noxw", "Failed to compile asset bundle", dialog, currentScenes);

            if (dialog)
            {
                EditorUtility.DisplayDialog("Nox Build Success", "Build successful", "OK");
                EditorUtility.RevealInFinder(assetBundleDirectory);
            }
            return EndBuild(assetBundleDirectory + buildId + ".noxw", null, dialog, currentScenes);
        }

        private static string[] _lastbuild_scene;
        private static string _lastbuild_id;
        private static string _lastbuild_directory;
        private static BuildTarget _lastbuild_target;

        [MenuItem("Nox/CCK/World/Get Descriptor")]
        public static void GetWorldDescriptor()
        {
            GetWorldDescriptor();
        }

        public static MainDescriptor[] GetWorldDescriptors(bool dialog = true)
        {
            var currentScene = SceneManager.GetActiveScene();
            var descriptors = currentScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MainDescriptor>()).ToArray();
            if (descriptors.Length == 0 && dialog)
                EditorUtility.DisplayDialog("No World Descriptor Found", "No World Descriptor was found in the current scene.", "OK");
            if (descriptors.Length > 1 && dialog)
                EditorUtility.DisplayDialog("Multiple Descriptors Found", "Multiple World Descriptors were found in the current scene.", "OK");
            if (dialog)
                foreach (var descriptor in descriptors)
                    Selection.activeObject = descriptor;
            return descriptors;
        }

        [MenuItem("Nox/CCK/World/Build Last")]
        public static void BuildLast()
        {
            if (_lastbuild_scene == null || _lastbuild_scene.Length == 0)
            {
                EditorUtility.DisplayDialog("No Last Build", "No last build was found.", "OK");
                return;
            }
            if (!Compile(_lastbuild_scene, _lastbuild_id, "Assets/Output/", _lastbuild_target))
            {
                EditorUtility.DisplayDialog("Failed to compile asset bundle", "Failed to compile asset bundle", "OK");
                return;
            }
            EditorUtility.DisplayDialog("Nox Build Success", "Build successful", "OK");
            EditorUtility.RevealInFinder(_lastbuild_directory);
        }

        private static bool Compile(string[] scenes, string buildId, string assetBundleDirectory, BuildTarget buildTarget)
        {
            var assetSet = new HashSet<string>();
            foreach (var scene in scenes)
                foreach (var dependency in AssetDatabase.GetDependencies(scene))
                {
                    if (Path.GetExtension(dependency) == ".cs") continue;
                    if (Path.GetExtension(dependency) == ".dll") continue;
                    if (Path.GetExtension(dependency) == ".meta") continue;
                    if (Path.GetExtension(dependency) == ".unity") continue;
                    assetSet.Add(dependency);
                }

            AssetBundleBuild definition = new()
            {
                assetBundleName = buildId + ".noxw",
                assetNames = scenes.ToArray(),
                addressableNames = assetSet.ToArray()
            };

            BuildAssetBundlesParameters input = new()
            {
                outputPath = assetBundleDirectory,
                targetPlatform = buildTarget,

                options = BuildAssetBundleOptions.None,
                bundleDefinitions = new AssetBundleBuild[] { definition }
            };

            Debug.Log("Building Asset Bundle: " + input.outputPath);
            foreach (var scene in input.bundleDefinitions)
            {
                Debug.Log("Scene: " + scene.assetBundleName);
                foreach (var asset in scene.assetNames)
                    Debug.Log("Asset: " + asset);
                foreach (var asset in scene.addressableNames)
                    Debug.Log("Addressable: " + asset);
            }


            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(input);

            return manifest != null;
        }

        // check BuildTarget buildTarget is disponible
        public static bool IsBuildTargetSupported(BuildTarget buildTarget)
        {
            return BuildPipeline.IsBuildTargetSupported(
                BuildPipeline.GetBuildTargetGroup(buildTarget),
                buildTarget
            );
        }

        [MenuItem("Nox/CCK/World/Make Descriptor")]
        public static MainDescriptor MakeWorldDescriptor()
        {
            var root = new GameObject("World Descriptor");
            var descriptor = root.AddComponent<MainDescriptor>();
            Selection.activeObject = descriptor;
            return descriptor;
        }
    }
    public class BuildResult
    {
        public bool Success;
        public string ErrorMessage;
        public string path;
    }
}
#endif