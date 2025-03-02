#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Mods.Metadata;
using Nox.CCK.Mods.Panels;
using Nox.CCK.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = Nox.CCK.Utils.Logger;

namespace dev.nox.game_builder
{
    public class GameBuilder : EditorModInitializer
    {
        public static EditorModCoreAPI CoreAPI;
        private EditorPanel buildPanel;

        public void OnInitializeEditor(EditorModCoreAPI api)
        {
            CoreAPI = api;
            buildPanel = api.PanelAPI.AddLocalPanel(new BuildGamePanel());
        }

        public void OnDispose()
        {
            CoreAPI.PanelAPI.RemoveLocalPanel(buildPanel);
        }

        public static string[] SelectedMods
        {
            get
            {
                var mods = CoreAPI.ModAPI.GetMods();
                return (from mod in mods select mod.GetMetadata() into meta where meta != null where meta.GetCustom("kernel", false) select meta.GetId()).ToArray();
            }
            set
            {
                var mods = CoreAPI.ModAPI.GetMods();
                foreach (var mod in mods)
                {
                    var meta = mod.GetMetadata();
                    meta?.SetCustom("kernel", value.Contains(meta.GetId()));
                    meta?.Save(mod.GetData("manifest", ""));
                }
            }
        }

        public static string BuildFolder
        {
            get
            {
                var path = Path.GetRelativePath(Path.Combine(Application.dataPath, ".."),
                    Config.LoadEditor().Get("gamebuilder.build_folder", "Build"));
                if (path.StartsWith(".."))
                    path = Path.GetFullPath(path);
                return path;
            }
            set
            {
                var relative = Path.GetRelativePath(Path.Combine(Application.dataPath, ".."), value);
                if (relative.StartsWith(".."))
                    relative = Path.GetFullPath(value);
                var config = Config.LoadEditor();
                config.Set("gamebuilder.build_folder", relative);
                config.Save();
            }
        }

        public static string[] UnAllowedFolders = new[]
        {
            Path.Combine(Application.dataPath, "..", "Assets"),
            Path.Combine(Application.dataPath, "..", "Library"),
            Path.Combine(Application.dataPath, "..", "ProjectSettings"),
            Path.Combine(Application.dataPath, "..", "Packages"),
            Path.Combine(Application.dataPath, "..", "UnAllowedFolder")
        };

        public static bool IsAllowedFolder(string path)
        {
            var fullPath = Path.GetFullPath(path);
            if (UnAllowedFolders.Any(folder => fullPath.StartsWith(Path.GetFullPath(folder))))
                return false;
            return Path.GetRelativePath(Application.dataPath, fullPath) != "..";
        }

