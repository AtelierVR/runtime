using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Users;
using Nox.CCK.Utils;
using Nox.Servers;
using Nox.Users;
using UnityEngine;

namespace api.nox.user {
	[Serializable]
	public class User : IUser, INoxObject {
		public string   server;
		public uint     id;
		public string   display;
		public string   username;
		public string   bio;
		public string   thumbnail;
		public string   banner;
		public Entry[]  links;
		public string[] tags;
		public float    rank;
		public string   certificate;
		public int      followers;
		public int      following;

		public uint GetId()
			=> id;

		public string GetDisplay()
			=> display;

		public string GetUsername()
			=> username;

		public string GetBio()
			=> bio;

		public string GetServerAddress()
			=> server;

		public UniTask<IServer> GetServer()
			=> Main.ServerAPI.Fetch(server);

		public string GetThumbnailUrl()
			=> thumbnail;

		public UniTask<Texture2D> GetThumbnail()
			=> Main.NetworkAPI.FetchTexture(thumbnail);

		public string GetBannerUrl()
			=> banner;

		public UniTask<Texture2D> GetBanner()
			=> Main.NetworkAPI.FetchTexture(banner);

		public IEntry[] GetLinks()
			=> links.Cast<IEntry>().ToArray();

		public IRelationship GetRelationships()
			=> null;

		public string[] GetTags()
			=> tags ?? Array.Empty<string>();

		public float GetRank()
			=> rank;

		public async UniTask<User> InternalRefresh()
			=> await Main.Instance.Network.Fetch(ToInternalIdentifier(), server);

		public async UniTask<IUser> Refresh()
			=> await InternalRefresh();

		public UserIdentifier ToInternalIdentifier()
			=> new(id, server);

		public IUserIdentifier ToIdentifier()
			=> ToInternalIdentifier();

		public string GetCertificate() {
			if (string.IsNullOrEmpty(certificate)) return null;
			if (certificate.StartsWith("-----BEGIN CERTIFICATE-----\n"))
				return certificate;

			var lines = new string[certificate.Length / 64 + 1];
			for (var i = 0; i < lines.Length; i++) {
				var start  = i * 64;
				var length = Math.Min(64, certificate.Length - start);
				lines[i] = certificate.Substring(start, length);
			}

			return "-----BEGIN CERTIFICATE-----\n"
				+ string.Join("\n", lines)
				+ "\n-----END CERTIFICATE-----";
		}

		public int GetFollowers()
			=> followers;

		public int GetFollowing()
			=> following;

		public override string ToString()
			=> $"{GetType().Name}[id={id}, username={username}, server={server}]";
	}
}