using System;
using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Network;
using Nox.CCK.Scripting;
using Nox.CCK.Utils;
using Nox.Jint;
using Nox.Players;
using Nox.Scripting;
using Nox.Sessions;
using Nox.Tables;
using Nox.Worlds;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.session.jint {
	public class Main : IMainModInitializer {
		static internal IMainModCoreAPI CoreAPI;
		private EventSubscription[] _events = Array.Empty<EventSubscription>();

		static internal IWorldAPI WorldAPI
			=> CoreAPI.ModAPI.GetMod("worlds").GetInstance<IWorldAPI>();

		static internal ITableAPI TableAPI
			=> CoreAPI.ModAPI.GetMod("tables").GetInstance<ITableAPI>();

		static internal IJintAPI JintAPI
			=> CoreAPI.ModAPI.GetMod("jint").GetInstance<IJintAPI>();

		static internal IScriptingAPI ScriptingAPI
			=> CoreAPI.ModAPI.GetMod("scripting").GetInstance<IScriptingAPI>();

		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI = api;
			_events = new[] {
				api.EventAPI.Subscribe("world_check_request", OnCheckRequest),
			};
			RegisterScriptingModules();
		}

		private static void OnCheckRequest(EventData context) {
			if (!context.TryGet<IWorldDescriptor>(0, out var descriptor))
				return;
			var valid = true;
			valid &= JintBackingModule.Check(descriptor);
			context.Callback(valid);
		}

		public void OnDisposeMain() {
			foreach (var e in _events)
				CoreAPI.EventAPI.Unsubscribe(e);
			_events = Array.Empty<EventSubscription>();
			UnregisterScriptingModules();
			CoreAPI = null;
		}

		// ── Scripting module registration ─────────────────────────────────────

		private static readonly string[] OwnModuleIds = {
			"console", "behaviour", "buffer", "tables", "players", "network",
		};

		private static void RegisterScriptingModules() {
			var api = ScriptingAPI;
			if (api == null) {
				Logger.LogWarning("[session.jint] scripting mod not found – modules not registered.", null);
				return;
			}

			// console
			api.RegisterModule(ScriptingModuleBuilder.Create("console")
				.AddMethod("log", (ctx, args) => {
					Logger.Log(string.Join(" ", args.Select(a => a?.ToString() ?? "null")),
						ctx.ScriptObject,
						$"Script_{ctx.ScriptObject?.GetEntityId().GetHashCode()}");
					return null;
				})
				.AddMethod("warn", (ctx, args) => {
					Logger.LogWarning(string.Join(" ", args.Select(a => a?.ToString() ?? "null")),
						ctx.ScriptObject,
						$"Script_{ctx.ScriptObject?.GetEntityId().GetHashCode()}");
					return null;
				})
				.AddMethod("error", (ctx, args) => {
					Logger.LogError(string.Join(" ", args.Select(a => a?.ToString() ?? "null")),
						ctx.ScriptObject,
						$"Script_{ctx.ScriptObject?.GetEntityId().GetHashCode()}");
					return null;
				})
				.Build());

			// behaviour  (values resolved per-instance at bind time via IScriptingContext)
			api.RegisterModule(ScriptingModuleBuilder.Create("behaviour")
				.AddVariable("gameObject", ctx => ctx.ScriptObject)
				.AddVariable("transform", ctx => ctx.ScriptObject?.transform)
				.AddVariable("rigidbody", ctx => ctx.ScriptObject?.GetComponent<Rigidbody>())
				.AddVariable("id", ctx => ctx.ScriptObject?.GetEntityId().GetHashCode() ?? 0)
				.Build());

			// buffer  (Node.js–compatible Buffer helpers)
			api.RegisterModule(ScriptingModuleBuilder.Create("buffer")
				.AddMethod("from", (_, args) => {
					var data     = args.Length > 0 ? args[0]?.ToString() ?? "" : "";
					var encoding = args.Length > 1 ? args[1]?.ToString() ?? "utf8" : "utf8";
					return NodeBufferImpl.from(data, encoding);
				})
				.AddMethod("toString", (_, args) => {
					if (args.Length == 0)
						return null;
					byte[] buf;
					switch (args[0]) {
						case byte[] b:
							buf = b;
							break;
						case object[] arr:
							buf = arr.Select(x => Convert.ToByte(x)).ToArray();
							break;
						default:
							return null;
					}
					var encoding = args.Length > 1 ? args[1]?.ToString() ?? "utf8" : "utf8";
					return NodeBufferImpl.toString(buf, encoding);
				})
				.Build());

			// tables  (session-scoped persistent storage)
			api.RegisterModule(ScriptingModuleBuilder.Create("tables")
				.AddAsyncMethod("getPrivate", async (ctx, _) => (object)await GetTable(ctx.Session, false))
				.AddAsyncMethod("getPublic", async (ctx,  _) => (object)await GetTable(ctx.Session, true))
				.AddAsyncMethod("setPrivate", async (ctx, args) => (object)await SetTable(ctx.Session, false, args))
				.AddAsyncMethod("setPublic", async (ctx,  args) => (object)await SetTable(ctx.Session, true, args))
				.AddAsyncMethod("delPrivate", async (ctx, _) => (object)await DeleteTable(ctx.Session, false))
				.AddAsyncMethod("delPublic", async (ctx,  _) => (object)await DeleteTable(ctx.Session, true))
				.Build());

			// players
			api.RegisterModule(ScriptingModuleBuilder.Create("players")
				.AddMethod("getLocal", (ctx,  _) => ctx.Session?.LocalPlayer)
				.AddMethod("getMaster", (ctx, _) => ctx.Session?.MasterPlayer)
				.AddMethod("getAll", (ctx,    _) => ctx.Session?.Entities.GetEntities<IPlayer>())
				.AddMethod("getCount", (ctx,  _) => ctx.Session?.Entities.GetCount<IPlayer>() ?? 0)
				.AddMethod("getAt", (ctx, args) => {
					if (ctx.Session == null || args.Length == 0)
						return null;
					var players = ctx.Session.Entities.GetEntities<IPlayer>();
					return players.ElementAtOrDefault(Convert.ToInt32(args[0]));
				})
				.Build());

			// network
			api.RegisterModule(ScriptingModuleBuilder.Create("network")
				.AddMethod("getTime", (ctx,     _) => (ctx.Session as INetSession)?.Time ?? DateTime.UtcNow)
				.AddMethod("isConnected", (ctx, _) => (ctx.Session as INetSession)?.IsConnected ?? false)
				.AddMethod("eventToHash", (_,   args) => args.Length > 0 ? (object)Hash.CRC64(args[0]?.ToString() ?? "") : null)
				.AddAsyncMethod("emitEvent", async (ctx, args) => {
					if (ctx.Session is not INetSession net) {
						Logger.LogWarning("[network] session is not a net session.", null);
						return (object)false;
					}
					var eventId = args.Length > 0 && args[0] is double d
						? (long)d
						: Hash.CRC64(args[0]?.ToString() ?? "");
					byte[] raw;
					if (args.Length < 2 || args[1] == null)
						raw = Array.Empty<byte>();
					else
						raw = args[1] switch {
							byte[] b     => b,
							object[] arr => arr.Select(x => Convert.ToByte(x)).ToArray(),
							_            => Array.Empty<byte>()
						};
					return (object)await net.EmitEvent(eventId, raw);
				})
				.Build());
		}

		private static void UnregisterScriptingModules() {
			var api = ScriptingAPI;
			if (api == null)
				return;
			foreach (var id in OwnModuleIds)
				api.UnregisterModule(new NameResolver(id));
		}

		// ── Table helpers ─────────────────────────────────────────────────────

		private static bool TryTableKey(ISession session, bool isPublic, out string key) {
			if (session?.Dimensions == null) {
				key = null;
				return false;
			}
			var id = session.Dimensions.Identifier;
			if (!id.IsValid()) {
				key = null;
				return false;
			}
			key = $"{(isPublic ? "public." : "")}worlds.{Uri.EscapeDataString(id.ToShortString(true))}";
			return true;
		}

		private static async System.Threading.Tasks.Task<byte[]> GetTable(ISession session, bool isPublic) {
			if (!TryTableKey(session, isPublic, out var key))
				return null;
			var entry = await TableAPI.Get(key);
			return entry?.AsBytes;
		}

		private static async System.Threading.Tasks.Task<byte[]> SetTable(ISession session, bool isPublic, object[] args) {
			if (args.Length == 0 || !TryTableKey(session, isPublic, out var key))
				return null;
			byte[] data;
			if (args[0] is byte[] b)
				data = b;
			else if (args[0] is object[] arr)
				data = arr.Select(Convert.ToByte).ToArray();
			else
				return null;
			var entry = await TableAPI.Set(key, data, "application/octet-stream+world");
			return entry?.AsBytes;
		}

		private static async System.Threading.Tasks.Task<bool> DeleteTable(ISession session, bool isPublic) {
			if (!TryTableKey(session, isPublic, out var key))
				return false;
			return await TableAPI.Delete(key);
		}
	}
}