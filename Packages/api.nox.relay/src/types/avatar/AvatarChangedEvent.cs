using System;
using Nox.Avatars;
using System.Collections.Generic;
using api.nox.relay.types.Instance;
using Buffer = Nox.CCK.Utils.Buffer;

namespace api.nox.relay.types.Avatar {
	public class AvatarChangedEvent : RelayInstanceResponse {
		public AvatarChangedResults Results;

		public bool IsError
			=> Results.HasFlag(AvatarChangedResults.Unknown);

		public bool IsReady
			=> Results.HasFlag(AvatarChangedResults.Ready) && !IsError;

		public bool UseUrl
			=> Results.HasFlag(AvatarChangedResults.UseUrl) && !IsError;

		public bool UseMaster
			=> Results.HasFlag(AvatarChangedResults.UseMaster) && !IsError;

		public string Reason;

		// if the flag UseUrl is set, this is the URL to download the dimension
		public string DownloadUrl;
		public string Hash;
		public uint   Size;

		// if the flag UseMaster is set, this is the ID of the master server
		public IAvatarIdentifier AvatarIdentifier;

		public override bool FromBuffer(Buffer buffer) {
			buffer.Goto(0);
			InternalId = buffer.ReadByte();

			Results = buffer.ReadEnum<AvatarChangedResults>();

			if (Results.HasFlag(AvatarChangedResults.Unknown)) {
				if (buffer.Remaining > 2)
					Reason = buffer.ReadString();
				return true;
			}

			if (Results.HasFlag(AvatarChangedResults.UseUrl)) {
				DownloadUrl = buffer.ReadString();
				Hash        = BitConverter.ToString(buffer.ReadBytes(32)).Replace("-", "");
				Size        = buffer.ReadUInt();
			} else if (Results.HasFlag(AvatarChangedResults.UseMaster)) {
				var id      = buffer.ReadUInt();
				var server  = buffer.ReadString();
				var version = buffer.ReadUShort();
				var meta    = new Dictionary<string, string[]>();
				if (version != ushort.MaxValue) meta.Add("version", new[] { version.ToString() });
				AvatarIdentifier = Main.AvatarAPI.Make(id, meta, server);
			}

			return true;
		}

		public static AvatarChangedEvent CreateUnknown(ushort id, byte iid, string reason)
			=> new() {
				InternalId   = iid,
				ConnectionId = id,
				Results      = AvatarChangedResults.Unknown,
				Reason       = reason
			};

		public override string ToString()
			=> $"{GetType().Name}[Results={Results}"
				+ (IsError ? $", Message=\"{Reason}\"" : "")
				+ (UseUrl ? $", Url=\"{DownloadUrl}\", Hash=\"{Hash}\", Size={Size}" : "")
				+ (UseMaster ? $", Avatar={AvatarIdentifier}" : "")
				+ "]";
	}
}