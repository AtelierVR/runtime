using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nox.VideoPlayer;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.videoplayer.handlers {
	public class Youtube : IHandler {
		public string GetId()
			=> "youtube";

		public string GetTitleKey()
			=> "videoplayer.handler.youtube";

		public string[] GetTitleArguments()
			=> new string[] { };

		public static bool IsUrl(string query)
			=> query.StartsWith("https://www.youtube.com/watch")
				|| query.StartsWith("https://youtu.be/");

		public static string FormatUrl(string original) {
			var id = "";

			if (original.StartsWith("https://www.youtube.com/watch")) {
				var uri   = new System.Uri(original);
				var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
				id = query.Get("v") ?? "";
			} else if (original.StartsWith("https://youtu.be/")) {
				var uri = new System.Uri(original);
				id = uri.AbsolutePath.TrimStart('/');
			}

			return $"https://www.youtube.com/watch?v={id}";
		}

		public async UniTask<IResult[]> Fetch(IFetchOptions options) {
			try {
				var response = IsUrl(options.GetQuery())
					? await YtDl.Extract(FormatUrl(options.GetQuery()), cancellationToken: options.GetCancellation().Token)
					: await YtDl.Extract($"ytsearch{options.GetLimit()}:{options.GetQuery()}", cancellationToken: options.GetCancellation().Token);

				if (response is not { Type: JTokenType.Object })
					throw new InvalidDataException("Response from yt-dlp is not an object");

				var type      = response["_type"]?.ToString();
				var extractor = response["extractor"]?.ToString() ?? "unknown";
				if (extractor != "youtube:search" && extractor != "youtube")
					throw new InvalidDataException($"Unexpected extractor: {extractor}");

				return new IResult[] {
					Result.FromData(
						type switch {
							"video"    => new[] { ParseVideo(response) },
							"playlist" => ParsePlaylist(response),
							_          => throw new InvalidDataException($"Unknown response type: {type}")
						}
					)
				};
			} catch (System.Exception e) {
				Logger.LogException(e);
				return new IResult[] { Result.FromError(e.Message) };
			}
		}

		private static Resolve ParseVideo(JToken video) {
			if (video is not { Type: JTokenType.Object })
				throw new InvalidDataException("Video is not an object");

			return new Resolve {
				Id         = video["id"]?.ToString()    ?? "",
				Title      = video["title"]?.ToString() ?? video["fulltitle"]?.ToString() ?? video["id"]?.ToString(),
				Thumbnails = ParseThumbnails(video["thumbnails"]),
				Subtitles  = ParseSubtitles(video["subtitles"]),
				Formats    = ParseFormats(video["formats"])
			};
		}

		private static Thumbnail[] ParseThumbnails(JToken thumbnails) {
			if (thumbnails is not { Type: JTokenType.Array })
				return System.Array.Empty<Thumbnail>();

			return (from thumb in thumbnails
				where thumb is { Type: JTokenType.Object }
				let url = thumb["url"]?.ToString()
				where !string.IsNullOrWhiteSpace(url)
				let width = thumb["width"]?.ToObject<int?>()   ?? -1
				let height = thumb["height"]?.ToObject<int?>() ?? -1
				select new Thumbnail { Url = url, Language = null, Resolution = new Vector2Int(width, height) }).ToArray();
		}

		private static Subtitle[] ParseSubtitles(JToken subtitles) {
			if (subtitles is not { Type: JTokenType.Object })
				return System.Array.Empty<Subtitle>();
			return (from entry in subtitles.Children<JProperty>()
				let lang = entry.Name
				where entry.Value is { Type: JTokenType.Array }
				from sub in entry.Value
				let ext = sub["ext"]?.ToString()
				let url = sub["url"]?.ToString()
				let name = sub["name"]?.ToString()
				where !string.IsNullOrWhiteSpace(url) && ext == "srt"
				select new Subtitle { Url = url, Language = lang, Title = name }).ToArray();
		}

		private static Format[] ParseFormats(JToken formats) {
			if (formats is not { Type: JTokenType.Array })
				return System.Array.Empty<Format>();
			var list = new List<Format>();
			foreach (var format in formats) {
				if (format is not { Type: JTokenType.Object })
					continue;

				var acodec = format["acodec"]?.ToString() ?? "none";
				var vcodec = format["vcodec"]?.ToString() ?? "none";

				Format fmt;
				if (acodec != "none" && vcodec != "none")
					fmt = new AudioVideoFormat();
				else if (acodec != "none")
					fmt = new AudioFormat();
				else if (vcodec != "none")
					fmt = new VideoFormat();
				else continue;

				fmt.Url       = format["url"]?.ToString();
				fmt.Container = format["container"]?.ToString();
				fmt.Language  = format["language"]?.ToString();
				fmt.Bitrate   = format["bitrate"]?.ToObject<uint>() ?? 0u;
				fmt.Quality   = format["quality"]?.ToObject<uint>() ?? 0f;

				if (fmt is AudioVideoFormat or VideoFormat) {
					fmt.Resolution = new Vector2Int(
						format["width"]?.ToObject<int>()  ?? 0,
						format["height"]?.ToObject<int>() ?? 0
					);
					fmt.Framerate    = format["fps"]?.ToObject<uint>() ?? 0u;
					fmt.VideoBitrate = format["tbr"]?.ToObject<uint>() ?? 0u;
					fmt.VideoCodec   = vcodec;
					fmt.DynamicRange = format["dynamic_range"]?.ToString() ?? "SDR";
				}

				if (fmt is AudioVideoFormat or AudioFormat) {
					if(format["abr"]?.Type != JTokenType.Null) 
						fmt.AudioBitrate = format["abr"]?.ToObject<uint>() ?? 0u;
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