using System;
using UnityEngine.Events;

namespace api.nox.discord
{
    public class DiscordRpcClient
    {
        private readonly object _client;
        public object GetObject() => _client;
        public Type GetObjectType() => _client.GetType();

        private Action<object, object> OnReadyHandler
            => (sender, args) => OnReady.Invoke(sender, new ReadyMessage(args));

        public DiscordRpcClient(string applicationId)
        {
            var discordRpcClient = DiscordSystem.Assembly.GetType("DiscordRPC.DiscordRpcClient");
            _client = Activator.CreateInstance(discordRpcClient, applicationId);
            var onReady = GetObjectType().GetEvent("OnReady");
            var onReadyDelegate = Delegate.CreateDelegate(
                onReady.EventHandlerType,
                OnReadyHandler.Target,
                OnReadyHandler.Method
            );
            onReady.AddEventHandler(_client, onReadyDelegate);
        }

        public readonly UnityEvent<object, ReadyMessage> OnReady = new();

        public void SetPresence(RichPresence presence)
            => GetObjectType().GetMethod("SetPresence")?.Invoke(_client, new[] { presence.GetObject() });

        public void Initialize()
            => GetObjectType().GetMethod("Initialize")?.Invoke(_client, null);

        public void Dispose()
            => GetObjectType().GetMethod("Dispose")?.Invoke(_client, null);
    }
}