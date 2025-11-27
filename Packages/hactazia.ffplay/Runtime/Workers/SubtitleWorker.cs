using System.Text;
using FFmpeg.AutoGen;
using FFmpeg.Unity;
using FFmpeg.Unity.Helpers;
using Hactazia.FFPlay.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Hactazia.FFPlay {
	public class SubtitleWorker : BaseWorker {
		public UnityEvent<string> onDisplay = new();
		public UnityEvent<string> onClear   = new();

		public string   currentText = string.Empty;
		public double[] currentTime = new double[2];

		private void PlaySubtitle(string text, double startTime, double endTime) {
			currentText    = text;
			currentTime[0] = startTime;
			currentTime[1] = endTime;
			onDisplay.Invoke(text);
		}

		/// <summary>
		/// Method for use with IStreamTimings from Core.
		/// </summary>
		public unsafe void PlayPacket(IStreamTimings timing, AVPacket frame) {
			// if (timing is not { IsValid: true } || timing.SubtitleDecoder == null || !timing.SubtitleDecoder.IsValid)
			// 	return;
			//
			// DecodeAndPlay(timing.SubtitleDecoder.Codec, frame, timing.StartTime);
		}

		/// <summary>
		/// New method for use with StreamDecoder from Core.
		/// </summary>
		public unsafe void PlayPacket(StreamDecoder decoder, AVPacket frame, double startTime) {
			if (decoder == null || !decoder.IsValid)
				return;

			DecodeAndPlay(decoder.Codec, frame, startTime);
		}

		/// <summary>
		/// Direct decode method using codec context.
		/// </summary>
		public unsafe void DecodeAndPlay(AVCodecContext* codec, AVPacket frame, double startTime) {
			if (codec == null)
				return;

			AVSubtitle sub;
			var        got = 0;

			var ret = ffmpeg.avcodec_decode_subtitle2(codec, &sub, &got, &frame);
			if (ret < 0 || got == 0)
				return;

			var text = string.Empty;

			for (var i = 0; i < sub.num_rects; i++) {
				var r = sub.rects[i];

				if (r->type == AVSubtitleType.SUBTITLE_TEXT) {
					var ptr = r->text;
					var len = 0;
					while (ptr[len] != 0)
						len++;
					var s = Encoding.UTF8.GetString(ptr, len);
					text += s + "\n";
				}
			}

			var startS = sub.start_display_time / 1000.0;
			var endS   = sub.end_display_time   / 1000.0;

			PlaySubtitle(text.Trim(), startTime + startS, startTime + endS);
		}
	}
}