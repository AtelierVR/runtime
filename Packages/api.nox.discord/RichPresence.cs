using System;

namespace api.nox.discord
{
    public class RichPresence
    {
        private readonly object _presence;
        public object GetObject() => _presence;
        public Type GetObjectType() => _presence.GetType();

        public RichPresence()
        {
            var discordRichPresence = DiscordSystem.Assembly.GetType("DiscordRPC.RichPresence");
            _presence = Activator.CreateInstance(discordRichPresence);
        }

        public RichPresence(object presence)
        {
            _presence = presence;
        }

        public string Details
        {
            get => _presence.GetType().GetProperty("Details")?.GetValue(_presence) as string;
            set => _presence.GetType().GetProperty("Details")?.SetValue(_presence, value);
        }

        public string State
        {
            get => _presence.GetType().GetProperty("State")?.GetValue(_presence) as string;
            set => _presence.GetType().GetProperty("State")?.SetValue(_presence, value);
        }

        public Assets Assets
        {
            get => new(_presence.GetType().GetProperty("Assets")?.GetValue(_presence));
            set => _presence.GetType().GetProperty("Assets")?.SetValue(_presence, value.GetObject());
        }
    }
}