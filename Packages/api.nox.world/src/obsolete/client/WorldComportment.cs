// using Cysharp.Threading.Tasks;
// using Nox.CCK.Language;
// using UnityEngine;
// using UnityEngine.UI;
// using Logger = Nox.CCK.Utils.Logger;
//
// namespace api.nox.world.client
// {
//     public class WorldComportment : MonoBehaviour
//     {
//         public Button refreshButton;
//         public Button backButton;
//
//         public RectTransform withThumbnailContainer;
//         public RectTransform withoutThumbnailContainer;
//
//         public RawImage thumbnail;
//         public TextLanguage withTitleText;
//         public TextLanguage withoutTitleText;
//
//         public RectTransform descriptionContainer;
//         public TextLanguage descriptionText;
//
//         private WorldPage _page;
//
//         internal void Initiate(WorldPage page)
//         {
//             if (_page != null) return;
//             _page = page;
//         }
//
//         private void Awake()
//         {
//             if (_page == null)
//             {
//                 Logger.LogError("ProfileComportment: ProfilePage is not initiated.");
//                 return;
//             }
//
//             refreshButton.onClick.AddListener(OnRefresh);
//             backButton.onClick.AddListener(OnBack);
//         }
//
//         private void OnRefresh()
//             => _page?.Refresh().Forget();
//
//         private void OnBack()
//             => _page?.GoBack();
//
//         private void OnDestroy()
//         {
//             refreshButton.onClick.RemoveListener(OnRefresh);
//             backButton.onClick.RemoveListener(OnBack);
//         }
//
//         internal void UpdateData()
//         {
//             var world = _page.World;
//             var isFetching = _page.IsFetching;
//             refreshButton.interactable = !isFetching;
//
//
//             if (world == null && isFetching)
//             {
//                 withThumbnailContainer.gameObject.SetActive(false);
//                 withoutThumbnailContainer.gameObject.SetActive(false);
//                 descriptionContainer.gameObject.SetActive(false);
//                 withoutTitleText.UpdateText("world.loading");
//                 withTitleText.UpdateText("world.loading");
//                 thumbnail.texture = null;
//             }
//
//             if (world == null)
//             {
//                 withThumbnailContainer.gameObject.SetActive(false);
//                 withoutThumbnailContainer.gameObject.SetActive(true);
//                 descriptionContainer.gameObject.SetActive(false);
//                 withoutTitleText.UpdateText("world.not_found");
//                 withTitleText.UpdateText("world.not_found");
//                 thumbnail.texture = null;
//                 return;
//             }
//
//             var server = world.GetField<string>("server");
//             var title = world.GetField<string>("title");
//             var thumbnailUrl = world.GetField<string>("thumbnail");
//             var description = world.GetField<string>("description");
//
//             withoutTitleText.UpdateText("world.title", new[] { title, server });
//             withTitleText.UpdateText("world.title", new[] { title, server });
//
//             descriptionContainer.gameObject.SetActive(!string.IsNullOrEmpty(description));
//             descriptionText.UpdateText(new[] { description });
//
//             FetchThumbnail(thumbnailUrl).Forget();
//         }
//
//         private async UniTask FetchThumbnail(string url)
//         {
//             var hasThumbnail = !string.IsNullOrEmpty(url);
//             if (!hasThumbnail)
//             {
//                 thumbnail.texture = null;
//                 withThumbnailContainer.gameObject.SetActive(false);
//                 withoutThumbnailContainer.gameObject.SetActive(true);
//                 return;
//             }
//
//             var hasTexture = thumbnail.texture;
//             if (hasTexture)
//             {
//                 withThumbnailContainer.gameObject.SetActive(true);
//                 withoutThumbnailContainer.gameObject.SetActive(false);
//                 return;
//             }
//
//             try
//             {
//                 var texture = await Main.NetworkAPI
//                     .CallAsyncMethod<Texture2D>("FetchTexture", url, null, null, null);
//                 if (!texture)
//                 {
//                     thumbnail.texture = null;
//                     withThumbnailContainer.gameObject.SetActive(false);
//                     withoutThumbnailContainer.gameObject.SetActive(true);
//                     return;
//                 }
//
//                 thumbnail.texture = texture;
//                 withThumbnailContainer.gameObject.SetActive(true);
//                 withoutThumbnailContainer.gameObject.SetActive(false);
//                 return;
//             }
//             catch
//             {
//                 // ignored
//             }
//
//             thumbnail.texture = null;
//             withThumbnailContainer.gameObject.SetActive(false);
//             withoutThumbnailContainer.gameObject.SetActive(true);
//         }
//     }
// }