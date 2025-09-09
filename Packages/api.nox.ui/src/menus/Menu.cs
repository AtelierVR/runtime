using System;
using System.Collections.Generic;
using System.Linq;
using api.nox.ui.defaults;
using api.nox.ui.histories;
using api.nox.ui.layouts;
using Cysharp.Threading.Tasks;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Utils;
using Nox.UI;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Object = UnityEngine.Object;

namespace api.nox.ui.menus {
	public class Menu : MonoBehaviour, INoxObject, IMenu {
		[Header("Menu Settings")]
		public string defaultKey = HomePage.GetStaticKey();

		public object[] defaultArguments = Array.Empty<object>();

		[Header("References")]
		public BottomOrbiter bottomOrbiter;

		public TopOrbiter    topOrbiter;
		public RectTransform container;
		public GameObject    parent;

		internal HistoryList History;
		internal Client      Client;


		public Dictionary<string, List<NavigationData>> GetDefaultData()
			=> new() {
				{
					"applications",
					new List<NavigationData> {
						new() {
							key = "home",
							// text               = "home",
							iconPath           = "icons/home.png",
							execution          = "home",
							flags              = NavigationFlags.Button,
							executionArguments = new object[] { },
							executionType      = NavigationExecution.Goto,
						},
						new() {
							key = "applications",
							// text               = "applications",
							iconPath           = "icons/apps.png",
							execution          = "applications",
							flags              = NavigationFlags.Button,
							executionArguments = new object[] { },
							executionType      = NavigationExecution.Goto
						},
						new() {
							key = "inventory",
							// text               = "inventory",
							iconPath           = "icons/inventory.png",
							execution          = "inventory",
							flags              = NavigationFlags.Button,
							executionArguments = new object[] { },
							executionType      = NavigationExecution.Goto
						},
						new() {
							key = "friends",
							// text               = "friends",
							iconPath           = "icons/friend.png",
							execution          = "friends",
							flags              = NavigationFlags.Button,
							executionArguments = new object[] { },
							executionType      = NavigationExecution.Goto
						},
						new() {
							key = "search",
							// text               = "search",
							iconPath           = "icons/explore.png",
							execution          = "search",
							flags              = NavigationFlags.Button,
							executionArguments = new object[] { },
							executionType      = NavigationExecution.Goto
						},
						new() {
							key = "settings",
							// text               = "settings",
							iconPath           = "icons/settings.png",
							execution          = "settings",
							flags              = NavigationFlags.Button,
							executionArguments = new object[] { },
							executionType      = NavigationExecution.Goto
						}
					}
				}, {
					"specials",
					new List<NavigationData> {
						new() {
							key = "help",
							// text               = "help",
							iconPath           = "icons/question.png",
							execution          = "help",
							flags              = NavigationFlags.Button,
							executionArguments = new object[] { "ui/how-to-use-menu" },
							executionType      = NavigationExecution.Goto,
						},
						new() {
							key = "mute",
							// text               = "mute",
							iconPath           = "icons/unmute.png",
							execution          = "mute",
							flags              = NavigationFlags.Button,
							executionArguments = new object[] { },
							executionType      = NavigationExecution.Event
						},
						new() {
							key = "session",
							// text               = "sessions",
							iconPath           = "icons/group.png",
							execution          = "session",
							flags              = NavigationFlags.Button,
							executionArguments = new object[] { },
							executionType      = NavigationExecution.Goto
						}
					}
				}, {
					"actions",
					new List<NavigationData> {
						new() {
							key = "notifications",
							// text               = "notifications",
							iconPath           = "icons/notifications.png",
							execution          = "notifications",
							flags              = NavigationFlags.Button,
							executionArguments = new object[] { },
							executionType      = NavigationExecution.Goto,
						},
						new() {
							key              = "time",
							text             = "time",
							flags            = NavigationFlags.Enable,
							getCustomContent = tr => Instantiate(PageManager.GetAsset<GameObject>("prefabs/time.prefab"), tr),
							executionType    = NavigationExecution.None,
						},
						new() {
							key = "exit",
							// text = "exit",
							iconPath           = "icons/power.png",
							execution          = "exit",
							flags              = NavigationFlags.Button,
							executionArguments = new object[] { },
							executionType      = NavigationExecution.Event,
						}
					}
				}, {
					"histories",
					new List<NavigationData> {
						new() {
							key = "back",
							// text               = "back",
							iconPath           = "icons/left.png",
							execution          = "back",
							flags              = NavigationFlags.Button,
							executionArguments = new object[] { },
							executionType      = NavigationExecution.Action,
						},
						new() {
							key = "forward",
							// text               = "forward",
							iconPath           = "icons/right.png",
							execution          = "forward",
							flags              = NavigationFlags.Button,
							executionArguments = new object[] { },
							executionType      = NavigationExecution.Action,
						},
						new() {
							key = "refresh",
							// text               = "refresh",
							iconPath           = "icons/refresh.png",
							execution          = "refresh",
							flags              = NavigationFlags.Button,
							executionArguments = new object[] { },
							executionType      = NavigationExecution.Action,
						}
					}
				}
			};

