using System;
using FFmpeg.AutoGen;

namespace Hactazia.FFPlay.Core {
	/// <summary>
	/// Common interface for timing systems (single source or separate sources).
	/// </summary>
	public interface IStreamTimings : IDisposable {
		bool IsValid { get; }

		bool IsEnd { get; }

		double StartTime { get; }

		double Length { get; }

		bool Has(MediaType type);

		StreamDecoder Get(MediaType type);

		void Update(MediaType type, double time);

		AVFrame GetFrame(MediaType type, double maxDelta = 250);

		int GetFrames(MediaType type, ref AVFrame[] frames, double maxDelta = 250);

		int GetPendingSize(MediaType type);

		void Seek(double timestamp);
	}
}