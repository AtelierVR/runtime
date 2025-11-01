using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Instances;
using Nox.Users;
using Nox.Worlds;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace api.nox.instance.client {
	public class InstanceComponent : MonoBehaviour {
		public  GameObject              withThumbnail;
		public  GameObject              withoutThumbnail;
		public  Image                   thumbnail;
		public  TextLanguage            title;
		public  TextLanguage            identifier;
		public  TextLanguage            label;
		public  Image                   labelIcon;
		public  RectTransform           content;
		public  InstancePage            Page;
		private CancellationTokenSource _thumbnailTokenSource;
		private CancellationTokenSource _playerListTokenSource;
		public  RectTransform           playerList;
		public  GameObject              playerInfobox;
		public  GameObject              playerListContainer;
		public  GameObject              descriptionContainer;
		public  TextLanguage            descriptionText;
		public  RectTransform           actions;
		public  Image                   joinIcon;
		public  TextLanguage            joinLabel;
		public  Button                  joinButton;

		public void UpdateError(string error) {
			title.UpdateText("instance.error");
			identifier.UpdateText("instance.error");
			label.UpdateText("instance.error");
			thumbnail.sprite = null;
			thumbnail.sprite = null;
			withThumbnail.SetActive(false);
			withoutThumbnail.SetActive(true);
			descriptionContainer.SetActive(false);
		}

		public void UpdateLoading() {
			title.UpdateText("instance.loading");
			identifier.UpdateText("instance.loading");
			label.UpdateText("instance.loading");
			thumbnail.sprite = null;
			thumbnail.sprite = null;
			withThumbnail.SetActive(false);
			withoutThumbnail.SetActive(true);
			descriptionContainer.SetActive(false);
		}

		public void UpdateContent(IInstance instance, IWorld world, IWorldAsset asset) {
			if (instance == null) return;

			title.UpdateText("instance.title", new[] { instance.GetTitle() });
			label.UpdateText("instance.about.title", new[] { instance.GetTitle() ?? instance.ToIdentifier().ToString() });
			identifier.UpdateText(
				"instance.identifier", new[] {
					instance.ToIdentifier().ToString(),
					instance.GetId().ToString(),
					instance.GetServerAddress()
				}
			);

			if (!string.IsNullOrEmpty(instance.GetDescription())) {
				descriptionText.SetMarkdown(instance.GetDescription());
				descriptionContainer.SetActive(true);
			} else descriptionContainer.SetActive(false);


			UpdateThumbnail(instance).Forget();
			UpdatePlayerList(instance).Forget();
		}

		private async UniTask UpdateThumbnail(IInstance instance) {
			if (_thumbnailTokenSource != null) {
				_thumbnailTokenSource?.Cancel();
				_thumbnailTokenSource?.Dispose();
			}

			_thumbnailTokenSource = new CancellationTokenSource();
			if (!string.IsNullOrEmpty(instance?.GetThumbnailUrl())) {
				var texture = await Main.NetworkAPI
					.FetchTexture(instance.GetThumbnailUrl())
					.AttachExternalCancellation(_thumbnailTokenSource.Token);
				if (texture) {
					thumbnail.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
					withThumbnail.SetActive(true);
					withoutThumbnail.SetActive(false);
				} else {
					thumbnail.sprite = null;
					withThumbnail.SetActive(false);
					withoutThumbnail.SetActive(true);
				}
			} else {
				thumbnail.sprite = null;
				withThumbnail.SetActive(false);
				withoutThumbnail.SetActive(true);
			}

			_thumbnailTokenSource = null;
		}

		private void OnJoinClicked()
			=> Main.SessionAPI.MakeSession(
				"external:"
				+ Page.Instance
					.GetConnectionData()
					.GetMethod(),
				new Dictionary<string, object> {
					{ "set_current", true },
					{ "server", Page.Instance.GetServerAddress() },
					{ "instance", Page.Instance.GetId() },
					{ "name", Page.Instance.GetTitle() },
					{ "short_name", Page.Instance.GetName() },
					{ "thumbnail", Main.NetworkAPI.FetchTexture(Page.Instance.GetThumbnailUrl()) },
					{ "data", Page.Instance.GetConnectionData().GetData<JObject>() }
				}
			);

		public static (GameObject, InstanceComponent) Generate(InstancePage instancePage, RectTransform parent) {
			var content              = Instantiate(Client.GetAsset<GameObject>("prefabs/split.prefab", "ui"), parent);
			var iconAsset            = Client.GetAsset<GameObject>("prefabs/header_icon.prefab", "ui");
			var labelAsset           = Client.GetAsset<GameObject>("prefabs/header_label.prefab", "ui");
			var withTitleAsset       = Client.GetAsset<GameObject>("prefabs/with_title.prefab", "ui");
			var listAsset            = Client.GetAsset<GameObject>("prefabs/list.prefab", "ui");
			var scrollAsset          = Client.GetAsset<GameObject>("prefabs/scroll.prefab", "ui");
			var boxAsset             = Client.GetAsset<GameObject>("prefabs/box.prefab", "ui");
			var actionButtonAsset    = Client.GetAsset<GameObject>("prefabs/action_button.prefab", "ui");
			var actionContainerAsset = Client.GetAsset<GameObject>("prefabs/action_container.prefab", "ui");

			var component = content.AddComponent<InstanceComponent>();
			component.Page = instancePage;
			content.name   = $"[{instancePage.GetKey()}_{content.GetInstanceID()}]";

			var splitContent   = Reference.GetComponent<RectTransform>("content", content);
			var containerAsset = Client.GetAsset<GameObject>("prefabs/container.prefab", "ui");

			// generate profile
			var container = Instantiate(containerAsset, splitContent);
			var profile = Instantiate(
				Client.GetAsset<GameObject>("prefabs/profile.prefab"),
				Reference.GetComponent<RectTransform>("content", container)
			);
			component.identifier       = Reference.GetComponent<TextLanguage>("identifier", profile);
			component.title            = Reference.GetComponent<TextLanguage>("title", profile);
			component.thumbnail        = Reference.GetComponent<Image>("thumbnail", profile);
			component.withThumbnail    = Reference.GetReference("with_thumbnail", profile);
			component.withoutThumbnail = Reference.GetReference("without_thumbnail", profile);

			// generate dashboard
			container = Instantiate(Client.GetAsset<GameObject>("prefabs/container_full.prefab", "ui"), splitContent);
			var withTitle = Instantiate(
				withTitleAsset,
				Reference.GetComponent<RectTransform>("content", container)
			);

			var header = Reference.GetReference("header", withTitle);
			var icon   = Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", header));
			var label  = Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", header));

			component.labelIcon        = Reference.GetComponent<Image>("image", icon);
			component.label            = Reference.GetComponent<TextLanguage>("text", label);
			component.labelIcon.sprite = Client.GetAsset<Sprite>("icons/location.png", "ui");

			var contentDash = Reference.GetComponent<RectTransform>("content", withTitle);
			// setup scroll + list
			var scroll = Instantiate(scrollAsset, contentDash);
			var list   = Instantiate(listAsset, Reference.GetComponent<RectTransform>("content", scroll));
			component.content = Reference.GetComponent<RectTransform>("content", list);

			// add box actions
			var boxActions = Instantiate(boxAsset, component.content);
			Reference.GetComponent<TextLanguage>("text", boxActions).UpdateText("instance.about.actions");
			component.actions = Reference.GetComponent<RectTransform>("content", Instantiate(actionContainerAsset, Reference.GetComponent<RectTransform>("content", boxActions)));

			// Bouton Join
			var join             = Instantiate(actionButtonAsset, component.actions);
			var joinEventTrigger = Reference.GetComponent<EventTrigger>("button", join);
			component.joinButton      = Reference.GetComponent<Button>("button", join);
			component.joinIcon        = Reference.GetComponent<Image>("image", join);
			component.joinLabel       = Reference.GetComponent<TextLanguage>("text", join);
			component.joinIcon.sprite = Client.GetAsset<Sprite>("icons/globe.png", "ui");
			component.joinLabel.UpdateText("instance.join");
			SetupEvents(
				joinEventTrigger,
				() => component.OnJoinClicked(),
				() => { }, // Pas d'effet hover pour l'instant
				() => { }
			);

			// add box description
			component.descriptionContainer = Instantiate(boxAsset, component.content);
			Reference.GetComponent<TextLanguage>("text", component.descriptionContainer).UpdateText("instance.about.description");
			component.descriptionText = Reference.GetComponent<TextLanguage>(
				"text", Instantiate(
					Client.GetAsset<GameObject>("prefabs/text.prefab", "ui"),
					Reference.GetComponent<RectTransform>("content", component.descriptionContainer)
				)
			);


			// generate instances
			container = Instantiate(containerAsset, splitContent);
			withTitle = Instantiate(withTitleAsset, Reference.GetComponent<RectTransform>("content", container));

			header = Reference.GetReference("header", withTitle);
			icon   = Instantiate(iconAsset, Reference.GetComponent<RectTransform>("before", header));
			label  = Instantiate(labelAsset, Reference.GetComponent<RectTransform>("content", header));

			Reference.GetComponent<Image>("image", icon).sprite = Client.GetAsset<Sprite>("icons/group.png", "ui");
			Reference.GetComponent<TextLanguage>("text", label).UpdateText("instance.players.title");

			var contentIn = Reference.GetComponent<RectTransform>("content", withTitle);
			component.playerInfobox = Instantiate(Client.GetAsset<GameObject>("prefabs/infobox.prefab", "ui"), contentIn);
			Reference.GetComponent<TextLanguage>("text", component.playerInfobox).UpdateText("instance.no_players");
			component.playerListContainer = Instantiate(scrollAsset, contentIn);
			list                          = Instantiate(listAsset, Reference.GetComponent<RectTransform>("content", component.playerListContainer));
			component.playerList          = Reference.GetComponent<RectTransform>("content", list);

			return (content, component);
		}

		// ReSharper disable Unity.PerformanceAnalysis
		private static void SetupEvents(EventTrigger eventTrigger, Action click, Action enter, Action exit) {
			if (!eventTrigger) return;
			var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
			entry.callback.AddListener(_ => click());
			eventTrigger.triggers.Add(entry);
			entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
			entry.callback.AddListener(_ => enter());
			eventTrigger.triggers.Add(entry);
			entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
			entry.callback.AddListener(_ => exit());
			eventTrigger.triggers.Add(entry);
		}

		public async UniTask UpdatePlayerList(IInstance instance) {
			if (_playerListTokenSource != null) {
				_playerListTokenSource?.Cancel();
				_playerListTokenSource?.Dispose();
				_playerListTokenSource = null;
			}

			if (instance == null) {
				playerInfobox.SetActive(true);
				playerListContainer.SetActive(false);
				return;
			}


			_playerListTokenSource = new CancellationTokenSource();
			var tasks = new List<UniTask<(IUser, IPlayer)[]>>();

			var players = instance.GetPlayers();
			var playersByServer = players
				.GroupBy(p => p.GetIdentifier().GetServerAddress())
				.ToDictionary(g => g.Key, g => g.ToArray());

			var isEmpty = true;
			var isFirst = true;
			var prefab  = PlayerComponent.PlayerPrefab;
			var action = new Action<(IUser, IPlayer)[]>(
				users => {
					Logger.LogDebug($"Found {users.Length} instances for world {instance.GetTitle()} ({instance.ToIdentifier()})");
					if (isFirst)
						foreach (Transform child in playerList.transform)
							Destroy(child.gameObject);
					isFirst = false;
					foreach (var user in users) {
						PlayerComponent.Generate(this, playerList.transform, prefab, user).Forget();
					}

					if (users.Length > 0) {
						isEmpty = false;
						playerInfobox.SetActive(false);
						playerListContainer.SetActive(true);
						UpdateLayout.UpdateImmediate(playerList);
					}
				}
			);

			foreach (var (server, users) in playersByServer) {
				if (_playerListTokenSource.IsCancellationRequested) {
					_playerListTokenSource = null;
					return;
				}

				if (users.Length == 0) continue;
				tasks.Add(SearchPlayers(users, server, _playerListTokenSource.Token, action));
			}

			await UniTask.WhenAll(tasks);
			if (isEmpty) {
				playerInfobox.SetActive(true);
				playerListContainer.SetActive(false);
			} else UpdateLayout.UpdateImmediate(playerList);
		}

		private async UniTask<(IUser, IPlayer)[]> SearchPlayers(IPlayer[] users, string server, CancellationToken token, Action<(IUser, IPlayer)[]> callback = null) {
			if (token.IsCancellationRequested)
				return Array.Empty<(IUser, IPlayer)>();

			var request = Main.UserAPI
				.MakeSearchRequest()
				.SetIds(users.Select(p => p.GetIdentifier().GetId()).ToArray());

			var response = await Main.UserAPI.Search(request, server)
				.AttachExternalCancellation(token);
			if (token.IsCancellationRequested)
				return Array.Empty<(IUser, IPlayer)>();
			var ress = response == null
				? Array.Empty<IUser>()
				: response.GetUsers();
			if (ress.Length == 0)
				return Array.Empty<(IUser, IPlayer)>();

			var res = new List<(IUser, IPlayer)>();
			foreach (var user in ress) {
				var matchingPlayers = users.Where(p => p.GetIdentifier().Equals(user.ToIdentifier()));
				res.AddRange(matchingPlayers.Select(player => (user, player)));
			}

			callback?.Invoke(res.ToArray());
			return res.ToArray();
		}

		public string[] GetSearchableServers() {
			var x0 = Config.Load().Get("servers");
			if (x0 == null) return Array.Empty<string>();
			var x1 = x0.ToObject<Dictionary<string, JObject>>();
			var x2 = new List<string>();
			foreach (var (address, value) in x1) {
				var features = value["features"]?.Values<string>().ToArray() ?? Array.Empty<string>();
				var search   = value["search"]?.ToObject<bool>()             ?? false;
				if (!(search && features.Contains("users"))) continue;
				x2.Add(address);
			}

			return x2.ToArray();
		}
	}
}