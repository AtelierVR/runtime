using System;

namespace api.nox.discord
{
    public class Assets
    {
        private readonly object _assets;
        public object GetObject() => _assets;
        
        public Assets()
        {
            var discordAssets = DiscordSystem.Assembly.GetType("DiscordRPC.Assets");
            _assets = Activator.CreateInstance(discordAssets);
        }
        
        public Assets(object assets)
        {
            _assets = assets;
        }
        
        public string LargeImageKey
        {
            get => _assets.GetType().GetProperty("LargeImageKey")?.GetValue(_assets) as string;
            set => _assets.GetType().GetProperty("LargeImageKey")?.SetValue(_assets, value);
        }
        
        public string LargeImageText
        {
            get => _assets.GetType().GetProperty("LargeImageText")?.GetValue(_assets) as string;
            set => _assets.GetType().GetProperty("LargeImageText")?.SetValue(_assets, value);
        }
        
        public string SmallImageKey
        {
            get => _assets.GetType().GetProperty("SmallImageKey")?.GetValue(_assets) as string;
            set => _assets.GetType().GetProperty("SmallImageKey")?.SetValue(_assets, value);
        }
        
        
        public string SmallImageText
        {
            get => _assets.GetType().GetProperty("SmallImageText")?.GetValue(_assets) as string;
            set => _assets.GetType().GetProperty("SmallImageText")?.SetValue(_assets, value);
        }
    }
}