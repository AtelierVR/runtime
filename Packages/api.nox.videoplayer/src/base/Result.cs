using System;
using System.Linq;
using Nox.VideoPlayer;

namespace api.nox.videoplayer {
	public class Result : IResult {
		public string       Error = null;
		public ResultData[] Data  = null;

		public bool IsError()
			=> !string.IsNullOrEmpty(Error);

		public string GetError()
			=> Error;

		public bool HasNext()
			=> false;

		public IResultData[] GetData()
			=> Data.Cast<IResultData>().ToArray();

		public static Result FromError(string error)
			=> new() {
				Error = error,
				Data  = Array.Empty<ResultData>()
			};

		public static Result FromData(ResultData[] data)
			=> new() {
				Error = null,
				Data  = data
			};
	}
}