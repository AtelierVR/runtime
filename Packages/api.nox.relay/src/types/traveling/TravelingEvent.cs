using System;
using System.Collections.Generic;
using api.nox.relay.types.Instance;
using Nox.Worlds;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Traveling {
	public class TravelingEvent : RelayInstanceResponse {
		public TravelingResults Results;

		public bool IsError
			=> Results.HasFlag(TravelingResults.Unknown);

		public bool IsReady
			=> Results.HasFlag(TravelingResults.Ready) && !IsError;

		public bool UseUrl
			=> Results.HasFlag(TravelingResults.UseUrl) && !IsError;

		public bool UseMaster
			=> Results.HasFlag(TravelingResults.UseMaster) && !IsError;

		public string Reason;

		// if the flag UseUrl is set, this is the URL to download the dimension
		public string DownloadUrl;
		public string Hash;
		public uint   Size;

		// if the flag UseMaster is set, this is the ID of the master server
		public IWorldIdentifier WorldIdentifier;

		public override bool FromBuffer(Buffer buffer) {
			buffer.Goto(0);
			InternalId = buffer.ReadByte();

			Results = buffer.ReadEnum<TravelingResults>();

			if (Results.HasFlag(TravelingResults.Unknown)) {
				if (buffer.Remaining > 2)
					Reason = buffer.ReadString();
				return true;
			}

			if (Results.HasFlag(TravelingResults.UseUrl)) {
				DownloadUrl = buffer.ReadString();
				Hash        = BitConverter.ToString(buffer.ReadBytes(32)).Replace("-", "");
				Size        = buffer.ReadUInt();
			} else if (Results.HasFlag(TravelingResults.UseMaster)) {
				var id      = buffer.ReadUInt();
				var server  = buffer.ReadString();
				var version = buffer.ReadUShort();
				var meta    = new Dictionary<string, string[]>();
				if (version != ushort.MaxValue) meta.Add("version", new[] { version.ToString() });
				WorldIdentifier = Main.WorldAPI.Make(id, meta, server);
			}

			return true;
		}

		public static TravelingEvent CreateUnknown(ushort id, byte iid, string reason)
			=> new() {
				InternalId   = iid,
				ConnectionId = id,
				Results      = TravelingResults.Unknown,
				Reason       = reason
			};

		public override string ToString()
			=> $"{GetType().Name}[Results={Results}"
				+ (IsError ? $", Message=\"{Reason}\"" : "")
				+ (UseUrl ? $", Url=\"{DownloadUrl}\", Hash=\"{Hash}\", Size={Size}" : "")
				+ (UseMaster ? $", World={WorldIdentifier.ToString()}" : "")
				+ "]";
	}
}