		public Menu() {
			History = new HistoryList(this);
		}

		private void Awake() {
			parent ??= gameObject;
		}

		private void Start() {
			StartAsync().Forget();
			Client.SendGoto(GetId(), defaultKey, defaultArguments);
		}

		private async UniTask StartAsync() {
			foreach (var o in GetInternalOrbiters())
			foreach (var p in o.GetInternalParts()) {
				p.SetActive(false);
				p.menu = this;
			}

			foreach (var data in GetDefaultData()) {
				var part = GetPart(data.Key);
				foreach (var entry in data.Value) {
					part.AddElement(entry);
					await UniTask.Yield();
				}
			}

			foreach (var o in GetInternalOrbiters())
			foreach (var p in o.GetInternalParts())
				if (p.GetChildren().Length > 0)
					p.SetActive(true);

			UpdateLayout.UpdateImmediate(gameObject);
		}

		public int GetId()
			=> GetInstanceID();

		public bool GetActive()
			=> parent.activeSelf;

		public void SetActive(bool active)
			=> parent.SetActive(active);

		public IOrbiter[] GetOrbiters()
			=> GetInternalOrbiters().Cast<IOrbiter>().ToArray();

		private Orbiter[] GetInternalOrbiters()
			=> new Orbiter[] { bottomOrbiter, topOrbiter };

		public IPart GetPart(string key)
			=> GetOrbiters()
				.SelectMany(o => o.GetParts())
				.FirstOrDefault(p => p.GetKey() == key);


		public void Dispose() {
			SetActive(false);
			History.Clear();
			foreach (UnityEngine.Transform child in container)
				Destroy(child.gameObject);
			History = null;
		}

		public void Go(IPage page)
			=> History.Add(page);

		public void GoBack(int count = 1)
			=> History.GoBack(count);

		public void GoForward(int count = 1)
			=> History.GoForward(count);

		public IPage GetCurrent()
			=> History.GetCurrent();

		public async UniTask SetPage(IPage newPage, IPage oldPage = null, PageFlags flags = PageFlags.None) {
			try {
				var content = await newPage.GetContentAsync(container);
				if (!content) {
					Debug.LogError($"Page {newPage.GetKey()} does not have content.");
					return;
				}

				var rect = content.GetComponent<RectTransform>();
				if (rect) {
					rect.anchorMin = Vector2.zero;
					rect.anchorMax = Vector2.one;
					rect.offsetMin = Vector2.zero;
					rect.offsetMax = Vector2.zero;
					rect.pivot     = new Vector2(0.5f, 0.5f);
				}

				foreach (UnityEngine.Transform child in container)
					if (child.gameObject.activeSelf && child.gameObject != content)
						child.gameObject.SetActive(false);

				oldPage?.OnHide(newPage);
				if (flags.HasFlag(PageFlags.IsNew))
					newPage.OnOpen(oldPage);

				if (flags.HasFlag(PageFlags.IsRestore))
					newPage.OnRestore(oldPage);

				newPage.OnDisplay(oldPage);
				content.SetActive(true);

				UpdateLayout.UpdateImmediate(content);
			} catch (Exception e) {
				Logger.LogException(e);
			}
		}
	}
}