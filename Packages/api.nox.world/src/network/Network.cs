using System;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Nox.Worlds;
using UnityEngine;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.world.network {
	public class Network {
		private readonly UnityEvent<World> _fetchEvent = new();

		private void InvokeFetch(World world) {
			if (world == null) return;
			_fetchEvent.Invoke(world);
			Main.Instance.CoreAPI.EventAPI.Emit("world_fetch", world);
		}

		public UniTask<World> Fetch(WorldIdentifier identifier, string from = null)
			=> Fetch(identifier.ToString(), from);

		public UniTask<World> Fetch(uint id, string from = null)
			=> Fetch(id.ToString(), from);

		public async UniTask<World> Fetch(string identifier, string from = null) {
			if (Main.Instance.NetworkAPI == null)
				return null;

			var ide = WorldIdentifier.FromString(identifier);
			if (ide.IsLocal())
				ide.Server = from;
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot fetch user {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.GetServerAddress())
				ide.Server = "::"; // Use "::" to indicate local server in the identifier

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/worlds/{ide.ToString()}");
			await request.Send();
			var response = request.GetMasterResponse<World>();
			if (response.HasError()) {
				Logger.LogError($"Failed to fetch user {identifier} from {address}: {response.GetError().GetMessage()}");
				return null;
			}

			var world = response.GetData();
			InvokeFetch(world);
			return world;
		}


		public async UniTask<SearchResponse> Search(SearchRequest data, string from = null) {
			if (Main.Instance.NetworkAPI == null)
				return null;

			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot search users: no server address provided.");
				return null;
			}

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/worlds?{data.ToParams()}");
			await request.Send();
			var response = request.GetMasterResponse<SearchResponse>();
			Logger.LogDebug(request.GetResponse<string>());
			if (response.HasError()) {
				Logger.LogError($"Failed to search users from {address}: {response.GetError().GetMessage()}");
				return null;
			}

			var users = response.GetData();

			foreach (var user in users.worlds)
				InvokeFetch(user);

			return users;
		}

		public async UniTask<World> Create(CreateWorldRequest data, string server) {
			if (Main.Instance.NetworkAPI == null)
				return null;

			if (string.IsNullOrEmpty(server)) {
				Logger.LogError("Cannot create world: no server address provided.");
				return null;
			}

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(server, "/api/worlds");
			request.SetBody(data.ToJson(), "application/json");
			request.SetMethod("PUT");
			await request.Send();
			var response = request.GetMasterResponse<World>();
			if (response.HasError()) {
				Logger.LogError($"Failed to create world on {server}: {response.GetError().GetMessage()}");
				return null;
			}

			var world = response.GetData();
			InvokeFetch(world);
			return world;
		}

		public async UniTask<World> Update(WorldIdentifier identifier, UpdateWorldRequest form, string from = null)
			=> await Update(identifier.ToString(), form, from);

		public async UniTask<World> Update(uint id, UpdateWorldRequest form, string from = null)
			=> await Update(id.ToString(), form, from);

		public async UniTask<World> Update(string identifier, UpdateWorldRequest form, string from = null) {
			if (Main.Instance.NetworkAPI == null)
				return null;

			var ide = WorldIdentifier.FromString(identifier);
			if (ide.IsLocal())
				ide.Server = from;
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot fetch world {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.GetServerAddress())
				ide.Server = "::"; // Use "::" to indicate local server in the identifier

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/worlds/{ide.ToString()}");
			request.SetBody(form.ToJson(), "application/json");
			request.SetMethod("POST");
			await request.Send();
			var response = request.GetMasterResponse<World>();
			if (response.HasError()) {
				Logger.LogError($"Failed to update world {identifier} from {address}: {response.GetError().GetMessage()}");
				return null;
			}

			var world = response.GetData();
			InvokeFetch(world);
			return world;
		}

		public async UniTask<bool> Delete(WorldIdentifier identifier, string from = null)
			=> await Delete(identifier.ToString(), from);

		public async UniTask<bool> Delete(uint id, string from = null)
			=> await Delete(id.ToString(), from);

		public async UniTask<bool> Delete(string identifier, string from = null) {
			if (Main.Instance.NetworkAPI == null)
				return false;

			var ide = WorldIdentifier.FromString(identifier);
			if (ide.IsLocal())
				ide.Server = from;
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot delete world {identifier}: no server address provided.");
				return false;
			}

			if (address == ide.GetServerAddress())
				ide.Server = "::"; // Use "::" to indicate local server in the identifier

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/worlds/{ide.ToString()}");
			request.SetMethod("DELETE");
			await request.Send();
			var response = request.GetMasterResponse<object>();
			if (!response.HasError()) return true;
			Logger.LogError($"Failed to delete world {identifier} from {address}: {response.GetError().GetMessage()}");
			return false;
		}

		public async UniTask<AssetSearchResponse> SearchAssets(WorldIdentifier identifier, AssetSearchRequest data, string from = null)
			=> await SearchAssets(identifier.ToString(), data, from);

		public async UniTask<AssetSearchResponse> SearchAssets(uint id, AssetSearchRequest data, string from = null)
			=> await SearchAssets(id.ToString(), data, from);

		public async UniTask<AssetSearchResponse> SearchAssets(string identifier, AssetSearchRequest data, string from = null) {
			if (Main.Instance.NetworkAPI == null)
				return null;

			var ide = WorldIdentifier.FromString(identifier);
			if (ide.IsLocal())
				ide.Server = from;
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot get assets for world {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.GetServerAddress())
				ide.Server = "::"; // Use "::" to indicate local server in the identifier

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/worlds/{ide.ToString()}/assets{data.ToParams()}");
			await request.Send();
			var response = request.GetMasterResponse<AssetSearchResponse>();
			if (!response.HasError()) return response.GetData();
			Logger.LogError($"Failed to get assets for world {identifier} from {address}: {response.GetError().GetMessage()}");
			return null;
		}

		public async UniTask<WorldAsset> CreateAsset(WorldIdentifier identifier, CreateAssetRequest data, string from = null)
			=> await CreateAsset(identifier.ToString(), data, from);

		public async UniTask<WorldAsset> CreateAsset(uint id, CreateAssetRequest data, string from = null)
			=> await CreateAsset(id.ToString(), data, from);

		public async UniTask<WorldAsset> CreateAsset(string identifier, CreateAssetRequest data, string from = null) {
			if (Main.Instance.NetworkAPI == null)
				return null;
			var ide = WorldIdentifier.FromString(identifier);
			if (ide.IsLocal())
				ide.Server = from;
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot create asset for world {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.GetServerAddress())
				ide.Server = "::"; // Use "::" to indicate local server in the identifier

			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/worlds/{ide.ToString()}/assets");
			request.SetBody(data.ToJson(), "application/json");
			request.SetMethod("PUT");
			await request.Send();
			var response = request.GetMasterResponse<WorldAsset>();
			if (response.HasError()) {
				Logger.LogError($"Failed to create asset for world {identifier} on {address}: {response.GetError().GetMessage()}");
				return null;
			}

			return response.GetData();
		}

		public async UniTask<bool> UploadThumbnail(WorldIdentifier identifier, Texture2D texture, string from = null, System.Action<float> onProgress = null)
			=> await UploadThumbnail(identifier.ToString(), texture, from, onProgress);

		public async UniTask<bool> UploadThumbnail(uint id, Texture2D texture, string from = null, System.Action<float> onProgress = null)
			=> await UploadThumbnail(id.ToString(), texture, from, onProgress);

		public async UniTask<bool> UploadThumbnail(string identifier, Texture2D texture, string from = null, System.Action<float> onProgress = null) {
			if (Main.Instance.NetworkAPI == null)
				return false;

			if (texture == null) {
				Logger.LogError($"Cannot upload thumbnail for world {identifier}: texture is null.");
				return false;
			}

			var ide = WorldIdentifier.FromString(identifier);
			if (ide.IsLocal())
				ide.Server = from;
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot upload thumbnail for world {identifier}: no server address provided.");
				return false;
			}

			if (address == ide.GetServerAddress())
				ide.Server = "::"; // Use "::" to indicate local server in the identifier
			// Convert texture to PNG byte array
			byte[] imageData;
			string fileHash = null;

			try {
				imageData = texture.EncodeToPNG();

				if (imageData == null || imageData.Length == 0) {
					Logger.LogError($"Failed to encode texture for world {identifier}: EncodeToPNG returned null or empty data. Check texture format and read/write settings.");
					return false;
				}

				// Calculate hash for validation
				using (var sha256 = System.Security.Cryptography.SHA256.Create()) {
					var hashBytes = sha256.ComputeHash(imageData);
					fileHash = System.BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
				}
			} catch (System.Exception ex) {
				Logger.LogError($"Failed to encode texture for world {identifier}: {ex.Message}");
				return false;
			} // Create multipart form data manually

			var boundary = "----formdata-nox-" + System.Guid.NewGuid().ToString();
			var formData = $"--{boundary}\r\n";
			formData += "Content-Disposition: form-data; name=\"file\"; filename=\"thumbnail.png\"\r\n";
			formData += "Content-Type: image/png\r\n\r\n";

			// Combine header, image data, and footer
			var headerBytes = System.Text.Encoding.UTF8.GetBytes(formData);
			var footerBytes = System.Text.Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n");

			var bodyData = new byte[headerBytes.Length + imageData.Length + footerBytes.Length];
			System.Array.Copy(headerBytes, 0, bodyData, 0, headerBytes.Length);
			System.Array.Copy(imageData, 0, bodyData, headerBytes.Length, imageData.Length);
			System.Array.Copy(footerBytes, 0, bodyData, headerBytes.Length + imageData.Length, footerBytes.Length);
			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/worlds/{ide.ToString()}/thumbnail");
			request.SetMethod("POST");
			request.SetBody(bodyData, $"multipart/form-data; boundary={boundary}");

			if (!string.IsNullOrEmpty(fileHash))
				request.SetHeader("x-file-hash", fileHash);

			// Send request with progress monitoring if callback provided
			if (onProgress != null) {
				var sendTask = request.Send();
				while (!sendTask.GetAwaiter().IsCompleted) {
					onProgress?.Invoke(request.GetUploadProgress());
					await UniTask.Yield();
				}

				await sendTask;           // Ensure the task completes
				onProgress?.Invoke(1.0f); // Ensure final progress is reported
			} else {
				await request.Send();
			}

			if (request.GetStatus() != 200) {
				Logger.LogError($"Failed to upload thumbnail for world {identifier} on {address}: {request.GetResponse<string>()}");
				return false;
			}

			return true;
		}

		public async UniTask<bool> UploadAssetFile(WorldIdentifier identifier, uint assetId, byte[] fileData, string fileName, string fileHash = null, string from = null, System.Action<float> onProgress = null)
			=> await UploadAssetFile(identifier.ToString(), assetId, fileData, fileName, fileHash, from, onProgress);

		public async UniTask<bool> UploadAssetFile(uint id, uint assetId, byte[] fileData, string fileName, string fileHash = null, string from = null, System.Action<float> onProgress = null)
			=> await UploadAssetFile(id.ToString(), assetId, fileData, fileName, fileHash, from, onProgress);

		public async UniTask<bool> UploadAssetFile(string identifier, uint assetId, byte[] fileData, string fileName, string fileHash = null, string from = null, System.Action<float> onProgress = null) {
			if (Main.Instance.NetworkAPI == null)
				return false;
			var ide = WorldIdentifier.FromString(identifier);
			if (ide.IsLocal())
				ide.Server = from;
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot upload asset file for world {identifier}: no server address provided.");
				return false;
			}

			if (address == ide.GetServerAddress())
				ide.Server = "::"; // Use "::" to indicate local server in the identifier

			// Create multipart form data manually (same pattern as UploadThumbnail)
			var boundary = "----formdata-nox-" + System.Guid.NewGuid().ToString();
			var formData = $"--{boundary}\r\n";
			formData += $"Content-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"\r\n";
			formData += "Content-Type: application/octet-stream\r\n\r\n";

			// Combine header, file data, and footer
			var headerBytes = System.Text.Encoding.UTF8.GetBytes(formData);
			var footerBytes = System.Text.Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n");

			var bodyData = new byte[headerBytes.Length + fileData.Length + footerBytes.Length];
			System.Array.Copy(headerBytes, 0, bodyData, 0, headerBytes.Length);
			System.Array.Copy(fileData, 0, bodyData, headerBytes.Length, fileData.Length);
			System.Array.Copy(footerBytes, 0, bodyData, headerBytes.Length + fileData.Length, footerBytes.Length);
			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/worlds/{ide.ToString()}/assets/{assetId}/file");
			request.SetMethod("POST");
			request.SetBody(bodyData, $"multipart/form-data; boundary={boundary}");
			if (!string.IsNullOrEmpty(fileHash))
				request.SetHeader("x-file-hash", fileHash);

			// Send request with progress monitoring if callback provided
			if (onProgress != null) {
				onProgress.Invoke(0.0f); // Initialize progress to 0
				var sendTask = request.Send();
				while (!sendTask.GetAwaiter().IsCompleted) {
					onProgress.Invoke(request.GetUploadProgress());
					await UniTask.Yield();
				}

				await sendTask;          // Ensure the task completes
				onProgress.Invoke(1.0f); // Ensure final progress is reported
			} else await request.Send();

			if (request.GetStatus() != 200) {
				Logger.LogError($"Failed to upload asset file for world {identifier} on {address}: {request.GetResponse<string>()}");
				return false;
			}

			return true;
		}

		public async UniTask<byte[]> DownloadAssetFile(WorldIdentifier identifier, uint assetId, string from = null, System.Action<float> onProgress = null)
			=> await DownloadAssetFile(identifier.ToString(), assetId, from, onProgress);

		public async UniTask<byte[]> DownloadAssetFile(uint id, uint assetId, string from = null, System.Action<float> onProgress = null)
			=> await DownloadAssetFile(id.ToString(), assetId, from, onProgress);

		public async UniTask<byte[]> DownloadAssetFile(string identifier, uint assetId, string from = null, System.Action<float> onProgress = null) {
			if (Main.Instance.NetworkAPI == null)
				return null;
			var ide = WorldIdentifier.FromString(identifier);
			if (ide.IsLocal())
				ide.Server = from;
			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress() ?? ide.GetServerAddress();
			if (string.IsNullOrEmpty(address)) {
				Logger.LogError($"Cannot download asset file for world {identifier}: no server address provided.");
				return null;
			}

			if (address == ide.GetServerAddress())
				ide.Server = "::"; // Use "::" to indicate local server in the identifier
			var request = Main.Instance.NetworkAPI.MakeRequest();
			await request.SetMasterUrl(address, $"/api/worlds/{ide.ToString()}/assets/{assetId}/file");

			// Send request with progress monitoring if callback provided
			if (onProgress != null) {
				onProgress.Invoke(0.0f); // Initialize progress at 0
				var sendTask = request.Send();
				while (!sendTask.GetAwaiter().IsCompleted) {
					onProgress.Invoke(request.GetDownloadProgress());
					await UniTask.Yield();
				}

				await sendTask;          // Ensure the task completes
				onProgress.Invoke(1.0f); // Ensure final progress is reported
			} else {
				await request.Send();
			}

			if (request.GetStatus() != 200) {
				Logger.LogError($"Failed to download asset file for world {identifier} from {address}: {request.GetResponse<string>()}");
				return null;
			}

			return request.GetResponse<byte[]>();
		}

		public async UniTask<WorldIdentifier[]> FetchFavorites(string from = null) {
			if (Main.Instance.TableAPI == null)
				return Array.Empty<WorldIdentifier>();

			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress();

			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot fetch favorites: no server address provided.");
				return Array.Empty<WorldIdentifier>();
			}

			var entry = await Main.Instance.TableAPI.Get("nox.worlds.favorites", address);
			if (entry != null)
				return entry
					.GetValue()
					.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(s => WorldIdentifier.FromString(s.Trim()))
					.Where(i => i != null && i.IsValid())
					.Distinct()
					.ToArray();

			Logger.LogError($"Failed to fetch favorites from {address}: entry not found.");
			return Array.Empty<WorldIdentifier>();
		}

		public async UniTask<WorldIdentifier[]> AddFavorite(string identifier, string from = null)
			=> await AddFavorites(new[] { identifier }, from);

		public async UniTask<WorldIdentifier[]> AddFavorites(string[] identifier, string from = null) {
			if (Main.Instance.TableAPI == null)
				return Array.Empty<WorldIdentifier>();

			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress();

			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot add favorites: no server address provided.");
				return Array.Empty<WorldIdentifier>();
			}

			var e = await FetchFavorites(from);

			var newE = identifier
				.Select(WorldIdentifier.FromString)
				.Where(i => i != null && i.IsValid())
				.Concat(e)
				.Distinct()
				.ToArray();

			var entry = await Main.Instance.TableAPI.Set(
				"nox.worlds.favorites",
				string.Join(",", newE.Select(i => i.ToString())),
				address
			);

			if (entry != null)
				return newE;

			Logger.LogError($"Failed to add favorites on {address}: entry not found.");
			return e;
		}

		public async UniTask<WorldIdentifier[]> RemoveFavorite(string identifier, string from = null)
			=> await RemoveFavorites(new[] { identifier }, from);

		public async UniTask<WorldIdentifier[]> RemoveFavorites(string[] identifier, string from = null) {
			if (Main.Instance.TableAPI == null)
				return Array.Empty<WorldIdentifier>();

			var address = from ?? Main.Instance.UserAPI?.GetCurrent()?.GetServerAddress();

			if (string.IsNullOrEmpty(address)) {
				Logger.LogError("Cannot remove favorites: no server address provided.");
				return Array.Empty<WorldIdentifier>();
			}

			var e = await FetchFavorites(from);

			var newE = e
				.Where(i => !identifier.Contains(i.ToString()))
				.ToArray();

			var entry = await Main.Instance.TableAPI.Set(
				"nox.worlds.favorites",
				string.Join(",", newE.Select(i => i.ToString())),
				address
			);

			if (entry != null)
				return newE;

			Logger.LogError($"Failed to add favorites on {address}: entry not found.");
			return e;
		}
	}
}