using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.VideoPlayer;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.videoplayer.handlers {
	public class Twitch : IHandler {
		public string GetId()
			=> "twitch";

		public string GetTitleKey()
			=> "videoplayer.handler.twitch";

		public string[] GetTitleArguments()
			=> new string[] { };

		public int EstimatePriority(IFetchOptions options)
			=> IsUrl(options.GetQuery()) ? 100 : -1;

		private static bool IsUrl(string query)
			=> query.StartsWith("https://www.twitch.tv/");

		private static string FormatUrl(string original) {
			var id = "";

			if (original.StartsWith("https://www.twitch.tv/")) {
				var uri = new Uri(original);
				id = uri.AbsolutePath.TrimStart('/');
			}

			return $"https://www.twitch.tv/{id}";
		}

		public async UniTask<IResult[]> Fetch(IFetchOptions options) {
			try {
				if (EstimatePriority(options) < 0)
					return new IResult[] { Result.FromError("Query is not a valid Twitch URL") };

				var response = await YtDl.Extract(FormatUrl(options.GetQuery()), cancellationToken: options.GetCancellation().Token);
				
				if (response is not { Type: JTokenType.Object })
					throw new InvalidDataException("Response from yt-dlp is not an object");

				var type      = ToObject(response["_type"], "");
				var extractor = ToObject(response["extractor"], "unknown");
				if (extractor != "twitch:stream" && extractor != "twitch")
					throw new InvalidDataException($"Unexpected extractor: {extractor}");

				return new IResult[] {
					Result.FromData(
						type switch {
							"video" => new[] { ParseVideo(response) },
							_       => throw new InvalidDataException($"Unknown response type: {type}")
						}
					)
				};
			} catch (Exception e) {
				Logger.LogException(e);
				return new IResult[] { Result.FromError(e.Message) };
			}
		}

		private static Resolve ParseVideo(JToken video) {
			if (video is not { Type: JTokenType.Object })
				throw new InvalidDataException("Video is not an object");

			return new Resolve {
				Id = video["id"]?.ToString() ?? "",
				Title = video["description"]?.ToString()
					?? video["title"]?.ToString()
					?? video["fulltitle"]?.ToString()
					?? video["id"]?.ToString(),
				Thumbnails = ParseThumbnails(video["thumbnails"]),
				Subtitles  = Array.Empty<Subtitle>(),
				Formats    = ParseFormats(video["formats"])
			};
		}

		private static Thumbnail[] ParseThumbnails(JToken thumbnails) {
			if (thumbnails is not { Type: JTokenType.Array })
				return Array.Empty<Thumbnail>();

			return (from thumb in thumbnails
				where thumb is { Type: JTokenType.Object }
				let url = ToObject(thumb["url"], "")
				where !string.IsNullOrWhiteSpace(url)
				select new Thumbnail { Url = url, Language = null, Resolution = ExtractResolutionFromThumbnailUrl(url) }).ToArray();
		}

		private static Vector2Int ExtractResolutionFromThumbnailUrl(string url) {
			try {
				var lastPart = url.Split('/').LastOrDefault();
				if (string.IsNullOrWhiteSpace(lastPart))
					return new Vector2Int(-1, -1);

				var sizePart = lastPart.Split('-').LastOrDefault();
				if (string.IsNullOrWhiteSpace(sizePart))
					return new Vector2Int(-1, -1);

				var dimensions = sizePart.Split('x');
				if (dimensions.Length != 2)
					return new Vector2Int(-1, -1);

				if (int.TryParse(dimensions[0], out var width) && int.TryParse(dimensions[1], out var height))
					return new Vector2Int(width, height);
			} catch (Exception e) {
				Logger.LogException(e);
			}

			return new Vector2Int(-1, -1);
		}

		private static T ToObject<T>(JToken input, T @default) {
			if (input is { Type: JTokenType.Null })
				return @default;
			try {
				return input.ToObject<T>();
			} catch {
				return @default;
			}
		}

		private static Format[] ParseFormats(JToken formats) {
			if (formats is not { Type: JTokenType.Array })
				return System.Array.Empty<Format>();
			var list = new List<Format>();
			foreach (var format in formats) {
				if (format is not { Type: JTokenType.Object })
					continue;

				var acodec = ToObject(format["acodec"], "none");
				var vcodec = ToObject(format["vcodec"], "none");

				Format fmt;
				if (acodec != "none" && vcodec != "none")
					fmt = new AudioVideoFormat();
				else if (acodec != "none")
					fmt = new AudioFormat();
				else if (vcodec != "none")
					fmt = new VideoFormat();
				else continue;

				fmt.Url       = ToObject(format["url"], "");
				fmt.Container = ToObject(format["container"], "");
				fmt.Language  = ToObject(format["language"], "");
				fmt.Bitrate   = ToObject(format["tbr"], 0u);
				fmt.Quality   = ToObject(format["quality"], 0f);

				if (fmt is AudioVideoFormat or VideoFormat) {
					fmt.Resolution = new Vector2Int(
						ToObject(format["width"], 0),
						ToObject(format["height"], 0)
					);
					fmt.Framerate    = ToObject(format["fps"], 0u);
					fmt.VideoBitrate = ToObject(format["vbr"], 0u);
					fmt.VideoCodec   = vcodec;
					fmt.DynamicRange = ToObject(format["dynamic_range"], "SDR");
				}

				if (fmt is AudioVideoFormat or AudioFormat) {
					fmt.AudioBitrate = ToObject(format["abr"], 0u);
					fmt.AudioCodec   = acodec;
				}

				if (!string.IsNullOrWhiteSpace(fmt.Url))
					list.Add(fmt);
			}

			return list.ToArray();
		}

		private static Resolve[] ParsePlaylist(JToken playlist) {
			if (playlist is not { Type: JTokenType.Object })
				throw new InvalidDataException("Playlist is not an object");
			var entries = playlist["entries"];
			return entries is not { Type: JTokenType.Array }
				? System.Array.Empty<Resolve>()
				: entries.Select(ParseVideo)
					.ToArray();
		}
	}
}