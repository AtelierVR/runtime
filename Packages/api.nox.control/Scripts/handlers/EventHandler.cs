using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nox.CCK.Mods.Events;
using Nox.CCK.Utils;

namespace api.nox.control.handlers {
	public class EventHandler {
		public static void OnEvent(EventData context) {
			var clients = Main.Server.GetClients();

			var data = new List<JToken>();

			foreach (var d in context.Data) {
				switch (d) {
					case JToken jToken:
						data.Add(jToken);
						break;
					case null:
						data.Add(JValue.CreateNull());
						break;
					default:
						try {
							data.Add(JToken.FromObject(d));
						} catch (Exception e) {
							data.Add(new JObject {
								["error"] = e.Message,
								["type"] = e.GetType().Name
							});
						}

						break;
				}
			}

			var channels = (
			from EventEntryFlags flag
				in Enum.GetValues(typeof(EventEntryFlags))
			where context.SourceChannel.HasFlag(flag)
			select flag.ToString().ToSnakeCase()
			).ToArray();


			foreach (var client in clients)
				client.Send("event", context.EventName, context.Source.GetMetadata().GetId(), data.ToArray(), channels).Forget();
		}
	}
}