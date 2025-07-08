#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nox.CCK.Language;
using Nox.CCK.Mods.Panels;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace api.nox.world
{
    public class WorldLoaderPanel : IEditorPanelBuilder, System.IDisposable
    {
        public string GetId() => "loader";
        public string GetName() => "World/Loader";
        public string GetTitle() => LanguageManager.Get("world.loader.title");
        public bool IsHidden() => false;

        private readonly VisualElement _root = new();
        private VisualElement _worldList;
        private TextField _searchField;
        private Toggle _autoPlayToggle;
        private Toggle _unloadScenesToggle;
        
        private string[] _allWorldFiles       = Array.Empty<string>();
        private string   _currentSearchFilter = "";

        public VisualElement Make(Dictionary<string, object> data)
        {
            _root.Clear();
            
            // Load the UXML interface
            var visualTree = Editor.CoreAPI.AssetAPI.GetAsset<VisualTreeAsset>("loader.uxml");
            if (visualTree != null)
            {
                var tree = visualTree.CloneTree();
                tree.style.flexGrow = 1;
                _root.Add(tree);
            }

            // Cache UI elements
            _worldList = _root.Q<VisualElement>("world-list");
            _searchField = _root.Q<TextField>("search-field");
            _autoPlayToggle = _root.Q<Toggle>("auto-play-toggle");
            _unloadScenesToggle = _root.Q<Toggle>("unload-scenes-toggle");
            
            var refreshButton = _root.Q<Button>("refresh-button");
            var clearCacheButton = _root.Q<Button>("clear-cache-button");
            var openBuildsButton = _root.Q<Button>("open-builds-button");
            var buildSettingsButton = _root.Q<Button>("build-settings-button");

            // Apply translations to UI elements
            ApplyTranslations();

            // Setup event handlers
            // Note: Search field event handler is setup in ApplyTranslations/SetupSearchFieldPlaceholder

            if (refreshButton != null)
            {
                refreshButton.clicked += RefreshWorldList;
            }

            if (clearCacheButton != null)
            {
                clearCacheButton.clicked += ClearCache;
            }

            if (openBuildsButton != null)
            {
                openBuildsButton.clicked += OpenBuildsFolder;
            }

            if (buildSettingsButton != null)
            {
                buildSettingsButton.clicked += OpenBuildSettings;
            }

            // Load initial settings
            LoadSettings();
            
            // Initial world list population
            RefreshWorldList();

            return _root;
        }

        private void ApplyTranslations()
        {
            // Apply translations to all Labels
            var allLabels = _root.Query<Label>().ToList();
            foreach (var label in allLabels)
            {
                if (label.text.StartsWith("world.loader."))
                {
                    label.text = LanguageManager.Get(label.text);
                }
            }

            // Apply translations to TextField labels and placeholders
            if (_searchField != null)
            {
                _searchField.label = LanguageManager.Get("world.loader.search_label");
                // For placeholder, we need to set it manually since UXML keys don't auto-translate
                var placeholderKey = "world.loader.search_placeholder";
                var placeholder = LanguageManager.Get(placeholderKey);
                _searchField.SetValueWithoutNotify("");
                // Unity doesn't have direct placeholder API, so we'll handle it in the UI
            }

            // Apply translations to Toggles
            if (_autoPlayToggle != null)
            {
                _autoPlayToggle.text = LanguageManager.Get("world.loader.auto_play");
            }

            if (_unloadScenesToggle != null)
            {
                _unloadScenesToggle.text = LanguageManager.Get("world.loader.unload_scenes");
            }

            // Apply translations to all Buttons
            var allButtons = _root.Query<Button>().ToList();
            foreach (var button in allButtons)
            {
                var buttonName = button.name;
                switch (buttonName)
                {
                    case "refresh-button":
                        button.text = LanguageManager.Get("world.loader.refresh");
                        break;
                    case "clear-cache-button":
                        button.text = LanguageManager.Get("world.loader.clear_cache");
                        break;
                    case "open-builds-button":
                        button.text = LanguageManager.Get("world.loader.open_builds");
                        break;
                    case "build-settings-button":
                        button.text = LanguageManager.Get("world.loader.build_settings");
                        break;
                }
            }

            // Apply translations to Foldout
            var foldout = _root.Q<Foldout>();
            if (foldout != null)
            {
                foldout.text = LanguageManager.Get("world.loader.advanced_options");
            }

            // Handle search field placeholder manually with a helper text
            SetupSearchFieldPlaceholder();
        }

        private void SetupSearchFieldPlaceholder()
        {
            if (_searchField != null)
            {
                var placeholder = LanguageManager.Get("world.loader.search_placeholder");
                
                // Create a visual placeholder effect
                var originalColor = _searchField.style.color;
                var placeholderColor = new Color(0.6f, 0.6f, 0.6f, 1f);

                // Set initial placeholder state if field is empty
                if (string.IsNullOrEmpty(_searchField.value))
                {
                    _searchField.SetValueWithoutNotify(placeholder);
                    _searchField.style.color = placeholderColor;
                }

                // Handle focus events for placeholder behavior
                _searchField.RegisterCallback<FocusInEvent>((evt) =>
                {
                    if (_searchField.value == placeholder)
                    {
                        _searchField.SetValueWithoutNotify("");
                        _searchField.style.color = originalColor;
                    }
                });

                _searchField.RegisterCallback<FocusOutEvent>((evt) =>
                {
                    if (string.IsNullOrEmpty(_searchField.value))
                    {
                        _searchField.SetValueWithoutNotify(placeholder);
                        _searchField.style.color = placeholderColor;
                    }
                });

                // Update the search change handler to handle placeholder
                _searchField.RegisterValueChangedCallback((evt) =>
                {
                    var actualValue = evt.newValue == placeholder ? "" : evt.newValue;
                    _currentSearchFilter = actualValue?.ToLower() ?? "";
                    FilterAndDisplayWorlds();
                });
            }
        }

        private void RefreshWorldList()
        {
            _allWorldFiles = GetWorldFiles();
            FilterAndDisplayWorlds();
        }

        private void FilterAndDisplayWorlds()
        {
            if (_worldList == null) return;

            _worldList.Clear();

            var filteredFiles = _allWorldFiles.Where(file =>
            {
                if (string.IsNullOrEmpty(_currentSearchFilter))
                    return true;
                
                var fileName = Path.GetFileNameWithoutExtension(file).ToLower();
                return fileName.Contains(_currentSearchFilter);
            }).ToArray();

            if (filteredFiles.Length == 0)
            {
                var noResultsLabel = new Label(LanguageManager.Get("world.loader.no_results"));
                noResultsLabel.AddToClassList("no-results");
                _worldList.Add(noResultsLabel);
                return;
            }

            foreach (var file in filteredFiles)
            {
                CreateWorldItem(file);
            }
        }

        private void CreateWorldItem(string filePath)
        {
            var worldItem = new VisualElement();
            worldItem.AddToClassList("world-item");

            // World name and info
            var infoContainer = new VisualElement();
            infoContainer.AddToClassList("world-info");

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var nameLabel = new Label(fileName);
            nameLabel.AddToClassList("world-name");

            var pathLabel = new Label(Path.GetDirectoryName(filePath));
            pathLabel.AddToClassList("world-path");

            infoContainer.Add(nameLabel);
            infoContainer.Add(pathLabel);

            // Action buttons
            var buttonsContainer = new VisualElement();
            buttonsContainer.AddToClassList("world-buttons");

            var loadButton = new Button(() => LoadWorld(filePath))
            {
                text = LanguageManager.Get("world.loader.load")
            };
            loadButton.AddToClassList("load-button");

            var loadAndPlayButton = new Button(() => StartAndLoadWorld(filePath))
            {
                text = LanguageManager.Get("world.loader.load_play")
            };
            loadAndPlayButton.AddToClassList("load-play-button");

            buttonsContainer.Add(loadButton);
            buttonsContainer.Add(loadAndPlayButton);

            worldItem.Add(infoContainer);
            worldItem.Add(buttonsContainer);
            _worldList.Add(worldItem);
        }

        private static string[] GetWorldFiles() 
            => Directory.GetFiles("Assets", "*.noxw", SearchOption.AllDirectories);

        private void LoadWorld(string path)
        {
            if (_unloadScenesToggle?.value == true)
            {
                UnloadCurrentScenes();
            }

            var bundle = AssetBundle.LoadFromFile(path);
            if (bundle == null)
            {
                Debug.LogError($"Failed to load world bundle: {path}");
                return;
            }

            var scenes = bundle.GetAllScenePaths();
            foreach (var scene in scenes)
            {
                SceneManager.LoadScene(scene, LoadSceneMode.Additive);
            }

            Debug.Log($"Loaded world: {Path.GetFileNameWithoutExtension(path)}");
        }

        private void StartAndLoadWorld(string path)
        {
            if (_autoPlayToggle?.value == true)
            {
                StartPlayMode();
            }
            LoadWorld(path);
        }

        private static void UnloadCurrentScenes()
        {
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.name != "DontDestroyOnLoad")
                {
                    SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        private static void StartPlayMode()
        {
            if (!Application.isPlaying)
                EditorApplication.isPlaying = true;
        }

        private void ClearCache()
        {
            // Clear any cached AssetBundles
            AssetBundle.UnloadAllAssetBundles(true);
            Debug.Log("Asset bundle cache cleared.");
        }

        private void OpenBuildsFolder()
        {
            var buildsPath = Path.Combine(Application.dataPath, "..", "Builds");
            if (!Directory.Exists(buildsPath))
            {
                Directory.CreateDirectory(buildsPath);
            }
            EditorUtility.RevealInFinder(buildsPath);
        }

        private void OpenBuildSettings()
        {
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
        }

        private void LoadSettings()
        {
            if (_autoPlayToggle != null)
            {
                _autoPlayToggle.value = EditorPrefs.GetBool("WorldLoader.AutoPlay", false);
                _autoPlayToggle.RegisterValueChangedCallback(evt => 
                    EditorPrefs.SetBool("WorldLoader.AutoPlay", evt.newValue));
            }

            if (_unloadScenesToggle != null)
            {
                _unloadScenesToggle.value = EditorPrefs.GetBool("WorldLoader.UnloadScenes", true);
                _unloadScenesToggle.RegisterValueChangedCallback(evt => 
                    EditorPrefs.SetBool("WorldLoader.UnloadScenes", evt.newValue));
            }
        }

        public void Dispose()
        {
            _root?.Clear();
        }
    }
}
#endif