        private static bool _isBuilding = false;
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static async UniTask BuildPlayer(string BuildPath, Platform platform)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            try
            {
                if (_isBuilding)
                {
                    EditorUtility.DisplayDialog("Building", "The game is already building.", "Ok");
                    return;
                }

                _isBuilding = true;

                if (!platform.IsSupported())
                {
                    EditorUtility.DisplayDialog("Unsupported platform",
                        $"The platform {platform.GetPlatformName()} is not supported on this editor version.", "Ok");
                    _isBuilding = false;
                    return;
                }

                if (!Directory.Exists(BuildFolder))
                {
                    EditorUtility.DisplayDialog("Invalid folder", "The selected folder does not exist.", "Ok");
                    _isBuilding = false;
                    return;
                }

                if (!IsAllowedFolder(BuildFolder))
                {
                    EditorUtility.DisplayDialog("Invalid folder", "The selected folder is not allowed.", "Ok");
                    _isBuilding = false;
                    return;
                }

                if (Directory.Exists(BuildPath))
                {
                    if (Directory.GetFiles(BuildPath).Length > 0 && !EditorUtility.DisplayDialog("Folder exists",
                            "The selected folder already exists, do you want to overwrite it?", "Yes", "No"))
                    {
                        EditorUtility.DisplayDialog("Build failed", "The game was not built successfully.", "Ok");
                        _isBuilding = false;
                        return;
                    }

                    Directory.Delete(BuildPath, true);
                }

                Directory.CreateDirectory(BuildPath);

                var resultbuild = BuildGame.BuildPlayer(platform, BuildPath, Application.productName);

                if (!resultbuild)
                {
                    EditorUtility.DisplayDialog("Build failed", "The game was not built successfully.", "Ok");
                    _isBuilding = false;
                    return;
                }


                var mods = CoreAPI.ModAPI.GetMods();

                var ouputData = Path.Combine(BuildPath, Application.productName + "_Data", "Nox");
                var gameData = Path.Combine(ouputData, "game_data.json");

                Directory.CreateDirectory(ouputData);
                Directory.CreateDirectory(ouputData);

                var resultassets = BuildAssets.BuildAsAssetBundles(mods, platform, ouputData);

                if (resultassets == null)
                {
                    EditorUtility.DisplayDialog("Build failed", "The game was not built successfully.", "Ok");
                    _isBuilding = false;
                    return;
                }

                var metadatas = new JArray();

                EditorUtility.DisplayProgressBar("Building game", "Processing mods...", 0);

                for (var i = 0; i < mods.Length; i++)
                {
                    var mod = mods[i];

                    var meta = mod.GetMetadata();

                    EditorUtility.DisplayProgressBar("Building game", "Processing mod " + meta.GetId(),
                        (float)i / mods.Length);

                    var obj = meta.ToObject();

                    obj["kernel"] = new JObject()
                    {
                        ["active"] = meta.GetCustom("kernel", false),
                        ["base_path"] =
                            Path.Combine("Assets",
                                    Path.GetRelativePath(Application.dataPath, mod.GetData<string>("folder")))
                                .Replace("\\", "/").ToLower(),
                        ["assets_path"] =
                            Path.Combine("Assets",
                                    Path.GetRelativePath(Application.dataPath, mod.GetData<string>("assets")))
                                .Replace("\\", "/").ToLower(),
                        ["definition"] =
                            Path.Combine("Assets",
                                    Path.GetRelativePath(Application.dataPath, mod.GetData<string>("definition")))
                                .Replace("\\", "/").ToLower(),
                        ["manifest"] = Path
                            .Combine("Assets",
                                Path.GetRelativePath(Application.dataPath, mod.GetData<string>("manifest")))
                            .Replace("\\", "/").ToLower()
                    };

                    var asset_result = resultassets.FirstOrDefault(r => r.mod.GetMetadata().Match(meta.GetId()));

                    JArray asset_obj = new();
                    if (asset_result != null)
                        foreach (var asset in asset_result.outputs)
                            if (File.Exists(asset))
                            {
                                var bundle = AssetBundle.LoadFromFile(asset);
                                if (bundle == null) continue;
                                asset_obj.Add(new JObject()
                                {
                                    ["name"] = Path.GetFileName(asset),
                                    ["file"] = Path.Combine(Path.GetRelativePath(ouputData, asset)).Replace("\\", "/")
                                        .ToLower(),
                                    ["assets"] = new JArray(bundle.GetAllAssetNames().Select(a => a.ToLower())),
                                    ["scenes"] = new JArray(bundle.GetAllScenePaths().Select(a => a.ToLower()))
                                });
                                bundle.Unload(true);
                            }

                    obj["kernel"]["assets"] = asset_obj;

                    metadatas.Add(obj);
                }

                EditorUtility.DisplayProgressBar("Building game", "Saving game data...", 1);

                File.WriteAllText(gameData, new JObject
                {
                    ["mods"] = metadatas,
                    ["engine"] = new JObject
                    {
                        ["name"] = Nox.CCK.Utils.Engine.Unity.GetEngineName(),
                        ["version"] = Application.unityVersion.Split('f')[0]
                    },
                    ["platform"] = platform.GetPlatformName()
                }.ToString());

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Build success", "The game was built successfully.", "Ok");
                EditorApplication.Beep();
                EditorUtility.RevealInFinder(BuildPath);
                
                
                _isBuilding = false;
            }
            catch (Exception e)
            {
                Logger.LogError(e);
                EditorUtility.DisplayDialog("Build failed", "An error occurred while building the game.", "Ok");
                EditorApplication.Beep();
                _isBuilding = false;
            }
        }
    }

    public class BuildGamePanel : EditorPanelBuilder
    {
        public string Id { get; } = "builder";
        public string Name { get; } = "Game/Builder";
        public bool Hidded { get; } = false;
        internal VisualElement _root = new();

        public VisualElement OnOpenned(Dictionary<string, object> data)
        {
            _root.ClearBindings();
            _root.Clear();
            var child = GameBuilder.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("builder.uxml").CloneTree();
            child.style.flexGrow = 1;
            _root.Add(child);
            _root.Q<Label>("version").text = "v" + GameBuilder.CoreAPI.ModMetadata.GetVersion();

            var config = Config.LoadEditor();

            _root.Q<TextField>("build-folder").value = GameBuilder.BuildFolder;
            _root.Q<TextField>("build-folder")
                .RegisterCallback<ChangeEvent<string>>(evt => GameBuilder.BuildFolder = evt.newValue);
            _root.Q<Button>("open-build-folder").clicked += () => EditorUtility.RevealInFinder(GameBuilder.BuildFolder);
            _root.Q<Button>("build-folder-select").clicked += () =>
            {
                var path = EditorUtility.SaveFolderPanel("Select folder to save build", "", "");
                if (string.IsNullOrEmpty(path)) return;
                if (!Directory.Exists(path))
                {
                    EditorUtility.DisplayDialog("Invalid folder", "The selected folder does not exist.", "Ok");
                    return;
                }

                if (!GameBuilder.IsAllowedFolder(path))
                {
                    EditorUtility.DisplayDialog("Invalid folder", "The selected folder is not allowed.", "Ok");
                    return;
                }

                GameBuilder.BuildFolder = path;
                _root.Q<TextField>("build-folder").value = GameBuilder.BuildFolder;
            };
            var platform = _root.Q<EnumField>("platform-field");
            platform.Init(PlatformExtensions.CurrentPlatform);
            _root.Q<Button>("detect-platform").clicked += () => platform.Init(PlatformExtensions.CurrentPlatform);
            platform.RegisterValueChangedCallback(evt =>
            {
                var value = (Platform)evt.newValue;

                if (!value.IsSupported())
                {
                    EditorUtility.DisplayDialog("Unsupported platform",
                        $"The platform {value.GetPlatformName()} is not supported on this editor version.", "Ok");
                    platform.Init((Platform)evt.previousValue);
                    return;
                }

                if (EditorUtility.DisplayDialog("Change platform",
                        $"Are you sure you want to change the platform to {value.GetPlatformName()}?", "Yes", "No"))
                {
                    PlatformExtensions.CurrentPlatform = value;
                    platform.Init(value);
                    return;
                }

                platform.Init((Platform)evt.previousValue);
            });

            _root.Q<Button>("build-button").clicked += () =>
                GameBuilder.BuildPlayer(GameBuilder.BuildFolder, (Platform)platform.value).Forget();

            var container = _root.Q<VisualElement>("list");

            var checkAll = _root.Q<Toggle>("check-all");
            checkAll.SetValueWithoutNotify(VerifAllSelected());
            checkAll.RegisterValueChangedCallback(evt =>
            {
                var mods = GameBuilder.CoreAPI.ModAPI.GetMods();
                GameBuilder.SelectedMods =
                    evt.newValue ? mods.Select(m => m.GetMetadata().GetId()).ToArray() : new string[0];
                RefreshList(container);
            });

            _root.Q<Button>("refresh-list").clicked += () => RefreshList(container);
            RefreshList(container);

            return _root;
        }


        private void RefreshList(VisualElement container)
        {
            var list = GameBuilder.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("list.uxml");

            container.Clear();
            var mods = GameBuilder.CoreAPI.ModAPI.GetMods();
            foreach (var mod in mods)
            {
                var item = list.CloneTree();
                item.Q<Label>("title").text = mod.GetMetadata().GetId();
                item.Q<Toggle>("check").value = GameBuilder.SelectedMods.Contains(mod.GetMetadata().GetId());

                item.Q<Toggle>("check").RegisterValueChangedCallback(evt =>
                {
                    var selected = GameBuilder.SelectedMods.ToList();
                    if (evt.newValue)
                        selected.Add(mod.GetMetadata().GetId());
                    else selected.Remove(mod.GetMetadata().GetId());
                    GameBuilder.SelectedMods = selected.ToArray();
                    _root.Q<Toggle>("check-all").SetValueWithoutNotify(VerifAllSelected());
                });

                item.Q<Toggle>("startup").value = mod.GetMetadata().GetCustom("kernel", false);
                item.Q<Toggle>("startup").RegisterValueChangedCallback(evt =>
                {
                    var meta = mod.GetMetadata();
                    meta.SetCustom("kernel", evt.newValue);
                    meta.Save(mod.GetData("manifest", ""));
                });

                item.Q<Button>("select").clicked += () =>
                {
                    var path = mod.GetData("manifest", "");
                    if (File.Exists(path))
                        EditorUtility.RevealInFinder(path);
                    else EditorUtility.DisplayDialog("Error!", "No folder found!", "Ok");
                };

                var noMain = item.Q("no-main");
                var noClient = item.Q("no-client");
                var hasCustom = item.Q("has-custom");
                var isEditor = item.Q("is-editor");
                
                noMain.style.display = mod.GetMains().Length == 0 ? DisplayStyle.Flex : DisplayStyle.None;
                noClient.style.display = mod.GetClients().Length == 0 ? DisplayStyle.Flex : DisplayStyle.None;
                hasCustom.style.display = mod.GetCustomEntries().Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
                isEditor.style.display = mod.GetEditors().Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;

                container.Add(item);
            }
        }

        private bool VerifAllSelected()
        {
            var mods = GameBuilder.CoreAPI.ModAPI.GetMods();
            return mods.All(m => GameBuilder.SelectedMods.Contains(m.GetMetadata().GetId()));
        }

        public void OnClosed()
        {
        }
    }
}
#endif