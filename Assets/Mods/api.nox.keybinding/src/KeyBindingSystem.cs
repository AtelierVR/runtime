using System.Collections.Generic;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using UnityEngine.InputSystem;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.keybinding
{
    public class KeyBindingSystem : MainModInitializer
    {
        private static MainModCoreAPI _coreAPI;

        private readonly List<KeyBinding> _bindings = new();

        [NoxPublic(NoxAccess.Method)]
        public KeyBinding AddKeyBinding(string id, InputAction action, string category = null)
            => AddKeyBinding(new KeyBinding(id, category, action));
        
        private KeyBinding AddKeyBinding(KeyBinding binding)
        {
            if (binding == null)
            {
                Logger.LogError("Cannot register a null key binding");
                return null;
            }

            if (string.IsNullOrWhiteSpace(binding.Id))
            {
                Logger.LogError("Cannot register a key binding with an empty id");
                return null;
            }

            if (_bindings.Exists(b => b.Id == binding.Id))
            {
                Logger.LogError($"Key binding with id {binding.Id} already exists");
                return null;
            }


            var config = Config.Load();
            var key = new[] { "settings", "key_bindings", binding.Id };
            if (config.Has(key))
            {
                var action = config.Get<string>(key);
                if (!string.IsNullOrWhiteSpace(action))
                {
                    binding.Action.LoadBindingOverridesFromJson(action);
                    binding.IsOverridden = true;
                }
            }

            binding.Action.Enable();
            _bindings.Add(binding);

            _coreAPI.EventAPI.Emit("key_binding_added", binding);

            return binding;
        }


        [NoxPublic(NoxAccess.Method)]
        public KeyBinding GetKeyBinding(string id)
            => _bindings.Find(b => b.Id == id);

        [NoxPublic(NoxAccess.Method)]
        public void RemoveKeyBinding(string id)
        {
            var binding = GetKeyBinding(id);
            if (binding == null)
            {
                Logger.LogError($"Key binding with id {id} does not exist");
                return;
            }

            var config = Config.Load();
            var key = new[] { "settings", "key_bindings", binding.Id };
            if (binding.IsOverridden)
            {
                var res = binding.Action.SaveBindingOverridesAsJson();
                if (!string.IsNullOrWhiteSpace(res))
                {
                    config.Set(key, res);
                    config.Save();
                }
            }
            else if (config.Has(key))
            {
                config.Remove(key);
                config.Save();
            }

            binding.Action.Disable();
            _bindings.Remove(binding);

            _coreAPI.EventAPI.Emit("key_binding_removed", binding);
        }

        [NoxPublic(NoxAccess.Method)]
        public bool HasKeyBinding(string id)
            => _bindings.Exists(b => b.Id == id);

        public void OnInitializeMain(MainModCoreAPI api)
        {
            _coreAPI = api;
        }

        public void OnDisposeMain()
        {
            foreach (var binding in _bindings.ToArray())
                RemoveKeyBinding(binding.Id);
            _coreAPI = null;
        }
    }
}