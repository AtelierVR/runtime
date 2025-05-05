using System.Collections.Generic;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using UnityEngine.Events;

namespace api.nox.search
{
    public class SearchSystem : MainModInitializer
    {
        internal readonly List<Handler> Handlers = new();

        internal static SearchSystem Instance;
        internal static ModCoreAPI CoreAPI;

        internal static readonly UnityEvent<Handler> OnHandlerAdded = new();
        internal static readonly UnityEvent<Handler> OnHandlerRemoved = new();

        [NoxPublic(NoxAccess.Method)]
        public Handler AddHandler(Dictionary<string, object> data)
            => AddHandler(Handler.From(data));

        private Handler AddHandler(Handler handler)
        {
            if (handler == null)
            {
                Logger.LogError("Cannot register a null search handler");
                return null;
            }

            if (string.IsNullOrWhiteSpace(handler.Id))
            {
                Logger.LogError("Cannot register a search handler with an empty id");
                return null;
            }

            if (Handlers.Exists(b => b.Id == handler.Id))
            {
                Logger.LogError($"Search handler with id {handler.Id} already exists");
                return null;
            }

            if (handler.GetWorkers == null)
            {
                Logger.LogError($"Search handler with id {handler.Id} has no workers");
                return null;
            }

            Handlers.Add(handler);
            CoreAPI.EventAPI.Emit("search_handler_add", handler);
            OnHandlerAdded.Invoke(handler);

            return handler;
        }


        [NoxPublic(NoxAccess.Method)]
        public void RemoveHandler(string id)
        {
            var handler = Handlers.Find(b => b.Id == id);
            if (handler == null)
            {
                Logger.LogError($"Search handler with id {id} does not exist");
                return;
            }

            Handlers.Remove(handler);
            CoreAPI.EventAPI.Emit("search_handler_remove", handler);
            OnHandlerRemoved.Invoke(handler);
        }

        [NoxPublic(NoxAccess.Method)]
        public Handler GetHandler(string id)
            => Handlers.Find(b => b.Id == id);

        [NoxPublic(NoxAccess.Method)]
        public bool HasHandler(string id)
            => Handlers.Exists(b => b.Id == id);

        private LanguagePack _languagePack;

        public void OnInitialize(ModCoreAPI api)
        {
            CoreAPI = api;
            Instance = this;
            _languagePack = api.AssetAPI.GetAsset<LanguagePack>("lang.asset");
            LanguageManager.AddPack(_languagePack);
        }

        public void OnDispose()
        {
            if (_languagePack)
            {
                LanguageManager.RemovePack(_languagePack);
                _languagePack = null;
            }

            CoreAPI = null;
            Instance = null;
        }
    }
}